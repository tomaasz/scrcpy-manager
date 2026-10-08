using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.Windows.Forms;

namespace ScrcpyManager
{
    public class AppTileButton : Button
    {
        private bool _isHovered;
        private bool _isPressed;
        private Image _appIcon;
        private bool _iconIsLight;
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

        [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Image AppIcon
        {
            get { return _appIcon; }
            set
            {
                if (_appIcon != value)
                {
                    _appIcon = RemoveWhiteBackground(value);
                    _iconIsLight = IsMostlyLight(_appIcon);
                    Invalidate();
                }
            }
        }

        [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
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

        [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
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

        [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
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

        [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
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

        [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
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

        protected override void OnGotFocus(EventArgs e)
        {
            base.OnGotFocus(e);
            Invalidate();
        }

        protected override void OnLostFocus(EventArgs e)
        {
            base.OnLostFocus(e);
            Invalidate();
        }

        /// <summary>Czy nazwa aplikacji mieści się w kafelku bez skracania (wielokropka).</summary>
        public bool IsTextTruncated
        {
            get
            {
                if (string.IsNullOrEmpty(Text)) return false;
                int contentX = _paddingLeft + (_appIcon != null ? _iconSize + _iconGap : 0);
                int textW = Math.Max(0, Width - contentX - 4);
                Size sz = TextRenderer.MeasureText(Text, Font, new Size(int.MaxValue, Height), TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix);
                return sz.Width > textW;
            }
        }

        private static bool IsNearWhite(Color c)
        {
            return c.A >= 128 && c.R >= 235 && c.G >= 235 && c.B >= 235;
        }

        // Icons that ship on a white square (e.g. Messages, Messenger, Windows App) look like a white
        // box on the dark theme. Flood-fill the near-white area connected to the icon border and make
        // it transparent; white parts enclosed by the glyph are left alone.
        private static Image RemoveWhiteBackground(Image image)
        {
            Bitmap source = image as Bitmap;
            if (source == null || source.Width < 8 || source.Height < 8 || source.Width > 512 || source.Height > 512) return image;
            try
            {
                int w = source.Width, h = source.Height;
                Bitmap bmp = new Bitmap(w, h, PixelFormat.Format32bppArgb);
                using (Graphics g = Graphics.FromImage(bmp)) g.DrawImage(source, 0, 0, w, h);

                bool[] remove = new bool[w * h];
                System.Collections.Generic.Stack<int> stack = new System.Collections.Generic.Stack<int>();
                Action<int, int> seed = (x, y) =>
                {
                    int i = y * w + x;
                    if (!remove[i] && IsNearWhite(bmp.GetPixel(x, y))) { remove[i] = true; stack.Push(i); }
                };
                for (int x = 0; x < w; x++) { seed(x, 0); seed(x, h - 1); }
                for (int y = 0; y < h; y++) { seed(0, y); seed(w - 1, y); }
                if (stack.Count == 0) { bmp.Dispose(); return image; }

                int removed = 0;
                while (stack.Count > 0)
                {
                    int i = stack.Pop();
                    removed++;
                    int x = i % w, y = i / w;
                    if (x > 0) seed(x - 1, y);
                    if (x < w - 1) seed(x + 1, y);
                    if (y > 0) seed(x, y - 1);
                    if (y < h - 1) seed(x, y + 1);
                }

                // Mostly-white icons (glyph on transparent) are handled by the dark tint instead.
                if (removed > w * h * 0.6) { bmp.Dispose(); return image; }

                for (int y = 0; y < h; y++)
                    for (int x = 0; x < w; x++)
                    {
                        int i = y * w + x;
                        if (remove[i]) { bmp.SetPixel(x, y, Color.FromArgb(0, 255, 255, 255)); continue; }
                        // Soften the light anti-aliased halo next to the removed area.
                        bool nextToRemoved = (x > 0 && remove[i - 1]) || (x < w - 1 && remove[i + 1]) || (y > 0 && remove[i - w]) || (y < h - 1 && remove[i + w]);
                        if (!nextToRemoved) continue;
                        Color c = bmp.GetPixel(x, y);
                        if (GetLuminance(c) > 0.85) bmp.SetPixel(x, y, Color.FromArgb(c.A / 3, c.R, c.G, c.B));
                    }
                return bmp;
            }
            catch { return image; }
        }

        private static double GetLuminance(Color c)
        {
            return (0.2126 * c.R + 0.7152 * c.G + 0.0722 * c.B) / 255.0;
        }

        // True when the opaque pixels of the icon are (almost) white, i.e. unreadable on a light background.
        private static bool IsMostlyLight(Image image)
        {
            Bitmap bmp = image as Bitmap;
            if (bmp == null) return false;
            try
            {
                int step = Math.Max(1, Math.Min(bmp.Width, bmp.Height) / 24);
                double sum = 0, sat = 0; int count = 0;
                for (int y = 0; y < bmp.Height; y += step)
                    for (int x = 0; x < bmp.Width; x += step)
                    {
                        Color p = bmp.GetPixel(x, y);
                        if (p.A < 128) continue;
                        sum += GetLuminance(p);
                        sat += (Math.Max(p.R, Math.Max(p.G, p.B)) - Math.Min(p.R, Math.Min(p.G, p.B))) / 255.0;
                        count++;
                    }
                // Only plain white/grey glyphs qualify; colourful icons keep their original look.
                return count > 0 && sum / count > 0.9 && sat / count < 0.05;
            }
            catch { return false; }
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
                if (Enabled && _iconIsLight && GetLuminance(bg) > 0.6)
                {
                    // White/very light glyph icons (e.g. system Settings) vanish on light tiles - draw them dark.
                    using (ImageAttributes attrs = new ImageAttributes())
                    {
                        attrs.SetColorMatrix(new ColorMatrix(new float[][]
                        {
                            new float[] { 0.25f, 0, 0, 0, 0 },
                            new float[] { 0, 0.25f, 0, 0, 0 },
                            new float[] { 0, 0, 0.25f, 0, 0 },
                            new float[] { 0, 0, 0, 1, 0 },
                            new float[] { 0, 0, 0, 0, 1 }
                        }));
                        g.DrawImage(_appIcon, new Rectangle(contentX, iconY, _iconSize, _iconSize), 0, 0, _appIcon.Width, _appIcon.Height, GraphicsUnit.Pixel, attrs);
                    }
                }
                else if (Enabled)
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
                    // Wyłączony stan: przygaszony kolor tekstu wymieszany z tłem (czytelniejszy niż sztywny Color.Gray)
                    Color textColor = Enabled ? ForeColor : Blend(ForeColor, BackColor, 0.55f);
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

            UiThemeHelper.DrawFocusRing(this, g, _borderRadius);
        }

        private static Color Blend(Color a, Color b, float amountA)
        {
            float amountB = 1f - amountA;
            return Color.FromArgb(
                (int)(a.R * amountA + b.R * amountB),
                (int)(a.G * amountA + b.G * amountB),
                (int)(a.B * amountA + b.B * amountB));
        }
    }
}

