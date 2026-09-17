using System;
using System.Diagnostics;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ScrcpyManager
{
    public sealed class DeviceGuideForm : Form
    {
        private readonly ThemeColors _c;
        private readonly Localization.Strings _t;
        private readonly AdbService _adb;
        private readonly Label _lblAdbStatus;
        private readonly Button _btnRestartAdb;

        public DeviceGuideForm(ThemeColors colors, string language, Icon icon, AdbService adb)
        {
            _c = colors;
            _t = Localization.Get(language);
            _adb = adb;

            Text = _t.GuideTitle;
            ClientSize = new Size(500, 560);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            BackColor = _c.Bg;
            ForeColor = _c.Text;
            Font = new Font("Segoe UI", 9f);
            if (icon != null) Icon = icon;
            NativeMethods.UseImmersiveDarkMode(Handle, _c == ThemeColors.Dark);

            Panel pnlMain = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(16, 14, 16, 14),
                AutoScroll = true
            };
            Controls.Add(pnlMain);

            int curY = 12;

            Label lblHeader = new Label
            {
                Text = _t.GuideTitle,
                Font = new Font("Segoe UI", 12f, FontStyle.Bold),
                Location = new Point(16, curY),
                Size = new Size(468, 26),
                ForeColor = _c.Text
            };
            pnlMain.Controls.Add(lblHeader);
            curY += 28;

            Label lblSubtitle = new Label
            {
                Text = _t.GuideSubtitle,
                Font = new Font("Segoe UI", 8.5f, FontStyle.Regular),
                Location = new Point(16, curY),
                Size = new Size(468, 20),
                ForeColor = _c.TextMuted
            };
            pnlMain.Controls.Add(lblSubtitle);
            curY += 26;

            curY = AddStepCard(pnlMain, curY, "1", _t.GuideStep1Header, _t.GuideStep1Text);
            curY = AddStepCard(pnlMain, curY, "2", _t.GuideStep2Header, _t.GuideStep2Text);
            curY = AddStepCard(pnlMain, curY, "3", _t.GuideStep3Header, _t.GuideStep3Text);

            Panel pnlTips = new Panel
            {
                Location = new Point(16, curY),
                Size = new Size(468, 88),
                BackColor = _c.Card
            };
            UiThemeHelper.SetupModernCard(pnlTips, 6, () => _c.CardBorder);
            pnlMain.Controls.Add(pnlTips);

            Label lblTipsTitle = new Label
            {
                Text = "💡 " + _t.GuideTipsHeader,
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                Location = new Point(10, 8),
                Size = new Size(448, 18),
                ForeColor = _c.Text,
                BackColor = Color.Transparent
            };
            pnlTips.Controls.Add(lblTipsTitle);

            Label lblTipsContent = new Label
            {
                Text = _t.GuideTipsText,
                Font = new Font("Segoe UI", 8f, FontStyle.Regular),
                Location = new Point(10, 28),
                Size = new Size(448, 54),
                ForeColor = _c.TextMuted,
                BackColor = Color.Transparent
            };
            pnlTips.Controls.Add(lblTipsContent);
            curY += 96;

            _lblAdbStatus = new Label
            {
                Location = new Point(16, curY),
                Size = new Size(468, 18),
                Font = new Font("Segoe UI", 8f, FontStyle.Italic),
                ForeColor = _c.StatusDotOnline,
                TextAlign = ContentAlignment.MiddleLeft
            };
            pnlMain.Controls.Add(_lblAdbStatus);
            curY += 22;

            Panel pnlButtons = new Panel
            {
                Location = new Point(16, curY),
                Size = new Size(468, 38)
            };
            pnlMain.Controls.Add(pnlButtons);

            _btnRestartAdb = new Button
            {
                Text = _t.GuideBtnRestartAdb,
                Location = new Point(0, 2),
                Size = new Size(135, 32),
                Font = new Font("Segoe UI", 8.5f, FontStyle.Regular),
                BackColor = _c.BtnTool,
                ForeColor = _c.BtnToolText,
                Cursor = Cursors.Hand
            };
            UiThemeHelper.SetupModernButton(_btnRestartAdb, 5, () => _c.BtnToolBorder);
            _btnRestartAdb.Click += async (s, e) => await RestartAdbAsync();
            pnlButtons.Controls.Add(_btnRestartAdb);

            Button btnDevMgr = new Button
            {
                Text = _t.GuideBtnDevMgr,
                Location = new Point(143, 2),
                Size = new Size(165, 32),
                Font = new Font("Segoe UI", 8.5f, FontStyle.Regular),
                BackColor = _c.BtnTool,
                ForeColor = _c.BtnToolText,
                Cursor = Cursors.Hand
            };
            UiThemeHelper.SetupModernButton(btnDevMgr, 5, () => _c.BtnToolBorder);
            btnDevMgr.Click += (s, e) =>
            {
                try { Process.Start("devmgmt.msc"); }
                catch (Exception ex)
                {
                    MessageBox.Show(this, ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };
            pnlButtons.Controls.Add(btnDevMgr);

            Button btnClose = new Button
            {
                Text = _t.GuideBtnClose,
                Location = new Point(368, 2),
                Size = new Size(100, 32),
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                BackColor = _c.BtnHero,
                ForeColor = _c.BtnHeroText,
                Cursor = Cursors.Hand
            };
            UiThemeHelper.SetupModernButton(btnClose, 5, () => _c.BtnHero);
            btnClose.Click += (s, e) => Close();
            pnlButtons.Controls.Add(btnClose);
            CancelButton = btnClose;
        }

        private int AddStepCard(Panel parent, int top, string number, string title, string description)
        {
            Panel card = new Panel
            {
                Location = new Point(16, top),
                Size = new Size(468, 66),
                BackColor = _c.Card
            };
            UiThemeHelper.SetupModernCard(card, 6, () => _c.CardBorder);
            parent.Controls.Add(card);

            Label lblNum = new Label
            {
                Text = number,
                Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                Location = new Point(10, 10),
                Size = new Size(24, 24),
                TextAlign = ContentAlignment.MiddleCenter,
                ForeColor = _c.BtnHero,
                BackColor = Color.Transparent
            };
            card.Controls.Add(lblNum);

            Label lblTitle = new Label
            {
                Text = title,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                Location = new Point(38, 6),
                Size = new Size(420, 18),
                ForeColor = _c.Text,
                BackColor = Color.Transparent
            };
            card.Controls.Add(lblTitle);

            Label lblDesc = new Label
            {
                Text = description,
                Font = new Font("Segoe UI", 8f, FontStyle.Regular),
                Location = new Point(38, 24),
                Size = new Size(420, 36),
                ForeColor = _c.TextMuted,
                BackColor = Color.Transparent
            };
            card.Controls.Add(lblDesc);

            return top + 72;
        }

        private async Task RestartAdbAsync()
        {
            _btnRestartAdb.Enabled = false;
            _lblAdbStatus.Text = "Restartowanie usługi ADB...";
            try
            {
                await AdbService.ExecuteAdbDetailedAsync("kill-server", 3000).ConfigureAwait(true);
                await AdbService.ExecuteAdbDetailedAsync("start-server", 6000).ConfigureAwait(true);
                _lblAdbStatus.Text = "✔ " + _t.GuideAdbRestarted;
            }
            catch (Exception ex)
            {
                _lblAdbStatus.Text = "Błąd: " + ex.Message;
            }
            finally
            {
                _btnRestartAdb.Enabled = true;
            }
        }
    }
}

