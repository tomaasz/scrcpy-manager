using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Windows.Forms;

namespace ScrcpyManager
{
    public class AppTileButton : Button
    {
        private bool _isHovered;
        private bool _isPressed;
        private Image _appIcon;
        private int _iconSize = 20;
        private int _iconGap = 6;
        private int _paddingLeft = 7;
        private int _borderRadius = 5;
        private Func<Color> _borderColorProvider;

        public AppTileButton()
        {
            SetStyle(
                ControlStyles.UserPaint |
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.ResizeRedraw,
                true);

            FlatStyle = FlatStyle.Flat;
            FlatAppearance.BorderSize = 0;
            Cursor = Cursors.Hand;
            DoubleBuffered = true;

            UiThemeHelper.ApplyRoundedCorners(this, _borderRadius);
        }

        public Image AppIcon
        {
            get { return _appIcon; }
            set
            {
                if (_appIcon != value)
                {
                    _appIcon = value;
                    Invalidate();
                }
            }
        }

        public int IconSize
        {
            get { return _iconSize; }
            set
            {
                if (_iconSize != value)
                {
                    _iconSize = value;
                    Invalidate();
                }
            }
        }

        public int IconGap
        {
            get { return _iconGap; }
            set
            {
                if (_iconGap != value)
                {
                    _iconGap = value;
                    Invalidate();
                }
            }
        }

        public int PaddingLeft
        {
            get { return _paddingLeft; }
            set
            {
                if (_paddingLeft != value)
                {
                    _paddingLeft = value;
                    Invalidate();
                }
            }
        }

        public int BorderRadius
        {
            get { return _borderRadius; }
            set
            {
                if (_borderRadius != value)
                {
                    _borderRadius = value;
                    UiThemeHelper.UpdateRegion(this, _borderRadius);
                    Invalidate();
                }
            }
        }

        public Func<Color> BorderColorProvider
        {
            get { return _borderColorProvider; }
            set
            {
                _borderColorProvider = value;
                Invalidate();
            }
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            base.OnMouseEnter(e);
            _isHovered = true;
            Invalidate();
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            _isHovered = false;
            _isPressed = false;
            Invalidate();
        }

        protected override void OnMouseDown(MouseEventArgs mevent)
        {
            base.OnMouseDown(mevent);
            if (mevent.Button == MouseButtons.Left)
            {
                _isPressed = true;
                Invalidate();
            }
        }

        protected override void OnMouseUp(MouseEventArgs mevent)
        {
            base.OnMouseUp(mevent);
            if (_isPressed)
            {
                _isPressed = false;
                Invalidate();
            }
        }

        protected override void OnMouseMove(MouseEventArgs mevent)
        {
            base.OnMouseMove(mevent);
            if (Capture)
            {
                bool inside = ClientRectangle.Contains(mevent.Location);
                if (_isPressed != inside)
                {
                    _isPressed = inside;
                    Invalidate();
                }
            }
        }

        protected override void OnTextChanged(EventArgs e)
        {
            base.OnTextChanged(e);
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs pevent)
        {
            Graphics g = pevent.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

            // Określenie koloru tła
            Color bg = BackColor;
            if (_isPressed)
            {
                bg = FlatAppearance.MouseDownBackColor.A > 0
                    ? FlatAppearance.MouseDownBackColor
                    : (FlatAppearance.MouseOverBackColor.A > 0 ? FlatAppearance.MouseOverBackColor : BackColor);
            }
            else if (_isHovered)
            {
                bg = FlatAppearance.MouseOverBackColor.A > 0
                    ? FlatAppearance.MouseOverBackColor
                    : BackColor;
            }

            Rectangle bounds = new Rectangle(0, 0, Width - 1, Height - 1);
            if (bounds.Width <= 2 || bounds.Height <= 2) return;

            // Zaokrąglone tło
            using (GraphicsPath path = UiThemeHelper.GetRoundedPath(bounds, _borderRadius))
            {
                using (Brush br = new SolidBrush(bg))
                {
                    g.FillPath(br, path);
                }

                // Obramowanie
                Color border = _borderColorProvider != null ? _borderColorProvider() : Color.Transparent;
                if (border.A > 0)
                {
                    using (Pen pen = new Pen(border, 1f))
                    {
                        g.DrawPath(pen, path);
                    }
                }
            }

            // Rysowanie ikony wycentrowanej w pionie
            int contentX = _paddingLeft;
            if (_appIcon != null)
            {
                int iconY = (Height - _iconSize) / 2;
                if (Enabled)
                {
                    g.DrawImage(_appIcon, contentX, iconY, _iconSize, _iconSize);
                }
                else
                {
                    ControlPaint.DrawImageDisabled(g, _appIcon, contentX, iconY, BackColor);
                }
                contentX += _iconSize + _iconGap;
            }

            // Rysowanie tekstu wycentrowanego w pionie (SingleLine + VerticalCenter uniemożliwia wielowierszowe skoki)
            if (!string.IsNullOrEmpty(Text))
            {
                int textW = Math.Max(0, Width - contentX - 4);
                if (textW > 0)
                {
                    Rectangle textRect = new Rectangle(contentX, 0, textW, Height);
                    Color textColor = Enabled ? ForeColor : Color.Gray;
                    TextRenderer.DrawText(
                        g,
                        Text,
                        Font,
                        textRect,
                        textColor,
                        TextFormatFlags.Left |
                        TextFormatFlags.VerticalCenter |
                        TextFormatFlags.SingleLine |
                        TextFormatFlags.EndEllipsis |
                        TextFormatFlags.NoPrefix
                    );
                }
            }
        }
    }
}

