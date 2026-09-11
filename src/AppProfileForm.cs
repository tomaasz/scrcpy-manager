using System;
using System.Drawing;
using System.Windows.Forms;

namespace ScrcpyManager
{
    public sealed class AppProfileForm : Form
    {
        private readonly AppEntry _app;
        private readonly bool _polish;
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

            _name = AddText(table, 0, _polish ? "Nazwa kafelka" : "Tile name", app.name, colors);
            _preset = AddCombo(table, 1, _polish ? "Preset" : "Preset", new[]
            {
                _polish ? "Domyślny" : "Default",
                _polish ? "Praca / RDP" : "Work / RDP",
                _polish ? "Gra" : "Gaming",
                _polish ? "Oszczędny Wi-Fi" : "Wi-Fi saver",
                _polish ? "Prezentacja" : "Presentation",
                _polish ? "Terminal / Duży tekst" : "Terminal / Large text"
            }, colors);
            _display = AddCombo(table, 2, _polish ? "Rozdzielczość / DPI" : "Resolution / DPI", new[]
            {
                "1920x1080/320", "1920x1080/240", "1920x1080/160",
                "2560x1440/320", "2560x1440/240", "2560x1440/160", "2560x1440/140",
                "1280x720/240", "3840x2160/320", "3840x2160/240", "1080x2400"
            }, colors, editable: true);
            _fps = AddNumber(table, 3, _polish ? "Limit FPS" : "FPS limit", 15, 240, colors);
            _bitRate = AddCombo(table, 4, _polish ? "Bitrate obrazu" : "Video bitrate", new[] { "2M", "4M", "8M", "12M", "16M", "24M", "32M" }, colors);
            _codec = AddCombo(table, 5, _polish ? "Kodek obrazu" : "Video codec", new[] { "h264", "h265", "av1", "vp8", "vp9" }, colors);
            _orientation = AddCombo(table, 6, _polish ? "Orientacja" : "Orientation", new[] { "auto", "0", "90", "180", "270" }, colors);
            _keyboard = AddCombo(table, 7, _polish ? "Klawiatura" : "Keyboard", new[] { "sdk", "uhid", "disabled" }, colors);
            _mouse = AddCombo(table, 8, _polish ? "Mysz" : "Mouse", new[] { "sdk", "uhid", "disabled" }, colors);
            _audio = AddCombo(table, 9, _polish ? "Dźwięk" : "Audio", new[]
            {
                _polish ? "Ustawienie główne" : "Global setting",
                _polish ? "Zawsze włączony" : "Always enabled",
                _polish ? "Zawsze wyłączony" : "Always disabled"
            }, colors);

            FlowLayoutPanel toggles = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = false, WrapContents = true };
            table.SetColumnSpan(toggles, 2);
            table.Controls.Add(toggles, 0, 10);
            _alwaysOnTop = AddCheck(toggles, _polish ? "Zawsze na wierzchu" : "Always on top", colors);
            _borderless = AddCheck(toggles, _polish ? "Okno bez ramek" : "Borderless window", colors);
            _fullscreen = AddCheck(toggles, _polish ? "Pełny ekran" : "Fullscreen", colors);
            _turnScreenOff = AddCheck(toggles, _polish ? "Wyłącz ekran telefonu" : "Turn phone screen off", colors);
            _record = AddCheck(toggles, _polish ? "Nagrywaj sesję MP4" : "Record MP4 session", colors);
            _forwardClicks = AddCheck(toggles, _polish ? "Przekazuj wszystkie kliknięcia" : "Forward all clicks", colors);

            Label packageLabel = new Label
            {
                Text = (_polish ? "Pakiet: " : "Package: ") + app.package,
                Location = new Point(18, 525),
                Size = new Size(475, 18),
                ForeColor = colors.TextMuted,
                AutoEllipsis = true
            };
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
            Controls.Add(cancel);
            CancelButton = cancel;

            LoadProfile(profile);
            _preset.SelectedIndexChanged += (s, e) => ApplyPreset(_preset.SelectedIndex);
        }

        private TextBox AddText(TableLayoutPanel table, int row, string label, string value, ThemeColors c)
        {
            AddLabel(table, row, label, c);
            TextBox control = new TextBox { Dock = DockStyle.Fill, Text = value ?? "", BackColor = c.InputBg, ForeColor = c.InputText, BorderStyle = BorderStyle.FixedSingle };
            table.Controls.Add(control, 1, row);
            return control;
        }

        private ComboBox AddCombo(TableLayoutPanel table, int row, string label, string[] values, ThemeColors c, bool editable = false)
        {
            AddLabel(table, row, label, c);
            ComboBox control = new ComboBox
            {
                Dock = DockStyle.Fill,
                DropDownStyle = editable ? ComboBoxStyle.DropDown : ComboBoxStyle.DropDownList,
                BackColor = c.InputBg,
                ForeColor = c.InputText
            };
            control.Items.AddRange(values);
            table.Controls.Add(control, 1, row);
            return control;
        }

        private NumericUpDown AddNumber(TableLayoutPanel table, int row, string label, int min, int max, ThemeColors c)
        {
            AddLabel(table, row, label, c);
            NumericUpDown control = new NumericUpDown { Dock = DockStyle.Fill, Minimum = min, Maximum = max, BackColor = c.InputBg, ForeColor = c.InputText };
            table.Controls.Add(control, 1, row);
            return control;
        }

        private static void AddLabel(TableLayoutPanel table, int row, string text, ThemeColors c)
        {
            table.Controls.Add(new Label { Text = text, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, ForeColor = c.Text }, 0, row);
        }

        private static CheckBox AddCheck(FlowLayoutPanel panel, string text, ThemeColors c)
        {
            CheckBox check = new CheckBox { Text = text, AutoSize = true, ForeColor = c.Text, Margin = new Padding(3, 5, 12, 3) };
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
            // Work/RDP mirrors the tuning that used to be hardcoded for the Remote Desktop
            // package (higher bitrate + UHID keyboard/mouse for full key passthrough like
            // Ctrl+Alt+Del) so any remote-desktop app can opt into it, not just one package id.
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
