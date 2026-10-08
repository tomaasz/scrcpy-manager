using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace ScrcpyManager
{
    // Check box with a clearly visible box in both the dark and the light theme.
    // The system-drawn box blends into dark backgrounds, so the glyph is painted manually.
    public class ThemedCheckBox : CheckBox
    {
        public static readonly Color Accent = Color.FromArgb(47, 111, 214);
        private const int BoxSize = 16;
        private const int TextGap = 8;
        private bool _hot;

        public ThemedCheckBox()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            AutoSize = true;
        }

        [Browsable(false), EditorBrowsable(EditorBrowsableState.Never)]
        public override bool AutoSize
        {
            get { return base.AutoSize; }
            set { base.AutoSize = value; }
        }

        public override Size GetPreferredSize(Size proposedSize)
        {
            Size text = string.IsNullOrEmpty(Text) ? Size.Empty : TextRenderer.MeasureText(Text, Font, new Size(int.MaxValue, int.MaxValue), TextFormatFlags.NoPadding | TextFormatFlags.SingleLine);
            int width = 2 + BoxSize + (text.Width > 0 ? TextGap + text.Width : 0) + 2;
            int height = Math.Max(BoxSize, text.Height) + 6;
            return new Size(width, height);
        }

        protected override void OnMouseEnter(EventArgs e) { _hot = true; Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { _hot = false; Invalidate(); base.OnMouseLeave(e); }
        protected override void OnEnabledChanged(EventArgs e) { Invalidate(); base.OnEnabledChanged(e); }
        protected override void OnGotFocus(EventArgs e) { Invalidate(); base.OnGotFocus(e); }
        protected override void OnLostFocus(EventArgs e) { Invalidate(); base.OnLostFocus(e); }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaintBackground(e);
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            Rectangle box = new Rectangle(2, (Height - BoxSize) / 2, BoxSize, BoxSize);
            DrawGlyph(g, box, Checked, Enabled, ForeColor, BackColor, _hot);

            if (!string.IsNullOrEmpty(Text))
            {
                Color textColor = Enabled ? ForeColor : Blend(ForeColor, BackColor, 0.5f);
                Rectangle textRect = new Rectangle(box.Right + TextGap, 0, Math.Max(0, Width - box.Right - TextGap), Height);
                TextRenderer.DrawText(g, Text, Font, textRect, textColor,
                    TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding);
                if (Focused && ShowFocusCues)
                {
                    Size size = TextRenderer.MeasureText(Text, Font, textRect.Size, TextFormatFlags.NoPadding | TextFormatFlags.SingleLine);
                    Rectangle focus = new Rectangle(textRect.X - 2, (Height - size.Height) / 2 - 1, Math.Min(size.Width + 4, textRect.Width), size.Height + 2);
                    ControlPaint.DrawFocusRectangle(g, focus, ForeColor, BackColor);
                }
            }
        }

        // Draws the check box glyph (also used for DataGridView cells).
        public static void DrawGlyph(Graphics g, Rectangle box, bool isChecked, bool enabled, Color fore, Color back, bool hot)
        {
            SmoothingMode previous = g.SmoothingMode;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            Color border = Blend(fore, back, hot ? 0.80f : 0.60f);
            Color fill = isChecked ? Accent : Blend(back, fore, 0.88f);
            if (isChecked) border = Accent;
            if (!enabled)
            {
                border = Blend(border, back, 0.45f);
                fill = isChecked ? Blend(Accent, back, 0.45f) : Blend(fill, back, 0.5f);
            }

            Rectangle r = new Rectangle(box.X, box.Y, box.Width - 1, box.Height - 1);
            using (GraphicsPath path = UiThemeHelper.GetRoundedPath(r, 4))
            using (SolidBrush brush = new SolidBrush(fill))
            using (Pen pen = new Pen(border, 1.4f))
            {
                g.FillPath(brush, path);
                g.DrawPath(pen, path);
            }

            if (isChecked)
            {
                using (Pen tick = new Pen(enabled ? Color.White : Blend(Color.White, back, 0.5f), 2f) { StartCap = LineCap.Round, EndCap = LineCap.Round, LineJoin = LineJoin.Round })
                {
                    float x = box.X, y = box.Y, w = box.Width, h = box.Height;
                    g.DrawLines(tick, new[]
                    {
                        new PointF(x + w * 0.24f, y + h * 0.52f),
                        new PointF(x + w * 0.43f, y + h * 0.71f),
                        new PointF(x + w * 0.76f, y + h * 0.31f)
                    });
                }
            }
            g.SmoothingMode = previous;
        }

        public static Color Blend(Color a, Color b, float amountOfA)
        {
            float inv = 1f - amountOfA;
            return Color.FromArgb(255,
                (int)Math.Round(a.R * amountOfA + b.R * inv),
                (int)Math.Round(a.G * amountOfA + b.G * inv),
                (int)Math.Round(a.B * amountOfA + b.B * inv));
        }
    }
}
