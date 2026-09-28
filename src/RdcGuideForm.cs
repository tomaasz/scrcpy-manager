using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace ScrcpyManager
{
    public sealed class RdcGuideForm : Form
    {
        private readonly ThemeColors _c;
        private readonly Localization.Strings _t;
        private readonly Label _lblStatus;
        private readonly Button _btnApplyAuto;

        public RdcGuideForm(ThemeColors colors, string language, Icon icon)
        {
            _c = colors;
            _t = Localization.Get(language);

            Text = _t.RdcGuideTitle;
            ClientSize = new Size(500, 580);
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
                Text = _t.RdcGuideHeader,
                Font = new Font("Segoe UI", 11.5f, FontStyle.Bold),
                Location = new Point(16, curY),
                Size = new Size(468, 26),
                ForeColor = _c.Text
            };
            pnlMain.Controls.Add(lblHeader);
            curY += 28;

            Label lblSubtitle = new Label
            {
                Text = _t.RdcGuideSubtitle,
                Font = new Font("Segoe UI", 8.5f, FontStyle.Regular),
                Location = new Point(16, curY),
                Size = new Size(468, 20),
                ForeColor = _c.TextMuted
            };
            pnlMain.Controls.Add(lblSubtitle);
            curY += 26;

            curY = AddStepCard(pnlMain, curY, "1", _t.RdcGuideStep1Header, _t.RdcGuideStep1Text);
            curY = AddStepCard(pnlMain, curY, "2", _t.RdcGuideStep2Header, _t.RdcGuideStep2Text);
            curY = AddStepCard(pnlMain, curY, "3", _t.RdcGuideStep3Header, _t.RdcGuideStep3Text);
            curY = AddStepCard(pnlMain, curY, "4", _t.RdcGuideStep4Header, _t.RdcGuideStep4Text);

            // Warning note card
            Panel pnlWarning = new Panel
            {
                Location = new Point(16, curY),
                Size = new Size(468, 76),
                BackColor = _c.Card
            };
            UiThemeHelper.SetupModernCard(pnlWarning, 6, () => _c.CardBorder);
            pnlMain.Controls.Add(pnlWarning);

            Label lblWarningTitle = new Label
            {
                Text = "⚠ " + _t.RdcGuideWarningHeader,
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                Location = new Point(10, 8),
                Size = new Size(448, 18),
                ForeColor = Color.Goldenrod,
                BackColor = Color.Transparent
            };
            pnlWarning.Controls.Add(lblWarningTitle);

            Label lblWarningContent = new Label
            {
                Text = _t.RdcGuideWarningText,
                Font = new Font("Segoe UI", 8f, FontStyle.Regular),
                Location = new Point(10, 26),
                Size = new Size(448, 44),
                ForeColor = _c.TextMuted,
                BackColor = Color.Transparent
            };
            pnlWarning.Controls.Add(lblWarningContent);
            curY += 84;

            _lblStatus = new Label
            {
                Location = new Point(16, curY),
                Size = new Size(468, 18),
                Font = new Font("Segoe UI", 8f, FontStyle.Italic),
                ForeColor = _c.StatusDotOnline,
                TextAlign = ContentAlignment.MiddleLeft
            };
            pnlMain.Controls.Add(_lblStatus);
            curY += 22;

            Panel pnlButtons = new Panel
            {
                Location = new Point(16, curY),
                Size = new Size(468, 38)
            };
            pnlMain.Controls.Add(pnlButtons);

            Button btnMstsc = new Button
            {
                Text = _t.RdcGuideBtnOpenMstsc,
                Location = new Point(0, 2),
                Size = new Size(180, 32),
                Font = new Font("Segoe UI", 8.5f, FontStyle.Regular),
                BackColor = _c.BtnTool,
                ForeColor = _c.BtnToolText,
                Cursor = Cursors.Hand
            };
            UiThemeHelper.SetupModernButton(btnMstsc, 5, () => _c.BtnToolBorder);
            btnMstsc.Click += (s, e) =>
            {
                try { Process.Start("mstsc.exe"); }
                catch (Exception ex)
                {
                    MessageBox.Show(this, ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };
            pnlButtons.Controls.Add(btnMstsc);

            _btnApplyAuto = new Button
            {
                Text = _t.RdcGuideBtnAutoApply,
                Location = new Point(186, 2),
                Size = new Size(170, 32),
                Font = new Font("Segoe UI", 8.5f, FontStyle.Regular),
                BackColor = _c.BtnTool,
                ForeColor = _c.BtnToolText,
                Cursor = Cursors.Hand
            };
            UiThemeHelper.SetupModernButton(_btnApplyAuto, 5, () => _c.BtnToolBorder);
            _btnApplyAuto.Click += (s, e) => ApplyRdpKeyboardSetting();
            pnlButtons.Controls.Add(_btnApplyAuto);

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
                Size = new Size(468, 64),
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

            return top + 70;
        }

        private void ApplyRdpKeyboardSetting()
        {
            try
            {
                string docs = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                string rdpFile = Path.Combine(docs, "Default.rdp");
                string setting = "keyboardhook:i:0";

                if (File.Exists(rdpFile))
                {
                    FileAttributes attrs = File.GetAttributes(rdpFile);
                    bool wasHidden = (attrs & FileAttributes.Hidden) != 0;
                    if (wasHidden)
                    {
                        File.SetAttributes(rdpFile, attrs & ~FileAttributes.Hidden);
                    }

                    string content = File.ReadAllText(rdpFile);
                    if (Regex.IsMatch(content, @"^keyboardhook:i:\d+", RegexOptions.Multiline))
                    {
                        content = Regex.Replace(content, @"^keyboardhook:i:\d+", setting, RegexOptions.Multiline);
                    }
                    else
                    {
                        content = content.TrimEnd() + Environment.NewLine + setting + Environment.NewLine;
                    }

                    File.WriteAllText(rdpFile, content);
                    if (wasHidden)
                    {
                        File.SetAttributes(rdpFile, attrs);
                    }
                }
                else
                {
                    File.WriteAllText(rdpFile, setting + Environment.NewLine);
                }

                _lblStatus.ForeColor = _c.StatusDotOnline;
                _lblStatus.Text = "✔ " + _t.RdcGuideAutoSuccess;
            }
            catch (Exception ex)
            {
                _lblStatus.ForeColor = Color.OrangeRed;
                _lblStatus.Text = "Błąd: " + ex.Message;
            }
        }
    }
}
