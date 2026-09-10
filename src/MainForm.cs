using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Windows.Forms;

namespace ScrcpyManager
{
    public class MainForm : Form
    {
        // Serwisy
        private readonly AdbService _adb;
        private readonly IconService _icons;
        private readonly UpdateService _updates;

        // Ścieżki konfiguracyjne
        private readonly string _userConfigRoot;
        private readonly string _userPrefFile;
        private readonly string _defaultPrefFile;
        private readonly string _appsConfigFile;
        private readonly string _defaultAppsFile;
        private readonly string _stateFile;

        // Stan aplikacji
        private UserPreferences _pref;
        private bool _isDarkMode = true;
        private string _currentLang = "PL";
        private int _appsLayout = 2; // 1, 2, 3
        private List<AppEntry> _appButtons = new List<AppEntry>();
        private DeviceInfo _currentDevice = new DeviceInfo();
        private readonly List<Process> _launchedProcesses = new List<Process>();
        private int _originalTimeout = 30000;
        private string _captureScreenshotLang = null;

        // Czcionki
        private readonly Font _fontRegular = new Font("Segoe UI", 9f, FontStyle.Regular);
        private readonly Font _fontBold = new Font("Segoe UI", 9f, FontStyle.Bold);
        private readonly Font _fontHero = new Font("Segoe UI", 10.5f, FontStyle.Bold);
        private readonly Font _fontTitle = new Font("Segoe UI", 9.5f, FontStyle.Bold);
        private readonly Font _fontSection = new Font("Segoe UI", 8.2f, FontStyle.Bold);
        private readonly Font _fontSmall = new Font("Segoe UI", 8.2f, FontStyle.Regular);
        private readonly Font _fontDot = new Font("Segoe UI", 11f, FontStyle.Bold);

        // Wartości rozdzielczości RDP
        private readonly string[] _resValues = new[]
        {
            "1920x1080/160",
            "2560x1440/160",
            "2560x1440/140",
            "3840x2160/240",
            "AUTO"
        };

        // Kontrolki UI
        private Panel _pnlStatus;
        private Label _lblStatusDot;
        private Label _lblDeviceTitle;
        private Label _lblStatusDetail;
        private Button _btnTheme;
        private Button _btnLang;
        private ContextMenuStrip _ctxLang;
        private Button _btnUpdateBadge;

        private Button _btnScrcpy;
        private CheckBox _chkFullScreen;
        private CheckBox _chkNavBar;

        private Label _lblSectionOptions;
        private Label _lblRes;
        private ComboBox _cmbRes;
        private CheckBox _chkAudio;
        private CheckBox _chkAutoTaskbar;
        private Button _btnWifi;
        private Button _btnKeyFix;
        private Button _btnClipFix;

        private Label _lblSectionApps;
        private Label _lblAppsSubtitle;
        private Button _btnLayout1;
        private Button _btnLayout2;
        private Button _btnLayout3;
        private Button _btnEditApps;
        private FlowLayoutPanel _flowAppButtons;

        private Label _lblCustom;
        private TextBox _txtCustom;
        private ListBox _lstCustomSuggestions;
        private Button _btnCustom;
        private Button _btnAddCustom;

        private Label _lblSectionDevice;
        private Button _btnDesktop;
        private Button _btnNormal;
        private Button _btnReboot;
        private Button _btnNavBack;
        private Button _btnNavHome;
        private Button _btnNavRecents;

        private ToolTip _tipMain;
        private Timer _keepAwakeTimer;
        private Timer _statusTimer;

        private readonly List<Button> _createdAppButtons = new List<Button>();
        private readonly List<Button> _createdRenameButtons = new List<Button>();
        private readonly List<Button> _createdDelButtons = new List<Button>();

        public MainForm(string[] args)
        {
            // Opcjonalne argumenty wiersza poleceń (np. -CaptureScreenshotLang PL)
            if (args != null)
            {
                for (int i = 0; i < args.Length; i++)
                {
                    if ((args[i].Equals("-CaptureScreenshotLang", StringComparison.OrdinalIgnoreCase) ||
                         args[i].Equals("--screenshot", StringComparison.OrdinalIgnoreCase)) && i + 1 < args.Length)
                    {
                        _captureScreenshotLang = args[i + 1].ToUpperInvariant();
                    }
                }
            }

            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            _userConfigRoot = Path.Combine(appData, "scrcpy-manager");
            _userPrefFile = Path.Combine(_userConfigRoot, "preferences.json");
            _defaultPrefFile = Path.Combine(baseDir, "preferences.json");
            _appsConfigFile = Path.Combine(_userConfigRoot, "apps.json");
            _defaultAppsFile = Path.Combine(baseDir, "apps.json");
            _stateFile = Path.Combine(Path.GetTempPath(), "scrcpy_manager_original_timeout.txt");

            _adb = new AdbService();
            _icons = new IconService(baseDir);
            _updates = new UpdateService();

            LoadPreferences();
            LoadApps();

            InitializeMainWindow();
            BuildControls();
            ApplyLanguage();
            ApplyTheme();
            UpdateAppButtonGrid();

            // Ustawienie ikony aplikacji, jeśli dostępna
            try
            {
                string icoPath = Path.Combine(baseDir, "app.ico");
                if (File.Exists(icoPath))
                {
                    Icon = new Icon(icoPath);
                }
            }
            catch {}

            Shown += OnFormShown;
            FormClosing += OnFormClosing;
        }

        private void InitializeMainWindow()
        {
            Text = "scrcpy Manager";
            ClientSize = new Size(404, 706);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            StartPosition = FormStartPosition.CenterScreen;
            Font = _fontRegular;
            _tipMain = new ToolTip();
        }

        private void LoadPreferences()
        {
            _pref = new UserPreferences();
            string prefToLoad = File.Exists(_userPrefFile) ? _userPrefFile : (File.Exists(_defaultPrefFile) ? _defaultPrefFile : null);
            if (prefToLoad != null)
            {
                try
                {
                    string json = File.ReadAllText(prefToLoad);
                    JavaScriptSerializer js = new JavaScriptSerializer();
                    UserPreferences loaded = js.Deserialize<UserPreferences>(json);
                    if (loaded != null)
                    {
                        _pref = loaded;
                    }
                }
                catch {}
            }

            _isDarkMode = !string.Equals(_pref.theme, "light", StringComparison.OrdinalIgnoreCase);
            if (!string.IsNullOrEmpty(_pref.lang) && Localization.SupportedLanguages.ContainsKey(_pref.lang.ToUpperInvariant()))
            {
                _currentLang = _pref.lang.ToUpperInvariant();
            }
            if (_pref.layout >= 1 && _pref.layout <= 3)
            {
                _appsLayout = _pref.layout;
            }

            if (!string.IsNullOrEmpty(_captureScreenshotLang) && Localization.SupportedLanguages.ContainsKey(_captureScreenshotLang))
            {
                _currentLang = _captureScreenshotLang;
            }
        }

        private void SavePreferences()
        {
            try
            {
                _pref.theme = _isDarkMode ? "dark" : "light";
                _pref.lang = _currentLang;
                _pref.layout = _appsLayout;

                JavaScriptSerializer js = new JavaScriptSerializer();
                string json = js.Serialize(_pref);

                if (!Directory.Exists(_userConfigRoot)) Directory.CreateDirectory(_userConfigRoot);
                File.WriteAllText(_userPrefFile, json);
                if (Directory.Exists(AppDomain.CurrentDomain.BaseDirectory))
                {
                    try { File.WriteAllText(_defaultPrefFile, json); } catch {}
                }
            }
            catch {}
        }

        private void LoadApps()
        {
            _appButtons = new List<AppEntry>();
            string appsFileToLoad = File.Exists(_appsConfigFile) ? _appsConfigFile : (File.Exists(_defaultAppsFile) ? _defaultAppsFile : null);
            if (appsFileToLoad != null)
            {
                try
                {
                    string json = File.ReadAllText(appsFileToLoad);
                    JavaScriptSerializer js = new JavaScriptSerializer();
                    List<AppEntry> loaded = js.Deserialize<List<AppEntry>>(json);
                    if (loaded != null && loaded.Count > 0)
                    {
                        _appButtons.AddRange(loaded);
                    }
                }
                catch {}
            }

            if (_appButtons.Count == 0)
            {
                _appButtons.AddRange(AppsEditorForm.DefaultPopularApps);
            }
        }

        private void BuildControls()
        {
            // 1. Karta stanu urządzenia (Status Card)
            _pnlStatus = new Panel
            {
                Location = new Point(16, 12),
                Size = new Size(372, 60),
                BorderStyle = BorderStyle.FixedSingle
            };
            Controls.Add(_pnlStatus);

            _lblStatusDot = new Label
            {
                Text = "●",
                Font = _fontDot,
                Location = new Point(10, 8),
                Size = new Size(18, 20),
                BackColor = Color.Transparent
            };
            _pnlStatus.Controls.Add(_lblStatusDot);

            _lblDeviceTitle = new Label
            {
                Text = "Wyszukiwanie urządzenia...",
                Font = _fontTitle,
                Location = new Point(28, 9),
                Size = new Size(225, 20),
                BackColor = Color.Transparent
            };
            _pnlStatus.Controls.Add(_lblDeviceTitle);

            _lblStatusDetail = new Label
            {
                Text = "Inicjalizacja...",
                Font = _fontSmall,
                Location = new Point(28, 32),
                Size = new Size(225, 20),
                BackColor = Color.Transparent
            };
            _pnlStatus.Controls.Add(_lblStatusDetail);

            _btnTheme = new Button
            {
                Location = new Point(260, 8),
                Size = new Size(62, 24),
                Font = _fontSmall,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            _btnTheme.FlatAppearance.BorderSize = 1;
            _btnTheme.Click += (s, e) =>
            {
                _isDarkMode = !_isDarkMode;
                SavePreferences();
                ApplyTheme();
                ApplyLanguage();
                UpdateAppButtonGrid();
            };
            _pnlStatus.Controls.Add(_btnTheme);

            _btnLang = new Button
            {
                Location = new Point(326, 8),
                Size = new Size(38, 24),
                Font = _fontSmall,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            _btnLang.FlatAppearance.BorderSize = 1;
            _ctxLang = new ContextMenuStrip();
            _ctxLang.ShowImageMargin = false;
            foreach (var langOpt in new[] {
                new { Code = "PL", Name = "🇵🇱  Polski (PL)" },
                new { Code = "EN", Name = "🇬🇧  English (EN)" },
                new { Code = "DE", Name = "🇩🇪  Deutsch (DE)" },
                new { Code = "ES", Name = "🇪🇸  Español (ES)" }
            })
            {
                string code = langOpt.Code;
                ToolStripMenuItem item = new ToolStripMenuItem(langOpt.Name);
                item.Click += (s, e) => SetAppLanguage(code);
                _ctxLang.Items.Add(item);
            }
            _btnLang.ContextMenuStrip = _ctxLang;
            _btnLang.Click += (s, e) =>
            {
                string[] avail = new[] { "PL", "EN", "DE", "ES" };
                int idx = Array.IndexOf(avail, _currentLang);
                int next = (idx + 1) % avail.Length;
                SetAppLanguage(avail[next]);
            };
            _pnlStatus.Controls.Add(_btnLang);

            _btnUpdateBadge = new Button
            {
                Location = new Point(260, 34),
                Size = new Size(104, 21),
                Font = new Font("Segoe UI", 7.5f, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Visible = false
            };
            _btnUpdateBadge.FlatAppearance.BorderSize = 1;
            _btnUpdateBadge.Click += (s, e) =>
            {
                ThemeColors c = _isDarkMode ? ThemeColors.Dark : ThemeColors.Light;
                Localization.Strings t = Localization.Get(_currentLang);
                _updates.ShowUpdateDialog(this, c, t, Icon);
            };
            _pnlStatus.Controls.Add(_btnUpdateBadge);

            // 2. Główny przycisk Hero: Uruchom scrcpy
            _btnScrcpy = new Button
            {
                Location = new Point(16, 82),
                Size = new Size(372, 40),
                Font = _fontHero,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            _btnScrcpy.FlatAppearance.BorderSize = 0;
            _btnScrcpy.Click += (s, e) => LaunchFullScrcpy();
            Controls.Add(_btnScrcpy);

            _chkFullScreen = new CheckBox
            {
                Location = new Point(18, 126),
                AutoSize = true,
                Font = _fontSmall
            };
            Controls.Add(_chkFullScreen);

            _chkNavBar = new CheckBox
            {
                Location = new Point(215, 126),
                AutoSize = true,
                Checked = _pref.navBar,
                Font = _fontSmall
            };
            _chkNavBar.CheckedChanged += (s, e) =>
            {
                NavBarManager.Enabled = _chkNavBar.Checked;
                _pref.navBar = _chkNavBar.Checked;
                SavePreferences();
            };
            Controls.Add(_chkNavBar);

            // 3. Obraz, dźwięk i sterowanie
            _lblSectionOptions = new Label
            {
                Location = new Point(16, 150),
                Size = new Size(372, 16),
                Font = _fontSection
            };
            Controls.Add(_lblSectionOptions);

            _lblRes = new Label
            {
                Location = new Point(16, 170),
                AutoSize = true,
                Font = _fontSmall
            };
            Controls.Add(_lblRes);

            _cmbRes = new ComboBox
            {
                Location = new Point(16, 188),
                Size = new Size(372, 26),
                DropDownStyle = ComboBoxStyle.DropDownList,
                DrawMode = DrawMode.OwnerDrawFixed,
                ItemHeight = 22,
                Font = _fontRegular
            };
            _cmbRes.DrawItem += OnCmbResDrawItem;
            Controls.Add(_cmbRes);

            _chkAudio = new CheckBox
            {
                Location = new Point(18, 220),
                AutoSize = true,
                Checked = true,
                Font = _fontSmall
            };
            Controls.Add(_chkAudio);

            _chkAutoTaskbar = new CheckBox
            {
                Location = new Point(205, 220),
                AutoSize = true,
                Checked = true,
                Font = _fontSmall
            };
            Controls.Add(_chkAutoTaskbar);

            _btnWifi = new Button
            {
                Location = new Point(16, 244),
                Size = new Size(118, 28),
                Font = _fontSmall,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            _btnWifi.FlatAppearance.BorderSize = 1;
            _btnWifi.Click += async (s, e) => await SwitchToWirelessAdbAsync();
            Controls.Add(_btnWifi);

            _btnKeyFix = new Button
            {
                Location = new Point(143, 244),
                Size = new Size(118, 28),
                Font = _fontSmall,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            _btnKeyFix.FlatAppearance.BorderSize = 1;
            _btnKeyFix.Click += async (s, e) =>
            {
                if (await _adb.IsDeviceConnectedAsync())
                {
                    await _adb.RunAdbAsync("shell am start -a android.settings.HARD_KEYBOARD_SETTINGS");
                }
                Localization.Strings t = Localization.Get(_currentLang);
                MessageBox.Show(this, t.MsgKeyDone, t.KeyBtn, MessageBoxButtons.OK, MessageBoxIcon.Information);
            };
            Controls.Add(_btnKeyFix);

            _btnClipFix = new Button
            {
                Location = new Point(270, 244),
                Size = new Size(118, 28),
                Font = _fontSmall,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            _btnClipFix.FlatAppearance.BorderSize = 1;
            _btnClipFix.Click += async (s, e) =>
            {
                if (await _adb.IsDeviceConnectedAsync())
                {
                    await _adb.RunAdbAsync("shell am broadcast -a clipper.get");
                }
                string remoteFixCmd = "taskkill /f /im rdpclip.exe & start rdpclip.exe";
                try { Clipboard.SetText(remoteFixCmd); } catch {}
                Localization.Strings t = Localization.Get(_currentLang);
                MessageBox.Show(this, string.Format(t.MsgClipDone, remoteFixCmd), t.ClipBtn, MessageBoxButtons.OK, MessageBoxIcon.Information);
            };
            Controls.Add(_btnClipFix);

            // 4. Aplikacje w oknach
            _lblSectionApps = new Label
            {
                Location = new Point(16, 282),
                Size = new Size(160, 16),
                Font = _fontSection
            };
            Controls.Add(_lblSectionApps);

            _lblAppsSubtitle = new Label
            {
                Location = new Point(16, 298),
                Size = new Size(372, 16),
                Font = _fontSmall
            };
            Controls.Add(_lblAppsSubtitle);

            _btnLayout1 = new Button { Location = new Point(248, 276), Size = new Size(28, 22), Text = "1", FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };
            _btnLayout2 = new Button { Location = new Point(278, 276), Size = new Size(28, 22), Text = "2", FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };
            _btnLayout3 = new Button { Location = new Point(308, 276), Size = new Size(28, 22), Text = "3", FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };
            _btnLayout1.Click += (s, e) => ChangeLayout(1);
            _btnLayout2.Click += (s, e) => ChangeLayout(2);
            _btnLayout3.Click += (s, e) => ChangeLayout(3);
            Controls.Add(_btnLayout1);
            Controls.Add(_btnLayout2);
            Controls.Add(_btnLayout3);

            _btnEditApps = new Button
            {
                Location = new Point(338, 276),
                Size = new Size(50, 22),
                Font = _fontSmall,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            _btnEditApps.FlatAppearance.BorderSize = 1;
            _btnEditApps.Click += (s, e) => OpenAppsEditor();
            Controls.Add(_btnEditApps);

            _flowAppButtons = new FlowLayoutPanel
            {
                Location = new Point(16, 314),
                Width = 372,
                Height = 186,
                AutoScroll = false,
                BackColor = Color.Transparent
            };
            Controls.Add(_flowAppButtons);

            // Pod siatką aplikacji: Inny pakiet Androida
            _lblCustom = new Label
            {
                Location = new Point(16, 506),
                Size = new Size(372, 16),
                Font = _fontSmall
            };
            Controls.Add(_lblCustom);

            _txtCustom = new TextBox
            {
                Location = new Point(16, 524),
                Size = new Size(228, 25),
                BorderStyle = BorderStyle.FixedSingle,
                Font = _fontRegular
            };
            _txtCustom.TextChanged += (s, e) => FilterCustomSuggestions();
            _txtCustom.KeyDown += OnTxtCustomKeyDown;
            _txtCustom.LostFocus += (s, e) =>
            {
                if (_lstCustomSuggestions != null && _lstCustomSuggestions.Visible)
                {
                    Point pt = _lstCustomSuggestions.PointToClient(Cursor.Position);
                    if (!_lstCustomSuggestions.ClientRectangle.Contains(pt))
                    {
                        _lstCustomSuggestions.Visible = false;
                    }
                }
            };
            Controls.Add(_txtCustom);

            _lstCustomSuggestions = new ListBox
            {
                Location = new Point(16, 550),
                Width = 284,
                Height = 118,
                BorderStyle = BorderStyle.FixedSingle,
                Font = _fontRegular,
                IntegralHeight = false,
                Visible = false,
                Cursor = Cursors.Hand
            };
            _lstCustomSuggestions.Click += (s, e) =>
            {
                if (_lstCustomSuggestions.SelectedItem != null)
                {
                    _txtCustom.Text = _lstCustomSuggestions.SelectedItem.ToString();
                    _txtCustom.SelectionStart = _txtCustom.Text.Length;
                    _lstCustomSuggestions.Visible = false;
                    _txtCustom.Focus();
                }
            };
            _lstCustomSuggestions.DoubleClick += (s, e) =>
            {
                if (_lstCustomSuggestions.SelectedItem != null)
                {
                    _txtCustom.Text = _lstCustomSuggestions.SelectedItem.ToString();
                    _lstCustomSuggestions.Visible = false;
                    LaunchCustomApp();
                }
            };
            Controls.Add(_lstCustomSuggestions);

            _btnCustom = new Button
            {
                Location = new Point(250, 523),
                Size = new Size(72, 26),
                Font = _fontSection,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            _btnCustom.FlatAppearance.BorderSize = 1;
            _btnCustom.Click += (s, e) => LaunchCustomApp();
            Controls.Add(_btnCustom);

            _btnAddCustom = new Button
            {
                Location = new Point(326, 523),
                Size = new Size(62, 26),
                Text = "+ Dodaj",
                Font = _fontSection,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            _btnAddCustom.FlatAppearance.BorderSize = 1;
            _btnAddCustom.Click += (s, e) => AddCustomPackageTile();
            Controls.Add(_btnAddCustom);

            // 5. Operacje na urządzeniu
            _lblSectionDevice = new Label
            {
                Location = new Point(16, 558),
                Size = new Size(372, 16),
                Font = _fontSection
            };
            Controls.Add(_lblSectionDevice);

            _btnDesktop = new Button
            {
                Location = new Point(16, 578),
                Size = new Size(180, 32),
                Font = _fontSection,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            _btnDesktop.FlatAppearance.BorderSize = 1;
            _btnDesktop.Click += async (s, e) =>
            {
                if (!await _adb.IsDeviceConnectedAsync()) { ShowNoDeviceWarning(); return; }
                await _adb.RunAdbAsync("shell wm density 250");
                await _adb.RunAdbAsync("shell settings put global window_animation_scale 0.5");
                await _adb.RunAdbAsync("shell settings put global transition_animation_scale 0.5");
                await _adb.RunAdbAsync("shell settings put global animator_duration_scale 0.5");
                await _adb.RunAdbAsync("shell settings put global enable_freeform_support 1");
                await _adb.RunAdbAsync("shell settings put secure force_resizable_activities 1");
                await _adb.RunAdbAsync("shell am start-service com.farmerbb.taskbar/.service.DashboardTileService");
                Localization.Strings t = Localization.Get(_currentLang);
                MessageBox.Show(this, t.MsgDesktopOn, t.DesktopMode, MessageBoxButtons.OK, MessageBoxIcon.Information);
            };
            Controls.Add(_btnDesktop);

            _btnNormal = new Button
            {
                Location = new Point(208, 578),
                Size = new Size(180, 32),
                Font = _fontSection,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            _btnNormal.FlatAppearance.BorderSize = 1;
            _btnNormal.Click += async (s, e) =>
            {
                if (!await _adb.IsDeviceConnectedAsync()) { ShowNoDeviceWarning(); return; }
                await _adb.RunAdbAsync("shell wm density reset");
                await _adb.RunAdbAsync("shell settings put global window_animation_scale 1.0");
                await _adb.RunAdbAsync("shell settings put global transition_animation_scale 1.0");
                await _adb.RunAdbAsync("shell settings put global animator_duration_scale 1.0");
                await _adb.RunAdbAsync("shell settings put system user_rotation 0");
                await _adb.RunAdbAsync("shell settings put system accelerometer_rotation 1");
                await _adb.RestoreScreenTimeoutAsync(_originalTimeout);
                await _adb.RunAdbAsync("shell am stopservice com.farmerbb.taskbar/.service.DashboardTileService");
                Localization.Strings t = Localization.Get(_currentLang);
                MessageBox.Show(this, t.MsgResetDone, t.RestoreDefault, MessageBoxButtons.OK, MessageBoxIcon.Information);
            };
            Controls.Add(_btnNormal);

            _btnReboot = new Button
            {
                Location = new Point(16, 618),
                Size = new Size(372, 30),
                Font = _fontSection,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            _btnReboot.FlatAppearance.BorderSize = 1;
            _btnReboot.Click += async (s, e) =>
            {
                if (!await _adb.IsDeviceConnectedAsync()) { ShowNoDeviceWarning(); return; }
                Localization.Strings t = Localization.Get(_currentLang);
                if (MessageBox.Show(this, t.MsgConfirmReboot, t.RebootTitle, MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
                {
                    await _adb.RunAdbAsync("reboot");
                }
            };
            Controls.Add(_btnReboot);

            // 6. Nawigacja Androida (Cofnij, Home, Ostatnie)
            _btnNavBack = new Button
            {
                Location = new Point(16, 658),
                Size = new Size(118, 32),
                Font = _fontSection,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            _btnNavBack.FlatAppearance.BorderSize = 1;
            _btnNavBack.Click += async (s, e) =>
            {
                if (await _adb.IsDeviceConnectedAsync())
                {
                    await AdbService.SendKeyEventAsync(4);
                }
            };
            Controls.Add(_btnNavBack);

            _btnNavHome = new Button
            {
                Location = new Point(143, 658),
                Size = new Size(118, 32),
                Font = _fontSection,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            _btnNavHome.FlatAppearance.BorderSize = 1;
            _btnNavHome.Click += async (s, e) =>
            {
                if (await _adb.IsDeviceConnectedAsync())
                {
                    await AdbService.SendKeyEventAsync(3);
                }
            };
            Controls.Add(_btnNavHome);

            _btnNavRecents = new Button
            {
                Location = new Point(270, 658),
                Size = new Size(118, 32),
                Font = _fontSection,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            _btnNavRecents.FlatAppearance.BorderSize = 1;
            _btnNavRecents.Click += async (s, e) =>
            {
                if (await _adb.IsDeviceConnectedAsync())
                {
                    await AdbService.SendKeyEventAsync(187);
                }
            };
            Controls.Add(_btnNavRecents);

            Click += (s, e) =>
            {
                if (_lstCustomSuggestions != null && _lstCustomSuggestions.Visible)
                {
                    _lstCustomSuggestions.Visible = false;
                }
            };
        }

        private void OnCmbResDrawItem(object sender, DrawItemEventArgs e)
        {
            if (e.Index < 0) return;
            bool isSelected = (e.State & DrawItemState.Selected) != 0;
            Color bgCol = isSelected ? (_isDarkMode ? Color.FromArgb(40, 95, 175) : Color.FromArgb(60, 125, 220))
                                     : (_isDarkMode ? Color.FromArgb(33, 35, 45) : Color.White);
            Color textCol = (isSelected || _isDarkMode) ? Color.FromArgb(240, 242, 248) : Color.FromArgb(25, 28, 36);

            using (SolidBrush bgBrush = new SolidBrush(bgCol))
            using (SolidBrush textBrush = new SolidBrush(textCol))
            {
                e.Graphics.FillRectangle(bgBrush, e.Bounds);
                string itemText = _cmbRes.Items[e.Index].ToString();
                int textY = e.Bounds.Y + Math.Max(0, (e.Bounds.Height - _cmbRes.Font.Height) / 2);
                e.Graphics.DrawString(itemText, _cmbRes.Font, textBrush, e.Bounds.X + 6, textY);
            }
        }

        private void ApplyTheme()
        {
            ThemeColors c = _isDarkMode ? ThemeColors.Dark : ThemeColors.Light;
            NativeMethods.UseImmersiveDarkMode(Handle, _isDarkMode);

            BackColor = c.Bg;
            ForeColor = c.Text;

            _pnlStatus.BackColor = c.Card;
            _pnlStatus.ForeColor = c.Text;
            _lblDeviceTitle.ForeColor = c.Text;
            _lblStatusDetail.ForeColor = c.TextMuted;

            _btnTheme.BackColor = c.ToggleBg;
            _btnTheme.ForeColor = c.ToggleText;
            _btnTheme.FlatAppearance.BorderColor = c.CardBorder;

            _btnLang.BackColor = c.ToggleBg;
            _btnLang.ForeColor = c.ToggleText;
            _btnLang.FlatAppearance.BorderColor = c.CardBorder;

            _ctxLang.BackColor = c.Card;
            _ctxLang.ForeColor = c.Text;
            foreach (ToolStripItem it in _ctxLang.Items)
            {
                it.BackColor = c.Card;
                it.ForeColor = c.Text;
            }

            _btnUpdateBadge.BackColor = c.BadgeUpdate;
            _btnUpdateBadge.ForeColor = c.BadgeUpdateText;
            _btnUpdateBadge.FlatAppearance.BorderColor = c.BadgeUpdateBorder;

            _btnScrcpy.BackColor = c.BtnHero;
            _btnScrcpy.ForeColor = c.BtnHeroText;

            _chkFullScreen.ForeColor = c.TextMuted;
            if (_chkNavBar != null) _chkNavBar.ForeColor = c.TextMuted;
            _lblSectionOptions.ForeColor = c.TextMuted;
            _lblRes.ForeColor = c.TextMuted;
            _chkAudio.ForeColor = c.Text;
            _chkAutoTaskbar.ForeColor = c.Text;

            _cmbRes.BackColor = c.Card;
            _cmbRes.ForeColor = c.Text;

            _btnWifi.BackColor = c.BtnTool;
            _btnWifi.ForeColor = c.BtnToolText;
            _btnWifi.FlatAppearance.BorderColor = c.BtnToolBorder;

            _btnKeyFix.BackColor = c.BtnTool;
            _btnKeyFix.ForeColor = c.BtnToolText;
            _btnKeyFix.FlatAppearance.BorderColor = c.BtnToolBorder;

            _btnClipFix.BackColor = c.BtnTool;
            _btnClipFix.ForeColor = c.BtnToolText;
            _btnClipFix.FlatAppearance.BorderColor = c.BtnToolBorder;

            _lblSectionApps.ForeColor = c.TextMuted;
            _lblAppsSubtitle.ForeColor = c.TextMuted;

            _btnEditApps.BackColor = c.BtnApp;
            _btnEditApps.ForeColor = c.BtnAppText;
            _btnEditApps.FlatAppearance.BorderColor = c.BtnAppBorder;

            UpdateLayoutButtonColors();

            _lblCustom.ForeColor = c.TextMuted;
            _txtCustom.BackColor = c.InputBg;
            _txtCustom.ForeColor = c.InputText;
            _lstCustomSuggestions.BackColor = c.InputBg;
            _lstCustomSuggestions.ForeColor = c.InputText;

            _btnCustom.BackColor = c.BtnApp;
            _btnCustom.ForeColor = c.BtnAppText;
            _btnCustom.FlatAppearance.BorderColor = c.BtnAppBorder;

            _btnAddCustom.BackColor = c.BtnApp;
            _btnAddCustom.ForeColor = c.BtnAppText;
            _btnAddCustom.FlatAppearance.BorderColor = c.BtnAppBorder;

            _lblSectionDevice.ForeColor = c.TextMuted;

            _btnDesktop.BackColor = c.BtnMode;
            _btnDesktop.ForeColor = c.BtnModeText;
            _btnDesktop.FlatAppearance.BorderColor = c.BtnModeBorder;

            _btnNormal.BackColor = c.BtnMode;
            _btnNormal.ForeColor = c.BtnModeText;
            _btnNormal.FlatAppearance.BorderColor = c.BtnModeBorder;

            _btnReboot.BackColor = c.BtnReboot;
            _btnReboot.ForeColor = c.BtnRebootText;
            _btnReboot.FlatAppearance.BorderColor = c.BtnRebootBorder;

            if (_btnNavBack != null)
            {
                _btnNavBack.BackColor = c.BtnTool;
                _btnNavBack.ForeColor = c.BtnToolText;
                _btnNavBack.FlatAppearance.BorderColor = c.BtnToolBorder;

                _btnNavHome.BackColor = c.BtnTool;
                _btnNavHome.ForeColor = c.BtnToolText;
                _btnNavHome.FlatAppearance.BorderColor = c.BtnToolBorder;

                _btnNavRecents.BackColor = c.BtnTool;
                _btnNavRecents.ForeColor = c.BtnToolText;
                _btnNavRecents.FlatAppearance.BorderColor = c.BtnToolBorder;
            }

            NavBarManager.SetDarkMode(_isDarkMode);

            // Kafelki aplikacji
            foreach (Button b in _createdAppButtons)
            {
                if (_appButtons.Count == 0)
                {
                    b.BackColor = c.Card;
                    b.ForeColor = c.TextMuted;
                    b.FlatAppearance.BorderColor = c.CardBorder;
                }
                else
                {
                    b.BackColor = c.BtnApp;
                    b.ForeColor = c.BtnAppText;
                    b.FlatAppearance.BorderColor = c.BtnAppBorder;
                }
            }
            foreach (Button r in _createdRenameButtons)
            {
                r.BackColor = c.BtnApp;
                r.ForeColor = c.TextMuted;
                r.FlatAppearance.BorderColor = c.BtnAppBorder;
            }
            foreach (Button d in _createdDelButtons)
            {
                d.BackColor = c.BtnApp;
                d.ForeColor = c.TextMuted;
                d.FlatAppearance.BorderColor = c.BtnAppBorder;
            }

            UpdateStatusDisplay();
            Invalidate(true);
        }

        private void UpdateLayoutButtonColors()
        {
            ThemeColors c = _isDarkMode ? ThemeColors.Dark : ThemeColors.Light;
            Color activeBg = c.ToggleChecked;
            Color activeFg = c.ToggleText;
            Color activeBorder = c.CardBorder;

            Button[] buttons = new[] { _btnLayout1, _btnLayout2, _btnLayout3 };
            for (int i = 0; i < 3; i++)
            {
                int val = i + 1;
                Button b = buttons[i];
                if (_appsLayout == val)
                {
                    b.BackColor = activeBg;
                    b.ForeColor = activeFg;
                    b.FlatAppearance.BorderColor = activeBorder;
                    b.Font = _fontSection;
                }
                else
                {
                    b.BackColor = c.ToggleBg;
                    b.ForeColor = c.TextMuted;
                    b.FlatAppearance.BorderColor = c.CardBorder;
                    b.Font = _fontSmall;
                }
            }
        }

        private void ApplyLanguage()
        {
            Localization.Strings t = Localization.Get(_currentLang);

            _btnTheme.Text = _isDarkMode ? t.ThemeDark : t.ThemeLight;
            _btnLang.Text = _currentLang;
            _tipMain.SetToolTip(_btnLang, t.LangTooltip);
            _btnUpdateBadge.Text = t.UpdateBadge;

            _btnScrcpy.Text = t.LaunchHero;
            _chkFullScreen.Text = t.FullScreenOpt;
            if (_chkNavBar != null)
            {
                _chkNavBar.Text = t.OptNavBar;
                _tipMain.SetToolTip(_chkNavBar, t.OptNavBarTooltip);
            }

            _lblSectionOptions.Text = t.SectionOptions;
            _lblRes.Text = t.ResLabel;
            _chkAudio.Text = t.AudioPass;
            _chkAutoTaskbar.Text = t.AutoTaskbar;
            _btnWifi.Text = t.WifiBtn;
            _btnKeyFix.Text = t.KeyBtn;
            _btnClipFix.Text = t.ClipBtn;

            _lblSectionApps.Text = t.SectionApps;
            _lblAppsSubtitle.Text = t.AppsSubtitle;
            _btnEditApps.Text = t.AppsEdit;
            _tipMain.SetToolTip(_btnEditApps, t.AppsEditTooltip);
            _tipMain.SetToolTip(_btnLayout1, t.Layout1Tooltip);
            _tipMain.SetToolTip(_btnLayout2, t.Layout2Tooltip);
            _tipMain.SetToolTip(_btnLayout3, t.Layout3Tooltip);

            _lblCustom.Text = t.CustomLabel;
            _btnCustom.Text = t.CustomBtn;
            _btnAddCustom.Text = t.CustomAddBtn;
            _tipMain.SetToolTip(_btnAddCustom, t.CustomAddTooltip);
            NativeMethods.SetCueBanner(_txtCustom.Handle, t.CustomPlaceholder);

            _lblSectionDevice.Text = t.SectionDevice;
            _btnDesktop.Text = t.DesktopMode;
            _btnNormal.Text = t.RestoreDefault;
            _btnReboot.Text = t.RebootBtn;
            if (_btnNavBack != null)
            {
                _btnNavBack.Text = t.NavBack;
                _btnNavHome.Text = t.NavHome;
                _btnNavRecents.Text = t.NavRecents;
                _tipMain.SetToolTip(_btnNavBack, "Cofnij (ESC / Keycode 4)");
                _tipMain.SetToolTip(_btnNavHome, "Ekran główny (Home / Keycode 3)");
                _tipMain.SetToolTip(_btnNavRecents, "Ostatnie aplikacje (Recents / Keycode 187)");
            }

            int currIdx = _cmbRes.SelectedIndex;
            if (currIdx < 0) currIdx = 0;
            _cmbRes.Items.Clear();
            foreach (string rn in t.ResNames)
            {
                _cmbRes.Items.Add(rn);
            }
            _cmbRes.SelectedIndex = Math.Min(currIdx, _cmbRes.Items.Count - 1);

            UpdateStatusDisplay();
        }

        public void SetAppLanguage(string langCode)
        {
            if (Localization.SupportedLanguages.ContainsKey(langCode))
            {
                _currentLang = langCode;
                SavePreferences();
                ApplyLanguage();
                ApplyTheme();
                UpdateAppButtonGrid();
            }
        }

        private void ChangeLayout(int layout)
        {
            if (_appsLayout == layout) return;
            _appsLayout = layout;
            SavePreferences();
            UpdateLayoutButtonColors();
            UpdateAppButtonGrid();
        }

        private void UpdateAppButtonGrid()
        {
            _flowAppButtons.SuspendLayout();
            try
            {
                while (_flowAppButtons.Controls.Count > 0)
                {
                    Control c = _flowAppButtons.Controls[0];
                    _flowAppButtons.Controls.RemoveAt(0);
                    c.Dispose();
                }
                _createdAppButtons.Clear();
                _createdRenameButtons.Clear();
                _createdDelButtons.Clear();

                ThemeColors cTheme = _isDarkMode ? ThemeColors.Dark : ThemeColors.Light;
                Localization.Strings t = Localization.Get(_currentLang);

                if (_appButtons.Count == 0)
                {
                    Button btnEmpty = new Button
                    {
                        Text = t.AppsAddPrompt,
                        Size = new Size(350, 40),
                        Margin = new Padding(12, 3, 3, 3),
                        Font = _fontSection,
                        FlatStyle = FlatStyle.Flat,
                        BackColor = cTheme.Card,
                        ForeColor = cTheme.TextMuted,
                        Cursor = Cursors.Hand
                    };
                    btnEmpty.FlatAppearance.BorderSize = 1;
                    btnEmpty.FlatAppearance.BorderColor = cTheme.CardBorder;
                    btnEmpty.Click += (s, e) => OpenAppsEditor();
                    _flowAppButtons.Controls.Add(btnEmpty);
                    _createdAppButtons.Add(btnEmpty);
                }
                else
                {
                    int tileW = 178, btnW = 130, renX = 129, renW = 25, delX = 153, delW = 25;
                    Padding tileMargin = new Padding(3);
                    Padding btnPad = new Padding(4, 0, 0, 0);
                    Padding btnIconPad = new Padding(4, 0, 0, 0);
                    int targetIconSize = 18, targetIconGap = 5;

                    if (_appsLayout == 1)
                    {
                        tileW = 360;
                        tileMargin = new Padding(6, 3, 6, 3);
                        btnW = 310;
                        renX = 309; renW = 26;
                        delX = 334; delW = 26;
                        btnPad = new Padding(8, 0, 0, 0);
                        btnIconPad = new Padding(8, 0, 0, 0);
                        targetIconSize = 18; targetIconGap = 6;
                    }
                    else if (_appsLayout == 3)
                    {
                        tileW = 118;
                        tileMargin = new Padding(2, 3, 2, 3);
                        btnW = 74;
                        renX = 73; renW = 23;
                        delX = 95; delW = 23;
                        btnPad = new Padding(2, 0, 0, 0);
                        btnIconPad = new Padding(2, 0, 0, 0);
                        targetIconSize = 16; targetIconGap = 4;
                    }

                    foreach (AppEntry app in _appButtons)
                    {
                        AppEntry currentApp = app;

                        Panel pnlTile = new Panel
                        {
                            Size = new Size(tileW, 30),
                            Margin = tileMargin,
                            BackColor = Color.Transparent
                        };

                        Button btn = new Button
                        {
                            Text = currentApp.name,
                            Location = new Point(0, 0),
                            Size = new Size(btnW, 30),
                            Font = _fontSmall,
                            TextAlign = ContentAlignment.MiddleLeft,
                            Padding = btnPad,
                            FlatStyle = FlatStyle.Flat,
                            BackColor = cTheme.BtnApp,
                            ForeColor = cTheme.BtnAppText,
                            AutoEllipsis = true,
                            Cursor = Cursors.Hand
                        };
                        btn.FlatAppearance.BorderSize = 1;
                        btn.FlatAppearance.BorderColor = cTheme.BtnAppBorder;

                        Bitmap iconImg = _icons.GetResizedIcon(currentApp.package, targetIconSize, targetIconGap);
                        if (iconImg != null)
                        {
                            btn.Image = iconImg;
                            btn.ImageAlign = ContentAlignment.MiddleLeft;
                            btn.TextAlign = ContentAlignment.MiddleLeft;
                            btn.TextImageRelation = TextImageRelation.ImageBeforeText;
                            btn.Padding = btnIconPad;
                        }

                        _tipMain.SetToolTip(btn, string.Format("{0}\n{1}", currentApp.name, currentApp.package));
                        btn.Click += (s, e) => LaunchAppTile(currentApp);

                        // Przycisk edycji (✎)
                        Button btnRename = new Button
                        {
                            Text = "✎",
                            Location = new Point(renX, 0),
                            Size = new Size(renW, 30),
                            Font = _fontSmall,
                            FlatStyle = FlatStyle.Flat,
                            BackColor = cTheme.BtnApp,
                            ForeColor = cTheme.TextMuted,
                            Cursor = Cursors.Hand
                        };
                        btnRename.FlatAppearance.BorderSize = 1;
                        btnRename.FlatAppearance.BorderColor = cTheme.BtnAppBorder;
                        _tipMain.SetToolTip(btnRename, string.Format(t.AppsRenameTooltip, currentApp.name));
                        btnRename.MouseEnter += (s, e) => btnRename.ForeColor = Color.FromArgb(100, 180, 255);
                        btnRename.MouseLeave += (s, e) => btnRename.ForeColor = (_isDarkMode ? ThemeColors.Dark.TextMuted : ThemeColors.Light.TextMuted);
                        btnRename.Click += (s, e) => RenameAppTile(currentApp);

                        // Przycisk usuwania (✕)
                        Button btnDel = new Button
                        {
                            Text = "✕",
                            Location = new Point(delX, 0),
                            Size = new Size(delW, 30),
                            Font = _fontSmall,
                            FlatStyle = FlatStyle.Flat,
                            BackColor = cTheme.BtnApp,
                            ForeColor = cTheme.TextMuted,
                            Cursor = Cursors.Hand
                        };
                        btnDel.FlatAppearance.BorderSize = 1;
                        btnDel.FlatAppearance.BorderColor = cTheme.BtnAppBorder;
                        _tipMain.SetToolTip(btnDel, string.Format(t.AppsRemoveTooltip, currentApp.name));
                        btnDel.MouseEnter += (s, e) => btnDel.ForeColor = Color.FromArgb(255, 90, 90);
                        btnDel.MouseLeave += (s, e) => btnDel.ForeColor = (_isDarkMode ? ThemeColors.Dark.TextMuted : ThemeColors.Light.TextMuted);
                        btnDel.Click += (s, e) => DeleteAppTile(currentApp);

                        // Menu kontekstowe
                        ContextMenuStrip ctx = new ContextMenuStrip();
                        ToolStripMenuItem itemRename = new ToolStripMenuItem(t.AppsRenameItem);
                        itemRename.Click += (s, e) => btnRename.PerformClick();
                        ToolStripMenuItem itemDel = new ToolStripMenuItem(t.AppsDeleteItem);
                        itemDel.Click += (s, e) => btnDel.PerformClick();
                        ctx.Items.Add(itemRename);
                        ctx.Items.Add(itemDel);
                        btn.ContextMenuStrip = ctx;

                        pnlTile.Controls.Add(btn);
                        pnlTile.Controls.Add(btnRename);
                        pnlTile.Controls.Add(btnDel);

                        _flowAppButtons.Controls.Add(pnlTile);
                        _createdAppButtons.Add(btn);
                        _createdRenameButtons.Add(btnRename);
                        _createdDelButtons.Add(btnDel);
                    }
                }
            }
            finally
            {
                _flowAppButtons.ResumeLayout();
            }

            // Dynamiczne dostosowanie wysokości okna i relokacja kontrolek poniżej
            AdjustWindowLayout();
        }

        private void AdjustWindowLayout()
        {
            int cols = _appsLayout == 1 ? 1 : (_appsLayout == 3 ? 3 : 2);
            int rowCount = _appButtons.Count == 0 ? 1 : (int)Math.Ceiling(_appButtons.Count / (double)cols);
            int neededFlowH = (rowCount * 36) + 6;

            int screenH = 900;
            try { screenH = Screen.FromControl(this).WorkingArea.Height; } catch {}
            int maxFlowH = Math.Max(186, screenH - 520);

            if (neededFlowH > maxFlowH)
            {
                _flowAppButtons.Height = maxFlowH;
                _flowAppButtons.AutoScroll = true;
            }
            else
            {
                _flowAppButtons.Height = neededFlowH;
                _flowAppButtons.AutoScroll = false;
            }

            int curY = _flowAppButtons.Bottom + 10;
            _lblCustom.Location = new Point(16, curY);

            curY = _lblCustom.Bottom + 4;
            _txtCustom.Location = new Point(16, curY);
            _btnCustom.Location = new Point(250, curY - 1);
            _btnAddCustom.Location = new Point(326, curY - 1);

            _lstCustomSuggestions.Location = new Point(_txtCustom.Left, _txtCustom.Bottom + 1);

            curY = _txtCustom.Bottom + 12;
            _lblSectionDevice.Location = new Point(16, curY);

            curY = _lblSectionDevice.Bottom + 6;
            _btnDesktop.Location = new Point(16, curY);
            _btnNormal.Location = new Point(208, curY);

            curY = _btnDesktop.Bottom + 8;
            _btnReboot.Location = new Point(16, curY);

            if (_btnNavBack != null)
            {
                curY = _btnReboot.Bottom + 8;
                _btnNavBack.Location = new Point(16, curY);
                _btnNavHome.Location = new Point(143, curY);
                _btnNavRecents.Location = new Point(270, curY);
                curY = _btnNavBack.Bottom + 16;
            }
            else
            {
                curY = _btnReboot.Bottom + 18;
            }
            ClientSize = new Size(404, curY);
        }

        private void UpdateStatusDisplay()
        {
            ThemeColors c = _isDarkMode ? ThemeColors.Dark : ThemeColors.Light;
            Localization.Strings t = Localization.Get(_currentLang);

            if (_currentDevice.IsOnline)
            {
                _lblStatusDot.ForeColor = c.StatusDotOnline;
                string connType = _currentDevice.IsWifiConnected ? t.StatusConnectedWifi : t.StatusConnectedUsb;
                string devName = !string.IsNullOrEmpty(_currentDevice.Model) ? _currentDevice.Model : "Android";
                _lblDeviceTitle.Text = string.Format("{0} - {1}", devName, connType);

                string bat = _currentDevice.BatteryLevel >= 0 ? string.Format("{0}%{1}", _currentDevice.BatteryLevel, _currentDevice.IsCharging ? t.ChargingStr : "") : "---";
                _lblStatusDetail.Text = string.Format("{0} {1}  |  {2}", t.BatteryLabel, bat, t.StatusActive);
            }
            else
            {
                _lblStatusDot.ForeColor = c.StatusDotOffline;
                _lblDeviceTitle.Text = t.StatusNoPhone;
                _lblStatusDetail.Text = t.StatusCheckConn;
            }
        }

        private async void OnFormShown(object sender, EventArgs e)
        {
            // Obsługa automatycznego zrzutu ekranu dla README
            if (!string.IsNullOrEmpty(_captureScreenshotLang))
            {
                await _adb.GetDeviceInfoAsync();
                SetAppLanguage(_captureScreenshotLang);
                ApplyTheme();

                // Dostosowanie nazw aplikacji dla języków obcych (jak w testach README)
                if (_captureScreenshotLang == "EN")
                {
                    foreach (Button btn in _createdAppButtons)
                    {
                        if (btn.Text == "Ustawienia") btn.Text = "Settings";
                        if (btn.Text.StartsWith("Gmail")) btn.Text = "Gmail (All)";
                        if (btn.Text.StartsWith("Wiadomości")) btn.Text = "Messages (Google)";
                    }
                }
                else if (_captureScreenshotLang == "DE")
                {
                    foreach (Button btn in _createdAppButtons)
                    {
                        if (btn.Text == "Ustawienia") btn.Text = "Einstellungen";
                        if (btn.Text.StartsWith("Gmail")) btn.Text = "Gmail (Alle)";
                        if (btn.Text.StartsWith("Wiadomości")) btn.Text = "Nachrichten (Google)";
                    }
                }
                else if (_captureScreenshotLang == "ES")
                {
                    foreach (Button btn in _createdAppButtons)
                    {
                        if (btn.Text == "Ustawienia") btn.Text = "Ajustes";
                        if (btn.Text.StartsWith("Gmail")) btn.Text = "Gmail (Todas)";
                        if (btn.Text.StartsWith("Wiadomości")) btn.Text = "Mensajes (Google)";
                    }
                }

                UpdateStatusDisplay();
                Refresh();
                Application.DoEvents();
                await Task.Delay(800);
                Application.DoEvents();

                using (Bitmap bmp = new Bitmap(Width, Height))
                {
                    using (Graphics g = Graphics.FromImage(bmp))
                    {
                        IntPtr hdc = g.GetHdc();
                        NativeMethods.PrintWindow(Handle, hdc, 2);
                        g.ReleaseHdc(hdc);
                    }

                    string docsDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "docs");
                    if (!Directory.Exists(docsDir)) Directory.CreateDirectory(docsDir);
                    string outPath = Path.Combine(docsDir, "screenshot." + _captureScreenshotLang.ToLower() + ".png");
                    bmp.Save(outPath, ImageFormat.Png);
                }

                Close();
                return;
            }

            // Standardowy rozruch
            KeyboardHook.Start();
            NavBarManager.Initialize(_isDarkMode);
            NavBarManager.Enabled = _chkNavBar != null && _chkNavBar.Checked;

            RefreshDeviceStatusAsync();
            InitTimeoutSettingsAsync();
            CheckUpdatesAsync();
            DownloadMissingIconsAsync();

            // Timer odpytywania stanu urządzenia co 3 sekundy
            _statusTimer = new Timer { Interval = 3000 };
            _statusTimer.Tick += (s, ev) => RefreshDeviceStatusAsync();
            _statusTimer.Start();

            // Timer czuwania ekranu co 8 sekund
            _keepAwakeTimer = new Timer { Interval = 8000 };
            _keepAwakeTimer.Tick += async (s, ev) =>
            {
                CleanExitedProcesses();
                if (_currentDevice.IsOnline)
                {
                    await _adb.PreventScreenLockAsync();
                    if (await _adb.IsKeyguardShowingAsync())
                    {
                        await _adb.UnlockDeviceAsync(Environment.GetEnvironmentVariable("SCRCPY_ADB_PIN"));
                    }
                }
            };
            _keepAwakeTimer.Start();
        }

        private async void RefreshDeviceStatusAsync()
        {
            _currentDevice = await _adb.GetDeviceInfoAsync();
            UpdateStatusDisplay();
        }

        private async void InitTimeoutSettingsAsync()
        {
            int timeout = await _adb.GetOriginalScreenTimeoutAsync();
            if (timeout > 0 && timeout != 2147483647)
            {
                _originalTimeout = timeout;
                try { File.WriteAllText(_stateFile, timeout.ToString()); } catch {}
            }
            await _adb.PreventScreenLockAsync();
        }

        private async void CheckUpdatesAsync()
        {
            bool updateAvailable = await _updates.CheckForUpdateAsync();
            if (updateAvailable)
            {
                _btnUpdateBadge.Visible = true;
                _btnUpdateBadge.Text = Localization.Get(_currentLang).UpdateBadge;
                ApplyTheme();
            }
        }

        private void DownloadMissingIconsAsync()
        {
            List<string> pkgs = new List<string>();
            foreach (AppEntry a in _appButtons)
            {
                if (!string.IsNullOrEmpty(a.package)) pkgs.Add(a.package);
            }
            _icons.StartAsyncIconDownload(pkgs, _adb, pkg =>
            {
                // Aktualizujemy ikony na kafelkach
                BeginInvoke((Action)(() =>
                {
                    UpdateAppButtonGrid();
                }));
            });
        }

        private void OnFormClosing(object sender, FormClosingEventArgs e)
        {
            KeyboardHook.Stop();
            NavBarManager.CloseAll();

            if (_statusTimer != null) { _statusTimer.Stop(); _statusTimer.Dispose(); }
            if (_keepAwakeTimer != null) { _keepAwakeTimer.Stop(); _keepAwakeTimer.Dispose(); }

            CleanExitedProcesses();
            List<int> pids = new List<int>();
            foreach (Process p in _launchedProcesses)
            {
                if (!p.HasExited) pids.Add(p.Id);
            }

            if (pids.Count == 0)
            {
                _adb.RestoreScreenTimeoutAsync(_originalTimeout);
            }
            else
            {
                // Uruchomienie cichego skryptu nadzorcy w tle
                string pidList = string.Join(",", pids);
                string script = string.Format(@"
$pids = @({0})
while ((Get-Process -Id $pids -ErrorAction SilentlyContinue).Count -gt 0) {{
    adb shell settings put system screen_off_timeout 2147483647 2>$null
    adb shell svc power stayon true 2>$null
    Start-Sleep -Seconds 5
}}
adb shell settings put system screen_off_timeout {1} 2>$null
adb shell svc power stayon false 2>$null
if (Test-Path '{2}') {{ Remove-Item '{2}' -Force -ErrorAction SilentlyContinue }}
", pidList, _originalTimeout, _stateFile.Replace("'", "''"));

                byte[] bytes = System.Text.Encoding.Unicode.GetBytes(script);
                string b64 = Convert.ToBase64String(bytes);
                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = "-NoProfile -WindowStyle Hidden -EncodedCommand " + b64,
                    CreateNoWindow = true,
                    UseShellExecute = false
                };
                try { Process.Start(psi); } catch {}
            }
        }

        private void CleanExitedProcesses()
        {
            _launchedProcesses.RemoveAll(p => p.HasExited);
        }

        private async void LaunchFullScrcpy()
        {
            if (!await _adb.IsDeviceConnectedAsync()) { ShowNoDeviceWarning(); return; }

            await _adb.PreventScreenLockAsync();
            if (_chkAutoTaskbar.Checked)
            {
                await _adb.RunAdbAsync("shell am start-service com.farmerbb.taskbar/.service.DashboardTileService");
            }

            await _adb.RunAdbAsync("shell settings put system accelerometer_rotation 0");
            await _adb.RunAdbAsync("shell settings put system user_rotation 1");
            await _adb.UnlockDeviceAsync(Environment.GetEnvironmentVariable("SCRCPY_ADB_PIN"));

            string title = !string.IsNullOrEmpty(_currentDevice.Model) ? string.Format("{0} (scrcpy)", _currentDevice.Model) : "Android (scrcpy)";
            string audioArg = _chkAudio.Checked ? "" : "--no-audio";
            string fsArg = _chkFullScreen.Checked ? "-f" : "";

            string argsToRun = string.Format("-S -w -K {0} {1} --window-title=\"{2}\"", fsArg, audioArg, title).Trim();

            try
            {
                Process proc = _adb.LaunchScrcpy(argsToRun);
                if (proc != null) _launchedProcesses.Add(proc);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Error starting scrcpy: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async void LaunchAppTile(AppEntry app)
        {
            if (_chkAutoTaskbar.Checked)
            {
                await _adb.RunAdbAsync("shell am start-service com.farmerbb.taskbar/.service.DashboardTileService");
            }

            string disp = "1080x2400";
            if (string.Equals(app.package, "com.microsoft.rdc.androidx", StringComparison.OrdinalIgnoreCase))
            {
                int idx = _cmbRes.SelectedIndex;
                if (idx >= 0 && idx < _resValues.Length)
                {
                    string val = _resValues[idx];
                    disp = val == "AUTO" ? "1080x2400" : val;
                }
                else
                {
                    disp = "1920x1080/160";
                }
            }

            bool useUhid = app.flags != null && app.flags.Contains("-UseUhidKeyboard");
            bool forwardClicks = app.flags != null && app.flags.Contains("-ForwardAllClicks");

            try
            {
                Process proc = await _adb.StartScrcpyAppAsync(app.package, app.name, useUhid, forwardClicks, disp, _chkAudio.Checked);
                if (proc != null) _launchedProcesses.Add(proc);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Error starting app: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void LaunchCustomApp()
        {
            string pkg = (_txtCustom.Text ?? "").Trim();
            Localization.Strings t = Localization.Get(_currentLang);
            if (string.IsNullOrWhiteSpace(pkg))
            {
                MessageBox.Show(this, t.MsgPkgEmpty, "scrcpy", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (_chkAutoTaskbar.Checked)
            {
                _adb.RunAdbAsync("shell am start-service com.farmerbb.taskbar/.service.DashboardTileService");
            }

            Task.Run(async () =>
            {
                try
                {
                    Process proc = await _adb.StartScrcpyAppAsync(pkg, pkg, false, false, "1080x2400", _chkAudio.Checked);
                    if (proc != null) _launchedProcesses.Add(proc);
                }
                catch {}
            });
        }

        private void AddCustomPackageTile()
        {
            string pkg = (_txtCustom.Text ?? "").Trim();
            Localization.Strings t = Localization.Get(_currentLang);
            if (string.IsNullOrWhiteSpace(pkg))
            {
                MessageBox.Show(this, t.MsgPkgEmpty, "scrcpy", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            AppEntry existing = _appButtons.Find(a => string.Equals(a.package, pkg, StringComparison.OrdinalIgnoreCase));
            if (existing != null)
            {
                MessageBox.Show(this, string.Format(t.MsgAppAlreadyExists, pkg, existing.name), "scrcpy", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            string chosenName = PromptName(t.AddAppDialogTitle, t.AddAppDialogLabel, t.AddAppDialogAdd, pkg);
            if (!string.IsNullOrWhiteSpace(chosenName))
            {
                _appButtons.Add(new AppEntry(chosenName, pkg));
                SaveAppsConfigFile();
                if (_lstCustomSuggestions != null) _lstCustomSuggestions.Visible = false;
                _txtCustom.Text = "";
                UpdateAppButtonGrid();
                DownloadMissingIconsAsync();
            }
        }

        private void RenameAppTile(AppEntry app)
        {
            Localization.Strings t = Localization.Get(_currentLang);
            string newName = PromptName(t.RenameAppDialogTitle, t.RenameAppDialogLabel, t.RenameAppDialogSave, app.package, app.name);
            if (!string.IsNullOrWhiteSpace(newName) && newName != app.name)
            {
                app.name = newName;
                SaveAppsConfigFile();
                UpdateAppButtonGrid();
            }
        }

        private void DeleteAppTile(AppEntry app)
        {
            Localization.Strings t = Localization.Get(_currentLang);
            string msg = string.Format(t.MsgConfirmRemoveApp, app.name);
            if (MessageBox.Show(this, msg, t.AppsEditorTitle, MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                _appButtons.Remove(app);
                SaveAppsConfigFile();
                UpdateAppButtonGrid();
            }
        }

        private string PromptName(string title, string label, string btnText, string package, string initial = null)
        {
            ThemeColors c = _isDarkMode ? ThemeColors.Dark : ThemeColors.Light;
            using (Form dlg = new Form())
            {
                dlg.Text = title;
                dlg.ClientSize = new Size(360, 156);
                dlg.StartPosition = FormStartPosition.CenterParent;
                dlg.FormBorderStyle = FormBorderStyle.FixedDialog;
                dlg.MaximizeBox = false;
                dlg.MinimizeBox = false;
                dlg.ShowInTaskbar = false;
                dlg.BackColor = c.Bg;
                dlg.ForeColor = c.Text;
                dlg.Font = _fontRegular;
                if (Icon != null) dlg.Icon = Icon;
                NativeMethods.UseImmersiveDarkMode(dlg.Handle, _isDarkMode);

                Label lbl = new Label { Location = new Point(16, 14), Size = new Size(328, 18), Text = label, Font = _fontSection, ForeColor = c.Text };
                dlg.Controls.Add(lbl);

                string[] parts = package.Split('.');
                string defaultText = initial != null ? initial : parts[parts.Length - 1];
                TextBox txt = new TextBox { Location = new Point(16, 36), Size = new Size(328, 26), Text = defaultText, Font = _fontRegular, BorderStyle = BorderStyle.FixedSingle, BackColor = c.InputBg, ForeColor = c.InputText };
                dlg.Controls.Add(txt);

                Label lblPkg = new Label { Location = new Point(16, 68), Size = new Size(328, 16), Text = "Pakiet: " + package, Font = _fontSmall, ForeColor = c.TextMuted };
                dlg.Controls.Add(lblPkg);

                Button btnOk = new Button { Text = btnText, Location = new Point(140, 106), Size = new Size(110, 30), FlatStyle = FlatStyle.Flat, BackColor = c.BtnHero, ForeColor = c.BtnHeroText, Font = _fontSection, DialogResult = DialogResult.OK };
                btnOk.FlatAppearance.BorderSize = 0;
                dlg.AcceptButton = btnOk;
                dlg.Controls.Add(btnOk);

                Button btnCancel = new Button { Text = Localization.Get(_currentLang).AppsEditorCancel, Location = new Point(258, 106), Size = new Size(86, 30), FlatStyle = FlatStyle.Flat, BackColor = c.BtnApp, ForeColor = c.BtnAppText, Font = _fontSection, DialogResult = DialogResult.Cancel };
                btnCancel.FlatAppearance.BorderSize = 1;
                btnCancel.FlatAppearance.BorderColor = c.BtnAppBorder;
                dlg.CancelButton = btnCancel;
                dlg.Controls.Add(btnCancel);

                dlg.Shown += (s, e) => { txt.Focus(); txt.SelectAll(); };
                if (dlg.ShowDialog(this) == DialogResult.OK)
                {
                    return txt.Text.Trim();
                }
            }
            return null;
        }

        private void SaveAppsConfigFile()
        {
            try
            {
                JavaScriptSerializer js = new JavaScriptSerializer();
                string json = js.Serialize(_appButtons);
                if (!Directory.Exists(_userConfigRoot)) Directory.CreateDirectory(_userConfigRoot);
                File.WriteAllText(_appsConfigFile, json, System.Text.Encoding.UTF8);
            }
            catch {}
        }

        private void OpenAppsEditor()
        {
            ThemeColors c = _isDarkMode ? ThemeColors.Dark : ThemeColors.Light;
            Localization.Strings t = Localization.Get(_currentLang);
            using (AppsEditorForm editor = new AppsEditorForm(_appButtons, _adb, c, t, _appsConfigFile, Icon))
            {
                if (editor.ShowDialog(this) == DialogResult.OK)
                {
                    UpdateAppButtonGrid();
                    DownloadMissingIconsAsync();
                }
            }
        }

        private async void FilterCustomSuggestions()
        {
            string query = (_txtCustom.Text ?? "").Trim();
            if (query.Length < 1)
            {
                _lstCustomSuggestions.Visible = false;
                return;
            }

            HashSet<string> pool = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (AppEntry a in _appButtons)
            {
                if (!string.IsNullOrEmpty(a.package)) pool.Add(a.package);
            }

            List<string> installed = await _adb.GetInstalledPackagesAsync();
            if (installed != null)
            {
                foreach (string p in installed) pool.Add(p);
            }

            string[] tokens = query.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            List<string> matches = new List<string>();
            foreach (string p in pool)
            {
                bool all = true;
                foreach (string tok in tokens)
                {
                    if (p.IndexOf(tok, StringComparison.OrdinalIgnoreCase) < 0) { all = false; break; }
                }
                if (all) matches.Add(p);
            }
            matches.Sort();

            if (matches.Count == 0)
            {
                _lstCustomSuggestions.Visible = false;
                return;
            }

            _lstCustomSuggestions.BeginUpdate();
            try
            {
                _lstCustomSuggestions.Items.Clear();
                int count = Math.Min(matches.Count, 30);
                for (int i = 0; i < count; i++) _lstCustomSuggestions.Items.Add(matches[i]);
                _lstCustomSuggestions.SelectedIndex = -1;
            }
            finally
            {
                _lstCustomSuggestions.EndUpdate();
            }

            int itemH = Math.Max(_lstCustomSuggestions.ItemHeight, 20);
            int visCount = Math.Min(matches.Count, 6);
            _lstCustomSuggestions.Height = Math.Min((visCount * itemH) + 4, 118);
            _lstCustomSuggestions.BringToFront();
            _lstCustomSuggestions.Visible = true;
        }

        private void OnTxtCustomKeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Down && _lstCustomSuggestions.Visible)
            {
                if (_lstCustomSuggestions.Items.Count > 0)
                {
                    if (_lstCustomSuggestions.SelectedIndex < _lstCustomSuggestions.Items.Count - 1)
                        _lstCustomSuggestions.SelectedIndex++;
                    else
                        _lstCustomSuggestions.SelectedIndex = 0;
                }
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.Up && _lstCustomSuggestions.Visible)
            {
                if (_lstCustomSuggestions.Items.Count > 0)
                {
                    if (_lstCustomSuggestions.SelectedIndex > 0)
                        _lstCustomSuggestions.SelectedIndex--;
                    else
                        _lstCustomSuggestions.SelectedIndex = _lstCustomSuggestions.Items.Count - 1;
                }
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.Enter)
            {
                if (_lstCustomSuggestions.Visible && _lstCustomSuggestions.SelectedIndex >= 0)
                {
                    _txtCustom.Text = _lstCustomSuggestions.SelectedItem.ToString();
                    _txtCustom.SelectionStart = _txtCustom.Text.Length;
                    _lstCustomSuggestions.Visible = false;
                }
                else
                {
                    _lstCustomSuggestions.Visible = false;
                    LaunchCustomApp();
                }
                e.Handled = true;
                e.SuppressKeyPress = true;
            }
            else if (e.KeyCode == Keys.Escape && _lstCustomSuggestions.Visible)
            {
                _lstCustomSuggestions.Visible = false;
                e.Handled = true;
            }
        }

        private async Task SwitchToWirelessAdbAsync()
        {
            Localization.Strings t = Localization.Get(_currentLang);
            if (!await _adb.IsDeviceConnectedAsync()) { ShowNoDeviceWarning(); return; }

            string ip = await _adb.GetWifiIpAddressAsync();
            if (string.IsNullOrEmpty(ip))
            {
                MessageBox.Show(this, t.MsgWifiNoIp, t.WifiBtn, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            bool ok = await _adb.ConnectWifiAsync(ip);
            if (ok)
            {
                MessageBox.Show(this, string.Format(t.MsgWifiDone, ip), t.WifiBtn, MessageBoxButtons.OK, MessageBoxIcon.Information);
                RefreshDeviceStatusAsync();
            }
            else
            {
                MessageBox.Show(this, string.Format("Nie udało się nawiązać połączenia bezprzewodowego z adresem {0}:5555.", ip), t.WifiBtn, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ShowNoDeviceWarning()
        {
            Localization.Strings t = Localization.Get(_currentLang);
            MessageBox.Show(this, t.MsgNoDevice, t.NoDevice, MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }
}

