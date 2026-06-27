using System;
using System.Runtime.InteropServices;
using System.Threading;
using Houmao.Interop;
using Microsoft.Extensions.Logging;

namespace Houmao.Services
{
    public sealed class HotKeyManager : IDisposable
    {
        private readonly ILogger<HotKeyManager> _logger;
        private IntPtr _hook;
        private bool _isDisposed;
        private readonly Thread _hookThread;
        private User32.LowLevelKeyboardProc? _hookProc; // 保持引用防止GC
        
        private DateTime _lastAltTime = DateTime.MinValue;
        
        public event EventHandler? DoubleAltPressed;
        
        public HotKeyManager(ILogger<HotKeyManager> logger)
        {
            _logger = logger;
            
            _logger.LogDebug("HotKeyManager created");
            
            _hookThread = new Thread(HookThreadProc)
            {
                IsBackground = true,
                Name = "HotKeyHook"
            };
            _hookThread.Start();
        }
        
        private void HookThreadProc()
        {
            try
            {
                _logger.LogDebug("Hook thread started");
                
                // 必须保持委托引用防止GC
                _hookProc = HookCallback;
                
                _hook = User32.SetWindowsHookEx(
                    User32.WH_KEYBOARD_LL,
                    _hookProc,
                    User32.GetModuleHandle(null),
                    0);
                
                if (_hook == IntPtr.Zero)
                {
                    var error = Marshal.GetLastWin32Error();
                    _logger.LogError("Failed to install hook: error {Error}", error);
                    return;
                }
                
                _logger.LogDebug("Hook installed OK");
                
                // 消息循环
                while (User32.GetMessage(out var msg, IntPtr.Zero, 0, 0))
                {
                    User32.TranslateMessage(ref msg);
                    User32.DispatchMessage(ref msg);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Hook thread error");
            }
        }
        
        private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && wParam == User32.WM_KEYDOWN)
            {
                var info = Marshal.PtrToStructure<User32.KBDLLHOOKSTRUCT>(lParam);
                
                if (info.vkCode == 0x1B) // Escape 键
                {
                    var now = DateTime.Now;
                    var elapsed = now - _lastAltTime;
                    
                    if (elapsed.TotalMilliseconds < 500 && elapsed.TotalMilliseconds > 30)
                    {
                        _logger.LogDebug("Double ALT detected");
                        DoubleAltPressed?.Invoke(this, EventArgs.Empty);
                        _lastAltTime = DateTime.MinValue;
                    }
                    else
                    {
                        _lastAltTime = now;
                    }
                }
            }
            
            return User32.CallNextHookEx(_hook, nCode, wParam, lParam);
        }
        
        public void Dispose()
        {
            if (_isDisposed) return;
            _isDisposed = true;
            
            if (_hook != IntPtr.Zero)
            {
                User32.UnhookWindowsHookEx(_hook);
                _hook = IntPtr.Zero;
            }
        }
    }
}