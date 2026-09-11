using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace ScrcpyManager
{
    public static class AppWindowIconManager
    {
        private class ProcessRecord
        {
            public Process Process { get; set; }
            public string Package { get; set; }
            public int Attempts { get; set; }
        }

        private static readonly object _lock = new object();
        private static readonly List<ProcessRecord> _pendingProcesses = new List<ProcessRecord>();
        private static readonly Dictionary<IntPtr, IntPtr> _appliedWindows = new Dictionary<IntPtr, IntPtr>();
        private static readonly Dictionary<IntPtr, string> _windowPackages = new Dictionary<IntPtr, string>();
        private static Timer _scanTimer;

        public static string PrepareAppIconDirectory(string package)
        {
            if (string.IsNullOrEmpty(package) || !AdbService.IsValidPackageName(package)) return null;

            try
            {
                string iconPath = null;
                if (IconService.Instance != null)
                {
                    iconPath = IconService.Instance.GetIconFilePath(package);
                }

                if (string.IsNullOrEmpty(iconPath) || !File.Exists(iconPath))
                {
                    string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                    string cacheFile = Path.Combine(appData, "scrcpy-manager", "icons", package + ".png");
                    if (File.Exists(cacheFile) && new FileInfo(cacheFile).Length > 100)
                    {
                        iconPath = cacheFile;
                    }
                    else
                    {
                        string repoFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "icons", package + ".png");
                        if (File.Exists(repoFile) && new FileInfo(repoFile).Length > 100)
                        {
                            iconPath = repoFile;
                        }
                    }
                }

                if (string.IsNullOrEmpty(iconPath) || !File.Exists(iconPath)) return null;

                string appDataDir = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                string targetDir = Path.Combine(appDataDir, "scrcpy-manager", "app_icons", package);
                Directory.CreateDirectory(targetDir);

                string targetPng = Path.Combine(targetDir, "scrcpy.png");
                if (!File.Exists(targetPng) || File.GetLastWriteTimeUtc(targetPng) != File.GetLastWriteTimeUtc(iconPath))
                {
                    File.Copy(iconPath, targetPng, true);
                }

                string disconnectedTarget = Path.Combine(targetDir, "disconnected.png");
                if (!File.Exists(disconnectedTarget) && !string.IsNullOrEmpty(AdbService.RuntimeDirectory))
                {
                    string srcDisconnected = Path.Combine(AdbService.RuntimeDirectory, "disconnected.png");
                    if (File.Exists(srcDisconnected))
                    {
                        File.Copy(srcDisconnected, disconnectedTarget, true);
                    }
                }

                return targetDir;
            }
            catch
            {
                return null;
            }
        }

        public static void RegisterProcess(Process proc, string package)
        {
            if (proc == null || string.IsNullOrEmpty(package)) return;

            lock (_lock)
            {
                _pendingProcesses.Add(new ProcessRecord
                {
                    Process = proc,
                    Package = package,
                    Attempts = 0
                });

                EnsureTimer();
            }
        }

        private static void EnsureTimer()
        {
            if (_scanTimer == null)
            {
                _scanTimer = new Timer();
                _scanTimer.Interval = 200;
                _scanTimer.Tick += OnScanTick;
                _scanTimer.Start();
            }
        }

        private static void OnScanTick(object sender, EventArgs e)
        {
            ScanWindows();
        }

        public static void ScanWindows()
        {
            lock (_lock)
            {
                // 1. Clean up closed windows and free unmanaged icon handles
                List<IntPtr> closedWindows = new List<IntPtr>();
                foreach (var kvp in _appliedWindows)
                {
                    if (!NativeMethods.IsWindow(kvp.Key))
                    {
                        closedWindows.Add(kvp.Key);
                    }
                }

                foreach (IntPtr hwnd in closedWindows)
                {
                    IntPtr hIcon = _appliedWindows[hwnd];
                    if (hIcon != IntPtr.Zero)
                    {
                        try { NativeMethods.DestroyIcon(hIcon); } catch { }
                    }
                    _appliedWindows.Remove(hwnd);
                    _windowPackages.Remove(hwnd);
                }

                // 2. Check pending processes
                List<ProcessRecord> remaining = new List<ProcessRecord>();
                foreach (var record in _pendingProcesses)
                {
                    try
                    {
                        if (record.Process.HasExited) continue;

                        record.Attempts++;
                        IntPtr hwnd = record.Process.MainWindowHandle;
                        if (hwnd != IntPtr.Zero && NativeMethods.IsWindow(hwnd))
                        {
                            ApplyWindowIcon(hwnd, record.Package);
                            continue; // Successfully processed
                        }

                        // Keep waiting up to 60 attempts (~12 seconds)
                        if (record.Attempts < 60)
                        {
                            remaining.Add(record);
                        }
                    }
                    catch
                    {
                        // Process error, drop
                    }
                }

                _pendingProcesses.Clear();
                _pendingProcesses.AddRange(remaining);

                if (_pendingProcesses.Count == 0 && _appliedWindows.Count == 0 && _scanTimer != null)
                {
                    _scanTimer.Stop();
                    _scanTimer.Dispose();
                    _scanTimer = null;
                }
            }
        }

        public static void ApplyWindowIcon(IntPtr hwnd, string package)
        {
            if (hwnd == IntPtr.Zero || string.IsNullOrEmpty(package)) return;

            lock (_lock)
            {
                if (_appliedWindows.ContainsKey(hwnd)) return;
            }

            Bitmap rawBmp = null;
            if (IconService.Instance != null)
            {
                rawBmp = IconService.Instance.GetRawIconBitmap(package);
            }

            if (rawBmp == null)
            {
                string iconPath = PrepareAppIconDirectory(package);
                if (!string.IsNullOrEmpty(iconPath))
                {
                    string pngFile = Path.Combine(iconPath, "scrcpy.png");
                    if (File.Exists(pngFile))
                    {
                        try
                        {
                            byte[] bytes = File.ReadAllBytes(pngFile);
                            using (MemoryStream ms = new MemoryStream(bytes))
                            using (Image img = Image.FromStream(ms))
                            {
                                rawBmp = new Bitmap(img);
                            }
                        }
                        catch { }
                    }
                }
            }

            if (rawBmp == null) return;

            try
            {
                IntPtr hIcon = rawBmp.GetHicon();
                if (hIcon != IntPtr.Zero)
                {
                    NativeMethods.SendMessage(hwnd, NativeMethods.WM_SETICON, (IntPtr)NativeMethods.ICON_SMALL, hIcon);
                    NativeMethods.SendMessage(hwnd, NativeMethods.WM_SETICON, (IntPtr)NativeMethods.ICON_BIG, hIcon);
                    NativeMethods.SetClassLongPtr(hwnd, NativeMethods.GCLP_HICON, hIcon);
                    NativeMethods.SetClassLongPtr(hwnd, NativeMethods.GCLP_HICONSM, hIcon);
                    NativeMethods.SetWindowAppId(hwnd, "ScrcpyManager.App." + package);

                    lock (_lock)
                    {
                        _appliedWindows[hwnd] = hIcon;
                        _windowPackages[hwnd] = package;
                    }
                }
            }
            catch { }
            finally
            {
                rawBmp.Dispose();
            }
        }

        public static void Shutdown()
        {
            lock (_lock)
            {
                if (_scanTimer != null)
                {
                    _scanTimer.Stop();
                    _scanTimer.Dispose();
                    _scanTimer = null;
                }

                foreach (var hIcon in _appliedWindows.Values)
                {
                    if (hIcon != IntPtr.Zero)
                    {
                        try { NativeMethods.DestroyIcon(hIcon); } catch { }
                    }
                }
                _appliedWindows.Clear();
                _windowPackages.Clear();
                _pendingProcesses.Clear();
            }
        }
    }
}

