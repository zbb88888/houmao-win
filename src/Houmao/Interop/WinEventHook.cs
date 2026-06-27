using System;
using System.Runtime.InteropServices;

namespace Houmao.Interop
{
    public class WinEventHook : IDisposable
    {
        private IntPtr _hook;
        private User32.WinEventDelegate? _callback;
        private bool _isDisposed;
        
        public event EventHandler<WinEventArgs>? WinEvent;
        
        public WinEventHook(uint eventMin, uint eventMax)
        {
            _callback = WinEventCallback;
            _hook = User32.SetWinEventHook(
                eventMin,
                eventMax,
                IntPtr.Zero,
                _callback,
                0,
                0,
                User32.WINEVENT_OUTOFCONTEXT);
            
            if (_hook == IntPtr.Zero)
            {
                throw new InvalidOperationException("Failed to set Windows event hook");
            }
        }
        
        private void WinEventCallback(IntPtr hWinEventHook, uint eventType, 
            IntPtr hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime)
        {
            WinEvent?.Invoke(this, new WinEventArgs
            {
                EventType = eventType,
                Hwnd = hwnd,
                ObjectId = idObject,
                ChildId = idChild,
                EventThread = dwEventThread,
                EventTime = dwmsEventTime
            });
        }
        
        public void Dispose()
        {
            if (_isDisposed) return;
            
            _isDisposed = true;
            
            if (_hook != IntPtr.Zero)
            {
                User32.UnhookWinEvent(_hook);
                _hook = IntPtr.Zero;
            }
            
            _callback = null;
        }
    }
    
    public class WinEventArgs : EventArgs
    {
        public uint EventType { get; set; }
        public IntPtr Hwnd { get; set; }
        public int ObjectId { get; set; }
        public int ChildId { get; set; }
        public uint EventThread { get; set; }
        public uint EventTime { get; set; }
    }
}