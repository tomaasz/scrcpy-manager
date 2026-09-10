using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ScrcpyManager
{
    public class IconService
    {
        private readonly string _cacheDir;
        private readonly string _repoIconsDir;
        private readonly Dictionary<string, Bitmap> _cachedBitmaps = new Dictionary<string, Bitmap>(StringComparer.OrdinalIgnoreCase);
        private readonly object _lock = new object();

        public IconService(string repoRoot = null)
        {
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

        public Bitmap GetResizedIcon(string package, int size = 18, int gap = 5)
        {
            if (string.IsNullOrWhiteSpace(package))
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
            if (string.IsNullOrWhiteSpace(package))
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
                    if (string.Equals(package, "com.android.settings", StringComparison.OrdinalIgnoreCase))
                    {
                        await adb.RunAdbAsync("shell \"unzip -p /system/framework/framework-res.apk res/drawable-xxhdpi-v4/ic_settings.png > /data/local/tmp/scrcpy_icon.png 2>/dev/null\"");
                        await adb.RunAdbAsync(string.Format("pull /data/local/tmp/scrcpy_icon.png \"{0}\"", cachedFile));
                        if (File.Exists(cachedFile) && new FileInfo(cachedFile).Length > 100)
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

                    if (!string.IsNullOrEmpty(apk))
                    {
                        string listOut = await adb.RunAdbAsync(string.Format("shell \"unzip -l {0} 2>/dev/null\"", apk));
                        if (!string.IsNullOrWhiteSpace(listOut))
                        {
                            MatchCollection matches = Regex.Matches(listOut, @"res/(?:mipmap|drawable)[^/\s]+/(?:ic_launcher|icon|app_icon)[^/\s]*\.png", RegexOptions.IgnoreCase);
                            if (matches.Count > 0)
                            {
                                string lastEntry = matches[matches.Count - 1].Value;
                                await adb.RunAdbAsync(string.Format("shell \"unzip -p {0} {1} > /data/local/tmp/scrcpy_icon.png 2>/dev/null\"", apk, lastEntry));
                                await adb.RunAdbAsync(string.Format("pull /data/local/tmp/scrcpy_icon.png \"{0}\"", cachedFile));
                                if (File.Exists(cachedFile) && new FileInfo(cachedFile).Length > 100)
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
                            await wc.DownloadFileTaskAsync(new Uri(imgUrl), cachedFile);
                            if (File.Exists(cachedFile) && new FileInfo(cachedFile).Length > 100)
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

        public void StartAsyncIconDownload(IEnumerable<string> packages, AdbService adb, Action<string> onIconFetched)
        {
            Task.Run(async () =>
            {
                foreach (string pkg in packages)
                {
                    if (string.IsNullOrWhiteSpace(pkg)) continue;
                    string cachedFile = Path.Combine(_cacheDir, pkg + ".png");
                    if (File.Exists(cachedFile) && new FileInfo(cachedFile).Length > 100)
                    {
                        continue;
                    }

                    bool success = await FetchSingleAppIconAsync(pkg, adb);
                    if (success && onIconFetched != null)
                    {
                        onIconFetched(pkg);
                    }
                }
            });
        }
    }
}

