using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ScrcpyManager
{
    public class AdbService
    {
        private static readonly List<Process> _activeScrcpyProcesses = new List<Process>();
        private static int _originalScreenTimeout = 30000;

        public static List<Process> ActiveProcesses
        {
            get
            {
                lock (_activeScrcpyProcesses)
                {
                    _activeScrcpyProcesses.RemoveAll(p => {
                        try { return p.HasExited; } catch { return true; }
                    });
                    return new List<Process>(_activeScrcpyProcesses);
                }
            }
        }

        // Instance wrappers allowing both myAdb.Method() and AdbService.Method()
        public Task<string> RunAdbAsync(string args, int timeoutMs = 5000)
        {
            return ExecuteAdbAsync(args, timeoutMs);
        }

        public Task<DeviceInfo> GetDeviceInfoAsync()
        {
            return FetchDeviceInfoAsync();
        }

        public Task<List<string>> GetInstalledPackagesAsync()
        {
            return FetchInstalledPackagesAsync();
        }

        public Task<bool> IsDeviceConnectedAsync()
        {
            return CheckDeviceConnectedAsync();
        }

        public Task<string> GetWifiIpAddressAsync()
        {
            return GetDeviceWifiIpAsync();
        }

        public Task<bool> ConnectWifiAsync(string ip)
        {
            return ConnectDeviceWifiAsync(ip);
        }

        public Task PreventScreenLockAsync()
        {
            return EnableKeepAwakeAsync();
        }

        public Task RestoreScreenTimeoutAsync(int timeout = 30000)
        {
            return RestoreKeepAwakeAsync(timeout);
        }

        public Task<int> GetOriginalScreenTimeoutAsync()
        {
            return QueryOriginalScreenTimeoutAsync();
        }

        public Task<bool> IsKeyguardShowingAsync()
        {
            return CheckKeyguardShowingAsync();
        }

        public Task UnlockDeviceAsync(string pin = null)
        {
            return PerformUnlockDeviceAsync(pin);
        }

        public Process LaunchScrcpy(string args)
        {
            return StartScrcpyProcess(args);
        }

        public Task<Process> StartScrcpyAppAsync(string package, string title, bool useUhid, bool forwardClicks, string displaySize, bool audio = true)
        {
            return LaunchScrcpyForAppAsync(package, title, useUhid, forwardClicks, displaySize, audio);
        }

        public Task SendKeyAsync(int keycode)
        {
            return SendKeyEventAsync(keycode);
        }

        // Static Implementations
        public static Task<string> ExecuteAdbAsync(string args, int timeoutMs = 5000)
        {
            return Task.Run(() =>
            {
                try
                {
                    ProcessStartInfo psi = new ProcessStartInfo
                    {
                        FileName = "adb.exe",
                        Arguments = args,
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        RedirectStandardInput = true,
                        CreateNoWindow = true,
                        WindowStyle = ProcessWindowStyle.Hidden
                    };

                    using (Process proc = Process.Start(psi))
                    {
                        if (proc == null) return string.Empty;
                        try { proc.StandardInput.Close(); } catch { }
                        string stdout = proc.StandardOutput.ReadToEnd();
                        if (!proc.WaitForExit(timeoutMs))
                        {
                            try { proc.Kill(); } catch { }
                            return stdout;
                        }
                        return stdout;
                    }
                }
                catch
                {
                    return string.Empty;
                }
            });
        }

        public static Task SendKeyEventAsync(int keycode)
        {
            return ExecuteAdbAsync(string.Format("shell input keyevent {0}", keycode), 1500);
        }

        public static async Task<bool> CheckDeviceConnectedAsync()
        {
            string devicesOut = await ExecuteAdbAsync("devices", 3000);
            return !string.IsNullOrEmpty(devicesOut) && Regex.IsMatch(devicesOut, @"\bdevice\b(?!\s*unauthorized)");
        }

        public static async Task<DeviceInfo> FetchDeviceInfoAsync()
        {
            DeviceInfo info = new DeviceInfo();

            string devicesOut = await ExecuteAdbAsync("devices", 3000);
            if (string.IsNullOrEmpty(devicesOut) || !Regex.IsMatch(devicesOut, @"\bdevice\b(?!\s*unauthorized)"))
            {
                info.IsOnline = false;
                return info;
            }

            info.IsOnline = true;
            info.IsWifiConnected = Regex.IsMatch(devicesOut, @"\b\d{1,3}(\.\d{1,3}){3}:\d+\b\s+device");

            // Model & Manufacturer & Battery
            Task<string> modelTask = ExecuteAdbAsync("shell getprop ro.product.model", 3000);
            Task<string> mfgTask = ExecuteAdbAsync("shell getprop ro.product.manufacturer", 3000);
            Task<string> batteryTask = ExecuteAdbAsync("shell dumpsys battery", 3000);

            await Task.WhenAll(modelTask, mfgTask, batteryTask);

            string model = (modelTask.Result ?? string.Empty).Trim();
            string mfg = (mfgTask.Result ?? string.Empty).Trim();

            if (!string.IsNullOrEmpty(model)) info.Model = model;
            if (!string.IsNullOrEmpty(mfg)) info.Manufacturer = mfg;

            string battery = batteryTask.Result ?? string.Empty;
            Match mLevel = Regex.Match(battery, @"level:\s*(\d+)");
            Match mStatus = Regex.Match(battery, @"status:\s*(\d+)");

            if (mLevel.Success)
            {
                int lvl;
                if (int.TryParse(mLevel.Groups[1].Value, out lvl))
                {
                    info.BatteryLevel = lvl;
                }
            }

            if (mStatus.Success)
            {
                string st = mStatus.Groups[1].Value;
                info.IsCharging = (st == "2" || st == "5");
            }

            return info;
        }

        public static async Task<List<string>> FetchInstalledPackagesAsync()
        {
            string outStr = await ExecuteAdbAsync("shell pm list packages", 8000);
            var result = new List<string>();
            if (string.IsNullOrEmpty(outStr)) return result;

            using (StringReader sr = new StringReader(outStr))
            {
                string line;
                while ((line = sr.ReadLine()) != null)
                {
                    line = line.Trim();
                    if (line.StartsWith("package:", StringComparison.OrdinalIgnoreCase))
                    {
                        string pkg = line.Substring("package:".Length).Trim();
                        if (!string.IsNullOrEmpty(pkg) && !result.Contains(pkg))
                        {
                            result.Add(pkg);
                        }
                    }
                }
            }

            result.Sort(StringComparer.OrdinalIgnoreCase);
            return result;
        }

        public static async Task<string> GetDeviceWifiIpAsync()
        {
            string route = await ExecuteAdbAsync("shell ip route", 3000);
            Match m = Regex.Match(route, @"src\s+(\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3})");
            if (m.Success) return m.Groups[1].Value;

            string addr = await ExecuteAdbAsync("shell ip -f inet addr show wlan0", 3000);
            Match m2 = Regex.Match(addr, @"inet\s+(\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3})");
            if (m2.Success) return m2.Groups[1].Value;

            return null;
        }

        public static async Task<bool> ConnectDeviceWifiAsync(string ip)
        {
            if (string.IsNullOrEmpty(ip)) return false;
            await ExecuteAdbAsync("tcpip 5555", 3000);
            await Task.Delay(1000);
            string res = await ExecuteAdbAsync("connect " + ip + ":5555", 5000);
            return res.IndexOf("connected to", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        public static async Task<int> QueryOriginalScreenTimeoutAsync()
        {
            try
            {
                string current = (await ExecuteAdbAsync("shell settings get system screen_off_timeout", 2000)).Trim();
                int val;
                if (int.TryParse(current, out val) && val > 0 && val != 2147483647)
                {
                    _originalScreenTimeout = val;
                    return val;
                }
            }
            catch { }
            return _originalScreenTimeout;
        }

        public static async Task EnableKeepAwakeAsync()
        {
            await ExecuteAdbAsync("shell settings put system screen_off_timeout 2147483647", 2000);
            await ExecuteAdbAsync("shell svc power stayon true", 2000);
        }

        public static async Task RestoreKeepAwakeAsync(int timeout = 30000)
        {
            int t = timeout > 0 ? timeout : (_originalScreenTimeout > 0 ? _originalScreenTimeout : 30000);
            await ExecuteAdbAsync("shell settings put system screen_off_timeout " + t, 2000);
            await ExecuteAdbAsync("shell svc power stayon false", 2000);
        }

        public static async Task<bool> CheckKeyguardShowingAsync()
        {
            string outDump = await ExecuteAdbAsync("shell dumpsys window", 3000);
            return Regex.IsMatch(outDump, @"isKeyguardShowing=true|mShowing=true");
        }

        public static async Task PerformUnlockDeviceAsync(string pin = null)
        {
            await ExecuteAdbAsync("shell input keyevent 224", 2000);
            await Task.Delay(150);
            await ExecuteAdbAsync("shell wm dismiss-keyguard", 2000);

            if (!string.IsNullOrEmpty(pin))
            {
                await Task.Delay(200);
                await ExecuteAdbAsync(string.Format("shell input text \"{0}\"", pin), 2000);
                await Task.Delay(100);
                await ExecuteAdbAsync("shell input keyevent 66", 2000);
            }
        }

        public static async Task<Process> LaunchScrcpyForAppAsync(string package, string title, bool useUhid, bool forwardClicks, string displaySize, bool audio = true)
        {
            if (string.IsNullOrWhiteSpace(package)) return null;

            await EnableKeepAwakeAsync();
            await PerformUnlockDeviceAsync(Environment.GetEnvironmentVariable("SCRCPY_ADB_PIN"));

            string winTitle = !string.IsNullOrEmpty(title) ? title : package;
            string disp = !string.IsNullOrWhiteSpace(displaySize) ? displaySize : "1080x2400";

            List<string> argsList = new List<string>
            {
                string.Format("--new-display={0}", disp),
                string.Format("--start-app={0}", package),
                string.Format("--window-title=\"{0}\"", winTitle),
                "-w"
            };

            if (useUhid) argsList.Add("-K");
            if (!audio) argsList.Add("--no-audio");

            bool isRdc = string.Equals(package, "com.microsoft.rdc.androidx", StringComparison.OrdinalIgnoreCase);
            if (isRdc)
            {
                argsList.Add("-b 16M");
                argsList.Add("--mouse-bind=++++");
                if (!useUhid) argsList.Add("-K");
            }
            else
            {
                argsList.Add("-x");
                if (forwardClicks)
                {
                    argsList.Add("--mouse-bind=++++");
                }
            }

            string scrcpyArgs = string.Join(" ", argsList);
            return StartScrcpyProcess(scrcpyArgs);
        }

        public static Process StartScrcpyProcess(string args)
        {
            try
            {
                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = "scrcpy.exe",
                    Arguments = args,
                    UseShellExecute = false,
                    CreateNoWindow = false
                };

                Process proc = Process.Start(psi);
                if (proc != null)
                {
                    lock (_activeScrcpyProcesses)
                    {
                        _activeScrcpyProcesses.Add(proc);
                    }
                }
                return proc;
            }
            catch
            {
                return null;
            }
        }
    }
}

