using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using Houmao.Interop;
using Microsoft.Extensions.Logging;

namespace Houmao.Services
{
    public sealed class SelectToCopyManager : IDisposable
    {
        private readonly ILogger<SelectToCopyManager> _logger;
        private readonly IAppSettings _settings;
        
        private Thread? _hookThread;
        private IntPtr _mouseHook;
        private bool _isDisposed;
        private bool _isMouseDown;
        private User32.POINT _mouseDownPoint;
        private readonly int _dragThreshold;
        
        public SelectToCopyManager(ILogger<SelectToCopyManager> logger, IAppSettings settings)
        {
            _logger = logger;
            _settings = settings;
            
            _logger.LogDebug("SelectToCopyManager created. Enabled={Enabled}", _settings.SelectToCopyEnabled);
            
            // 获取系统拖拽阈值
            _dragThreshold = User32.GetSystemMetrics(User32.SM_CXDRAG);
            _logger.LogDebug("Drag threshold: {Threshold}", _dragThreshold);
            
            // 订阅设置变化
            _settings.SettingsChanged += Settings_SettingsChanged;
            
            // 根据设置启动或停止
            if (_settings.SelectToCopyEnabled)
            {
                Start();
            }
        }
        
        private void Settings_SettingsChanged(object? sender, EventArgs e)
        {
            if (_settings.SelectToCopyEnabled)
            {
                Start();
            }
            else
            {
                Stop();
            }
        }
        
        public void Start()
        {
            if (_mouseHook != IntPtr.Zero) return;
            
            try
            {
                _hookThread = new Thread(RunHookLoop)
                {
                    IsBackground = true,
                    Name = "SelectToCopyHook"
                };
                _hookThread.Start();
                
                _logger.LogInformation("Select to copy started");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to start select to copy");
            }
        }
        
        public void Stop()
        {
            if (_mouseHook == IntPtr.Zero) return;
            
            try
            {
                User32.UnhookWindowsHookEx(_mouseHook);
                _mouseHook = IntPtr.Zero;
                
                _logger.LogInformation("Select to copy stopped");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to stop select to copy");
            }
        }
        
        private void RunHookLoop()
        {
            try
            {
                _mouseHook = User32.SetWindowsHookEx(
                    User32.WH_MOUSE_LL,
                    new User32.LowLevelMouseProc(MouseHookCallback),
                    User32.GetModuleHandle(null),
                    0);
                
                if (_mouseHook == IntPtr.Zero)
                {
                    _logger.LogError("Failed to install mouse hook");
                    return;
                }
                
                // 消息循环
                while (User32.GetMessage(out var msg, IntPtr.Zero, 0, 0))
                {
                    User32.TranslateMessage(ref msg);
                    User32.DispatchMessage(ref msg);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in mouse hook loop");
            }
        }
        
        private IntPtr MouseHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                var hookStruct = Marshal.PtrToStructure<User32.MSLLHOOKSTRUCT>(lParam);
                
                switch (wParam.ToInt32())
                {
                    case User32.WM_LBUTTONDOWN:
                        _isMouseDown = true;
                        _mouseDownPoint = hookStruct.pt;
                        break;
                    
                    case User32.WM_LBUTTONUP:
                        if (_isMouseDown)
                        {
                            _isMouseDown = false;
                            
                            var dx = hookStruct.pt.X - _mouseDownPoint.X;
                            var dy = hookStruct.pt.Y - _mouseDownPoint.Y;
                            var distance = Math.Sqrt(dx * dx + dy * dy);
                            
                            if (distance > _dragThreshold)
                            {
                                PerformCopy();
                            }
                        }
                        break;
                }
            }
            
            return User32.CallNextHookEx(_mouseHook, nCode, wParam, lParam);
        }
        
        private void PerformCopy()
        {
            // 在独立线程中执行复制，避免鼠标钩子干扰
            var copyThread = new Thread(() =>
            {
                try
                {
                    Thread.Sleep(150); // 等待选择完成
                    SendCopyCommand();
                    _logger.LogDebug("Copy command sent");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Copy error");
                }
            });
            copyThread.IsBackground = true;
            copyThread.Start();
        }
        
        private void SendCopyCommand()
        {
            // 使用 keybd_event 模拟 Ctrl+C
            User32.keybd_event(User32.VK_CONTROL, 0, 0, 0);  // Ctrl 按下
            User32.keybd_event(User32.VK_C, 0, 0, 0);        // C 按下
            User32.keybd_event(User32.VK_C, 0, User32.KEYEVENTF_KEYUP, 0);  // C 释放
            User32.keybd_event(User32.VK_CONTROL, 0, User32.KEYEVENTF_KEYUP, 0);  // Ctrl 释放
        }
        
        private string? GetClipboardText()
        {
            try
            {
                return Clipboard.ContainsText() ? Clipboard.GetText() : null;
            }
            catch
            {
                return null;
            }
        }
        
        public void Dispose()
        {
            if (_isDisposed) return;
            
            _isDisposed = true;
            Stop();
            
            _settings.SettingsChanged -= Settings_SettingsChanged;
        }
    }
}