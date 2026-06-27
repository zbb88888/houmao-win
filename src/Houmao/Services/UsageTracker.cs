using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Automation;
using Houmao.Interop;
using Houmao.Models;
using Microsoft.Extensions.Logging;

namespace Houmao.Services
{
    public class UsageTracker : IUsageTracker, IDisposable
    {
        private readonly ILogger<UsageTracker> _logger;
        private readonly IHistoryStore _historyStore;
        private readonly IAppSettings _settings;
        
        private Thread? _hookThread;
        private IntPtr _keyboardHook;
        private IntPtr _foregroundHook;
        private bool _isTracking;
        private string _keystrokeBuffer = string.Empty;
        private string _lastApplicationName = string.Empty;
        private DateTime _lastKeystrokeTime = DateTime.MinValue;
        
        // 键盘钩子相关
        private User32.LowLevelKeyboardProc? _keyboardProc;
        
        public bool IsTracking => _isTracking;
        public event EventHandler<UsageRecordEventArgs>? UsageRecorded;
        
        public UsageTracker(ILogger<UsageTracker> logger, IHistoryStore historyStore, IAppSettings settings)
        {
            _logger = logger;
            _historyStore = historyStore;
            _settings = settings;
        }
        
        public void Start()
        {
            if (_isTracking) return;
            
            try
            {
                _isTracking = true;
                
                // 创建键盘钩子线程
                _hookThread = new Thread(RunHookLoop)
                {
                    IsBackground = true,
                    Name = "UsageTrackerHook"
                };
                _hookThread.Start();
                
                // 注册前台窗口变化事件
                RegisterForegroundHook();
                
                _logger.LogInformation("Usage tracking started");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to start usage tracking");
                _isTracking = false;
            }
        }
        
        public void Stop()
        {
            if (!_isTracking) return;
            
            try
            {
                _isTracking = false;
                
                // 移除钩子
                if (_keyboardHook != IntPtr.Zero)
                {
                    User32.UnhookWindowsHookEx(_keyboardHook);
                    _keyboardHook = IntPtr.Zero;
                }
                
                if (_foregroundHook != IntPtr.Zero)
                {
                    User32.UnhookWinEvent(_foregroundHook);
                    _foregroundHook = IntPtr.Zero;
                }
                
                _logger.LogInformation("Usage tracking stopped");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to stop usage tracking");
            }
        }
        
        private void RunHookLoop()
        {
            try
            {
                _keyboardProc = KeyboardHookCallback;
                _keyboardHook = User32.SetWindowsHookEx(
                    User32.WH_KEYBOARD_LL,
                    _keyboardProc,
                    User32.GetModuleHandle(null),
                    0);
                
                if (_keyboardHook == IntPtr.Zero)
                {
                    _logger.LogError("Failed to install keyboard hook");
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
                _logger.LogError(ex, "Error in hook loop");
            }
        }
        
        private IntPtr KeyboardHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && wParam == User32.WM_KEYDOWN)
            {
                var hookStruct = Marshal.PtrToStructure<User32.KBDLLHOOKSTRUCT>(lParam);
                ProcessKeystroke(hookStruct.vkCode);
            }
            
            return User32.CallNextHookEx(_keyboardHook, nCode, wParam, lParam);
        }
        
        private void ProcessKeystroke(uint vkCode)
        {
            // 忽略 houmao 自身窗口
            var foregroundWindow = User32.GetForegroundWindow();
            var foregroundProcess = GetProcessByWindow(foregroundWindow);
            if (foregroundProcess?.ProcessName == "Houmao")
            {
                return;
            }
            
            // 处理按键
            var now = DateTime.UtcNow;
            var timeSinceLastKeystroke = now - _lastKeystrokeTime;
            
            // 如果间隔超过 5 秒，清空缓冲区
            if (timeSinceLastKeystroke > TimeSpan.FromSeconds(5))
            {
                _keystrokeBuffer = string.Empty;
            }
            
            _lastKeystrokeTime = now;
            
            // 处理特殊按键
            if (vkCode == User32.VK_RETURN)
            {
                // 回车键 - 尝试记录
                ProcessEnterKey();
            }
            else if (vkCode == User32.VK_BACK)
            {
                // 退格键 - 删除最后一个字符
                if (_keystrokeBuffer.Length > 0)
                {
                    _keystrokeBuffer = _keystrokeBuffer[..^1];
                }
            }
            else if (vkCode == User32.VK_ESCAPE)
            {
                // Esc 键 - 清空缓冲区
                _keystrokeBuffer = string.Empty;
            }
            else
            {
                // 普通按键 - 添加到缓冲区
                var c = VkCodeToChar(vkCode);
                if (c.HasValue)
                {
                    _keystrokeBuffer += c.Value;
                }
            }
        }
        
        private async void ProcessEnterKey()
        {
            try
            {
                // 获取当前应用信息
                var appInfo = await GetCurrentApplicationAsync();
                if (appInfo == null)
                {
                    _keystrokeBuffer = string.Empty;
                    return;
                }
                
                // 检测应用切换
                if (_lastApplicationName != appInfo.ProcessName)
                {
                    if (!string.IsNullOrEmpty(_lastApplicationName))
                    {
                        // 记录应用切换
                        var switchRecord = new UsageRecord
                        {
                            ApplicationName = $"{_lastApplicationName} → {appInfo.ProcessName}",
                            InputText = "[Switch]",
                            Timestamp = DateTime.UtcNow
                        };
                        
                        await _historyStore.AppendAsync(switchRecord);
                        UsageRecorded?.Invoke(this, new UsageRecordEventArgs(switchRecord));
                    }
                    
                    _lastApplicationName = appInfo.ProcessName;
                    _keystrokeBuffer = string.Empty;
                    return;
                }
                
                // 尝试通过 UI Automation 获取文本
                var text = await GetFocusedTextAsync();
                
                // 回退策略：如果 UIA 失败或返回文本过长，使用键盘缓冲区
                if (string.IsNullOrEmpty(text) || text.Length > _keystrokeBuffer.Length * 3)
                {
                    text = _keystrokeBuffer;
                }
                
                // 记录使用情况
                if (!string.IsNullOrEmpty(text))
                {
                    var record = new UsageRecord
                    {
                        ApplicationName = appInfo.ProcessName,
                        InputText = text,
                        Timestamp = DateTime.UtcNow
                    };
                    
                    await _historyStore.AppendAsync(record);
                    UsageRecorded?.Invoke(this, new UsageRecordEventArgs(record));
                    
                    _logger.LogDebug("Recorded usage from {App}: {Text}", 
                        appInfo.ProcessName, text.Length > 50 ? text[..50] + "..." : text);
                }
                
                // 清空缓冲区
                _keystrokeBuffer = string.Empty;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing enter key");
                _keystrokeBuffer = string.Empty;
            }
        }
        
        private void RegisterForegroundHook()
        {
            _foregroundHook = User32.SetWinEventHook(
                User32.EVENT_SYSTEM_FOREGROUND,
                User32.EVENT_SYSTEM_FOREGROUND,
                IntPtr.Zero,
                ForegroundHookCallback,
                0,
                0,
                User32.WINEVENT_OUTOFCONTEXT);
        }
        
        private void ForegroundHookCallback(IntPtr hWinEventHook, uint eventType, 
            IntPtr hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime)
        {
            if (eventType == User32.EVENT_SYSTEM_FOREGROUND)
            {
                // 清空键盘缓冲区
                _keystrokeBuffer = string.Empty;
                
                // 获取新的前台应用信息
                var appInfo = GetProcessByWindow(hwnd);
                if (appInfo != null)
                {
                    _lastApplicationName = appInfo.ProcessName;
                }
            }
        }
        
        public async Task<ApplicationInfo?> GetCurrentApplicationAsync()
        {
            return await Task.Run(() =>
            {
                var foregroundWindow = User32.GetForegroundWindow();
                return GetProcessByWindow(foregroundWindow);
            });
        }
        
        private ApplicationInfo? GetProcessByWindow(IntPtr hwnd)
        {
            try
            {
                if (hwnd == IntPtr.Zero) return null;
                
                User32.GetWindowThreadProcessId(hwnd, out var processId);
                var process = Process.GetProcessById((int)processId);
                
                var title = new StringBuilder(256);
                User32.GetWindowText(hwnd, title, 256);
                
                return new ApplicationInfo
                {
                    ProcessName = process.ProcessName,
                    WindowTitle = title.ToString(),
                    ProcessId = (int)processId
                };
            }
            catch
            {
                return null;
            }
        }
        
        public async Task<string?> GetFocusedTextAsync()
        {
            return await Task.Run(() =>
            {
                try
                {
                    var focusedElement = AutomationElement.FocusedElement;
                    if (focusedElement == null) return null;
                    
                    // 检查控件类型
                    var controlType = focusedElement.Current.ControlType;
                    if (controlType == ControlType.Button ||
                        controlType == ControlType.CheckBox ||
                        controlType == ControlType.MenuItem ||
                        controlType == ControlType.Image ||
                        controlType == ControlType.Table)
                    {
                        return null;
                    }
                    
                    // 尝试获取 ValuePattern
                    if (focusedElement.TryGetCurrentPattern(ValuePattern.Pattern, out var valuePattern))
                    {
                        var value = ((ValuePattern)valuePattern).Current.Value;
                        if (!string.IsNullOrEmpty(value))
                        {
                            return value;
                        }
                    }
                    
                    // 尝试获取 TextPattern
                    if (focusedElement.TryGetCurrentPattern(TextPattern.Pattern, out var textPattern))
                    {
                        var text = ((TextPattern)textPattern).DocumentRange.GetText(-1);
                        if (!string.IsNullOrEmpty(text))
                        {
                            return text;
                        }
                    }
                    
                    return null;
                }
                catch
                {
                    return null;
                }
            });
        }
        
        private char? VkCodeToChar(uint vkCode)
        {
            // 简化实现：只处理字母、数字和常见符号
            if (vkCode >= 0x30 && vkCode <= 0x39) // 0-9
            {
                return (char)('0' + (vkCode - 0x30));
            }
            
            if (vkCode >= 0x41 && vkCode <= 0x5A) // A-Z
            {
                var isShift = (User32.GetKeyState(User32.VK_SHIFT) & 0x8000) != 0;
                var isCapsLock = (User32.GetKeyState(User32.VK_CAPITAL) & 0x0001) != 0;
                
                var c = (char)('a' + (vkCode - 0x41));
                
                if (isShift ^ isCapsLock)
                {
                    c = char.ToUpper(c);
                }
                
                return c;
            }
            
            // 空格键
            if (vkCode == User32.VK_SPACE)
            {
                return ' ';
            }
            
            return null;
        }
        
        public void Dispose()
        {
            Stop();
        }
    }
}