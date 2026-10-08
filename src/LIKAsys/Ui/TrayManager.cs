using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Forms;
using LIKAsys.Core;
using LIKAsys.Monitoring;

namespace LIKAsys.Ui
{
    /// <summary>Taskbar tray icon + dark context menu. LIKAsys lives here, never in the taskbar.</summary>
    public sealed class TrayManager : IDisposable
    {
        private readonly AppSettings _settings;
        private NotifyIcon _icon;
        private ContextMenuStrip _menu;
        private ToolStripMenuItem _miShow, _miMin, _miTop, _miClick, _miLock, _miPos;

        // --- live icon state
        private Icon _liveIcon;
        private IntPtr _liveHandle = IntPtr.Zero;
        private string _lastDrawn;
        private readonly Font[] _fontCache = new Font[8];

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool DestroyIcon(IntPtr handle);

        public event EventHandler ToggleWidget;
        public event EventHandler ToggleMinimize;
        public event EventHandler<UiProfile> ProfilePicked;
        public event EventHandler OpenSettings;
        public event EventHandler CheckUpdates;
        public event EventHandler OpenLog;
        public event EventHandler RescueWidget;
        public event EventHandler ExitApp;
        public event EventHandler Changed;

        public TrayManager(AppSettings settings)
        {
            _settings = settings;
            Build();
        }

        private void Build()
        {
            _menu = new ContextMenuStrip
            {
                Renderer = new ToolStripProfessionalRenderer(new DarkColors()),
                BackColor = Color.FromArgb(19, 26, 38),
                ForeColor = Color.FromArgb(234, 242, 255),
                ShowImageMargin = false,
                Font = new Font("Segoe UI", 9f)
            };

            var header = new ToolStripMenuItem($"{AppInfo.Name}  {AppInfo.VersionText}") { Enabled = false };
            _menu.Items.Add(header);
            _menu.Items.Add(new ToolStripSeparator());

            _miShow = Check(Lang.T("Shfaq widget-in"), _settings.WidgetVisible, (s, e) => ToggleWidget?.Invoke(this, EventArgs.Empty));
            _miMin = Check(Lang.T("Minimizo widget-in"), _settings.Minimized, (s, e) => ToggleMinimize?.Invoke(this, EventArgs.Empty));
            _miTop = Check(Lang.T("Gjithmone siper (always on top)"), _settings.AlwaysOnTop, (s, e) =>
            {
                _settings.AlwaysOnTop = !_settings.AlwaysOnTop;
                Sync(); Changed?.Invoke(this, EventArgs.Empty);
            });
            _miClick = Check(Lang.T("Kalo klikimet pertej (click-through)"), _settings.ClickThrough, (s, e) =>
            {
                _settings.ClickThrough = !_settings.ClickThrough;
                Sync(); Changed?.Invoke(this, EventArgs.Empty);
            });
            _miLock = Check(Lang.T("Blloko pozicionin"), _settings.Locked, (s, e) =>
            {
                _settings.Locked = !_settings.Locked;
                Sync(); Changed?.Invoke(this, EventArgs.Empty);
            });

            _menu.Items.Add(_miShow);
            _menu.Items.Add(_miMin);
            _menu.Items.Add(_miTop);
            _menu.Items.Add(_miClick);
            _menu.Items.Add(_miLock);

            // quick profile switch - one click, no settings window
            var prof = new ToolStripMenuItem(Lang.T("Profili"));
            AddProfile(prof, "Gaming", UiProfile.Gaming);
            AddProfile(prof, "IT", UiProfile.It);
            AddProfile(prof, "IT Apple", UiProfile.Apple);
            _menu.Items.Add(prof);
            _menu.Items.Add(new ToolStripSeparator());

            _miPos = new ToolStripMenuItem(Lang.T("Pozicioni"));
            var pos = _miPos;
            AddPos(pos, Lang.T("Lart majtas"), WidgetPosition.TopLeft);
            AddPos(pos, Lang.T("Lart ne mes"), WidgetPosition.TopCenter);
            AddPos(pos, Lang.T("Lart djathtas"), WidgetPosition.TopRight);
            pos.DropDownItems.Add(new ToolStripSeparator());
            AddPos(pos, Lang.T("Mes majtas"), WidgetPosition.MiddleLeft);
            AddPos(pos, Lang.T("Qendra"), WidgetPosition.Center);
            AddPos(pos, Lang.T("Mes djathtas"), WidgetPosition.MiddleRight);
            pos.DropDownItems.Add(new ToolStripSeparator());
            AddPos(pos, Lang.T("Poshte majtas"), WidgetPosition.BottomLeft);
            AddPos(pos, Lang.T("Poshte ne mes"), WidgetPosition.BottomCenter);
            AddPos(pos, Lang.T("Poshte djathtas"), WidgetPosition.BottomRight);
            _menu.Items.Add(pos);

            _menu.Items.Add(new ToolStripMenuItem(Lang.T("Rikthe widget-in ne ekran"), null, (s, e) => RescueWidget?.Invoke(this, EventArgs.Empty)));

            _menu.Items.Add(new ToolStripSeparator());
            _menu.Items.Add(new ToolStripMenuItem(Lang.T("Cilesimet..."), null, (s, e) => OpenSettings?.Invoke(this, EventArgs.Empty)));
            _menu.Items.Add(new ToolStripMenuItem(Lang.T("Kontrollo per update..."), null, (s, e) => CheckUpdates?.Invoke(this, EventArgs.Empty)));
            _menu.Items.Add(new ToolStripMenuItem(Lang.T("Hap regjistrin (log)"), null, (s, e) => OpenLog?.Invoke(this, EventArgs.Empty)));
            _menu.Items.Add(new ToolStripMenuItem("Likaapps.com", null, (s, e) => AppInfo.OpenUrl(AppInfo.Website)));
            _menu.Items.Add(new ToolStripSeparator());
            _menu.Items.Add(new ToolStripMenuItem(Lang.T("Dil"), null, (s, e) => ExitApp?.Invoke(this, EventArgs.Empty)));

            _icon = new NotifyIcon
            {
                Icon = LoadIcon(),
                Text = $"{AppInfo.Name} {AppInfo.VersionText}",
                Visible = true,
                ContextMenuStrip = _menu
            };
            _lastDrawn = null;
            _icon.DoubleClick += (s, e) => ToggleWidget?.Invoke(this, EventArgs.Empty);
            _icon.BalloonTipClicked += (s, e) => CheckUpdates?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>Throws the menu away and builds it again in the current language.</summary>
        public void Rebuild()
        {
            try
            {
                var old = _menu;
                var icon = _icon;
                _icon = null;
                Build();
                if (icon != null) { icon.Visible = false; icon.Dispose(); }
                if (old != null) old.Dispose();
                Sync();
            }
            catch { }
        }

        private ToolStripMenuItem Check(string text, bool state, EventHandler handler)
        {
            var mi = new ToolStripMenuItem(text, null, handler) { Checked = state, CheckOnClick = false };
            return mi;
        }

        private void AddProfile(ToolStripMenuItem parent, string text, UiProfile p)
        {
            var mi = new ToolStripMenuItem(text, null, (s, e) => ProfilePicked?.Invoke(this, p))
            { Checked = _settings.Profile == p };
            parent.DropDownItems.Add(mi);
        }

        private void AddPos(ToolStripMenuItem parent, string text, WidgetPosition p)
        {
            var mi = new ToolStripMenuItem(text, null, (s, e) =>
            {
                _settings.Position = p;
                Sync();
                Changed?.Invoke(this, EventArgs.Empty);
            })
            { Checked = _settings.Position == p };
            parent.DropDownItems.Add(mi);
        }

        public void Sync()
        {
            try
            {
                if (_miShow != null) _miShow.Checked = _settings.WidgetVisible;
                if (_miMin != null) _miMin.Checked = _settings.Minimized;
                if (_miTop != null) _miTop.Checked = _settings.AlwaysOnTop;
                if (_miClick != null) _miClick.Checked = _settings.ClickThrough;
                if (_miLock != null) _miLock.Checked = _settings.Locked;
                foreach (ToolStripItem item in _menu.Items)
                {
                    if (item is ToolStripMenuItem m && ReferenceEquals(m, _miPos))
                    {
                        int i = 0;
                        foreach (ToolStripItem sub in m.DropDownItems)
                        {
                            if (sub is ToolStripMenuItem sm)
                            {
                                var order = new[]
                                {
                                    WidgetPosition.TopLeft, WidgetPosition.TopCenter, WidgetPosition.TopRight,
                                    WidgetPosition.MiddleLeft, WidgetPosition.Center, WidgetPosition.MiddleRight,
                                    WidgetPosition.BottomLeft, WidgetPosition.BottomCenter, WidgetPosition.BottomRight
                                };
                                if (i < order.Length) sm.Checked = _settings.Position == order[i];
                                i++;
                            }
                        }
                    }
                }
            }
            catch { }
        }


        // ================================================================== live icon

        /// <summary>
        /// Draws the chosen number straight onto the tray icon, so the reading is there
        /// even with the widget hidden - and it costs no screen space at all.
        /// </summary>
        public void UpdateLiveIcon(MetricsSnapshot snap)
        {
            try
            {
                if (_icon == null) return;

                var mode = _settings.TrayIconMode;
                if (mode == TrayIconMode.Logo)
                {
                    if (_lastDrawn != null) { _lastDrawn = null; SwapIcon(null); }
                    return;
                }
                if (snap == null) return;

                double value, pct;
                switch (mode)
                {
                    case TrayIconMode.Cpu: value = snap.CpuLoad; pct = snap.CpuLoad; break;
                    case TrayIconMode.CpuTemp: value = snap.CpuTemp; pct = snap.CpuTemp; break;
                    case TrayIconMode.Gpu: value = snap.GpuLoad; pct = snap.GpuLoad; break;
                    case TrayIconMode.GpuTemp: value = snap.GpuTemp; pct = snap.GpuTemp; break;
                    case TrayIconMode.Ram: value = snap.RamLoad; pct = snap.RamLoad; break;
                    case TrayIconMode.Fps: value = snap.Fps; pct = snap.Fps > 0 ? snap.Fps / 2.4 : 0; break;
                    default: return;
                }

                string text = value > 0 ? Math.Round(value).ToString("0") : "--";
                if (text.Length > 3) text = "999";
                if (text == _lastDrawn) return;          // nothing changed - skip the GDI work
                _lastDrawn = text;

                using (var bmp = Render(text, pct, Tint(pct)))
                {
                    IntPtr h = bmp.GetHicon();
                    try { SwapIcon(Icon.FromHandle(h), h); }
                    catch { DestroyIcon(h); }
                }
            }
            catch { }
        }

        private void SwapIcon(Icon fresh, IntPtr handle = default)
        {
            var oldIcon = _liveIcon;
            var oldHandle = _liveHandle;

            _liveIcon = fresh;
            _liveHandle = handle;
            if (_icon != null) _icon.Icon = fresh ?? LoadIcon();

            // the handle behind Icon.FromHandle is ours to free, or GDI objects pile up
            try { oldIcon?.Dispose(); } catch { }
            if (oldHandle != IntPtr.Zero) { try { DestroyIcon(oldHandle); } catch { } }
        }

        private Color Tint(double pct)
        {
            if (pct >= 90) return Hex(_settings.DangerColor, Color.FromArgb(0xFF, 0x4D, 0x5E));
            if (pct >= 75) return Hex(_settings.WarnColor, Color.FromArgb(0xFF, 0xB0, 0x20));
            return Hex(_settings.Accent, Color.FromArgb(0x00, 0xE5, 0xFF));
        }

        private static Color Hex(string hex, Color fallback)
        {
            try
            {
                var t = (hex ?? "").Trim().TrimStart('#');
                if (t.Length == 8) t = t.Substring(2);          // drop alpha
                if (t.Length != 6) return fallback;
                return Color.FromArgb(
                    Convert.ToInt32(t.Substring(0, 2), 16),
                    Convert.ToInt32(t.Substring(2, 2), 16),
                    Convert.ToInt32(t.Substring(4, 2), 16));
            }
            catch { return fallback; }
        }

        /// <summary>
        /// A dark rounded badge with the number on it. The badge is what keeps the reading
        /// legible on a light taskbar as well as a dark one.
        /// </summary>
        private Bitmap Render(string text, double pct, Color tint)
        {
            int size = 16;
            try { size = SystemInformation.SmallIconSize.Width; } catch { }
            if (size < 16) size = 16;
            if (size > 32) size = 32;

            var bmp = new Bitmap(size, size, PixelFormat.Format32bppArgb);
            using (var g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
                g.Clear(Color.Transparent);

                int r = Math.Max(2, size / 5);
                using (var path = Rounded(new Rectangle(0, 0, size, size), r))
                using (var bg = new SolidBrush(Color.FromArgb(232, 10, 14, 21)))
                    g.FillPath(bg, path);

                int barH = Math.Max(2, size / 8);
                int barY = size - barH - 1;
                using (var track = new SolidBrush(Color.FromArgb(70, 255, 255, 255)))
                    g.FillRectangle(track, 2, barY, size - 4, barH);
                double clamped = pct < 0 ? 0 : (pct > 100 ? 100 : pct);
                float w = (float)((size - 4) * clamped / 100.0);
                if (w > 0.5f)
                    using (var fill = new SolidBrush(tint))
                        g.FillRectangle(fill, 2, barY, w, barH);

                var font = FontFor(text.Length, size, barH);
                var area = new RectangleF(0, -1, size, size - barH - 1);
                using (var fmt = new StringFormat
                {
                    Alignment = StringAlignment.Center,
                    LineAlignment = StringAlignment.Center,
                    FormatFlags = StringFormatFlags.NoWrap
                })
                using (var brush = new SolidBrush(tint))
                    g.DrawString(text, font, brush, area, fmt);
            }
            return bmp;
        }

        private Font FontFor(int len, int size, int barH)
        {
            int slot = Math.Max(1, Math.Min(3, len));
            if (_fontCache[slot] != null) return _fontCache[slot];

            int usable = size - barH - 1;
            float em = slot <= 2 ? usable * 0.95f : usable * 0.70f;
            if (em < 6f) em = 6f;
            _fontCache[slot] = new Font(new System.Drawing.FontFamily("Segoe UI"), em,
                System.Drawing.FontStyle.Bold, GraphicsUnit.Pixel);
            return _fontCache[slot];
        }

        private static GraphicsPath Rounded(Rectangle b, int r)
        {
            var p = new GraphicsPath();
            int d = r * 2;
            p.AddArc(b.X, b.Y, d, d, 180, 90);
            p.AddArc(b.Right - d, b.Y, d, d, 270, 90);
            p.AddArc(b.Right - d, b.Bottom - d, d, d, 0, 90);
            p.AddArc(b.X, b.Bottom - d, d, d, 90, 90);
            p.CloseFigure();
            return p;
        }

        public void Notify(string title, string text, bool warning = false)
        {
            try
            {
                _icon.BalloonTipTitle = title;
                _icon.BalloonTipText = text;
                _icon.BalloonTipIcon = warning ? ToolTipIcon.Warning : ToolTipIcon.Info;
                _icon.ShowBalloonTip(8000);
            }
            catch { }
        }

        public void SetTooltip(string text)
        {
            try { if (_icon != null) _icon.Text = text.Length > 62 ? text.Substring(0, 62) : text; } catch { }
        }

        private static Icon LoadIcon()
        {
            try
            {
                var uri = new Uri("pack://application:,,,/assets/LIKAsys.ico", UriKind.Absolute);
                var info = System.Windows.Application.GetResourceStream(uri);
                if (info != null) return new Icon(info.Stream, SystemInformation.SmallIconSize);   // pick the frame that fits, never upscale
            }
            catch { }
            try { return Icon.ExtractAssociatedIcon(AppInfo.ExePath); } catch { }
            return SystemIcons.Application;
        }

        public void Dispose()
        {
            try { if (_icon != null) { _icon.Visible = false; _icon.Dispose(); } } catch { }
            try { _menu?.Dispose(); } catch { }
            try { _liveIcon?.Dispose(); } catch { }
            if (_liveHandle != IntPtr.Zero) { try { DestroyIcon(_liveHandle); } catch { } }
            foreach (var f in _fontCache) { try { f?.Dispose(); } catch { } }
            _liveHandle = IntPtr.Zero;
            _liveIcon = null;
            _icon = null;
        }

        private sealed class DarkColors : ProfessionalColorTable
        {
            private static readonly Color Bg = Color.FromArgb(19, 26, 38);
            private static readonly Color Hover = Color.FromArgb(32, 43, 62);
            private static readonly Color Line = Color.FromArgb(44, 56, 76);

            public override Color MenuItemSelected => Hover;
            public override Color MenuItemSelectedGradientBegin => Hover;
            public override Color MenuItemSelectedGradientEnd => Hover;
            public override Color MenuItemBorder => Color.FromArgb(0, 229, 255);
            public override Color MenuBorder => Line;
            public override Color ToolStripDropDownBackground => Bg;
            public override Color ImageMarginGradientBegin => Bg;
            public override Color ImageMarginGradientMiddle => Bg;
            public override Color ImageMarginGradientEnd => Bg;
            public override Color SeparatorDark => Line;
            public override Color SeparatorLight => Line;
            public override Color MenuItemPressedGradientBegin => Bg;
            public override Color MenuItemPressedGradientEnd => Bg;
            public override Color CheckBackground => Color.FromArgb(0, 229, 255);
            public override Color CheckSelectedBackground => Color.FromArgb(0, 229, 255);
        }
    }
}
