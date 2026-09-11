using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Web.Script.Serialization;
using System.Windows.Forms;

namespace ScrcpyManager
{
    public class AppsEditorForm : Form
    {
        // Trzymana w DataGridViewRow.Tag, wiąże wiersz z oryginalnym wpisem (a nie z aktualnie
        // wpisaną nazwą pakietu), aby profil uruchamiania nie "przeskakiwał" między aplikacjami,
        // gdy użytkownik edytuje pole pakietu na wartość zbieżną z inną istniejącą aplikacją.
        private sealed class RowExtra
        {
            public List<string> Flags = new List<string>();
            public AppLaunchProfile OriginalProfile;
        }

        private readonly List<AppEntry> _apps;
        private readonly AdbService _adb;
        private readonly ThemeColors _c;
        private readonly Localization.Strings _t;
        private readonly string _appsConfigPath;

        private TextBox _txtSearch;
        private Button _btnClearSearch;
        private ComboBox _cmbPhoneApps;
        private Button _btnRefreshApps;
        private Button _btnAddApp;
        private DataGridView _grid;
        private Button _btnRemoveApp;
        private Button _btnMoveUp;
        private Button _btnMoveDown;
        private Button _btnPopularApps;
        private Button _btnCancel;
        private Button _btnSave;

        private readonly List<string> _allPhonePackages = new List<string>();

        public static readonly List<AppEntry> DefaultPopularApps = new List<AppEntry>
        {
            new AppEntry("YouTube", "com.google.android.youtube"),
            new AppEntry("Spotify", "com.spotify.music"),
            new AppEntry("Chrome", "com.android.chrome", "-ForwardAllClicks"),
            new AppEntry("WhatsApp", "com.whatsapp"),
            new AppEntry("Messenger", "com.facebook.orca"),
            new AppEntry("Gmail", "com.google.android.gm"),
            new AppEntry("Mapy Google", "com.google.android.apps.maps"),
            new AppEntry("Wiadomości", "com.google.android.apps.messaging"),
            new AppEntry("Ustawienia", "com.android.settings"),
            new AppEntry("Claude", "com.anthropic.claude"),
            new AppEntry("Zdjęcia", "com.google.android.apps.photos"),
            new AppEntry("Facebook", "com.facebook.katana"),
            new AppEntry("Instagram", "com.instagram.android")
        };

        public AppsEditorForm(List<AppEntry> currentApps, AdbService adb, ThemeColors theme, Localization.Strings loc, string appsConfigPath, Icon appIcon)
        {
            _apps = currentApps ?? new List<AppEntry>();
            _adb = adb;
            _c = theme;
            _t = loc;
            _appsConfigPath = appsConfigPath;

            Text = _t.AppsEditorTitle;
            ClientSize = new Size(760, 506);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            BackColor = _c.Bg;
            ForeColor = _c.Text;
            Font = new Font("Segoe UI", 9f, FontStyle.Regular);
            if (appIcon != null) Icon = appIcon;

            NativeMethods.UseImmersiveDarkMode(Handle, theme == ThemeColors.Dark);

            InitializeComponents();
            LoadCurrentApps();
            LoadPhoneAppsAsync();
        }

        private void InitializeComponents()
        {
            Label lblIntro = new Label
            {
                Text = _t.AppsEditorIntro,
                Location = new Point(16, 12),
                Size = new Size(728, 20),
                ForeColor = _c.TextMuted,
                Font = new Font("Segoe UI", 9f, FontStyle.Regular)
            };
            Controls.Add(lblIntro);

            _txtSearch = new TextBox
            {
                Location = new Point(16, 36),
                Size = new Size(692, 26),
                Font = new Font("Segoe UI", 9f, FontStyle.Regular),
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = _c.InputBg,
                ForeColor = _c.InputText
            };
            _txtSearch.TextChanged += (s, e) => ApplyFilter();
            _txtSearch.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter)
                {
                    e.SuppressKeyPress = true;
                    AddSelectedApp();
                }
                else if (e.KeyCode == Keys.Down)
                {
                    e.SuppressKeyPress = true;
                    _cmbPhoneApps.Focus();
                    if (_cmbPhoneApps.Items.Count > 1)
                    {
                        if (_cmbPhoneApps.SelectedIndex <= 0) _cmbPhoneApps.SelectedIndex = 1;
                        _cmbPhoneApps.DroppedDown = true;
                    }
                }
                else if (e.KeyCode == Keys.Escape)
                {
                    if (!string.IsNullOrEmpty(_txtSearch.Text))
                    {
                        _txtSearch.Text = "";
                        e.SuppressKeyPress = true;
                    }
                }
            };
            Controls.Add(_txtSearch);
            NativeMethods.SetCueBanner(_txtSearch.Handle, _t.AppsEditorSearchPlaceholder);

            _btnClearSearch = new Button
            {
                Text = "✕",
                Location = new Point(714, 35),
                Size = new Size(30, 28),
                Font = new Font("Segoe UI", 8.2f, FontStyle.Regular),
                FlatStyle = FlatStyle.Flat,
                BackColor = _c.BtnMode,
                ForeColor = _c.BtnModeText,
                Cursor = Cursors.Hand
            };
            _btnClearSearch.FlatAppearance.BorderColor = _c.BtnModeBorder;
            _btnClearSearch.Click += (s, e) =>
            {
                _txtSearch.Text = "";
                _txtSearch.Focus();
            };
            Controls.Add(_btnClearSearch);

            _cmbPhoneApps = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Location = new Point(16, 70),
                Size = new Size(500, 28),
                DropDownWidth = 520,
                Font = new Font("Segoe UI", 9f, FontStyle.Regular),
                BackColor = _c.InputBg,
                ForeColor = _c.InputText
            };
            _cmbPhoneApps.Items.Add(_t.AppsEditorPhoneList);
            _cmbPhoneApps.SelectedIndex = 0;
            _cmbPhoneApps.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter)
                {
                    e.SuppressKeyPress = true;
                    AddSelectedApp();
                }
                else if (e.KeyCode == Keys.Up && _cmbPhoneApps.SelectedIndex <= 1)
                {
                    _txtSearch.Focus();
                }
            };
            Controls.Add(_cmbPhoneApps);

            _btnRefreshApps = new Button
            {
                Text = _t.AppsEditorRefresh,
                Location = new Point(522, 69),
                Size = new Size(104, 30),
                FlatStyle = FlatStyle.Flat,
                BackColor = _c.BtnTool,
                ForeColor = _c.BtnToolText,
                Cursor = Cursors.Hand
            };
            _btnRefreshApps.FlatAppearance.BorderColor = _c.BtnToolBorder;
            _btnRefreshApps.Click += (s, e) => LoadPhoneAppsAsync();
            Controls.Add(_btnRefreshApps);

            _btnAddApp = new Button
            {
                Text = _t.AppsEditorAdd,
                Location = new Point(634, 69),
                Size = new Size(110, 30),
                FlatStyle = FlatStyle.Flat,
                BackColor = _c.BtnHero,
                ForeColor = _c.BtnHeroText,
                Cursor = Cursors.Hand
            };
            _btnAddApp.FlatAppearance.BorderSize = 0;
            _btnAddApp.Click += (s, e) => AddSelectedApp();
            Controls.Add(_btnAddApp);

            // DataGridView
            _grid = new DataGridView
            {
                Location = new Point(16, 108),
                Size = new Size(728, 334),
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeRows = false,
                RowHeadersVisible = false,
                MultiSelect = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.None,
                BorderStyle = BorderStyle.FixedSingle,
                BackgroundColor = _c.Card,
                GridColor = _c.CardBorder,
                EnableHeadersVisualStyles = false
            };
            _grid.RowTemplate.Height = 28;
            _grid.ColumnHeadersDefaultCellStyle.BackColor = _c.ToggleBg;
            _grid.ColumnHeadersDefaultCellStyle.ForeColor = _c.Text;
            _grid.ColumnHeadersDefaultCellStyle.SelectionBackColor = _c.ToggleBg;
            _grid.DefaultCellStyle.BackColor = _c.InputBg;
            _grid.DefaultCellStyle.ForeColor = _c.InputText;
            _grid.DefaultCellStyle.SelectionBackColor = _c.ToggleChecked;
            _grid.DefaultCellStyle.SelectionForeColor = _c.InputText;

            DataGridViewTextBoxColumn colName = new DataGridViewTextBoxColumn
            {
                Name = "AppName",
                HeaderText = _t.AppsEditorName,
                Width = 180
            };
            _grid.Columns.Add(colName);

            DataGridViewTextBoxColumn colPackage = new DataGridViewTextBoxColumn
            {
                Name = "Package",
                HeaderText = _t.AppsEditorPackage,
                Width = 280
            };
            _grid.Columns.Add(colPackage);

            DataGridViewCheckBoxColumn colUhid = new DataGridViewCheckBoxColumn
            {
                Name = "UseUhid",
                HeaderText = _t.AppsEditorUhid,
                Width = 105,
                FlatStyle = FlatStyle.Flat
            };
            _grid.Columns.Add(colUhid);

            DataGridViewCheckBoxColumn colClicks = new DataGridViewCheckBoxColumn
            {
                Name = "ForwardClicks",
                HeaderText = _t.AppsEditorClicks,
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
                FlatStyle = FlatStyle.Flat
            };
            _grid.Columns.Add(colClicks);

            Controls.Add(_grid);

            // Dolne przyciski
            _btnRemoveApp = new Button
            {
                Text = _t.AppsEditorRemove,
                Location = new Point(16, 458),
                Size = new Size(90, 32),
                FlatStyle = FlatStyle.Flat,
                BackColor = _c.BtnReboot,
                ForeColor = _c.BtnRebootText,
                Cursor = Cursors.Hand
            };
            _btnRemoveApp.FlatAppearance.BorderColor = _c.BtnRebootBorder;
            _btnRemoveApp.Click += (s, e) =>
            {
                if (_grid.CurrentRow != null)
                {
                    _grid.Rows.RemoveAt(_grid.CurrentRow.Index);
                }
            };
            Controls.Add(_btnRemoveApp);

            _btnMoveUp = new Button
            {
                Text = _t.AppsEditorUp,
                Location = new Point(112, 458),
                Size = new Size(90, 32),
                FlatStyle = FlatStyle.Flat,
                BackColor = _c.BtnMode,
                ForeColor = _c.BtnModeText,
                Cursor = Cursors.Hand
            };
            _btnMoveUp.FlatAppearance.BorderColor = _c.BtnModeBorder;
            _btnMoveUp.Click += (s, e) => MoveRow(-1);
            Controls.Add(_btnMoveUp);

            _btnMoveDown = new Button
            {
                Text = _t.AppsEditorDown,
                Location = new Point(208, 458),
                Size = new Size(90, 32),
                FlatStyle = FlatStyle.Flat,
                BackColor = _c.BtnMode,
                ForeColor = _c.BtnModeText,
                Cursor = Cursors.Hand
            };
            _btnMoveDown.FlatAppearance.BorderColor = _c.BtnModeBorder;
            _btnMoveDown.Click += (s, e) => MoveRow(1);
            Controls.Add(_btnMoveDown);

            _btnPopularApps = new Button
            {
                Text = _t.AppsEditorPopular,
                Location = new Point(308, 458),
                Size = new Size(150, 32),
                FlatStyle = FlatStyle.Flat,
                BackColor = _c.BtnTool,
                ForeColor = _c.BtnToolText,
                Cursor = Cursors.Hand
            };
            _btnPopularApps.FlatAppearance.BorderColor = _c.BtnToolBorder;
            _btnPopularApps.Click += (s, e) => LoadPopularApps();
            Controls.Add(_btnPopularApps);

            _btnCancel = new Button
            {
                Text = _t.AppsEditorCancel,
                Location = new Point(550, 458),
                Size = new Size(92, 32),
                FlatStyle = FlatStyle.Flat,
                BackColor = _c.BtnMode,
                ForeColor = _c.BtnModeText,
                DialogResult = DialogResult.Cancel,
                Cursor = Cursors.Hand
            };
            _btnCancel.FlatAppearance.BorderColor = _c.BtnModeBorder;
            CancelButton = _btnCancel;
            Controls.Add(_btnCancel);

            _btnSave = new Button
            {
                Text = _t.AppsEditorSave,
                Location = new Point(652, 458),
                Size = new Size(92, 32),
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat,
                BackColor = _c.BtnHero,
                ForeColor = _c.BtnHeroText,
                Cursor = Cursors.Hand
            };
            _btnSave.FlatAppearance.BorderSize = 0;
            _btnSave.Click += (s, e) => SaveApps();
            Controls.Add(_btnSave);
        }

        private void LoadCurrentApps()
        {
            _grid.Rows.Clear();
            foreach (AppEntry app in _apps)
            {
                bool uhid = app.flags != null && app.flags.Contains("-UseUhidKeyboard");
                bool clicks = app.flags != null && app.flags.Contains("-ForwardAllClicks");
                int ri = _grid.Rows.Add(app.name, app.package, uhid, clicks);

                List<string> extraFlags = new List<string>();
                if (app.flags != null)
                {
                    foreach (string f in app.flags)
                    {
                        if (f != "-UseUhidKeyboard" && f != "-ForwardAllClicks")
                        {
                            extraFlags.Add(f);
                        }
                    }
                }
                _grid.Rows[ri].Tag = new RowExtra { Flags = extraFlags, OriginalProfile = app.profile };
            }
        }

        private async void LoadPhoneAppsAsync()
        {
            UseWaitCursor = true;
            try
            {
                List<string> packages = await _adb.GetInstalledPackagesAsync();
                _allPhonePackages.Clear();
                if (packages != null && packages.Count > 0)
                {
                    _allPhonePackages.AddRange(packages);
                }
                ApplyFilter();
                if (_allPhonePackages.Count == 0)
                {
                    MessageBox.Show(this, _t.MsgAppsPhoneError, _t.AppsEditorTitle, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            finally
            {
                UseWaitCursor = false;
            }
        }

        private void ApplyFilter()
        {
            string query = (_txtSearch.Text ?? "").Trim();
            _cmbPhoneApps.BeginUpdate();
            try
            {
                _cmbPhoneApps.Items.Clear();
                if (_allPhonePackages.Count == 0)
                {
                    _cmbPhoneApps.Items.Add(_t.AppsEditorPhoneList);
                    _cmbPhoneApps.SelectedIndex = 0;
                    return;
                }

                string[] tokens = query.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                List<string> filtered = new List<string>();
                if (tokens.Length == 0)
                {
                    filtered.AddRange(_allPhonePackages);
                }
                else
                {
                    foreach (string pkg in _allPhonePackages)
                    {
                        bool allMatch = true;
                        foreach (string tok in tokens)
                        {
                            if (pkg.IndexOf(tok, StringComparison.OrdinalIgnoreCase) < 0)
                            {
                                allMatch = false;
                                break;
                            }
                        }
                        if (allMatch) filtered.Add(pkg);
                    }
                }

                if (filtered.Count > 0)
                {
                    string headerText = tokens.Length == 0 ? _t.AppsEditorPhoneList : string.Format("{0} ({1})", _t.AppsEditorPhoneList, filtered.Count);
                    _cmbPhoneApps.Items.Add(headerText);
                    foreach (string pkg in filtered)
                    {
                        _cmbPhoneApps.Items.Add(pkg);
                    }
                    _cmbPhoneApps.SelectedIndex = tokens.Length > 0 ? 1 : 0;
                }
                else
                {
                    _cmbPhoneApps.Items.Add(_t.AppsEditorNoMatches);
                    _cmbPhoneApps.SelectedIndex = 0;
                }
            }
            finally
            {
                _cmbPhoneApps.EndUpdate();
            }
        }

        private void AddSelectedApp()
        {
            string package = "";
            if (_cmbPhoneApps.SelectedIndex > 0)
            {
                package = _cmbPhoneApps.SelectedItem.ToString();
            }
            else if (!string.IsNullOrWhiteSpace(_txtSearch.Text))
            {
                string cand = _txtSearch.Text.Trim();
                if (AdbService.IsValidPackageName(cand))
                {
                    package = cand;
                }
            }

            if (string.IsNullOrWhiteSpace(package)) return;

            string[] parts = package.Split('.');
            string defaultName = parts[parts.Length - 1];
            int ri = _grid.Rows.Add(defaultName, package, false, false);
            _grid.Rows[ri].Tag = new RowExtra();
            _grid.CurrentCell = _grid.Rows[ri].Cells[0];
            _grid.BeginEdit(true);
        }

        private void MoveRow(int offset)
        {
            if (_grid.CurrentRow == null) return;
            _grid.EndEdit();
            int src = _grid.CurrentRow.Index;
            int tgt = src + offset;
            if (tgt < 0 || tgt >= _grid.Rows.Count) return;

            object[] vals = new object[_grid.Columns.Count];
            for (int i = 0; i < _grid.Columns.Count; i++)
            {
                vals[i] = _grid.Rows[src].Cells[i].Value;
            }
            object tag = _grid.Rows[src].Tag;

            for (int i = 0; i < _grid.Columns.Count; i++)
            {
                _grid.Rows[src].Cells[i].Value = _grid.Rows[tgt].Cells[i].Value;
                _grid.Rows[tgt].Cells[i].Value = vals[i];
            }
            _grid.Rows[src].Tag = _grid.Rows[tgt].Tag;
            _grid.Rows[tgt].Tag = tag;

            _grid.CurrentCell = _grid.Rows[tgt].Cells[0];
        }

        private void LoadPopularApps()
        {
            HashSet<string> existing = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (DataGridViewRow row in _grid.Rows)
            {
                string p = Convert.ToString(row.Cells["Package"].Value);
                if (!string.IsNullOrWhiteSpace(p)) existing.Add(p.Trim());
            }

            if (_grid.Rows.Count > 0)
            {
                if (MessageBox.Show(this, _t.MsgLoadPopular, _t.AppsEditorPopular, MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                {
                    return;
                }
            }

            foreach (AppEntry app in DefaultPopularApps)
            {
                if (!existing.Contains(app.package))
                {
                    bool uhid = app.flags != null && app.flags.Contains("-UseUhidKeyboard");
                    bool clicks = app.flags != null && app.flags.Contains("-ForwardAllClicks");
                    int ri = _grid.Rows.Add(app.name, app.package, uhid, clicks);
                    _grid.Rows[ri].Tag = new RowExtra { OriginalProfile = app.profile };
                }
            }

            if (_grid.Rows.Count > 0)
            {
                _grid.CurrentCell = _grid.Rows[_grid.Rows.Count - 1].Cells[0];
            }
        }

        private void SaveApps()
        {
            _grid.EndEdit();
            if (_grid.Rows.Count == 0)
            {
                MessageBox.Show(this, _t.MsgAppsEmpty, _t.AppsEditorTitle, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            List<AppEntry> updated = new List<AppEntry>();
            foreach (DataGridViewRow row in _grid.Rows)
            {
                string name = Convert.ToString(row.Cells["AppName"].Value).Trim();
                string package = Convert.ToString(row.Cells["Package"].Value).Trim();
                if (string.IsNullOrWhiteSpace(name) || !AdbService.IsValidPackageName(package))
                {
                    _grid.CurrentCell = row.Cells[0];
                    MessageBox.Show(this, _t.MsgAppsInvalid, _t.AppsEditorTitle, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                List<string> flags = new List<string>();
                RowExtra rowExtra = row.Tag as RowExtra;
                if (rowExtra != null && rowExtra.Flags != null) flags.AddRange(rowExtra.Flags);

                bool useUhid = Convert.ToBoolean(row.Cells["UseUhid"].Value);
                bool forwardClicks = Convert.ToBoolean(row.Cells["ForwardClicks"].Value);
                if (useUhid) flags.Add("-UseUhidKeyboard");
                if (forwardClicks) flags.Add("-ForwardAllClicks");

                AppEntry entry = new AppEntry(name, package, flags.ToArray());
                AppLaunchProfile previousProfile = rowExtra != null ? rowExtra.OriginalProfile : null;
                entry.profile = previousProfile != null ? previousProfile.Clone() : new AppLaunchProfile();
                // Pola powiązane z checkboxami edytora są jednoznacznie wyznaczane przez ich
                // aktualny stan (włącz/wyłącz), a nie tylko przez obecność starej flagi -UseUhidKeyboard.
                entry.profile.keyboardMode = useUhid ? "uhid" : (entry.profile.keyboardMode == "uhid" ? "sdk" : entry.profile.keyboardMode);
                entry.profile.forwardAllClicks = forwardClicks;
                updated.Add(entry);
            }

            try
            {
                ConfigStore.SaveAtomic(_appsConfigPath, updated);

                _apps.Clear();
                _apps.AddRange(updated);

                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, string.Format(_t.MsgAppsSaveError, ex.Message), _t.AppsEditorTitle, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
