using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ScrcpyManager
{
    public class AdbService
    {
        public static string RuntimeDirectory { get; set; }
        public sealed class CommandResult
        {
            public int ExitCode { get; set; }
            public string StandardOutput { get; set; }
            public string StandardError { get; set; }
            public bool TimedOut { get; set; }
            public bool Succeeded { get { return !TimedOut && ExitCode == 0; } }
        }

        private static readonly List<Process> _activeScrcpyProcesses = new List<Process>();
        private static readonly object _serialLock = new object();
        private static string _selectedSerial;

        private static readonly Regex PackageNameRegex = new Regex(
            @"^[A-Za-z0-9_]+(?:\.[A-Za-z0-9_]+)+$",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        public static List<Process> ActiveProcesses
        {
            get
            {
                lock (_activeScrcpyProcesses)
                {
                    for (int i = _activeScrcpyProcesses.Count - 1; i >= 0; i--)
                    {
                        Process process = _activeScrcpyProcesses[i];
                        bool remove;
                        try { remove = process.HasExited; } catch { remove = true; }
                        if (remove)
                        {
                            _activeScrcpyProcesses.RemoveAt(i);
                            try { process.Dispose(); } catch { }
                        }
                    }
                    return new List<Process>(_activeScrcpyProcesses);
                }
            }
        }

        public static bool IsActiveScrcpyProcessId(int processId)
        {
            lock (_activeScrcpyProcesses)
            {
                foreach (Process process in _activeScrcpyProcesses)
                {
                    try
                    {
                        if (!process.HasExited && process.Id == processId) return true;
                    }
                    catch { }
                }
                return false;
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

        public Task RestoreScreenTimeoutAsync(int timeout = 0)
        {
            return RestoreKeepAwakeAsync(timeout);
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

        public Task<Process> StartScrcpyAppAsync(string package, string title, bool useUhid, bool forwardClicks, string displaySize, bool audio = true, bool autoTaskbar = false)
        {
            return LaunchScrcpyForAppAsync(package, title, useUhid, forwardClicks, displaySize, audio, autoTaskbar);
        }

        public Task<Process> StartScrcpyAppAsync(string package, string title, AppLaunchProfile profile, bool audio = true, bool autoTaskbar = false)
        {
            return LaunchScrcpyForAppAsync(package, title, profile, audio, autoTaskbar);
        }

        public Task SendKeyAsync(int keycode)
        {
            return SendKeyEventAsync(keycode);
        }

        public Task<bool> IsPackageInstalledAsync(string package, int timeoutMs = 2500)
        {
            return CheckPackageInstalledAsync(package, timeoutMs);
        }

        public Task<bool> InstallPackageAsync(string apkPath, int timeoutMs = 60000)
        {
            return ExecuteInstallPackageAsync(apkPath, timeoutMs);
        }

        public Task GrantTaskbarPermissionsAsync()
        {
            return ExecuteGrantTaskbarPermissionsAsync();
        }

        public Task OpenPlayStoreAsync(string package)
        {
            return ExecuteOpenPlayStoreAsync(package);
        }

        // Static Implementations
        public static async Task<string> ExecuteAdbAsync(string args, int timeoutMs = 5000)
        {
            CommandResult result = await ExecuteAdbDetailedAsync(args, timeoutMs).ConfigureAwait(false);
            return result.StandardOutput ?? string.Empty;
        }

        public static Task<CommandResult> ExecuteAdbDetailedAsync(string args, int timeoutMs = 5000)
        {
            return ExecuteProcessAsync("adb.exe", AddDeviceSelector(args), timeoutMs);
        }

        private static async Task<CommandResult> ExecuteProcessAsync(string fileName, string args, int timeoutMs)
        {
            StringBuilder stdout = new StringBuilder();
            StringBuilder stderr = new StringBuilder();

            using (Process proc = new Process())
            {
                TaskCompletionSource<bool> exited = new TaskCompletionSource<bool>(
                    TaskCreationOptions.RunContinuationsAsynchronously);

                try
                {
                    proc.StartInfo = new ProcessStartInfo
                    {
                        FileName = fileName,
                        Arguments = args,
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true,
                        WindowStyle = ProcessWindowStyle.Hidden
                    };
                    proc.EnableRaisingEvents = true;
                    proc.OutputDataReceived += (s, e) =>
                    {
                        if (e.Data != null) lock (stdout) stdout.AppendLine(e.Data);
                    };
                    proc.ErrorDataReceived += (s, e) =>
                    {
                        if (e.Data != null) lock (stderr) stderr.AppendLine(e.Data);
                    };
                    proc.Exited += (s, e) => exited.TrySetResult(true);

                    if (!proc.Start())
                    {
                        return FailedResult("Nie udało się uruchomić " + fileName + ".");
                    }

                    proc.BeginOutputReadLine();
                    proc.BeginErrorReadLine();

                    Task winner = await Task.WhenAny(exited.Task, Task.Delay(Math.Max(1, timeoutMs))).ConfigureAwait(false);
                    bool timedOut = winner != exited.Task;
                    if (timedOut)
                    {
                        try { proc.Kill(); } catch { }
                        await Task.WhenAny(exited.Task, Task.Delay(1000)).ConfigureAwait(false);
                    }

                    bool hasExited;
                    try { hasExited = proc.HasExited; } catch { hasExited = false; }
                    if (hasExited)
                    {
                        // Flushes the final asynchronous OutputDataReceived/ErrorDataReceived events.
                        proc.WaitForExit();
                    }

                    int exitCode = -1;
                    if (hasExited)
                    {
                        try { exitCode = proc.ExitCode; } catch { }
                    }

                    return new CommandResult
                    {
                        ExitCode = exitCode,
                        StandardOutput = stdout.ToString(),
                        StandardError = stderr.ToString(),
                        TimedOut = timedOut
                    };
                }
                catch (Exception ex)
                {
                    return FailedResult(ex.Message);
                }
            }
        }

        private static CommandResult FailedResult(string error)
        {
            return new CommandResult
            {
                ExitCode = -1,
                StandardOutput = string.Empty,
                StandardError = error ?? string.Empty,
                TimedOut = false
            };
        }

        private static string AddDeviceSelector(string args)
        {
            if (string.IsNullOrWhiteSpace(args)) return args;
            string trimmed = args.TrimStart();
            if (trimmed.StartsWith("devices", StringComparison.OrdinalIgnoreCase) ||
                trimmed.StartsWith("connect ", StringComparison.OrdinalIgnoreCase) ||
                trimmed.StartsWith("disconnect", StringComparison.OrdinalIgnoreCase) ||
                trimmed.StartsWith("start-server", StringComparison.OrdinalIgnoreCase) ||
                trimmed.StartsWith("kill-server", StringComparison.OrdinalIgnoreCase) ||
                trimmed.StartsWith("-s ", StringComparison.OrdinalIgnoreCase))
            {
                return args;
            }

            string serial;
            lock (_serialLock) serial = _selectedSerial;
            return string.IsNullOrEmpty(serial) ? args : "-s " + QuoteWindowsArgument(serial) + " " + args;
        }

        public static bool IsValidPackageName(string package)
        {
            return !string.IsNullOrWhiteSpace(package) && PackageNameRegex.IsMatch(package);
        }

        private static readonly Regex DisplaySizeRegex = new Regex(
            @"^\d+x\d+(?:/\d+)?$", RegexOptions.Compiled | RegexOptions.CultureInvariant);

        // Single source of truth for what a legal "--new-display" value looks like
        // (WIDTHxHEIGHT with an optional /DPI suffix). Used by the launcher itself as
        // well as by the profile editors so they can never accept/reject different inputs.
        public static bool IsValidDisplaySize(string value)
        {
            return !string.IsNullOrWhiteSpace(value) && DisplaySizeRegex.IsMatch(value);
        }

        public static string QuoteWindowsArgument(string value)
        {
            if (value == null) return "\"\"";
            value = value.Replace("\r", " ").Replace("\n", " ");
            StringBuilder quoted = new StringBuilder("\"");
            int slashes = 0;
            foreach (char ch in value)
            {
                if (ch == '\\')
                {
                    slashes++;
                }
                else if (ch == '"')
                {
                    quoted.Append('\\', (slashes * 2) + 1);
                    quoted.Append('"');
                    slashes = 0;
                }
                else
                {
                    quoted.Append('\\', slashes);
                    quoted.Append(ch);
                    slashes = 0;
                }
            }
            quoted.Append('\\', slashes * 2);
            quoted.Append('"');
            return quoted.ToString();
        }

        public static Task SendKeyEventAsync(int keycode)
        {
            return ExecuteAdbAsync(string.Format("shell input keyevent {0}", keycode), 1500);
        }

        public static async Task<bool> CheckDeviceConnectedAsync()
        {
            DeviceInfo detected = await DetectDeviceAsync();
            return detected.ConnectionState == DeviceConnectionState.Online;
        }

        public static async Task<DeviceInfo> FetchDeviceInfoAsync()
        {
            DeviceInfo info = await DetectDeviceAsync();
            if (!info.IsOnline)
            {
                return info;
            }

            info.IsWifiConnected = Regex.IsMatch(info.Serial ?? string.Empty, @"^\d{1,3}(\.\d{1,3}){3}:\d+$");

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

        private static async Task<DeviceInfo> DetectDeviceAsync()
        {
            DeviceInfo info = new DeviceInfo();
            CommandResult result = await ExecuteAdbDetailedAsync("devices -l", 3000).ConfigureAwait(false);
            if (result.TimedOut)
            {
                info.ConnectionError = "ADB timeout";
                return info;
            }
            if (result.ExitCode != 0)
            {
                info.ConnectionError = result.StandardError;
                return info;
            }

            List<Tuple<string, string>> devices = new List<Tuple<string, string>>();
            using (StringReader reader = new StringReader(result.StandardOutput ?? string.Empty))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    line = line.Trim();
                    if (line.Length == 0 || line.StartsWith("List of devices", StringComparison.OrdinalIgnoreCase)) continue;
                    string[] parts = Regex.Split(line, @"\s+");
                    if (parts.Length >= 2) devices.Add(Tuple.Create(parts[0], parts[1].ToLowerInvariant()));
                }
            }

            List<Tuple<string, string>> online = devices.FindAll(d => d.Item2 == "device");
            if (online.Count == 1)
            {
                info.Serial = online[0].Item1;
                info.IsOnline = true;
                info.ConnectionState = DeviceConnectionState.Online;
                lock (_serialLock) _selectedSerial = info.Serial;
                return info;
            }

            lock (_serialLock) _selectedSerial = null;
            if (online.Count > 1)
            {
                info.ConnectionState = DeviceConnectionState.Multiple;
                info.ConnectionError = "Podłączono więcej niż jedno urządzenie.";
                return info;
            }

            if (devices.Exists(d => d.Item2 == "unauthorized")) info.ConnectionState = DeviceConnectionState.Unauthorized;
            else if (devices.Exists(d => d.Item2 == "offline")) info.ConnectionState = DeviceConnectionState.Offline;
            else if (devices.Exists(d => d.Item2 == "recovery")) info.ConnectionState = DeviceConnectionState.Recovery;
            else info.ConnectionState = DeviceConnectionState.None;
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

        public static async Task<bool> CheckPackageInstalledAsync(string package, int timeoutMs = 2500)
        {
            if (string.IsNullOrWhiteSpace(package)) return false;
            CommandResult res = await ExecuteAdbDetailedAsync("shell pm path " + QuoteWindowsArgument(package), timeoutMs).ConfigureAwait(false);
            if (res.TimedOut || res.ExitCode != 0) return false;
            return (res.StandardOutput ?? string.Empty).IndexOf("package:", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        public static async Task<bool> ExecuteInstallPackageAsync(string apkPath, int timeoutMs = 60000)
        {
            if (string.IsNullOrWhiteSpace(apkPath) || !File.Exists(apkPath)) return false;
            CommandResult res = await ExecuteAdbDetailedAsync("install -r " + QuoteWindowsArgument(apkPath), timeoutMs).ConfigureAwait(false);
            if (res.TimedOut || res.ExitCode != 0) return false;
            return (res.StandardOutput ?? string.Empty).IndexOf("Success", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        public static async Task ExecuteGrantTaskbarPermissionsAsync()
        {
            try
            {
                await ExecuteAdbAsync("shell pm grant com.farmerbb.taskbar android.permission.WRITE_SECURE_SETTINGS", 3000).ConfigureAwait(false);
                await ExecuteAdbAsync("shell appops set com.farmerbb.taskbar SYSTEM_ALERT_WINDOW allow", 3000).ConfigureAwait(false);
            }
            catch { }
        }

        public static async Task ExecuteOpenPlayStoreAsync(string package)
        {
            try
            {
                await ExecuteAdbAsync("shell am start -a android.intent.action.VIEW -d " + QuoteWindowsArgument("market://details?id=" + package), 3000).ConfigureAwait(false);
            }
            catch { }
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
            IPAddress parsedIp;
            if (!IPAddress.TryParse(ip, out parsedIp) || parsedIp.AddressFamily != AddressFamily.InterNetwork) return false;
            ip = parsedIp.ToString();
            await ExecuteAdbAsync("tcpip 5555", 3000);
            await Task.Delay(1000);
            string res = await ExecuteAdbAsync("connect " + ip + ":5555", 5000);
            return res.IndexOf("connected to", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        // Cache of the device's screen_off_timeout as it was before we last maxed it out,
        // so RestoreKeepAwakeAsync can put back the user's real preference instead of a
        // hardcoded guess. Captured lazily, once per device session.
        private static int _originalScreenTimeout = -1;

        public static async Task<int> QueryOriginalScreenTimeoutAsync()
        {
            try
            {
                string current = (await ExecuteAdbAsync("shell settings get system screen_off_timeout", 2000)).Trim();
                int val;
                if (int.TryParse(current, out val) && val > 0 && val != int.MaxValue)
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
            if (_originalScreenTimeout <= 0)
            {
                await QueryOriginalScreenTimeoutAsync();
            }
            await ExecuteAdbAsync("shell settings put system screen_off_timeout 2147483647", 2000);
            await ExecuteAdbAsync("shell svc power stayon true", 2000);
        }

        public static async Task RestoreKeepAwakeAsync(int timeout = 0)
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

            if (!string.IsNullOrEmpty(pin) && Regex.IsMatch(pin, @"^\d{1,32}$"))
            {
                await Task.Delay(200);
                await ExecuteAdbAsync("shell input text " + pin, 2000);
                await Task.Delay(100);
                await ExecuteAdbAsync("shell input keyevent 66", 2000);
            }
        }

        public static async Task<Process> LaunchScrcpyForAppAsync(string package, string title, bool useUhid, bool forwardClicks, string displaySize, bool audio = true, bool autoTaskbar = false)
        {
            AppLaunchProfile profile = new AppLaunchProfile { displaySize = displaySize };
            if (useUhid) profile.keyboardMode = "uhid";
            profile.forwardAllClicks = forwardClicks;
            return await LaunchScrcpyForAppAsync(package, title, profile, audio, autoTaskbar).ConfigureAwait(false);
        }

        public static async Task<Process> LaunchScrcpyForAppAsync(string package, string title, AppLaunchProfile profile, bool audio = true, bool autoTaskbar = false)
        {
            if (!IsValidPackageName(package)) return null;

            await EnableKeepAwakeAsync();
            await PerformUnlockDeviceAsync(Environment.GetEnvironmentVariable("SCRCPY_ADB_PIN"));

            profile = profile ?? new AppLaunchProfile();
            string winTitle = !string.IsNullOrEmpty(title) ? title : package;
            if (winTitle.Length > 128) winTitle = winTitle.Substring(0, 128);
            string disp = IsValidDisplaySize(profile.displaySize) ? profile.displaySize : "2560x1440/160";

            List<string> argsList = new List<string>
            {
                string.Format("--new-display={0}", disp),
                string.Format("--start-app={0}", package),
                "--window-title=" + QuoteWindowsArgument(winTitle),
                "-w"
            };

            bool hideTaskbar = profile.taskbarMode == "hidden" || (profile.taskbarMode != "shown" && !autoTaskbar);
            if (hideTaskbar)
            {
                argsList.Add("--no-vd-system-decorations");
            }

            int fps = Math.Max(15, Math.Min(240, profile.maxFps > 0 ? profile.maxFps : 60));
            argsList.Add("--max-fps=" + fps);
            if (Regex.IsMatch(profile.videoBitRate ?? string.Empty, @"^\d{1,3}[KM]$", RegexOptions.IgnoreCase))
                argsList.Add("--video-bit-rate=" + profile.videoBitRate.ToUpperInvariant());
            if (Regex.IsMatch(profile.videoCodec ?? string.Empty, @"^(h264|h265|av1|vp8|vp9)$", RegexOptions.IgnoreCase))
                argsList.Add("--video-codec=" + profile.videoCodec.ToLowerInvariant());
            if (Regex.IsMatch(profile.orientation ?? string.Empty, @"^(0|90|180|270)$"))
                argsList.Add("--orientation=" + profile.orientation);
            if (Regex.IsMatch(profile.keyboardMode ?? string.Empty, @"^(sdk|uhid|disabled)$"))
                argsList.Add("--keyboard=" + profile.keyboardMode);
            if (Regex.IsMatch(profile.mouseMode ?? string.Empty, @"^(sdk|uhid|disabled)$"))
                argsList.Add("--mouse=" + profile.mouseMode);
            if (profile.alwaysOnTop) argsList.Add("--always-on-top");
            if (profile.borderless) argsList.Add("--window-borderless");
            if (profile.fullscreen) argsList.Add("--fullscreen");
            if (profile.turnScreenOff) argsList.Add("--turn-screen-off");
            if (profile.recordSession)
            {
                string videos = Environment.GetFolderPath(Environment.SpecialFolder.MyVideos);
                string recordDir = Path.Combine(videos, "scrcpy-manager");
                Directory.CreateDirectory(recordDir);
                string safeName = Regex.Replace(winTitle, @"[^A-Za-z0-9._-]+", "_").Trim('_');
                if (safeName.Length == 0) safeName = "Android";
                string recordPath = Path.Combine(recordDir, safeName + "_" + DateTime.Now.ToString("yyyyMMdd_HHmmss_fff") + ".mp4");
                argsList.Add("--record=" + QuoteWindowsArgument(recordPath));
            }

            string serial;
            lock (_serialLock) serial = _selectedSerial;
            if (!string.IsNullOrEmpty(serial)) argsList.Insert(0, "--serial=" + QuoteWindowsArgument(serial));

            bool effectiveAudio = profile.audioMode == "on" || (profile.audioMode != "off" && audio);
            if (!effectiveAudio) argsList.Add("--no-audio");

            bool isRdc = string.Equals(package, "com.microsoft.rdc.androidx", StringComparison.OrdinalIgnoreCase);
            if (isRdc)
            {
                argsList.Add("--mouse-bind=++++");
            }
            else
            {
                argsList.Add("-x");
                if (profile.forwardAllClicks)
                {
                    argsList.Add("--mouse-bind=++++");
                }
            }

            string scrcpyArgs = string.Join(" ", argsList);
            return StartScrcpyProcess(scrcpyArgs, package);
        }

        public static Process StartScrcpyProcess(string args, string package = null)
        {
            try
            {
                if ((args ?? string.Empty).IndexOf("--serial", StringComparison.OrdinalIgnoreCase) < 0)
                {
                    string serial;
                    lock (_serialLock) serial = _selectedSerial;
                    if (!string.IsNullOrEmpty(serial)) args = "--serial=" + QuoteWindowsArgument(serial) + " " + args;
                }
                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = (!string.IsNullOrEmpty(RuntimeDirectory) ? Path.Combine(RuntimeDirectory, "scrcpy.exe") : "scrcpy.exe"),
                    Arguments = args,
                    UseShellExecute = false,
                    CreateNoWindow = false
                };

                if (!string.IsNullOrEmpty(package))
                {
                    string iconDir = AppWindowIconManager.PrepareAppIconDirectory(package);
                    if (!string.IsNullOrEmpty(iconDir))
                    {
                        psi.EnvironmentVariables["SCRCPY_ICON_DIR"] = iconDir;
                    }
                }

                Process proc = Process.Start(psi);
                if (proc != null)
                {
                    if (!string.IsNullOrEmpty(package))
                    {
                        AppWindowIconManager.RegisterProcess(proc, package);
                    }
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
