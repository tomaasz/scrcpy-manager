using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace ScrcpyManager
{
    public static class UiThemeHelper
    {
        [DllImport("gdi32.dll", EntryPoint = "CreateRoundRectRgn")]
        private static extern IntPtr CreateRoundRectRgn(int nLeftRect, int nTopRect, int nRightRect, int nBottomRect, int nWidthEllipse, int nHeightEllipse);

        [DllImport("gdi32.dll", ExactSpelling = true, SetLastError = true)]
        private static extern bool DeleteObject(IntPtr hObject);

        public static void ApplyRoundedCorners(Control ctrl, int radius)
        {
            if (ctrl == null) return;
            UpdateRegion(ctrl, radius);
            ctrl.SizeChanged += (s, e) => UpdateRegion(ctrl, radius);
        }

        public static void UpdateRegion(Control ctrl, int radius)
        {
            if (ctrl == null || ctrl.Width <= 0 || ctrl.Height <= 0) return;
            try
            {
                IntPtr hRgn = CreateRoundRectRgn(0, 0, ctrl.Width + 1, ctrl.Height + 1, radius, radius);
                if (hRgn != IntPtr.Zero)
                {
                    ctrl.Region = Region.FromHrgn(hRgn);
                    DeleteObject(hRgn);
                }
            }
            catch { }
        }

        public static GraphicsPath GetRoundedPath(Rectangle rect, int radius)
        {
            GraphicsPath path = new GraphicsPath();
            float r = radius;
            float d = r * 2F;
            if (d > rect.Width) d = rect.Width;
            if (d > rect.Height) d = rect.Height;

            path.StartFigure();
            path.AddArc(rect.X, rect.Y, d, d, 180, 90);
            path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
            path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
            path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }

        public static void SetupModernButton(Button btn, int radius, Func<Color> getBorderColor)
        {
            if (btn == null) return;
            btn.FlatStyle = FlatStyle.Flat;
            btn.FlatAppearance.BorderSize = 0;
            btn.Cursor = Cursors.Hand;

            ApplyRoundedCorners(btn, radius);

            btn.Paint += (s, e) =>
            {
                if (btn.Width <= 2 || btn.Height <= 2) return;
                Color border = getBorderColor != null ? getBorderColor() : Color.Transparent;
                if (border.A > 0)
                {
                    e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                    using (GraphicsPath path = GetRoundedPath(new Rectangle(0, 0, btn.Width - 1, btn.Height - 1), radius))
                    using (Pen pen = new Pen(border, 1f))
                    {
                        e.Graphics.DrawPath(pen, path);
                    }
                }
            };
        }

        public static void SetupModernCard(Panel pnl, int radius, Func<Color> getBorderColor)
        {
            if (pnl == null) return;
            pnl.BorderStyle = BorderStyle.None;
            ApplyRoundedCorners(pnl, radius);

            pnl.Paint += (s, e) =>
            {
                if (pnl.Width <= 2 || pnl.Height <= 2) return;
                Color border = getBorderColor != null ? getBorderColor() : Color.Transparent;
                if (border.A > 0)
                {
                    e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                    using (GraphicsPath path = GetRoundedPath(new Rectangle(0, 0, pnl.Width - 1, pnl.Height - 1), radius))
                    using (Pen pen = new Pen(border, 1f))
                    {
                        e.Graphics.DrawPath(pen, path);
                    }
                }
            };
        }
    }
}

