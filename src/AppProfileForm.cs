using System;
using System.Drawing;
using System.Windows.Forms;

namespace ScrcpyManager
{
    public sealed class AppProfileForm : Form
    {
        private readonly AppEntry _app;
        private readonly bool _polish;
        private readonly ToolTip _toolTip;
        private readonly TextBox _name;
        private readonly ComboBox _preset;
        private readonly ComboBox _display;
        private readonly NumericUpDown _fps;
        private readonly ComboBox _bitRate;
        private readonly ComboBox _codec;
        private readonly ComboBox _orientation;
        private readonly ComboBox _keyboard;
        private readonly ComboBox _mouse;
        private readonly ComboBox _audio;
        private readonly CheckBox _alwaysOnTop;
        private readonly CheckBox _borderless;
        private readonly CheckBox _turnScreenOff;
        private readonly CheckBox _record;
        private readonly CheckBox _forwardClicks;
        private readonly CheckBox _fullscreen;

        public AppProfileForm(AppEntry app, ThemeColors colors, string language, Icon icon)
        {
            _app = app;
            _polish = string.Equals(language, "PL", StringComparison.OrdinalIgnoreCase);
            AppLaunchProfile profile = app.profile ?? new AppLaunchProfile();

            Text = _polish ? "Profil uruchamiania aplikacji" : "Application launch profile";
            ClientSize = new Size(510, 595);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            BackColor = colors.Bg;
            ForeColor = colors.Text;
            Font = new Font("Segoe UI", 9f);
            if (icon != null) Icon = icon;
            NativeMethods.UseImmersiveDarkMode(Handle, colors == ThemeColors.Dark);

            _toolTip = new ToolTip
            {
                AutoPopDelay = 12000,
                InitialDelay = 350,
                ReshowDelay = 150,
                ShowAlways = true
            };

            TableLayoutPanel table = new TableLayoutPanel
            {
                Location = new Point(16, 14),
                Size = new Size(478, 502),
                ColumnCount = 2,
                RowCount = 12,
                BackColor = Color.Transparent
            };
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 175));
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            for (int i = 0; i < 12; i++) table.RowStyles.Add(new RowStyle(SizeType.Absolute, i == 10 ? 96 : 34));
            Controls.Add(table);

            _name = AddText(table, 0, _polish ? "Nazwa kafelka" : "Tile name", app.name, colors,
                _polish ? "Wyświetlana nazwa kafelka w menedżerze oraz tytuł tworzonego okna scrcpy."
                        : "Display name on the dashboard tile and the title of the created scrcpy window.");

            _preset = AddCombo(table, 1, _polish ? "Preset" : "Preset", new[]
            {
                _polish ? "Domyślny" : "Default",
                _polish ? "Praca / RDP" : "Work / RDP",
                _polish ? "Gra" : "Gaming",
                _polish ? "Oszczędny Wi-Fi" : "Wi-Fi saver",
                _polish ? "Prezentacja" : "Presentation",
                _polish ? "Terminal / Duży tekst" : "Terminal / Large text"
            }, colors, editable: false, tooltip:
                _polish ? "Szybki zestaw ustawień zoptymalizowany dla konkretnych scenariuszy. Wybranie presetu automatycznie uzupełnia poniższe pola."
                        : "Preconfigured quality profile. Selecting a preset automatically configures the fields below.");

            _display = AddCombo(table, 2, _polish ? "Rozdzielczość / DPI" : "Resolution / DPI", new[]
            {
                "1920x1080/320", "1920x1080/240", "1920x1080/160",
                "2560x1440/320", "2560x1440/240", "2560x1440/160", "2560x1440/140",
                "1280x720/240", "3840x2160/320", "3840x2160/240", "1080x2400"
            }, colors, editable: true, tooltip:
                _polish ? "Rozdzielczość wirtualnego ekranu i gęstość DPI w formacie SZERxWYS/DPI (np. 1920x1080/320).\n• Wyższa wartość DPI (np. /320 zamiast /160) powiększa tekst, przyciski i cały interfejs aplikacji.\n• Możesz wybrać opcję z listy lub wpisać własne wartości."
                        : "Virtual display resolution and DPI density (WIDTHxHEIGHT/DPI format).\n• Higher DPI (e.g. /320 vs /160) enlarges text, buttons, and app interface.\n• You can pick from the list or type custom values.");

            _fps = AddNumber(table, 3, _polish ? "Limit FPS" : "FPS limit", 15, 240, colors,
                _polish ? "Maksymalna liczba klatek na sekundę strumieniowanych ze scrcpy (15–240).\n• 60 FPS zapewnia pełną płynność animacji.\n• Mniejsze wartości (np. 30 FPS) redukują obciążenie procesora, baterię i pasmo Wi-Fi."
                        : "Maximum streamed frames per second (15–240).\n• 60 FPS provides smooth motion.\n• Lower values (e.g. 30 FPS) save CPU, battery, and Wi-Fi bandwidth.");

            _bitRate = AddCombo(table, 4, _polish ? "Bitrate obrazu" : "Video bitrate", new[] { "2M", "4M", "8M", "12M", "16M", "24M", "32M" }, colors, editable: false, tooltip:
                _polish ? "Przepustowość kodowania wideo (np. 8M, 12M, 16M).\n• Wyższy bitrate eliminuje rozmycia i artefakty wokół drobnego tekstu.\n• Niższy bitrate (np. 4M) jest zalecany przy słabym sygnale Wi-Fi."
                        : "Video streaming bitrate (e.g. 8M, 12M, 16M).\n• Higher bitrate produces crisp text without compression artifacts.\n• Lower bitrate is recommended on weaker Wi-Fi networks.");

            _codec = AddCombo(table, 5, _polish ? "Kodek obrazu" : "Video codec", new[] { "h264", "h265", "av1", "vp8", "vp9" }, colors, editable: false, tooltip:
                _polish ? "Format kompresji strumienia wideo:\n• h264: maksymalna kompatybilność ze wszystkimi urządzeniami.\n• h265 / av1: lepsza jakość obrazu przy mniejszym zużyciu sieci (wymaga sprzętowego dekodera w PC)."
                        : "Video compression format:\n• h264: universal compatibility.\n• h265 / av1: superior visual quality at lower bitrates (requires hardware decoder).");

            _orientation = AddCombo(table, 6, _polish ? "Orientacja" : "Orientation", new[] { "auto", "0", "90", "180", "270" }, colors, editable: false, tooltip:
                _polish ? "Wymuszenie orientacji wirtualnego ekranu:\n• auto: automatycznie według preferencji aplikacji,\n• 0: pionowa (portrait),\n• 90 / 270: pozioma (landscape),\n• 180: pionowa odwrócona."
                        : "Display orientation lock:\n• auto: app default,\n• 0: portrait,\n• 90 / 270: landscape,\n• 180: reverse portrait.");

            _keyboard = AddCombo(table, 7, _polish ? "Klawiatura" : "Keyboard", new[] { "sdk", "uhid", "disabled" }, colors, editable: false, tooltip:
                _polish ? "Sposób przesyłania naciśnięć klawiatury:\n• uhid: symulacja fizycznej klawiatury USB na telefonie (obsługuje wszystkie skróty terminala, Ctrl+C, Ctrl+D, Alt, ESC i polskie znaki z AltGr),\n• sdk: wprowadzanie znaków przez klawiaturę ekranową Androida (IME),\n• disabled: klawiatura wyłączona."
                        : "Keyboard simulation mode:\n• uhid: hardware USB keyboard simulation (supports terminal shortcuts, Alt, ESC, and AltGr diacritics),\n• sdk: characters injected via Android IME,\n• disabled: keyboard input disabled.");

            _mouse = AddCombo(table, 8, _polish ? "Mysz" : "Mouse", new[] { "sdk", "uhid", "disabled" }, colors, editable: false, tooltip:
                _polish ? "Sposób obsługi myszy:\n• sdk: kliknięcia są traktowane jak dotyk palcem na ekranie telefonu,\n• uhid: komputerowa mysz fizyczna podłączona do telefonu,\n• disabled: kursor wyłączony."
                        : "Mouse simulation mode:\n• sdk: clicks simulate touchscreen taps,\n• uhid: raw physical USB mouse simulation,\n• disabled: mouse input disabled.");

            _audio = AddCombo(table, 9, _polish ? "Dźwięk" : "Audio", new[]
            {
                _polish ? "Ustawienie główne" : "Global setting",
                _polish ? "Zawsze włączony" : "Always enabled",
                _polish ? "Zawsze wyłączony" : "Always disabled"
            }, colors, editable: false, tooltip:
                _polish ? "Przesyłanie dźwięku z Androida:\n• Ustawienie główne: zgodnie z przełącznikiem 'Przesyłaj dźwięk' w oknie głównym,\n• Zawsze włączony: dźwięk z tej aplikacji zawsze trafia do głośników PC,\n• Zawsze wyłączony: całkowite wyciszenie dźwięku scrcpy."
                        : "Audio playback forwarding:\n• Global setting: follows main window audio toggle,\n• Always enabled: sound from this app always streams to PC speakers,\n• Always disabled: audio muted.");

            FlowLayoutPanel toggles = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = false, WrapContents = true };
            table.SetColumnSpan(toggles, 2);
            table.Controls.Add(toggles, 0, 10);
            _alwaysOnTop = AddCheck(toggles, _polish ? "Zawsze na wierzchu" : "Always on top", colors,
                _polish ? "Utrzymuje okno aplikacji nad wszystkimi innymi otwartymi oknami w systemie Windows."
                        : "Keeps this application window floating above all other desktop windows.");

            _borderless = AddCheck(toggles, _polish ? "Okno bez ramek" : "Borderless window", colors,
                _polish ? "Ukrywa pasek tytułowy i ramki systemowe Windows, tworząc czyste, pływające okno."
                        : "Removes window borders and title bar for a modern borderless look.");

            _fullscreen = AddCheck(toggles, _polish ? "Pełny ekran" : "Fullscreen", colors,
                _polish ? "Uruchamia aplikację natychmiast w trybie pełnoekranowym (skrót do wyjścia: Lewy Alt + F)."
                        : "Launches the application directly in fullscreen mode (toggle: Left Alt + F).");

            _turnScreenOff = AddCheck(toggles, _polish ? "Wyłącz ekran telefonu" : "Turn phone screen off", colors,
                _polish ? "Wyłącza fizyczny wyświetlacz telefonu podczas korzystania z aplikacji na PC. Oszczędza baterię i zapobiega nagrzewaniu się urządzenia."
                        : "Turns off the physical device screen while streaming. Saves battery and reduces heating.");

            _record = AddCheck(toggles, _polish ? "Nagrywaj sesję MP4" : "Record MP4 session", colors,
                _polish ? "Automatycznie rejestruje całą sesję do pliku wideo MP4 w folderze 'Wideo\\scrcpy-manager'."
                        : "Records the session directly into timestamped MP4 files in 'Videos\\scrcpy-manager'.");

            _forwardClicks = AddCheck(toggles, _polish ? "Przekazuj wszystkie kliknięcia" : "Forward all clicks", colors,
                _polish ? "Przesyła prawy przycisk myszy i kółko bezpośrednio do aplikacji Androida zamiast wykonywać akcje systemowe scrcpy (np. cofanie)."
                        : "Passes right-click and middle-click directly into the app instead of triggering scrcpy shortcuts.");

            Label packageLabel = new Label
            {
                Text = (_polish ? "Pakiet: " : "Package: ") + app.package,
                Location = new Point(18, 525),
                Size = new Size(475, 18),
                ForeColor = colors.TextMuted,
                AutoEllipsis = true
            };
            _toolTip.SetToolTip(packageLabel, _polish ? "Identyfikator pakietu Androida uruchamianego w tym oknie." : "Android package identifier executed in this window.");
            Controls.Add(packageLabel);

            Button save = new Button
            {
                Text = _polish ? "Zapisz profil" : "Save profile",
                Location = new Point(276, 555),
                Size = new Size(126, 30),
                BackColor = colors.BtnHero,
                ForeColor = colors.BtnHeroText,
                FlatStyle = FlatStyle.Flat
            };
            save.FlatAppearance.BorderSize = 0;
            save.Click += OnSave;
            _toolTip.SetToolTip(save, _polish ? "Zapisuje profil i stosuje konfigurację przy kolejnych uruchomieniach kafelka." : "Saves profile and applies configuration on next launch.");
            Controls.Add(save);
            AcceptButton = save;

            Button cancel = new Button
            {
                Text = _polish ? "Anuluj" : "Cancel",
                Location = new Point(410, 555),
                Size = new Size(84, 30),
                BackColor = colors.BtnApp,
                ForeColor = colors.BtnAppText,
                FlatStyle = FlatStyle.Flat,
                DialogResult = DialogResult.Cancel
            };
            _toolTip.SetToolTip(cancel, _polish ? "Zamyka okno bez zapisywania zmian." : "Closes window without saving changes.");
            Controls.Add(cancel);
            CancelButton = cancel;

            LoadProfile(profile);
            _preset.SelectedIndexChanged += (s, e) => ApplyPreset(_preset.SelectedIndex);
        }

        private Label AddLabel(TableLayoutPanel table, int row, string text, ThemeColors c, string tooltip = null)
        {
            Label lbl = new Label { Text = text, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, ForeColor = c.Text };
            if (!string.IsNullOrEmpty(tooltip)) _toolTip.SetToolTip(lbl, tooltip);
            table.Controls.Add(lbl, 0, row);
            return lbl;
        }

        private TextBox AddText(TableLayoutPanel table, int row, string label, string value, ThemeColors c, string tooltip = null)
        {
            AddLabel(table, row, label, c, tooltip);
            TextBox control = new TextBox { Dock = DockStyle.Fill, Text = value ?? "", BackColor = c.InputBg, ForeColor = c.InputText, BorderStyle = BorderStyle.FixedSingle };
            if (!string.IsNullOrEmpty(tooltip)) _toolTip.SetToolTip(control, tooltip);
            table.Controls.Add(control, 1, row);
            return control;
        }

        private ComboBox AddCombo(TableLayoutPanel table, int row, string label, string[] values, ThemeColors c, bool editable = false, string tooltip = null)
        {
            AddLabel(table, row, label, c, tooltip);
            ComboBox control = new ComboBox
            {
                Dock = DockStyle.Fill,
                DropDownStyle = editable ? ComboBoxStyle.DropDown : ComboBoxStyle.DropDownList,
                BackColor = c.InputBg,
                ForeColor = c.InputText
            };
            control.Items.AddRange(values);
            if (!string.IsNullOrEmpty(tooltip)) _toolTip.SetToolTip(control, tooltip);
            table.Controls.Add(control, 1, row);
            return control;
        }

        private NumericUpDown AddNumber(TableLayoutPanel table, int row, string label, int min, int max, ThemeColors c, string tooltip = null)
        {
            AddLabel(table, row, label, c, tooltip);
            NumericUpDown control = new NumericUpDown { Dock = DockStyle.Fill, Minimum = min, Maximum = max, BackColor = c.InputBg, ForeColor = c.InputText };
            if (!string.IsNullOrEmpty(tooltip)) _toolTip.SetToolTip(control, tooltip);
            table.Controls.Add(control, 1, row);
            return control;
        }

        private CheckBox AddCheck(FlowLayoutPanel panel, string text, ThemeColors c, string tooltip = null)
        {
            CheckBox check = new CheckBox { Text = text, AutoSize = true, ForeColor = c.Text, Margin = new Padding(3, 5, 12, 3) };
            if (!string.IsNullOrEmpty(tooltip)) _toolTip.SetToolTip(check, tooltip);
            panel.Controls.Add(check);
            return check;
        }

        private void LoadProfile(AppLaunchProfile p)
        {
            _preset.SelectedIndex = PresetIndex(p.preset);
            Select(_display, p.displaySize, "2560x1440/160");
            _fps.Value = Math.Max(_fps.Minimum, Math.Min(_fps.Maximum, p.maxFps > 0 ? p.maxFps : 60));
            Select(_bitRate, p.videoBitRate, "8M");
            Select(_codec, p.videoCodec, "h264");
            Select(_orientation, p.orientation, "auto");
            Select(_keyboard, p.keyboardMode, "sdk");
            Select(_mouse, p.mouseMode, "sdk");
            _audio.SelectedIndex = p.audioMode == "on" ? 1 : (p.audioMode == "off" ? 2 : 0);
            _alwaysOnTop.Checked = p.alwaysOnTop;
            _borderless.Checked = p.borderless;
            _fullscreen.Checked = p.fullscreen;
            _turnScreenOff.Checked = p.turnScreenOff;
            _record.Checked = p.recordSession;
            _forwardClicks.Checked = p.forwardAllClicks;
        }

        private static void Select(ComboBox combo, string value, string fallback)
        {
            if (string.IsNullOrEmpty(value)) value = fallback;
            int index = combo.Items.IndexOf(value);
            if (index >= 0)
            {
                combo.SelectedIndex = index;
            }
            else if (combo.DropDownStyle == ComboBoxStyle.DropDown)
            {
                combo.Text = value;
            }
            else
            {
                index = combo.Items.IndexOf(fallback);
                combo.SelectedIndex = Math.Max(0, index);
            }
        }

        private static int PresetIndex(string preset)
        {
            if (preset == "work") return 1;
            if (preset == "gaming") return 2;
            if (preset == "wifi") return 3;
            if (preset == "presentation") return 4;
            if (preset == "terminal") return 5;
            return 0;
        }

        private void ApplyPreset(int index)
        {
            if (index == 1) SetQuality("2560x1440/160", 60, "16M", "h264", false, false, "uhid", "uhid", true);
            else if (index == 2) SetQuality("1920x1080/160", 90, "16M", "h264", false, false, "sdk", "sdk", false);
            else if (index == 3) SetQuality("1920x1080/160", 30, "4M", "h264", false, false, "sdk", "sdk", false);
            else if (index == 4) SetQuality("2560x1440/160", 60, "8M", "h264", true, true, "sdk", "sdk", false);
            else if (index == 5) SetQuality("1920x1080/320", 60, "12M", "h264", false, false, "uhid", "sdk", false);
            else SetQuality("2560x1440/160", 60, "8M", "h264", false, false, "sdk", "sdk", false);
        }

        private void SetQuality(string display, int fps, string bitRate, string codec, bool top, bool borderless,
            string keyboardMode, string mouseMode, bool forwardClicks)
        {
            Select(_display, display, "2560x1440/160");
            _fps.Value = fps;
            Select(_bitRate, bitRate, "8M");
            Select(_codec, codec, "h264");
            _alwaysOnTop.Checked = top;
            _borderless.Checked = borderless;
            Select(_keyboard, keyboardMode, "sdk");
            Select(_mouse, mouseMode, "sdk");
            _forwardClicks.Checked = forwardClicks;
        }

        private void OnSave(object sender, EventArgs e)
        {
            string name = (_name.Text ?? "").Trim();
            if (name.Length == 0 || name.Length > 128 || !AdbService.IsValidDisplaySize(_display.Text))
            {
                MessageBox.Show(this, _polish ? "Sprawdź nazwę i rozdzielczość profilu." : "Check the profile name and resolution.", Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            _app.name = name;
            AppLaunchProfile p = _app.profile ?? new AppLaunchProfile();
            p.preset = new[] { "default", "work", "gaming", "wifi", "presentation", "terminal" }[_preset.SelectedIndex];
            p.displaySize = _display.Text;
            p.maxFps = (int)_fps.Value;
            p.videoBitRate = _bitRate.Text;
            p.videoCodec = _codec.Text;
            p.orientation = _orientation.Text;
            p.keyboardMode = _keyboard.Text;
            p.mouseMode = _mouse.Text;
            p.audioMode = _audio.SelectedIndex == 1 ? "on" : (_audio.SelectedIndex == 2 ? "off" : "global");
            p.alwaysOnTop = _alwaysOnTop.Checked;
            p.borderless = _borderless.Checked;
            p.fullscreen = _fullscreen.Checked;
            p.turnScreenOff = _turnScreenOff.Checked;
            p.recordSession = _record.Checked;
            p.forwardAllClicks = _forwardClicks.Checked;
            _app.profile = p;

            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
