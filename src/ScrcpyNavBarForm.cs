using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace ScrcpyManager
{
    public class ScrcpyNavBarForm : Form
    {
        private readonly IntPtr _targetHwnd;
        private readonly Timer _trackTimer;
        private bool _isDarkMode = true;

        private Button _btnBack;
        private Button _btnHome;
        private Button _btnRecents;
        private ToolTip _tip;

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ExStyle |= 0x08000000; // WS_EX_NOACTIVATE
                cp.ExStyle |= 0x00000080; // WS_EX_TOOLWINDOW
                return cp;
            }
        }

        public IntPtr TargetHwnd
        {
            get { return _targetHwnd; }
        }

        public ScrcpyNavBarForm(IntPtr scrcpyHwnd, bool isDarkMode)
        {
            _targetHwnd = scrcpyHwnd;
            _isDarkMode = isDarkMode;

            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.Manual;
            Size = new Size(240, 36);
            MinimumSize = new Size(180, 36);
            DoubleBuffered = true;

            InitializeButtons();
            ApplyTheme();

            _trackTimer = new Timer();
            _trackTimer.Interval = 40;
            _trackTimer.Tick += OnTrackTimerTick;
            _trackTimer.Start();

            UpdatePosition();
        }

        private void InitializeButtons()
        {
            _tip = new ToolTip();

            _btnBack = CreateNavButton("◀", "Cofnij (ESC / Alt+B)", async (s, e) =>
            {
                await AdbService.SendKeyEventAsync(4);
            });

            _btnHome = CreateNavButton("●", "Ekran główny (Alt+H)", async (s, e) =>
            {
                await AdbService.SendKeyEventAsync(3);
            });

            _btnRecents = CreateNavButton("▢", "Ostatnie aplikacje (Alt+S)", async (s, e) =>
            {
                await AdbService.SendKeyEventAsync(187);
            });

            Controls.Add(_btnBack);
            Controls.Add(_btnHome);
            Controls.Add(_btnRecents);

            LayoutButtons();
            Resize += (s, e) => LayoutButtons();
        }

        private Button CreateNavButton(string text, string tooltip, EventHandler onClick)
        {
            Button btn = new Button
            {
                Text = text,
                Font = new Font("Segoe UI Symbol", 12f, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Size = new Size(58, 28)
            };
            btn.FlatAppearance.BorderSize = 0;
            btn.Click += onClick;
            _tip.SetToolTip(btn, tooltip);
            return btn;
        }

        private void LayoutButtons()
        {
            int btnWidth = 58;
            int btnHeight = 28;
            int spacing = 14;
            int totalWidth = (btnWidth * 3) + (spacing * 2);
            int startX = Math.Max(0, (ClientSize.Width - totalWidth) / 2);
            int startY = Math.Max(0, (ClientSize.Height - btnHeight) / 2);

            _btnBack.Location = new Point(startX, startY);
            _btnBack.Size = new Size(btnWidth, btnHeight);

            _btnHome.Location = new Point(startX + btnWidth + spacing, startY);
            _btnHome.Size = new Size(btnWidth, btnHeight);

            _btnRecents.Location = new Point(startX + (btnWidth + spacing) * 2, startY);
            _btnRecents.Size = new Size(btnWidth, btnHeight);
        }

        public void SetTheme(bool isDarkMode)
        {
            _isDarkMode = isDarkMode;
            ApplyTheme();
        }

        private void ApplyTheme()
        {
            Color bg = _isDarkMode ? Color.FromArgb(20, 22, 30) : Color.FromArgb(235, 238, 245);
            Color btnBg = _isDarkMode ? Color.FromArgb(32, 35, 48) : Color.FromArgb(255, 255, 255);
            Color text = _isDarkMode ? Color.FromArgb(225, 230, 245) : Color.FromArgb(30, 35, 45);

            BackColor = bg;

            Button[] buttons = { _btnBack, _btnHome, _btnRecents };
            foreach (var b in buttons)
            {
                b.BackColor = btnBg;
                b.ForeColor = text;
                b.FlatAppearance.MouseOverBackColor = _isDarkMode ? Color.FromArgb(48, 54, 75) : Color.FromArgb(215, 225, 240);
                b.FlatAppearance.MouseDownBackColor = _isDarkMode ? Color.FromArgb(60, 70, 98) : Color.FromArgb(190, 205, 230);
            }
        }

        private void OnTrackTimerTick(object sender, EventArgs e)
        {
            if (!NativeMethods.IsWindow(_targetHwnd))
            {
                _trackTimer.Stop();
                Close();
                return;
            }

            if (NativeMethods.IsIconic(_targetHwnd))
            {
                if (Visible) Visible = false;
                return;
            }

            if (!Visible) Visible = true;
            UpdatePosition();
        }

        private void UpdatePosition()
        {
            NativeMethods.RECT rect;
            if (!NativeMethods.GetWindowRect(_targetHwnd, out rect)) return;

            int targetWidth = rect.Width;
            if (targetWidth < 180) targetWidth = 180;

            int posX = rect.Left;
            int posY = rect.Bottom;
            int barHeight = 36;

            Screen currentScreen = Screen.FromRectangle(new Rectangle(rect.Left, rect.Top, rect.Width, rect.Height));
            if (posY + barHeight > currentScreen.WorkingArea.Bottom)
            {
                // If it overflows below working area, place inside bottom of window
                posY = rect.Bottom - barHeight - 8;
            }

            if (Location.X != posX || Location.Y != posY || Size.Width != targetWidth)
            {
                Location = new Point(posX, posY);
                Size = new Size(targetWidth, barHeight);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_trackTimer != null)
                {
                    _trackTimer.Stop();
                    _trackTimer.Dispose();
                }
                if (_tip != null) _tip.Dispose();
            }
            base.Dispose(disposing);
        }
    }

    public static class NavBarManager
    {
        private static readonly Dictionary<IntPtr, ScrcpyNavBarForm> _activeBars = new Dictionary<IntPtr, ScrcpyNavBarForm>();
        private static Timer _scanTimer;
        private static bool _enabled = true;
        private static bool _isDarkMode = true;

        public static bool Enabled
        {
            get { return _enabled; }
            set
            {
                _enabled = value;
                if (!_enabled) CloseAll();
            }
        }

        public static void Initialize(bool isDarkMode)
        {
            _isDarkMode = isDarkMode;
            if (_scanTimer == null)
            {
                _scanTimer = new Timer();
                _scanTimer.Interval = 300;
                _scanTimer.Tick += (s, e) => ScanActiveScrcpyWindows();
                _scanTimer.Start();
            }
        }

        public static void SetDarkMode(bool isDarkMode)
        {
            _isDarkMode = isDarkMode;
            lock (_activeBars)
            {
                foreach (var bar in _activeBars.Values)
                {
                    try { bar.SetTheme(_isDarkMode); } catch { }
                }
            }
        }

        public static void ScanActiveScrcpyWindows()
        {
            if (!_enabled) return;

            lock (_activeBars)
            {
                // Remove closed forms
                List<IntPtr> toRemove = new List<IntPtr>();
                foreach (var kvp in _activeBars)
                {
                    if (kvp.Value.IsDisposed || !NativeMethods.IsWindow(kvp.Key))
                    {
                        toRemove.Add(kvp.Key);
                    }
                }
                foreach (var k in toRemove)
                {
                    try { _activeBars[k].Dispose(); } catch { }
                    _activeBars.Remove(k);
                }

                // Check active scrcpy processes
                foreach (Process p in AdbService.ActiveProcesses)
                {
                    try
                    {
                        if (p.HasExited) continue;
                        IntPtr hwnd = p.MainWindowHandle;
                        if (hwnd != IntPtr.Zero && NativeMethods.IsWindow(hwnd))
                        {
                            if (!_activeBars.ContainsKey(hwnd))
                            {
                                ScrcpyNavBarForm bar = new ScrcpyNavBarForm(hwnd, _isDarkMode);
                                bar.Show();
                                _activeBars[hwnd] = bar;
                            }
                        }
                    }
                    catch { }
                }
            }
        }

        public static void CloseAll()
        {
            lock (_activeBars)
            {
                foreach (var bar in _activeBars.Values)
                {
                    try { bar.Close(); bar.Dispose(); } catch { }
                }
                _activeBars.Clear();
            }
        }

        public static void Shutdown()
        {
            if (_scanTimer != null)
            {
                _scanTimer.Stop();
                _scanTimer.Dispose();
                _scanTimer = null;
            }
            CloseAll();
        }
    }
}
