using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

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
                // Reset regardless of the call's outcome: on failure the handle is no longer
                // valid to retry, and leaving it set would make Start() a permanent no-op.
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
                    IntPtr fg = NativeMethods.GetForegroundWindow();
                    bool isScrcpy = fg != IntPtr.Zero && IsScrcpyWindow(fg);

                    if (isScrcpy)
                    {
                        if (kb.vkCode == NativeMethods.VK_ESCAPE)
                        {
                            if ((DateTime.UtcNow - _lastBackTime).TotalMilliseconds > 120)
                            {
                                _lastBackTime = DateTime.UtcNow;
                                AdbService.SendKeyEventAsync(4);
                            }
                            return (IntPtr)1; // Consume raw ESC
                        }

                        // Klawisz F12: natychmiastowe przywrócenie rozciągnięcia 3840x1080 na dwa monitory
                        if (kb.vkCode == NativeMethods.VK_F12)
                        {
                            RestoreDualMonitorSpan(fg);
                            return (IntPtr)1;
                        }

                        // Przechwytywanie Win + Strzałki:
                        // System Windows 11 przechwytuje Win+Strzałki globalnie i kafelkuje/maksymalizuje
                        // okno scrcpy do POJEDYNCZEGO monitora (niszcząc rozciągnięcie 3840x1080).
                        // Blokujemy przechwycenie przez system Windows i przekazujemy skrót do Androida/RDP.
                        bool isWinDown = (NativeMethods.GetAsyncKeyState(NativeMethods.VK_LWIN) < 0 || NativeMethods.GetAsyncKeyState(NativeMethods.VK_RWIN) < 0);
                        if (isWinDown && (kb.vkCode == NativeMethods.VK_LEFT || kb.vkCode == NativeMethods.VK_RIGHT || kb.vkCode == NativeMethods.VK_UP || kb.vkCode == NativeMethods.VK_DOWN))
                        {
                            int dpad = kb.vkCode == NativeMethods.VK_LEFT ? 21 :
                                       kb.vkCode == NativeMethods.VK_RIGHT ? 22 :
                                       kb.vkCode == NativeMethods.VK_UP ? 19 : 20;
                            System.Threading.Tasks.Task ignored = AdbService.ExecuteAdbAsync("shell input keycombination 117 " + dpad, 1000);
                            return (IntPtr)1; // Blokujemy dla lokalnego hosta Windows!
                        }
                    }
                }
                else if (msg == NativeMethods.WM_KEYUP || msg == NativeMethods.WM_SYSKEYUP)
                {
                    NativeMethods.KBDLLHOOKSTRUCT kb = (NativeMethods.KBDLLHOOKSTRUCT)Marshal.PtrToStructure(lParam, typeof(NativeMethods.KBDLLHOOKSTRUCT));
                    IntPtr fg = NativeMethods.GetForegroundWindow();
                    bool isScrcpy = fg != IntPtr.Zero && IsScrcpyWindow(fg);

                    if (isScrcpy)
                    {
                        if (kb.vkCode == NativeMethods.VK_ESCAPE || kb.vkCode == NativeMethods.VK_F12)
                        {
                            return (IntPtr)1;
                        }
                        bool isWinDown = (NativeMethods.GetAsyncKeyState(NativeMethods.VK_LWIN) < 0 || NativeMethods.GetAsyncKeyState(NativeMethods.VK_RWIN) < 0);
                        if (isWinDown && (kb.vkCode == NativeMethods.VK_LEFT || kb.vkCode == NativeMethods.VK_RIGHT || kb.vkCode == NativeMethods.VK_UP || kb.vkCode == NativeMethods.VK_DOWN))
                        {
                            return (IntPtr)1;
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

                return AdbService.IsActiveScrcpyProcessId((int)pid);
            }
            catch { }
            return false;
        }

        public static void RestoreDualMonitorSpan(IntPtr scrcpyHwnd)
        {
            if (scrcpyHwnd == IntPtr.Zero) return;
            try
            {
                int minX = 0;
                int minY = 0;
                int totalW = 3840;
                int totalH = 1080;
                try
                {
                    var screens = System.Windows.Forms.Screen.AllScreens;
                    if (screens != null && screens.Length > 1)
                    {
                        int mx = int.MaxValue;
                        int my = int.MaxValue;
                        int maxR = int.MinValue;
                        int maxB = int.MinValue;
                        foreach (var s in screens)
                        {
                            if (s.Bounds.X < mx) mx = s.Bounds.X;
                            if (s.Bounds.Y < my) my = s.Bounds.Y;
                            if (s.Bounds.Right > maxR) maxR = s.Bounds.Right;
                            if (s.Bounds.Bottom > maxB) maxB = s.Bounds.Bottom;
                        }
                        if (mx != int.MaxValue) minX = mx;
                        if (my != int.MaxValue) minY = my;
                        if (maxR > minX) totalW = maxR - minX;
                        if (maxB > minY) totalH = maxB - minY;
                    }
                }
                catch { }

                NativeMethods.SetWindowPos(scrcpyHwnd, IntPtr.Zero, minX, minY, totalW, totalH,
                    NativeMethods.SWP_NOZORDER | NativeMethods.SWP_SHOWWINDOW);
            }
            catch { }
        }
    }
}
