using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.Linq;
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
        private ToolStripMenuItem _miShow, _miMin, _miTop, _miClick, _miLock, _miPos, _miSize;

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
        public event EventHandler<string> ThemePicked;
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
            try { Microsoft.Win32.SystemEvents.UserPreferenceChanged += OnSystemPreferenceChanged; } catch { }
        }

        private void Build()
        {
            ReadSystemTheme();

            _menu = new ContextMenuStrip
            {
                Renderer = _skin,
                ShowImageMargin = false,
                ShowCheckMargin = false,
                Font = MenuFont()
            };

            // --- what the widget is doing right now
            _miShow = Check(Lang.T("Shfaq widget-in"), _settings.WidgetVisible,
                (s, e) => ToggleWidget?.Invoke(this, EventArgs.Empty));
            // Simple mode keeps nine items. The full menu has seventeen, which is a lot
            // to read while a game is running.
            bool adv = _settings.Advanced;

            _menu.Items.Add(_miShow);
            if (adv)
                _menu.Items.Add(Plain(Lang.T("Rikthe widget-in në ekran"),
                    (s, e) => RescueWidget?.Invoke(this, EventArgs.Empty)));
            _menu.Items.Add(Plain(Lang.T("Cilësimet..."),
                (s, e) => OpenSettings?.Invoke(this, EventArgs.Empty)));

            _menu.Items.Add(new ToolStripSeparator());

            // --- the four switches
            _miTop = Check(Lang.T("Gjithmonë sipër"), _settings.AlwaysOnTop, (s, e) =>
            {
                _settings.AlwaysOnTop = !_settings.AlwaysOnTop;
                Sync(); Changed?.Invoke(this, EventArgs.Empty);
            });
            _miClick = Check(Lang.T("Përshkueshme nga klikimi"), _settings.ClickThrough, (s, e) =>
            {
                _settings.ClickThrough = !_settings.ClickThrough;
                Sync(); Changed?.Invoke(this, EventArgs.Empty);
            });
            _miLock = Check(Lang.T("Blloko pozicionin"), _settings.Locked, (s, e) =>
            {
                _settings.Locked = !_settings.Locked;
                Sync(); Changed?.Invoke(this, EventArgs.Empty);
            });
            _miMin = Check(Lang.T("Minimizo"), _settings.Minimized,
                (s, e) => ToggleMinimize?.Invoke(this, EventArgs.Empty));

            _menu.Items.Add(_miTop);
            if (adv)
            {
                _menu.Items.Add(_miClick);
                _menu.Items.Add(_miLock);
                _menu.Items.Add(_miMin);
            }

            // --- everything that opens a submenu

            // quick profile switch - one click, no settings window
            var prof = new ToolStripMenuItem(Lang.T("Profili"));
            AddProfile(prof, "Gaming", UiProfile.Gaming);
            AddProfile(prof, "IT", UiProfile.It);
            _menu.Items.Add(prof);

            if (!adv)
            {
                // one theme list instead of two, holding the looks of the profile in use
                var quick = new ToolStripMenuItem(Lang.T("Tema"));
                string grp = _settings.Profile == UiProfile.It ? ThemeLibrary.GPune : ThemeLibrary.GLoja;
                foreach (var t in ThemeLibrary.All.Where(x => x.Group == grp))
                    AddTheme(quick, t.Name);
                if (quick.DropDownItems.Count > 0) _menu.Items.Add(quick);
            }

            // the game looks are one right-click away - no settings window in the middle
            // of a match, which is the only time anyone actually wants to change them.
            var games = new ToolStripMenuItem(Lang.T("Tema e lojës"));
            if (!adv) games = null;
            if (games != null)
            {
                foreach (var t in ThemeLibrary.All.Where(x => x.Group == ThemeLibrary.GLoja))
                    AddTheme(games, t.Name);
                if (games.DropDownItems.Count > 0) _menu.Items.Add(games);

                var work = new ToolStripMenuItem(Lang.T("Tema e punës"));
                foreach (var t in ThemeLibrary.All.Where(x => x.Group == ThemeLibrary.GPune))
                    AddTheme(work, t.Name);
                if (work.DropDownItems.Count > 0) _menu.Items.Add(work);
            }

            // pointer packs belong to the IT profile, so the item is simply absent in Gaming
            if (adv && _settings.Profile == UiProfile.It)
            {
                var mouse = new ToolStripMenuItem(Lang.T("Kursori i mouse-it"));
                foreach (var pack in MouseCursors.All) AddCursor(mouse, pack);
                _menu.Items.Add(mouse);
            }

            // size, right here. Dragging the corner of the widget does the same thing.
            _miSize = new ToolStripMenuItem(Lang.T("Madhësia"));
            foreach (var pct in SizeSteps)
            {
                int p = pct;
                var mi = new ToolStripMenuItem(p + "%", null, (s, e) =>
                {
                    _settings.Scale = p / 100.0;
                    Sync(); Changed?.Invoke(this, EventArgs.Empty);
                })
                { Checked = Math.Abs(_settings.Scale - p / 100.0) < 0.005 };
                _miSize.DropDownItems.Add(mi);
            }
            _menu.Items.Add(_miSize);

            _miPos = new ToolStripMenuItem(Lang.T("Pozicioni"));
            AddPos(_miPos, Lang.T("Lart majtas"), WidgetPosition.TopLeft);
            AddPos(_miPos, Lang.T("Lart në mes"), WidgetPosition.TopCenter);
            AddPos(_miPos, Lang.T("Lart djathtas"), WidgetPosition.TopRight);
            _miPos.DropDownItems.Add(new ToolStripSeparator());
            AddPos(_miPos, Lang.T("Mes majtas"), WidgetPosition.MiddleLeft);
            AddPos(_miPos, Lang.T("Qendra"), WidgetPosition.Center);
            AddPos(_miPos, Lang.T("Mes djathtas"), WidgetPosition.MiddleRight);
            _miPos.DropDownItems.Add(new ToolStripSeparator());
            AddPos(_miPos, Lang.T("Poshtë majtas"), WidgetPosition.BottomLeft);
            AddPos(_miPos, Lang.T("Poshtë në mes"), WidgetPosition.BottomCenter);
            AddPos(_miPos, Lang.T("Poshtë djathtas"), WidgetPosition.BottomRight);
            _menu.Items.Add(_miPos);

            _menu.Items.Add(new ToolStripSeparator());

            _menu.Items.Add(Plain(Lang.T("Kontrollo për update"),
                (s, e) => CheckUpdates?.Invoke(this, EventArgs.Empty)));

            // the version, the credit and the log all moved in here, so the menu itself
            // stays short enough to read in one go
            var about = new ToolStripMenuItem(Lang.T("Rreth LIKAsys"));
            about.DropDownItems.Add(new ToolStripMenuItem(
                AppInfo.Name + "  " + AppInfo.VersionText) { Enabled = false });
            about.DropDownItems.Add(new ToolStripMenuItem(
                "Made in Kosovo with \u2764") { Enabled = false });
            about.DropDownItems.Add(new ToolStripSeparator());
            about.DropDownItems.Add(Plain("Likaapps.com", (s, e) => AppInfo.OpenUrl(AppInfo.Website)));
            about.DropDownItems.Add(Plain(Lang.T("Hap regjistrin"),
                (s, e) => OpenLog?.Invoke(this, EventArgs.Empty)));
            _menu.Items.Add(about);

            _menu.Items.Add(new ToolStripSeparator());
            _menu.Items.Add(Plain(Lang.T("Dil"), (s, e) => ExitApp?.Invoke(this, EventArgs.Empty)));

            Skin(_menu);

            _icon = new NotifyIcon
            {
                Icon = LoadIcon(),
                Text = AppInfo.Name + " " + AppInfo.VersionText,
                Visible = true,
                ContextMenuStrip = _menu
            };
            _lastDrawn = null;
            _icon.DoubleClick += (s, e) => ToggleWidget?.Invoke(this, EventArgs.Empty);
            _icon.BalloonTipClicked += (s, e) => CheckUpdates?.Invoke(this, EventArgs.Empty);
        }

        private static ToolStripMenuItem Plain(string text, EventHandler handler)
        {
            return new ToolStripMenuItem(text, null, handler);
        }

        private static Font MenuFont()
        {
            try { return new Font("Segoe UI", 9.75f, System.Drawing.FontStyle.Regular, GraphicsUnit.Point); }
            catch { return System.Drawing.SystemFonts.MenuFont ?? System.Drawing.SystemFonts.DefaultFont; }
        }

        // ================================================================== the look

        /// <summary>
        /// Paints every dropdown in the palette, gives each row room to breathe and
        /// hangs the rounded-corner call on the open event.
        /// </summary>
        private void Skin(ToolStripDropDown dd)
        {
            if (dd == null) return;
            var p = _pal;

            dd.Renderer = _skin;
            dd.BackColor = p.Bg;
            dd.ForeColor = p.Text;
            dd.Padding = new Padding(0, 6, 0, 6);

            var dm = dd as ToolStripDropDownMenu;
            if (dm != null) { dm.ShowImageMargin = false; dm.ShowCheckMargin = false; }

            foreach (ToolStripItem it in dd.Items)
            {
                it.BackColor = p.Bg;
                it.ForeColor = p.Text;

                if (it is ToolStripSeparator) { it.Margin = new Padding(0, 4, 0, 4); continue; }

                // the left pad is the gutter the tick is drawn in, the right pad is the
                // room the chevron needs. Both are reserved here so the row measures wide
                // enough and the text never runs underneath either of them.
                it.Padding = new Padding(MenuSkin.Gutter, 5, MenuSkin.RightPad, 5);

                var mi = it as ToolStripMenuItem;
                if (mi != null && mi.HasDropDownItems) Skin(mi.DropDown);
            }

            dd.Opened -= OnDropOpened;
            dd.Opened += OnDropOpened;
        }

        private void OnDropOpened(object sender, EventArgs e)
        {
            var dd = sender as ToolStripDropDown;
            if (dd == null) return;
            try { RoundCorners(dd); } catch { }
        }

        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int size);

        private static readonly bool Win11 = Environment.OSVersion.Version.Build >= 22000;

        /// <summary>
        /// Windows 11 rounds the window for us, properly, shadow and all. On Windows 10
        /// there is no such call, so the window gets clipped to a rounded shape by hand.
        /// </summary>
        private static void RoundCorners(ToolStripDropDown dd)
        {
            if (Win11)
            {
                int round = 2;                       // DWMWCP_ROUND
                DwmSetWindowAttribute(dd.Handle, 33, ref round, sizeof(int));
                return;
            }
            using (var path = MenuSkin.Round(new Rectangle(0, 0, dd.Width, dd.Height), 7))
                dd.Region = new Region(path);
        }

        // ---- light or dark, whichever Windows is in

        private Palette _pal = Palette.Dark;
        private readonly MenuSkin _skin = new MenuSkin();

        /// <summary>Reads the Windows setting and points the renderer at the right palette.</summary>
        private void ReadSystemTheme()
        {
            _pal = SystemIsDark() ? Palette.Dark : Palette.Light;
            MenuSkin.Active = _pal;
        }

        private static bool SystemIsDark()
        {
            try
            {
                using (var k = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(
                    @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize"))
                {
                    if (k != null && k.GetValue("AppsUseLightTheme") is int v) return v == 0;
                }
            }
            catch { }
            return true;   // LIKAsys is a dark app by nature, so dark is the safe guess
        }

        private void OnSystemPreferenceChanged(object sender, Microsoft.Win32.UserPreferenceChangedEventArgs e)
        {
            if (e.Category != Microsoft.Win32.UserPreferenceCategory.General &&
                e.Category != Microsoft.Win32.UserPreferenceCategory.Color &&
                e.Category != Microsoft.Win32.UserPreferenceCategory.VisualStyle) return;
            try
            {
                var app = System.Windows.Application.Current;
                if (app == null) { Repaint(); return; }
                app.Dispatcher.BeginInvoke(new Action(Repaint));
            }
            catch { }
        }

        private void Repaint()
        {
            try
            {
                ReadSystemTheme();
                if (_menu != null) { Skin(_menu); _menu.Invalidate(); }
            }
            catch { }
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

        /// <summary>The percentages the tray offers. Anything in between comes from the grip.</summary>
        private static readonly int[] SizeSteps = { 75, 90, 100, 110, 125, 150, 175, 200 };

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

        private void AddTheme(ToolStripMenuItem parent, string name)
        {
            var mi = new ToolStripMenuItem(name, null, (s, e) => ThemePicked?.Invoke(this, name))
            { Checked = string.Equals(_settings.ThemeName, name, StringComparison.OrdinalIgnoreCase) };
            parent.DropDownItems.Add(mi);
        }

        private void AddCursor(ToolStripMenuItem parent, CursorPack pack)
        {
            var mi = new ToolStripMenuItem(Lang.T(pack.Name), null, (s, e) =>
            {
                _settings.MouseCursor = pack.Style;
                Sync();
                Changed?.Invoke(this, EventArgs.Empty);
            })
            { Checked = _settings.MouseCursor == pack.Style };
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
                if (_miSize != null)
                {
                    int i = 0;
                    foreach (ToolStripItem sub in _miSize.DropDownItems)
                    {
                        if (sub is ToolStripMenuItem sm && i < SizeSteps.Length)
                            sm.Checked = Math.Abs(_settings.Scale - SizeSteps[i] / 100.0) < 0.005;
                        i++;
                    }
                }
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
            try { Microsoft.Win32.SystemEvents.UserPreferenceChanged -= OnSystemPreferenceChanged; } catch { }
            try { if (_icon != null) { _icon.Visible = false; _icon.Dispose(); } } catch { }
            try { _menu?.Dispose(); } catch { }
            try { _liveIcon?.Dispose(); } catch { }
            if (_liveHandle != IntPtr.Zero) { try { DestroyIcon(_liveHandle); } catch { } }
            foreach (var f in _fontCache) { try { f?.Dispose(); } catch { } }
            _liveHandle = IntPtr.Zero;
            _liveIcon = null;
            _icon = null;
        }

        /// <summary>The nine colours the menu is built out of.</summary>
        private sealed class Palette
        {
            public Color Bg, Hover, Text, Muted, Line, Accent, Border;

            public static readonly Palette Dark = new Palette
            {
                Bg = Color.FromArgb(28, 31, 38),
                Hover = Color.FromArgb(43, 48, 58),
                Text = Color.FromArgb(228, 231, 238),
                Muted = Color.FromArgb(138, 146, 161),
                Line = Color.FromArgb(48, 52, 61),
                Accent = Color.FromArgb(0, 229, 255),
                Border = Color.FromArgb(54, 59, 70)
            };

            public static readonly Palette Light = new Palette
            {
                Bg = Color.FromArgb(252, 252, 253),
                Hover = Color.FromArgb(237, 240, 244),
                Text = Color.FromArgb(24, 26, 31),
                Muted = Color.FromArgb(120, 128, 140),
                Line = Color.FromArgb(226, 229, 234),
                Accent = Color.FromArgb(0, 137, 163),
                Border = Color.FromArgb(211, 215, 222)
            };
        }

        /// <summary>
        /// The menu, drawn by hand.
        ///
        /// The renderer Windows Forms ships with paints grey gradients, a sunken check box
        /// and a solid border, which looks nothing like the rest of LIKAsys. This one paints
        /// a flat panel, a rounded highlight under the pointer, a tick in the accent colour
        /// and a thin chevron where there is a submenu. Every colour comes from the palette
        /// that matches whatever Windows is in, light or dark.
        /// </summary>
        private sealed class MenuSkin : ToolStripRenderer
        {
            public const int Gutter = 31;       // the strip on the left the tick lives in
            public const int RightPad = 30;     // the strip on the right the chevron lives in

            public static Palette Active = Palette.Dark;

            /// <summary>
            /// How much bigger everything is than at 100% screen scaling.
            ///
            /// Windows hands the item a taller box when the user runs at 125% or 150%,
            /// because the font grew. Reading the scale back out of that height keeps the
            /// tick and the gutter in step with the text without asking anyone for the DPI.
            /// </summary>
            private static float K(ToolStripItem item)
            {
                float k = item.Height / 30f;
                return k < 1f ? 1f : (k > 4f ? 4f : k);
            }

            protected override void OnRenderToolStripBackground(ToolStripRenderEventArgs e)
            {
                using (var b = new SolidBrush(Active.Bg))
                    e.Graphics.FillRectangle(b, e.AffectedBounds);
            }

            protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e)
            {
                var r = e.AffectedBounds;
                r.Width -= 1; r.Height -= 1;
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using (var path = Round(r, 8))
                using (var pen = new Pen(Active.Border))
                    e.Graphics.DrawPath(pen, path);
            }

            protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
            {
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;

                float k = K(e.Item);

                if (e.Item.Selected && e.Item.Enabled)
                {
                    int in1 = (int)Math.Round(4 * k);
                    var r = new Rectangle(in1, 1, Math.Max(1, e.Item.Width - in1 * 2 - 1),
                                          Math.Max(1, e.Item.Height - 2));
                    using (var path = Round(r, (int)Math.Round(6 * k)))
                    using (var b = new SolidBrush(Active.Hover))
                        g.FillPath(b, path);
                }

                var mi = e.Item as ToolStripMenuItem;
                if (mi != null && mi.Checked)
                    Tick(g, e.Item.Height, k, e.Item.Enabled ? Active.Accent : Active.Muted);
            }

            /// <summary>The tick in the gutter. Two strokes, round caps, nothing else.</summary>
            private static void Tick(Graphics g, int height, float k, Color c)
            {
                float cy = height / 2f;
                using (var pen = new Pen(c, 1.7f * k)
                {
                    StartCap = LineCap.Round,
                    EndCap = LineCap.Round,
                    LineJoin = LineJoin.Round
                })
                {
                    g.DrawLines(pen, new[]
                    {
                        new PointF(11.5f * k, cy + 0.4f * k),
                        new PointF(15f * k, cy + 3.9f * k),
                        new PointF(21.5f * k, cy - 4.1f * k)
                    });
                }
            }

            protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
            {
                float k = K(e.Item);
                int gut = (int)Math.Round(Gutter * k);
                int pad = (int)Math.Round(RightPad * k);

                e.TextColor = e.Item.Enabled ? Active.Text : Active.Muted;
                e.TextRectangle = new Rectangle(
                    gut, 0, Math.Max(10, e.Item.Width - gut - pad), e.Item.Height);
                e.TextFormat = TextFormatFlags.Left | TextFormatFlags.VerticalCenter
                             | TextFormatFlags.NoPrefix | TextFormatFlags.EndEllipsis;
                base.OnRenderItemText(e);
            }

            protected override void OnRenderArrow(ToolStripArrowRenderEventArgs e)
            {
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                float k = K(e.Item);
                var r = e.ArrowRectangle;
                float x = r.Right - 7f * k, cy = r.Top + r.Height / 2f;
                using (var pen = new Pen(e.Item.Enabled ? Active.Muted : Active.Line, 1.5f * k)
                {
                    StartCap = LineCap.Round,
                    EndCap = LineCap.Round,
                    LineJoin = LineJoin.Round
                })
                {
                    g.DrawLines(pen, new[]
                    {
                        new PointF(x - 3.2f * k, cy - 3.6f * k),
                        new PointF(x + 0.4f * k, cy),
                        new PointF(x - 3.2f * k, cy + 3.6f * k)
                    });
                }
            }

            protected override void OnRenderSeparator(ToolStripSeparatorRenderEventArgs e)
            {
                int y = e.Item.Height / 2;
                using (var pen = new Pen(Active.Line))
                    e.Graphics.DrawLine(pen, 12, y, Math.Max(13, e.Item.Width - 12), y);
            }

            protected override void OnRenderImageMargin(ToolStripRenderEventArgs e) { }

            /// <summary>A rectangle with the corners rounded off.</summary>
            public static GraphicsPath Round(Rectangle r, int radius)
            {
                var path = new GraphicsPath();
                int d = radius * 2;
                if (d <= 0 || r.Width <= d || r.Height <= d) { path.AddRectangle(r); return path; }
                path.AddArc(r.X, r.Y, d, d, 180, 90);
                path.AddArc(r.Right - d, r.Y, d, d, 270, 90);
                path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
                path.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
                path.CloseFigure();
                return path;
            }
        }
    }
}
