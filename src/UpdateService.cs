using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Windows.Forms;

namespace ScrcpyManager
{
    public class GitHubAsset
    {
        public string name { get; set; }
        public string browser_download_url { get; set; }
    }

    public class GitHubRelease
    {
        public string tag_name { get; set; }
        public string html_url { get; set; }
        public string body { get; set; }
        public List<GitHubAsset> assets { get; set; }
    }

    public class UpdateService
    {
        public const string CurrentVersion = "1.1.1";
        public GitHubRelease LatestRelease { get; private set; }

        public async Task<bool> CheckForUpdateAsync()
        {
            try
            {
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                HttpWebRequest req = (HttpWebRequest)WebRequest.Create("https://api.github.com/repos/tomaasz/scrcpy-manager/releases/latest");
                req.UserAgent = "scrcpy-manager-desktop";
                req.Timeout = 6000;

                using (WebResponse resp = await req.GetResponseAsync())
                using (Stream stream = resp.GetResponseStream())
                using (StreamReader sr = new StreamReader(stream))
                {
                    string json = await sr.ReadToEndAsync();
                    JavaScriptSerializer serializer = new JavaScriptSerializer();
                    GitHubRelease release = serializer.Deserialize<GitHubRelease>(json);

                    if (release != null && !string.IsNullOrEmpty(release.tag_name))
                    {
                        string latestClean = Regex.Replace(Regex.Replace(release.tag_name, @"^v", ""), @"-.*$", "").Trim();
                        string curClean = Regex.Replace(Regex.Replace(CurrentVersion, @"^v", ""), @"-.*$", "").Trim();

                        Version vLatest, vCur;
                        if (Version.TryParse(latestClean, out vLatest) && Version.TryParse(curClean, out vCur))
                        {
                            if (vLatest > vCur)
                            {
                                LatestRelease = release;
                                return true;
                            }
                        }
                    }
                }
            }
            catch {}

            return false;
        }

        public void ShowUpdateDialog(IWin32Window parent, ThemeColors c, Localization.Strings t, Icon appIcon)
        {
            if (LatestRelease == null) return;

            using (Form dlg = new Form())
            {
                dlg.Text = t.UpdateDialogTitle;
                dlg.Size = new Size(460, 420);
                dlg.StartPosition = FormStartPosition.CenterParent;
                dlg.FormBorderStyle = FormBorderStyle.FixedDialog;
                dlg.MaximizeBox = false;
                dlg.MinimizeBox = false;
                dlg.BackColor = c.Bg;
                dlg.ForeColor = c.Text;
                if (appIcon != null) dlg.Icon = appIcon;

                Label lblTitle = new Label
                {
                    Text = t.UpdateDialogHeader,
                    Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                    Location = new Point(20, 16),
                    Size = new Size(400, 26),
                    ForeColor = c.Text
                };
                dlg.Controls.Add(lblTitle);

                string cur = string.Format(t.UpdateDialogCurrent, "v" + CurrentVersion);
                string lat = string.Format(t.UpdateDialogLatest, LatestRelease.tag_name);
                Label lblVersions = new Label
                {
                    Text = string.Format("{0}  ➜  {1}", cur, lat),
                    Font = new Font("Segoe UI", 9f, FontStyle.Regular),
                    Location = new Point(20, 46),
                    Size = new Size(400, 20),
                    ForeColor = c.TextMuted
                };
                dlg.Controls.Add(lblVersions);

                Label lblNotesTitle = new Label
                {
                    Text = t.UpdateDialogNotes,
                    Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                    Location = new Point(20, 74),
                    Size = new Size(400, 20),
                    ForeColor = c.Text
                };
                dlg.Controls.Add(lblNotesTitle);

                TextBox txtNotes = new TextBox
                {
                    Multiline = true,
                    ReadOnly = true,
                    ScrollBars = ScrollBars.Vertical,
                    Location = new Point(20, 98),
                    Size = new Size(404, 210),
                    BackColor = c.Card,
                    ForeColor = c.Text,
                    BorderStyle = BorderStyle.FixedSingle,
                    Font = new Font("Segoe UI", 9f, FontStyle.Regular),
                    Text = !string.IsNullOrEmpty(LatestRelease.body) ? LatestRelease.body : ("scrcpy Manager " + LatestRelease.tag_name)
                };
                dlg.Controls.Add(txtNotes);

                Panel pnlButtons = new Panel
                {
                    Location = new Point(20, 320),
                    Size = new Size(404, 46)
                };
                dlg.Controls.Add(pnlButtons);

                Button btnDownload = new Button
                {
                    Text = t.UpdateBtnDownload,
                    Location = new Point(0, 8),
                    Size = new Size(125, 30),
                    Font = new Font("Segoe UI", 8.2f, FontStyle.Regular),
                    FlatStyle = FlatStyle.Flat,
                    BackColor = c.BtnTool,
                    ForeColor = c.BtnToolText,
                    Cursor = Cursors.Hand
                };
                btnDownload.FlatAppearance.BorderColor = c.BtnToolBorder;
                btnDownload.FlatAppearance.BorderSize = 1;
                btnDownload.Click += (s, e) =>
                {
                    string url = !string.IsNullOrEmpty(LatestRelease.html_url) ? LatestRelease.html_url : "https://github.com/tomaasz/scrcpy-manager/releases/latest";
                    try { Process.Start(url); } catch {}
                    dlg.Close();
                };
                pnlButtons.Controls.Add(btnDownload);

                Button btnLater = new Button
                {
                    Text = t.UpdateBtnLater,
                    Location = new Point(135, 8),
                    Size = new Size(80, 30),
                    Font = new Font("Segoe UI", 8.2f, FontStyle.Regular),
                    FlatStyle = FlatStyle.Flat,
                    BackColor = c.BtnMode,
                    ForeColor = c.BtnModeText,
                    Cursor = Cursors.Hand
                };
                btnLater.FlatAppearance.BorderColor = c.BtnModeBorder;
                btnLater.FlatAppearance.BorderSize = 1;
                btnLater.Click += (s, e) => dlg.Close();
                pnlButtons.Controls.Add(btnLater);

                Button btnInstall = new Button
                {
                    Text = t.UpdateBtnInstall,
                    Location = new Point(230, 8),
                    Size = new Size(174, 30),
                    Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                    FlatStyle = FlatStyle.Flat,
                    BackColor = c.BtnHero,
                    ForeColor = c.BtnHeroText,
                    Cursor = Cursors.Hand
                };
                btnInstall.FlatAppearance.BorderSize = 0;
                btnInstall.Click += async (s, e) =>
                {
                    btnInstall.Enabled = false;
                    btnLater.Enabled = false;
                    btnInstall.Text = t.UpdateDownloading;
                    dlg.Update();

                    bool success = await Task.Run(() => PerformAutoUpdate(LatestRelease));
                    if (!success)
                    {
                        btnInstall.Enabled = true;
                        btnLater.Enabled = true;
                        btnInstall.Text = t.UpdateBtnInstall;
                        string msg = string.Format(t.UpdateFailed, "Download failed.");
                        if (MessageBox.Show(dlg, msg, t.UpdateDialogTitle, MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
                        {
                            string url = !string.IsNullOrEmpty(LatestRelease.html_url) ? LatestRelease.html_url : "https://github.com/tomaasz/scrcpy-manager/releases/latest";
                            try { Process.Start(url); } catch {}
                        }
                    }
                };
                pnlButtons.Controls.Add(btnInstall);

                dlg.ShowDialog(parent);
            }
        }

        private bool PerformAutoUpdate(GitHubRelease release)
        {
            if (release == null || release.assets == null || release.assets.Count == 0) return false;

            GitHubAsset targetAsset = null;
            foreach (GitHubAsset a in release.assets)
            {
                if (a.name != null && a.name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                {
                    targetAsset = a;
                    if (a.name.IndexOf("Portable", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        break;
                    }
                }
            }

            if (targetAsset == null || string.IsNullOrEmpty(targetAsset.browser_download_url))
            {
                return false;
            }

            string currentExe = Process.GetCurrentProcess().MainModule.FileName;
            string tempFile = Path.Combine(Path.GetTempPath(), "ScrcpyManager_update_" + Guid.NewGuid().ToString("N") + ".exe");

            try
            {
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                using (WebClient wc = new WebClient())
                {
                    wc.Headers.Add("User-Agent", "scrcpy-manager-desktop");
                    wc.DownloadFile(targetAsset.browser_download_url, tempFile);
                }

                if (!File.Exists(tempFile) || new FileInfo(tempFile).Length < 100000)
                {
                    return false;
                }

                string script = string.Format(@"
Start-Sleep -Milliseconds 800
$count = 0
while ($count -lt 30) {{
    try {{
        Move-Item -LiteralPath '{0}' -Destination '{1}' -Force -ErrorAction Stop
        break
    }} catch {{
        Start-Sleep -Milliseconds 500
        $count++
    }}
}}
Start-Process -FilePath '{1}'
", tempFile.Replace("'", "''"), currentExe.Replace("'", "''"));

                byte[] bytes = Encoding.Unicode.GetBytes(script);
                string b64 = Convert.ToBase64String(bytes);

                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = "-NoProfile -WindowStyle Hidden -EncodedCommand " + b64,
                    CreateNoWindow = true,
                    UseShellExecute = false
                };
                Process.Start(psi);

                Application.Exit();
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}

