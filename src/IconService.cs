using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace ScrcpyManager
{
    public class IconService : IDisposable
    {
        private readonly string _cacheDir;
        private readonly string _repoIconsDir;
        private readonly Dictionary<string, Bitmap> _cachedBitmaps = new Dictionary<string, Bitmap>(StringComparer.OrdinalIgnoreCase);
        private readonly object _lock = new object();
        private readonly HashSet<string> _downloadsInProgress = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        // Zamiast trwałej "czarnej listy" na cały czas życia procesu, pamiętamy tylko kiedy
        // ostatnio się nie udało - po krótkim czasie próba pobrania ikony jest powtarzana
        // (np. gdy telefon był chwilowo niedostępny albo padło zapytanie do Play Store).
        private readonly Dictionary<string, DateTime> _recentFailures = new Dictionary<string, DateTime>(StringComparer.OrdinalIgnoreCase);
        private static readonly TimeSpan FailureRetryCooldown = TimeSpan.FromMinutes(2);
        private readonly CancellationTokenSource _disposeToken = new CancellationTokenSource();
        private bool _disposed;

        public static IconService Instance { get; private set; }

        public IconService(string repoRoot = null)
        {
            Instance = this;
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            _cacheDir = Path.Combine(appData, "scrcpy-manager", "icons");
            try
            {
                if (!Directory.Exists(_cacheDir))
                {
                    Directory.CreateDirectory(_cacheDir);
                }
            }
            catch {}

            if (!string.IsNullOrEmpty(repoRoot))
            {
                _repoIconsDir = Path.Combine(repoRoot, "icons");
            }
            else
            {
                _repoIconsDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "icons");
            }
        }

        public string GetIconFilePath(string package)
        {
            if (string.IsNullOrEmpty(package) || !AdbService.IsValidPackageName(package))
            {
                return null;
            }

            string cachedFile = Path.Combine(_cacheDir, package + ".png");
            if (!File.Exists(cachedFile) && !string.IsNullOrEmpty(_repoIconsDir))
            {
                string repoFile = Path.Combine(_repoIconsDir, package + ".png");
                if (File.Exists(repoFile))
                {
                    try
                    {
                        File.Copy(repoFile, cachedFile, true);
                    }
                    catch { }
                }
            }

            return File.Exists(cachedFile) ? cachedFile : null;
        }

        public Bitmap GetRawIconBitmap(string package)
        {
            string file = GetIconFilePath(package);
            if (string.IsNullOrEmpty(file) || !File.Exists(file)) return null;

            try
            {
                byte[] bytes = File.ReadAllBytes(file);
                using (MemoryStream ms = new MemoryStream(bytes))
                using (Image img = Image.FromStream(ms))
                {
                    return new Bitmap(img);
                }
            }
            catch
            {
                return null;
            }
        }

        public Bitmap GetResizedIcon(string package, int size = 18, int gap = 5)
        {
            if (_disposed || !AdbService.IsValidPackageName(package))
            {
                return null;
            }

            string key = string.Format("{0}_{1}_{2}", package, size, gap);
            lock (_lock)
            {
                Bitmap bmp;
                if (_cachedBitmaps.TryGetValue(key, out bmp) && bmp != null)
                {
                    return bmp;
                }
            }

            string cachedFile = Path.Combine(_cacheDir, package + ".png");
            if (!File.Exists(cachedFile) && !string.IsNullOrEmpty(_repoIconsDir))
            {
                string repoFile = Path.Combine(_repoIconsDir, package + ".png");
                if (File.Exists(repoFile))
                {
                    try
                    {
                        File.Copy(repoFile, cachedFile, true);
                    }
                    catch {}
                }
            }

            if (File.Exists(cachedFile))
            {
                try
                {
                    byte[] bytes = File.ReadAllBytes(cachedFile);
                    using (MemoryStream ms = new MemoryStream(bytes))
                    using (Image src = Image.FromStream(ms))
                    {
                        int totalW = size + gap;
                        Bitmap dest = new Bitmap(totalW, size, PixelFormat.Format32bppArgb);
                        using (Graphics g = Graphics.FromImage(dest))
                        {
                            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                            g.SmoothingMode = SmoothingMode.HighQuality;
                            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                            g.CompositingQuality = CompositingQuality.HighQuality;
                            g.DrawImage(src, 0, 0, size, size);
                        }

                        lock (_lock)
                        {
                            if (_disposed)
                            {
                                dest.Dispose();
                                return null;
                            }
                            Bitmap existing;
                            if (_cachedBitmaps.TryGetValue(key, out existing) && existing != null)
                            {
                                dest.Dispose();
                                return existing;
                            }
                            _cachedBitmaps[key] = dest;
                        }
                        return dest;
                    }
                }
                catch {}
            }

            return null;
        }

        public async Task<bool> FetchSingleAppIconAsync(string package, AdbService adb)
        {
            if (_disposed || !AdbService.IsValidPackageName(package))
            {
                return false;
            }

            string cachedFile = Path.Combine(_cacheDir, package + ".png");
            if (File.Exists(cachedFile) && new FileInfo(cachedFile).Length > 100)
            {
                return true;
            }

            // Metoda 1: Bezposrednio z telefonu przez ADB
            if (adb != null)
            {
                try
                {
                    string remoteIcon = "/data/local/tmp/scrcpy_icon_" + package.Replace('.', '_') + ".png";
                    string localTemp = cachedFile + "." + Guid.NewGuid().ToString("N") + ".tmp";
                    if (string.Equals(package, "com.android.settings", StringComparison.OrdinalIgnoreCase))
                    {
                        await adb.RunAdbAsync("shell \"unzip -p /system/framework/framework-res.apk res/drawable-xxhdpi-v4/ic_settings.png > " + remoteIcon + " 2>/dev/null\"");
                        await adb.RunAdbAsync("pull " + remoteIcon + " " + AdbService.QuoteWindowsArgument(localTemp));
                        if (CommitDownloadedIcon(localTemp, cachedFile))
                        {
                            return true;
                        }
                    }

                    string pathOut = await adb.RunAdbAsync(string.Format("shell pm path {0}", package));
                    string apk = null;
                    if (!string.IsNullOrWhiteSpace(pathOut))
                    {
                        foreach (string line in pathOut.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
                        {
                            string trimmed = line.Trim();
                            if (trimmed.StartsWith("package:", StringComparison.OrdinalIgnoreCase))
                            {
                                apk = trimmed.Substring("package:".Length).Trim();
                                break;
                            }
                        }
                    }

                    if (IsSafeAndroidPath(apk))
                    {
                        string listOut = await adb.RunAdbAsync(string.Format("shell \"unzip -l {0} 2>/dev/null\"", apk));
                        if (!string.IsNullOrWhiteSpace(listOut))
                        {
                            MatchCollection matches = Regex.Matches(listOut, @"res/(?:mipmap|drawable)[^/\s]+/(?:ic_launcher|icon|app_icon)[^/\s]*\.png", RegexOptions.IgnoreCase);
                            if (matches.Count > 0)
                            {
                                string lastEntry = matches[matches.Count - 1].Value;
                                if (!Regex.IsMatch(lastEntry, @"^[A-Za-z0-9_./+@-]+$")) return false;
                                await adb.RunAdbAsync(string.Format("shell \"unzip -p {0} {1} > {2} 2>/dev/null\"", apk, lastEntry, remoteIcon));
                                await adb.RunAdbAsync("pull " + remoteIcon + " " + AdbService.QuoteWindowsArgument(localTemp));
                                if (CommitDownloadedIcon(localTemp, cachedFile))
                                {
                                    return true;
                                }
                            }
                        }
                    }
                }
                catch {}
            }

            // Metoda 2: Google Play Store fallback dla ikon wektorowych/adaptacyjnych
            try
            {
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                HttpWebRequest req = (HttpWebRequest)WebRequest.Create("https://play.google.com/store/apps/details?id=" + Uri.EscapeDataString(package));
                req.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64)";
                req.Timeout = 3000;

                using (WebResponse resp = await req.GetResponseAsync())
                using (Stream stream = resp.GetResponseStream())
                using (StreamReader sr = new StreamReader(stream))
                {
                    string html = await sr.ReadToEndAsync();
                    Match m = Regex.Match(html, @"(https://play-lh\.googleusercontent\.com/[^""'>\s=]+)");
                    if (m.Success)
                    {
                        string imgUrl = m.Groups[1].Value + "=s64";
                        using (WebClient wc = new WebClient())
                        {
                            wc.Headers.Add("User-Agent", "Mozilla/5.0");
                            string localTemp = cachedFile + "." + Guid.NewGuid().ToString("N") + ".tmp";
                            await wc.DownloadFileTaskAsync(new Uri(imgUrl), localTemp);
                            if (CommitDownloadedIcon(localTemp, cachedFile))
                            {
                                return true;
                            }
                        }
                    }
                }
            }
            catch {}

            return false;
        }

        private static bool IsSafeAndroidPath(string path)
        {
            return !string.IsNullOrEmpty(path) &&
                   Regex.IsMatch(path, @"^/[A-Za-z0-9_./=+@:-]+$") &&
                   path.IndexOf("..", StringComparison.Ordinal) < 0;
        }

        public void StartAsyncIconDownload(IEnumerable<string> packages, AdbService adb, Action<string> onIconFetched)
        {
            if (packages == null) return;
            List<string> work = new List<string>();
            CancellationToken cancellationToken = _disposeToken.Token;
            lock (_lock)
            {
                if (_disposed) return;
                foreach (string pkg in packages)
                {
                    if (!AdbService.IsValidPackageName(pkg) || !_downloadsInProgress.Add(pkg)) continue;
                    DateTime lastFailure;
                    if (_recentFailures.TryGetValue(pkg, out lastFailure) && DateTime.UtcNow - lastFailure < FailureRetryCooldown)
                    {
                        _downloadsInProgress.Remove(pkg);
                        continue;
                    }
                    work.Add(pkg);
                }
            }

            Task.Run(async () =>
            {
                foreach (string pkg in work)
                {
                    if (cancellationToken.IsCancellationRequested) break;
                    bool success = false;
                    try
                    {
                        string cachedFile = Path.Combine(_cacheDir, pkg + ".png");
                        success = File.Exists(cachedFile) && new FileInfo(cachedFile).Length > 100;
                        if (!success) success = await FetchSingleAppIconAsync(pkg, adb);
                        if (success && onIconFetched != null && !cancellationToken.IsCancellationRequested) onIconFetched(pkg);
                    }
                    catch { }
                    finally
                    {
                        lock (_lock)
                        {
                            _downloadsInProgress.Remove(pkg);
                            if (!success) _recentFailures[pkg] = DateTime.UtcNow;
                            else _recentFailures.Remove(pkg);
                        }
                    }
                }
            });
        }

        private static bool CommitDownloadedIcon(string temp, string destination)
        {
            try
            {
                if (!File.Exists(temp) || new FileInfo(temp).Length <= 100) return false;
                if (File.Exists(destination)) File.Replace(temp, destination, null, true);
                else File.Move(temp, destination);
                return true;
            }
            catch { return false; }
            finally { try { if (File.Exists(temp)) File.Delete(temp); } catch { } }
        }

        public void Dispose()
        {
            lock (_lock)
            {
                if (_disposed) return;
                _disposed = true;
                _disposeToken.Cancel();
                foreach (Bitmap bitmap in _cachedBitmaps.Values)
                {
                    try { bitmap.Dispose(); } catch { }
                }
                _cachedBitmaps.Clear();
            }
            _disposeToken.Dispose();
        }
    }
}
