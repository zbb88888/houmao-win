using System;
using System.Runtime.InteropServices;

namespace Houmao.Interop
{
    public static class DwmApi
    {
        // 常量
        public const int DWMWA_SYSTEMBACKDROP_TYPE = 38;
        public const int DWMWA_WINDOW_CORNER_PREFERENCE = 33;
        public const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;
        
        public const int DWMSBT_AUTO = 0;
        public const int DWMSBT_NONE = 1;
        public const int DWMSBT_MAINWINDOW = 2;     // Mica
        public const int DWMSBT_TRANSIENTWINDOW = 3; // Acrylic
        public const int DWMSBT_TABBEDWINDOW = 4;    // Tabbed
        
        public const int DWMWCP_DEFAULT = 0;
        public const int DWMWCP_DONOTROUND = 1;
        public const int DWMWCP_ROUND = 2;
        public const int DWMWCP_ROUNDSMALL = 3;
        
        // 结构体
        [StructLayout(LayoutKind.Sequential)]
        public struct AccentPolicy
        {
            public AccentState AccentState;
            public int AccentFlags;
            public uint GradientColor;
            public int AnimationId;
        }
        
        [StructLayout(LayoutKind.Sequential)]
        public struct WindowCompositionAttributeData
        {
            public WindowCompositionAttribute Attribute;
            public IntPtr Data;
            public int SizeOfData;
        }
        
        // 枚举
        public enum AccentState
        {
            ACCENT_DISABLED = 0,
            ACCENT_ENABLE_GRADIENT = 1,
            ACCENT_ENABLE_TRANSPARENTGRADIENT = 2,
            ACCENT_ENABLE_ACRYLICBLURBEHIND = 3,
            ACCENT_ENABLE_HOSTBACKDROP = 5,
            ACCENT_INVALID_STATE = 6
        }
        
        public enum WindowCompositionAttribute
        {
            WCA_ACCENT_POLICY = 19
        }
        
        // 函数
        [DllImport("dwmapi.dll")]
        public static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, 
            ref int attrValue, int attrSize);
        
        [DllImport("user32.dll")]
        public static extern int SetWindowCompositionAttribute(IntPtr hwnd, 
            ref WindowCompositionAttributeData data);
        
        /// <summary>
        /// 设置 Acrylic 效果（Windows 11 22H2+）
        /// </summary>
        public static void SetAcrylic(IntPtr hWnd)
        {
            int attr = DWMSBT_TRANSIENTWINDOW;
            DwmSetWindowAttribute(hWnd, DWMWA_SYSTEMBACKDROP_TYPE, ref attr, sizeof(int));
        }
        
        /// <summary>
        /// 设置 Mica 效果（Windows 11）
        /// </summary>
        public static void SetMica(IntPtr hWnd)
        {
            int attr = DWMSBT_MAINWINDOW;
            DwmSetWindowAttribute(hWnd, DWMWA_SYSTEMBACKDROP_TYPE, ref attr, sizeof(int));
        }
        
        /// <summary>
        /// 设置圆角（Windows 11）
        /// </summary>
        public static void SetRoundCorners(IntPtr hWnd)
        {
            int pref = DWMWCP_ROUND;
            DwmSetWindowAttribute(hWnd, DWMWA_WINDOW_CORNER_PREFERENCE, ref pref, sizeof(int));
        }
        
        /// <summary>
        /// 设置深色模式
        /// </summary>
        public static void SetDarkMode(IntPtr hWnd, bool dark)
        {
            int darkMode = dark ? 1 : 0;
            DwmSetWindowAttribute(hWnd, DWMWA_USE_IMMERSIVE_DARK_MODE, ref darkMode, sizeof(int));
        }
        
        /// <summary>
        /// Windows 10 Fallback: Composition Attribute Acrylic 效果
        /// </summary>
        public static void SetAcrylicWin10(IntPtr hWnd, uint tintColor = 0x990F0F0F)
        {
            var data = new AccentPolicy
            {
                AccentState = AccentState.ACCENT_ENABLE_ACRYLICBLURBEHIND,
                AccentFlags = 2,
                GradientColor = tintColor
            };
            
            var size = Marshal.SizeOf(data);
            var pointer = Marshal.AllocHGlobal(size);
            Marshal.StructureToPtr(data, pointer, false);
            
            var compositionData = new WindowCompositionAttributeData
            {
                Attribute = WindowCompositionAttribute.WCA_ACCENT_POLICY,
                Data = pointer,
                SizeOfData = size
            };
            
            SetWindowCompositionAttribute(hWnd, ref compositionData);
            Marshal.FreeHGlobal(pointer);
        }
    }
}