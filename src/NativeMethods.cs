using System;
using System.Drawing;
using System.Runtime.InteropServices;

namespace ScrcpyManager
{
    public static class NativeMethods
    {
        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        public static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, string lParam);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool PrintWindow(IntPtr hwnd, IntPtr hdcBlt, uint nFlags);

        [DllImport("dwmapi.dll")]
        public static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

        public const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;
        public const int EM_SETCUEBANNER = 0x1501;

        public static void SetDarkMode(IntPtr hWnd, bool enableDark)
        {
            try
            {
                int val = enableDark ? 1 : 0;
                DwmSetWindowAttribute(hWnd, DWMWA_USE_IMMERSIVE_DARK_MODE, ref val, sizeof(int));
            }
            catch { }
        }

        public static void UseImmersiveDarkMode(IntPtr hWnd, bool enableDark)
        {
            SetDarkMode(hWnd, enableDark);
        }

        public static void SetCueBanner(IntPtr hWnd, string placeholder)
        {
            try
            {
                SendMessage(hWnd, EM_SETCUEBANNER, IntPtr.Zero, placeholder);
            }
            catch { }
        }
    }
}
