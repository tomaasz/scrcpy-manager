using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace ScrcpyManager
{
    public static class KeyboardHook
    {
        private static IntPtr _hookId = IntPtr.Zero;
        private static NativeMethods.LowLevelKeyboardProc _proc = HookCallback;
        private static DateTime _lastBackTime = DateTime.MinValue;

        public static void Start()
        {
            if (_hookId == IntPtr.Zero)
            {
                try
                {
                    using (Process curProcess = Process.GetCurrentProcess())
                    using (ProcessModule curModule = curProcess.MainModule)
                    {
                        IntPtr hMod = NativeMethods.GetModuleHandle(curModule.ModuleName);
                        _hookId = NativeMethods.SetWindowsHookEx(
                            NativeMethods.WH_KEYBOARD_LL,
                            _proc,
                            hMod,
                            0);
                    }
                }
                catch { }
            }
        }

        public static void Stop()
        {
            if (_hookId != IntPtr.Zero)
            {
                try
                {
                    NativeMethods.UnhookWindowsHookEx(_hookId);
                }
                catch { }
                _hookId = IntPtr.Zero;
            }
        }

        private static IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                int msg = wParam.ToInt32();
                if (msg == NativeMethods.WM_KEYDOWN || msg == NativeMethods.WM_SYSKEYDOWN)
                {
                    NativeMethods.KBDLLHOOKSTRUCT kb = (NativeMethods.KBDLLHOOKSTRUCT)Marshal.PtrToStructure(lParam, typeof(NativeMethods.KBDLLHOOKSTRUCT));
                    if (kb.vkCode == NativeMethods.VK_ESCAPE)
                    {
                        IntPtr fg = NativeMethods.GetForegroundWindow();
                        if (fg != IntPtr.Zero && IsScrcpyWindow(fg))
                        {
                            if ((DateTime.UtcNow - _lastBackTime).TotalMilliseconds > 120)
                            {
                                _lastBackTime = DateTime.UtcNow;
                                TriggerBackNavigation(fg);
                            }
                            return (IntPtr)1; // Consume raw ESC
                        }
                    }
                }
                else if (msg == NativeMethods.WM_KEYUP || msg == NativeMethods.WM_SYSKEYUP)
                {
                    NativeMethods.KBDLLHOOKSTRUCT kb = (NativeMethods.KBDLLHOOKSTRUCT)Marshal.PtrToStructure(lParam, typeof(NativeMethods.KBDLLHOOKSTRUCT));
                    if (kb.vkCode == NativeMethods.VK_ESCAPE)
                    {
                        IntPtr fg = NativeMethods.GetForegroundWindow();
                        if (fg != IntPtr.Zero && IsScrcpyWindow(fg))
                        {
                            return (IntPtr)1; // Consume raw ESC keyup
                        }
                    }
                }
            }
            return NativeMethods.CallNextHookEx(_hookId, nCode, wParam, lParam);
        }

        public static void TriggerBackNavigation(IntPtr scrcpyHwnd)
        {
            try
            {
                // Send native Alt+B to scrcpy window
                NativeMethods.keybd_event(NativeMethods.VK_MENU, 0, 0, UIntPtr.Zero);
                NativeMethods.keybd_event(NativeMethods.VK_B, 0, 0, UIntPtr.Zero);
                NativeMethods.keybd_event(NativeMethods.VK_B, 0, NativeMethods.KEYEVENTF_KEYUP, UIntPtr.Zero);
                NativeMethods.keybd_event(NativeMethods.VK_MENU, 0, NativeMethods.KEYEVENTF_KEYUP, UIntPtr.Zero);
            }
            catch { }
        }

        public static void TriggerHomeNavigation(IntPtr scrcpyHwnd)
        {
            try
            {
                // Send native Alt+H to scrcpy window
                NativeMethods.keybd_event(NativeMethods.VK_MENU, 0, 0, UIntPtr.Zero);
                NativeMethods.keybd_event(NativeMethods.VK_H, 0, 0, UIntPtr.Zero);
                NativeMethods.keybd_event(NativeMethods.VK_H, 0, NativeMethods.KEYEVENTF_KEYUP, UIntPtr.Zero);
                NativeMethods.keybd_event(NativeMethods.VK_MENU, 0, NativeMethods.KEYEVENTF_KEYUP, UIntPtr.Zero);
            }
            catch { }
        }

        public static void TriggerRecentsNavigation(IntPtr scrcpyHwnd)
        {
            try
            {
                // Send native Alt+S to scrcpy window
                NativeMethods.keybd_event(NativeMethods.VK_MENU, 0, 0, UIntPtr.Zero);
                NativeMethods.keybd_event(NativeMethods.VK_S, 0, 0, UIntPtr.Zero);
                NativeMethods.keybd_event(NativeMethods.VK_S, 0, NativeMethods.KEYEVENTF_KEYUP, UIntPtr.Zero);
                NativeMethods.keybd_event(NativeMethods.VK_MENU, 0, NativeMethods.KEYEVENTF_KEYUP, UIntPtr.Zero);
            }
            catch { }
        }

        public static bool IsScrcpyWindow(IntPtr hWnd)
        {
            if (hWnd == IntPtr.Zero) return false;
            try
            {
                uint pid;
                NativeMethods.GetWindowThreadProcessId(hWnd, out pid);
                if (pid == 0) return false;

                foreach (Process p in AdbService.ActiveProcesses)
                {
                    try
                    {
                        if (p.Id == (int)pid) return true;
                    }
                    catch { }
                }

                Process proc = Process.GetProcessById((int)pid);
                if (proc != null && proc.ProcessName.IndexOf("scrcpy", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return true;
                }
            }
            catch { }
            return false;
        }
    }
}

