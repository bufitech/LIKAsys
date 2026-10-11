using System;
using System.Windows;
using System.Windows.Interop;
using LIKAsys.Core;
using Forms = System.Windows.Forms;

namespace LIKAsys.Ui
{
    /// <summary>
    /// Pixel-exact placement that behaves correctly on multi-monitor / mixed-DPI setups
    /// (everything is done in physical pixels through SetWindowPos).
    /// </summary>
    internal static class WidgetPlacement
    {
        /// <summary>Transparent padding around the card that carries the drop shadow (DIPs).</summary>
        public const double ShadowPad = 14;

        public static void Apply(Window window, AppSettings s)
        {
            try
            {
                var helper = new WindowInteropHelper(window);
                if (helper.Handle == IntPtr.Zero) return;

                var screen = Screens.Pick(s);
                var wa = new System.Drawing.Rectangle(
                    screen.WorkLeft, screen.WorkTop, screen.WorkWidth, screen.WorkHeight);

                double scale = Native.GetScaleForPoint(wa.Left + wa.Width / 2, wa.Top + wa.Height / 2);
                int w = (int)Math.Ceiling(window.ActualWidth * scale);
                int h = (int)Math.Ceiling(window.ActualHeight * scale);
                if (w <= 0 || h <= 0) return;

                int pad = (int)Math.Round((s.Blur ? 0 : ShadowPad) * scale * s.Scale);
                int mx = (int)Math.Round(s.MarginX * scale) - pad;
                int my = (int)Math.Round(s.MarginY * scale) - pad;

                int x, y;
                if (s.Position == WidgetPosition.Custom)
                {
                    x = s.CustomX;
                    y = s.CustomY;
                }
                else
                {
                    switch (s.Position)
                    {
                        case WidgetPosition.TopLeft: x = wa.Left + mx; y = wa.Top + my; break;
                        case WidgetPosition.TopCenter: x = wa.Left + (wa.Width - w) / 2; y = wa.Top + my; break;
                        case WidgetPosition.TopRight: x = wa.Right - w - mx; y = wa.Top + my; break;
                        case WidgetPosition.MiddleLeft: x = wa.Left + mx; y = wa.Top + (wa.Height - h) / 2; break;
                        case WidgetPosition.Center: x = wa.Left + (wa.Width - w) / 2; y = wa.Top + (wa.Height - h) / 2; break;
                        case WidgetPosition.MiddleRight: x = wa.Right - w - mx; y = wa.Top + (wa.Height - h) / 2; break;
                        case WidgetPosition.BottomLeft: x = wa.Left + mx; y = wa.Bottom - h - my; break;
                        case WidgetPosition.BottomCenter: x = wa.Left + (wa.Width - w) / 2; y = wa.Bottom - h - my; break;
                        default: x = wa.Right - w - mx; y = wa.Bottom - h - my; break; // BottomRight
                    }
                }

                // keep it on screen
                var vb = Forms.SystemInformation.VirtualScreen;
                x = Math.Max(vb.Left - pad, Math.Min(x, vb.Right - w + pad));
                y = Math.Max(vb.Top - pad, Math.Min(y, vb.Bottom - h + pad));

                Native.SetWindowPos(helper.Handle,
                    s.AlwaysOnTop ? Native.HWND_TOPMOST : Native.HWND_NOTOPMOST,
                    x, y, 0, 0, Native.SWP_NOSIZE | Native.SWP_NOACTIVATE);
            }
            catch (Exception ex) { AppInfo.Log("Placement failed: " + ex.Message); }
        }

        /// <summary>
        /// True when the saved spot is on no monitor at all, which is what happens after
        /// a screen is unplugged or the resolution drops.
        /// </summary>
        public static bool IsStranded(AppSettings s)
        {
            try
            {
                if (s.Position != WidgetPosition.Custom) return false;
                return Screens.OffScreen(s.CustomX + 20, s.CustomY + 20);
            }
            catch { return false; }
        }

        /// <summary>After a manual drag: remember the exact spot, optionally snapping to the nearest corner.</summary>
        public static void StoreCurrent(Window window, AppSettings s)
        {
            try
            {
                var helper = new WindowInteropHelper(window);
                if (helper.Handle == IntPtr.Zero) return;
                if (!Native.GetWindowRect(helper.Handle, out var r)) return;

                int w = r.Right - r.Left, h = r.Bottom - r.Top;
                // Drag the card to another monitor and that becomes the chosen one,
                // stored by device path so unplugging and replugging keeps it right.
                var si = Screens.FromPoint(r.Left + w / 2, r.Top + h / 2);
                var wa = new System.Drawing.Rectangle(si.WorkLeft, si.WorkTop, si.WorkWidth, si.WorkHeight);
                s.MonitorIndex = si.Index;
                if (s.MonitorMode == MonitorMode.Fixed) s.MonitorId = si.Id;

                if (s.SnapToCorners)
                {
                    double scale = Native.GetScaleForPoint(wa.Left + wa.Width / 2, wa.Top + wa.Height / 2);
                    int threshold = (int)(90 * scale);
                    bool left = r.Left - wa.Left < threshold;
                    bool right = wa.Right - r.Right < threshold;
                    bool top = r.Top - wa.Top < threshold;
                    bool bottom = wa.Bottom - r.Bottom < threshold;

                    WidgetPosition? pos = null;
                    if (top && left) pos = WidgetPosition.TopLeft;
                    else if (top && right) pos = WidgetPosition.TopRight;
                    else if (bottom && left) pos = WidgetPosition.BottomLeft;
                    else if (bottom && right) pos = WidgetPosition.BottomRight;
                    else if (top) pos = WidgetPosition.TopCenter;
                    else if (bottom) pos = WidgetPosition.BottomCenter;
                    else if (left) pos = WidgetPosition.MiddleLeft;
                    else if (right) pos = WidgetPosition.MiddleRight;

                    if (pos.HasValue)
                    {
                        s.Position = pos.Value;
                        Apply(window, s);
                        return;
                    }
                }

                s.CustomX = r.Left;
                s.CustomY = r.Top;
                s.Position = WidgetPosition.Custom;
            }
            catch (Exception ex) { AppInfo.Log("StoreCurrent failed: " + ex.Message); }
        }

        public static void ApplyWindowFlags(Window window, AppSettings s)
        {
            try
            {
                var hwnd = new WindowInteropHelper(window).Handle;
                if (hwnd == IntPtr.Zero) return;

                int ex = Native.GetWindowLong(hwnd, Native.GWL_EXSTYLE);
                ex |= Native.WS_EX_TOOLWINDOW;            // keep it out of alt-tab
                if (s.ClickThrough) ex |= Native.WS_EX_LAYERED | Native.WS_EX_TRANSPARENT;
                else ex &= ~Native.WS_EX_TRANSPARENT;
                Native.SetWindowLong(hwnd, Native.GWL_EXSTYLE, ex);
            }
            catch { }
        }
    }
}
