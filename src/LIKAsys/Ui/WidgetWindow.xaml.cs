using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Effects;
using LIKAsys.Core;
using LIKAsys.Monitoring;

namespace LIKAsys.Ui
{
    public partial class WidgetWindow : Window
    {
        private readonly AppSettings _settings;
        private readonly MetricsService _metrics;
        private readonly ObservableCollection<MetricRowVm> _rows = new ObservableCollection<MetricRowVm>();
        private double _fpsScale = 60;
        private double _netScale = 10;
        private bool _ready;
        private bool _hiddenByGameRule;

        public event EventHandler SettingsRequested;
        public event EventHandler<Point> MenuRequested;

        public WidgetWindow(AppSettings settings, MetricsService metrics)
        {
            InitializeComponent();
            _settings = settings;
            _metrics = metrics;

            RowsHost.ItemsSource = _rows;

            MouseLeftButtonDown += OnDragStart;
            MouseLeftButtonUp += OnDragEnd;
            MouseRightButtonUp += (s, e) => MenuRequested?.Invoke(this, PointToScreen(e.GetPosition(this)));
            SizeChanged += (s, e) => { if (_ready) { UpdateBlurAndRegion(); WidgetPlacement.Apply(this, _settings); } };

            if (_metrics != null) _metrics.Updated += OnMetrics;
            Localize();
            BuildRows();
            ApplySettings();
        }

        /// <summary>Re-renders the tooltips and the footer in the current language.</summary>
        public void Localize()
        {
            try { Lang.Localize(this); } catch { }
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            WidgetPlacement.ApplyWindowFlags(this, _settings);
            _ready = true;
            UpdateBlurAndRegion();
            Dispatcher.BeginInvoke(new Action(() =>
            {
                UpdateBlurAndRegion();
                WidgetPlacement.Apply(this, _settings);
            }), System.Windows.Threading.DispatcherPriority.Loaded);
        }

        // ================================================================== rows

        public void BuildRows()
        {
            _rows.Clear();
            if (_settings.ShowCpu) _rows.Add(NewRow("cpu", "CPU", "IconCpu"));
            if (_settings.ShowGpu) _rows.Add(NewRow("gpu", "GPU", "IconGpu"));
            if (_settings.ShowVram) _rows.Add(NewRow("vram", "VRAM", "IconGpu"));
            if (_settings.ShowRam) _rows.Add(NewRow("ram", "RAM", "IconRam"));
            if (_settings.ShowFps) _rows.Add(NewRow("fps", "FPS", "IconFps"));
            if (_settings.ShowFpsLow) _rows.Add(NewRow("fpslow", "1% LOW", "IconLow"));
            if (_settings.ShowFrameTime) _rows.Add(NewRow("frametime", "FRAME", "IconFrame"));
            if (_settings.ShowNet) _rows.Add(NewRow("net", "NET", "IconNet"));
            if (_settings.ShowPing) _rows.Add(NewRow("ping", "PING", "IconPing"));
            if (_rows.Count == 0) _rows.Add(NewRow("cpu", "CPU", "IconCpu"));
            StyleRows();
            if (_metrics?.Latest != null) OnMetrics(_metrics.Latest);
        }

        private MetricRowVm NewRow(string key, string label, string iconKey)
        {
            Geometry geo = null;
            try { geo = Application.Current.TryFindResource(iconKey) as Geometry; } catch { }
            return new MetricRowVm
            {
                Key = key,
                Label = _settings.UpperCaseLabels ? label.ToUpperInvariant() : label,
                Icon = geo
            };
        }

        // ================================================================== colours

        private Color C(string hex, Color fallback)
        {
            try { return (Color)ColorConverter.ConvertFromString(hex); }
            catch { return fallback; }
        }

        private Color Accent => C(_settings.Accent, Color.FromRgb(0x00, 0xE5, 0xFF));
        private Color Accent2 => C(_settings.Accent2, Accent);
        private Color TextC => C(_settings.TextColor, Color.FromRgb(0xEA, 0xF2, 0xFF));
        private Color LabelC => C(_settings.LabelColor, Color.FromRgb(0x93, 0xA6, 0xBE));
        private Color DetailC => C(_settings.DetailColor, Color.FromRgb(0x5D, 0x6E, 0x85));
        private Color TrackC => C(_settings.TrackColor, Color.FromRgb(0x1B, 0x24, 0x33));
        private Color WarnC => C(_settings.WarnColor, Color.FromRgb(0xFF, 0xB0, 0x20));
        private Color DangerC => C(_settings.DangerColor, Color.FromRgb(0xFF, 0x4D, 0x5E));

        private static SolidColorBrush Solid(Color c) { var b = new SolidColorBrush(c); b.Freeze(); return b; }

        private static FontWeight Weight(string name)
        {
            switch ((name ?? "").ToLowerInvariant())
            {
                case "thin": return FontWeights.Thin;
                case "light": return FontWeights.Light;
                case "normal": return FontWeights.Normal;
                case "medium": return FontWeights.Medium;
                case "semibold": return FontWeights.SemiBold;
                case "bold": return FontWeights.Bold;
                case "black": return FontWeights.Black;
                default: return FontWeights.SemiBold;
            }
        }

        // ================================================================== look

        public void ApplySettings()
        {
            try
            {
                try { FontFamily = new FontFamily(string.IsNullOrWhiteSpace(_settings.FontFamily) ? "Segoe UI" : _settings.FontFamily); }
                catch { FontFamily = new FontFamily("Segoe UI"); }

                ScaleHost.LayoutTransform = Math.Abs(_settings.Scale - 1.0) < 0.001
                    ? null
                    : new ScaleTransform(_settings.Scale, _settings.Scale);

                double pad = _settings.Blur ? 0 : WidgetPlacement.ShadowPad;
                ScaleHost.Margin = new Thickness(pad);

                Card.CornerRadius = new CornerRadius(_settings.CornerRadius);
                Card.BorderThickness = new Thickness(_settings.BorderThickness);
                Card.Padding = new Thickness(_settings.PaddingH, _settings.PaddingV,
                                             _settings.PaddingH, Math.Max(2, _settings.PaddingV - 1));

                // --- background -------------------------------------------------
                byte a = (byte)Math.Round(_settings.BackgroundOpacity * 255);
                if (a == 0) a = 1;   // invisible to the eye, still catches the mouse
                var top = C(_settings.BgTop, Color.FromRgb(0x15, 0x1C, 0x2B));
                var bot = C(_settings.BgBottom, Color.FromRgb(0x0A, 0x0D, 0x14));

                Brush bg;
                if (_settings.Blur)
                {
                    // the acrylic tint does the heavy lifting - keep only a light sheen on top
                    var g = new LinearGradientBrush { StartPoint = new Point(0, 0), EndPoint = new Point(0.4, 1) };
                    g.GradientStops.Add(new GradientStop(Color.FromArgb(0x1E, top.R, top.G, top.B), 0));
                    g.GradientStops.Add(new GradientStop(Color.FromArgb(0x08, bot.R, bot.G, bot.B), 1));
                    g.Freeze();
                    bg = g;
                }
                else
                {
                    var g = new LinearGradientBrush { StartPoint = new Point(0, 0), EndPoint = new Point(0.4, 1) };
                    g.GradientStops.Add(new GradientStop(Color.FromArgb(a, top.R, top.G, top.B), 0));
                    g.GradientStops.Add(new GradientStop(Color.FromArgb(a, bot.R, bot.G, bot.B), 1));
                    g.Freeze();
                    bg = g;
                }
                Card.Background = bg;

                // --- border -----------------------------------------------------
                if (_settings.BorderThickness <= 0)
                {
                    Card.BorderBrush = Brushes.Transparent;
                }
                else
                {
                    var bc = C(_settings.BorderColor, Color.FromArgb(0x4D, 0x00, 0xE5, 0xFF));
                    if (_settings.AccentGradient)
                    {
                        var ac = Accent;
                        var gb = new LinearGradientBrush { StartPoint = new Point(0, 0), EndPoint = new Point(1, 1) };
                        gb.GradientStops.Add(new GradientStop(bc, 0));
                        gb.GradientStops.Add(new GradientStop(Color.FromArgb((byte)(bc.A / 3), 0xFF, 0xFF, 0xFF), 0.55));
                        gb.GradientStops.Add(new GradientStop(Color.FromArgb(bc.A, ac.R, ac.G, ac.B), 1));
                        gb.Freeze();
                        Card.BorderBrush = gb;
                    }
                    else Card.BorderBrush = Solid(bc);
                }

                // --- shadow -----------------------------------------------------
                Card.Effect = (_settings.ShadowEnabled && !_settings.Blur)
                    ? new DropShadowEffect
                    {
                        BlurRadius = 22,
                        ShadowDepth = 4,
                        Direction = 270,
                        Opacity = _settings.ShadowStrength,
                        Color = Colors.Black
                    }
                    : null;

                // --- text shadow (what makes a background-less widget readable) --
                CardContent.Effect = _settings.TextShadow
                    ? new DropShadowEffect
                    {
                        BlurRadius = 4.5,
                        ShadowDepth = 1.4,
                        Direction = 270,
                        Opacity = 0.95,
                        Color = Colors.Black,
                        RenderingBias = RenderingBias.Quality
                    }
                    : null;

                // --- header / footer -------------------------------------------
                HeaderRow.Visibility = _settings.ShowHeader ? Visibility.Visible : Visibility.Collapsed;
                FooterRow.Visibility = _settings.ShowFooter ? Visibility.Visible : Visibility.Collapsed;
                BrandDot.Visibility = _settings.ShowBrandDot ? Visibility.Visible : Visibility.Collapsed;

                var accent = Accent;
                BrandText.Foreground = Solid(TextC);
                BrandText.FontSize = Math.Max(8, _settings.FontSize - 2);
                BrandDot.Fill = Solid(accent);
                DotGlow.Color = accent;
                DotGlow.Opacity = _settings.GlowEffect ? 0.9 : 0;
                GearIcon.Stroke = Solid(LabelC);
                CloseIcon.Stroke = Solid(LabelC);

                FooterMade.Foreground = Solid(DetailC);
                FooterSep.Foreground = Solid(Color.FromArgb(0x60, DetailC.R, DetailC.G, DetailC.B));
                SiteLink.Foreground = Solid(accent);
                FooterLine.Background = Solid(Color.FromArgb(0x24, TextC.R, TextC.G, TextC.B));
                double footSize = Math.Max(7, _settings.FontSize - 4.5);
                FooterMade.FontSize = footSize;
                FooterSep.FontSize = footSize;
                SiteLink.FontSize = footSize;

                RowsHost.ItemsPanel = (System.Windows.Controls.ItemsPanelTemplate)
                    FindResource(_settings.Layout == WidgetLayout.Vertical ? "VerticalPanel" : "HorizontalPanel");

                Topmost = _settings.AlwaysOnTop;
                StyleRows();

                WidgetPlacement.ApplyWindowFlags(this, _settings);
                UpdateBlurAndRegion();
                if (_ready) WidgetPlacement.Apply(this, _settings);
            }
            catch (Exception ex) { AppInfo.Log("ApplySettings: " + ex.Message); }
        }

        private void UpdateBlurAndRegion()
        {
            try
            {
                var hwnd = new WindowInteropHelper(this).Handle;
                if (hwnd == IntPtr.Zero) return;

                if (_settings.Blur)
                {
                    var bot = C(_settings.BgBottom, Color.FromRgb(0x0A, 0x0D, 0x14));
                    byte a = (byte)Math.Round(Math.Max(0.05, _settings.BackgroundOpacity) * 255);
                    Native.SetBlur(hwnd, true, a, bot.R, bot.G, bot.B);

                    double dpi = 1.0;
                    var src = PresentationSource.FromVisual(this);
                    if (src?.CompositionTarget != null) dpi = src.CompositionTarget.TransformToDevice.M11;

                    int w = (int)Math.Round(ActualWidth * dpi);
                    int h = (int)Math.Round(ActualHeight * dpi);
                    int r = (int)Math.Round(_settings.CornerRadius * _settings.Scale * dpi);
                    if (w > 2 && h > 2) Native.SetRoundRegion(hwnd, w, h, r);
                }
                else
                {
                    Native.SetBlur(hwnd, false, 0, 0, 0, 0);
                    Native.ClearRegion(hwnd);
                }
            }
            catch (Exception ex) { AppInfo.Log("Blur: " + ex.Message); }
        }

        private void StyleRows()
        {
            var accent = Accent;
            var fs = _settings.FontSize;
            bool horizontal = _settings.Layout != WidgetLayout.Vertical;
            bool compact = _settings.Layout == WidgetLayout.Compact;

            Brush iconStroke;
            if (_settings.AccentGradient)
            {
                var gb = new LinearGradientBrush { StartPoint = new Point(0, 0), EndPoint = new Point(1, 1) };
                gb.GradientStops.Add(new GradientStop(Lighten(accent, 0.45), 0));
                gb.GradientStops.Add(new GradientStop(accent, 0.55));
                gb.GradientStops.Add(new GradientStop(Accent2, 1));
                gb.Freeze();
                iconStroke = gb;
            }
            else iconStroke = Solid(accent);

            var shadowBrush = Solid(Color.FromArgb(0x8C, 0x00, 0x00, 0x00));
            var labelBrush = Solid(LabelC);
            var detailBrush = Solid(DetailC);
            var trackBrush = Solid(TrackC);
            var unitBrush = Solid(Color.FromArgb(0xD0, DetailC.R, DetailC.G, DetailC.B));

            var barRadius = _settings.BarStyle == BarStyle.Square
                ? new CornerRadius(0)
                : new CornerRadius(Math.Max(0, _settings.BarHeight / 2.0));
            Brush mask = _settings.BarStyle == BarStyle.Segmented ? SegmentMask() : null;

            foreach (var r in _rows)
            {
                r.LabelSize = Math.Max(7, fs + _settings.LabelSizeOffset);
                r.DetailSize = Math.Max(7, fs + _settings.LabelSizeOffset - 0.5);
                r.ValueSize = Math.Max(8, fs + _settings.ValueSizeOffset);
                r.UnitSize = Math.Max(7, fs - 3.5);
                r.ValueWeight = Weight(_settings.ValueWeight);
                r.LabelWeight = Weight(_settings.LabelWeight);
                r.ValueMinWidth = fs * 2.9;
                r.ValueWidth = TextWidth(ReserveValue(r.Key), r.ValueSize, r.ValueWeight);
                r.DetailWidth = TextWidth(ReserveDetail(r.Key), r.DetailSize, FontWeights.Normal);
                r.IconBox = Math.Max(8, fs + _settings.IconSizeOffset);
                r.BarHeight = _settings.BarHeight;
                r.BarRadius = barRadius;
                r.BarMask = mask;
                r.LabelBrush = labelBrush;
                r.DetailBrush = detailBrush;
                r.TrackBrush = trackBrush;
                r.UnitBrush = unitBrush;
                r.UnitVisibility = _settings.ShowUnits ? Visibility.Visible : Visibility.Collapsed;
                r.BarVisibility = (_settings.ShowBars && !compact && _settings.BarStyle != BarStyle.None)
                    ? Visibility.Visible : Visibility.Collapsed;
                r.RowMargin = horizontal
                    ? new Thickness(0, 1, 16, 1)
                    : new Thickness(0, _settings.RowSpacing, 0, _settings.RowSpacing);

                switch (_settings.IconStyle)
                {
                    case IconStyle.ThreeD:
                        r.IconVisibility = Visibility.Visible;
                        r.ShadowVisibility = Visibility.Visible;
                        r.IconShadowBrush = shadowBrush;
                        r.IconStroke = iconStroke;
                        r.IconFill = null;
                        r.IconThickness = 1.6;
                        r.IconEffect = _settings.GlowEffect ? Glow(accent) : null;
                        break;
                    case IconStyle.Outline:
                        r.IconVisibility = Visibility.Visible;
                        r.ShadowVisibility = Visibility.Collapsed;
                        r.IconStroke = Solid(accent);
                        r.IconFill = null;
                        r.IconThickness = 1.5;
                        r.IconEffect = null;
                        break;
                    case IconStyle.Solid:
                        r.IconVisibility = Visibility.Visible;
                        r.ShadowVisibility = Visibility.Collapsed;
                        r.IconStroke = null;
                        r.IconFill = iconStroke;
                        r.IconThickness = 0;
                        r.IconEffect = _settings.GlowEffect ? Glow(accent) : null;
                        break;
                    default:
                        r.IconVisibility = Visibility.Collapsed;
                        r.ShadowVisibility = Visibility.Collapsed;
                        r.IconEffect = null;
                        break;
                }
            }
        }

        private static Brush SegmentMask()
        {
            var dg = new DrawingGroup();
            dg.Children.Add(new GeometryDrawing(Brushes.White, null, new RectangleGeometry(new Rect(0, 0, 4, 1))));
            var db = new DrawingBrush(dg)
            {
                TileMode = TileMode.Tile,
                Viewbox = new Rect(0, 0, 6, 1),
                ViewboxUnits = BrushMappingMode.Absolute,
                Viewport = new Rect(0, 0, 6, 1),
                ViewportUnits = BrushMappingMode.Absolute,
                Stretch = Stretch.Fill
            };
            db.Freeze();
            return db;
        }

        private static DropShadowEffect Glow(Color c)
        {
            var e = new DropShadowEffect { Color = c, BlurRadius = 9, ShadowDepth = 0, Opacity = 0.75 };
            e.Freeze();
            return e;
        }

        private static Color Lighten(Color c, double amount) => Color.FromRgb(
            (byte)Math.Min(255, c.R + (255 - c.R) * amount),
            (byte)Math.Min(255, c.G + (255 - c.G) * amount),
            (byte)Math.Min(255, c.B + (255 - c.B) * amount));

        // ================================================================== values

        // ================================================================== stable sizing

        /// <summary>
        /// Width of a string in the widget font. Used to nail down the number column so the
        /// card stops resizing - and therefore stops jumping - every time a value changes.
        /// </summary>
        private double TextWidth(string sample, double size, FontWeight weight)
        {
            if (string.IsNullOrEmpty(sample) || size <= 0) return double.NaN;
            try
            {
                double pixelsPerDip = 1.0;
                try { pixelsPerDip = VisualTreeHelper.GetDpi(this).PixelsPerDip; } catch { }

                var tf = new Typeface(FontFamily, FontStyles.Normal, weight, FontStretches.Normal);
                var ft = new FormattedText(sample, CultureInfo.InvariantCulture, FlowDirection.LeftToRight,
                                           tf, size, Brushes.Black, pixelsPerDip);
                return Math.Ceiling(ft.WidthIncludingTrailingWhitespace) + 1;
            }
            catch { return double.NaN; }
        }

        /// <summary>The widest number this row can ever print. '8' is the widest digit.</summary>
        private string ReserveValue(string key)
        {
            switch (key)
            {
                case "vram": return "88.88";
                case "fps": return "8888";
                case "fpslow": return "8888";
                case "frametime": return "88.8";
                case "net": return "888.8";
                case "ping": return "8888";
                default:
                    return _settings.Decimals == 2 ? "888.88"
                         : _settings.Decimals == 1 ? "888.8"
                         : "888";
            }
        }

        /// <summary>The widest detail line this row can ever print.</summary>
        private string ReserveDetail(string key)
        {
            switch (key)
            {
                case "cpu":
                    {
                        var parts = new List<string>();
                        if (_settings.ShowCpuTemp) parts.Add("888\u00B0C");
                        if (_settings.ShowCpuClock) parts.Add("8.8 GHz");
                        return string.Join("  ", parts);
                    }
                case "gpu": return _settings.ShowGpuTemp ? "888\u00B0C" : "";
                case "vram": return "/ 88.8 GB";
                case "ram": return "88.8 / 88.8 GB";
                case "fps": return _settings.ShowFpsApp ? "nnnnnnnnnn" : "";
                case "fpslow": return "0.1% 8888";
                case "frametime": return "";
                case "net": return "\u2191 888.8";
                case "ping": return "nnnnnnnnnn";
                default: return "";
            }
        }

        private string Fmt(double v)
        {
            switch (_settings.Decimals)
            {
                case 1: return v.ToString("0.0", CultureInfo.InvariantCulture);
                case 2: return v.ToString("0.00", CultureInfo.InvariantCulture);
                default: return v.ToString("0", CultureInfo.InvariantCulture);
            }
        }

        private string Temp(double celsius)
        {
            bool f = string.Equals(_settings.TempUnit, "F", StringComparison.OrdinalIgnoreCase);
            double v = f ? celsius * 9.0 / 5.0 + 32.0 : celsius;
            return v.ToString("0", CultureInfo.InvariantCulture) + (f ? "\u00B0F" : "\u00B0C");
        }

        private void OnMetrics(MetricsSnapshot s)
        {
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.BeginInvoke(new Action(() => OnMetrics(s)));
                return;
            }
            if (s == null) return;

            try
            {
                ApplyGameRule(s);

                foreach (var r in _rows)
                {
                    switch (r.Key)
                    {
                        case "cpu":
                            r.Value = Fmt(s.CpuLoad);
                            r.Unit = "%";
                            r.Percent = s.CpuLoad;
                            r.Detail = BuildCpuDetail(s);
                            Paint(r, s.CpuLoad);
                            break;

                        case "gpu":
                            r.Value = Fmt(s.GpuLoad);
                            r.Unit = "%";
                            r.Percent = s.GpuLoad;
                            r.Detail = (_settings.ShowGpuTemp && s.GpuTemp > 0) ? Temp(s.GpuTemp) : "";
                            Paint(r, s.GpuLoad);
                            break;

                        case "vram":
                            {
                                double usedGb = s.VramUsedMb / 1024.0;
                                double totalGb = s.VramTotalMb / 1024.0;
                                double pct = totalGb > 0.05 ? usedGb / totalGb * 100.0 : 0;
                                r.Value = usedGb >= 10 ? usedGb.ToString("0.0", CultureInfo.InvariantCulture)
                                                       : usedGb.ToString("0.00", CultureInfo.InvariantCulture);
                                r.Unit = "GB";
                                r.Percent = pct;
                                r.Detail = totalGb > 0.05 ? "/ " + totalGb.ToString("0.#", CultureInfo.InvariantCulture) + " GB" : "";
                                Paint(r, pct);
                                break;
                            }

                        case "ram":
                            r.Value = Fmt(s.RamLoad);
                            r.Unit = "%";
                            r.Percent = s.RamLoad;
                            r.Detail = s.RamTotalGb > 0
                                ? s.RamUsedGb.ToString("0.0", CultureInfo.InvariantCulture) + " / " +
                                  s.RamTotalGb.ToString("0.#", CultureInfo.InvariantCulture) + " GB"
                                : "";
                            Paint(r, s.RamLoad);
                            break;

                        case "net":
                            {
                                double down = s.NetDownMbps;
                                if (down > _netScale) _netScale = Math.Min(1000, down);
                                _netScale = Math.Max(10, _netScale * 0.995);     // slowly relaxes back

                                r.Value = down >= 100 ? down.ToString("0", CultureInfo.InvariantCulture)
                                                      : down.ToString("0.0", CultureInfo.InvariantCulture);
                                r.Unit = "Mb/s";
                                r.Percent = _netScale > 0 ? down / _netScale * 100.0 : 0;
                                r.Detail = "\u2191 " + (s.NetUpMbps >= 100
                                    ? s.NetUpMbps.ToString("0", CultureInfo.InvariantCulture)
                                    : s.NetUpMbps.ToString("0.0", CultureInfo.InvariantCulture));
                                r.ValueBrush = Solid(TextC);
                                r.BarBrush = Solid(Accent);
                                break;
                            }

                        case "ping":
                            {
                                if (s.PingMs >= 0)
                                {
                                    r.Value = s.PingMs.ToString("0", CultureInfo.InvariantCulture);
                                    r.Unit = "ms";
                                    // higher is worse: 150 ms trips the warning, 180 ms the danger colour
                                    Paint(r, Math.Min(100, s.PingMs / 2.0));
                                }
                                else
                                {
                                    r.Value = "--";
                                    r.Unit = "ms";
                                    r.Percent = 0;
                                    r.ValueBrush = Solid(DetailC);
                                    r.BarBrush = Solid(Accent);
                                }
                                r.Detail = _settings.PingHost ?? "";
                                break;
                            }

                        case "fpslow":
                            {
                                double max = _settings.FpsBarMax > 0 ? _settings.FpsBarMax : _fpsScale;
                                if (s.FpsLow1 > 0)
                                {
                                    r.Value = s.FpsLow1.ToString("0", CultureInfo.InvariantCulture);
                                    r.Percent = max > 0 ? s.FpsLow1 / max * 100.0 : 0;
                                    r.Detail = s.FpsLow01 > 0
                                        ? "0.1% " + s.FpsLow01.ToString("0", CultureInfo.InvariantCulture)
                                        : "0.1% --";
                                    r.ValueBrush = Solid(Accent);
                                }
                                else
                                {
                                    r.Value = "--";
                                    r.Percent = 0;
                                    r.Detail = Lang.T(s.Fps > 0 ? "duke mbledhur" : "pa loje");
                                    r.ValueBrush = Solid(DetailC);
                                }
                                r.Unit = "FPS";
                                r.BarBrush = Solid(Accent);
                                break;
                            }

                        case "frametime":
                            {
                                double ft = s.FrameTimeMs;
                                if (ft > 0)
                                {
                                    r.Value = ft.ToString(ft < 100 ? "0.0" : "0", CultureInfo.InvariantCulture);
                                    // 33.3 ms (30 FPS) is a full bar: warn past 25 ms, danger past 30 ms
                                    Paint(r, Math.Min(100.0, ft / 33.3 * 100.0));
                                }
                                else
                                {
                                    r.Value = "--";
                                    r.Percent = 0;
                                    r.ValueBrush = Solid(DetailC);
                                    r.BarBrush = Solid(Accent);
                                }
                                r.Unit = "ms";
                                r.Detail = "";
                                break;
                            }

                        case "fps":
                            if (s.Fps > 0)
                            {
                                double max = _settings.FpsBarMax > 0 ? _settings.FpsBarMax : _fpsScale;
                                if (_settings.FpsBarMax <= 0 && s.Fps > _fpsScale) _fpsScale = Math.Min(400, s.Fps);
                                max = _settings.FpsBarMax > 0 ? _settings.FpsBarMax : _fpsScale;

                                r.Value = s.Fps.ToString("0", CultureInfo.InvariantCulture);
                                r.Unit = "FPS";
                                r.Percent = max > 0 ? s.Fps / max * 100.0 : 0;
                                r.Detail = (_settings.ShowFpsApp && !string.IsNullOrEmpty(s.FpsSource)) ? Shorten(s.FpsSource) : "";
                                r.ValueBrush = Solid(Accent);
                                r.BarBrush = Solid(Accent);
                            }
                            else
                            {
                                r.Value = "--";
                                r.Unit = "FPS";
                                r.Percent = 0;
                                r.Detail = Lang.T(s.FpsSource == "admin" ? "kerkon admin" : "pa loje");
                                r.ValueBrush = Solid(DetailC);
                                r.BarBrush = Solid(Accent);
                            }
                            break;
                    }
                }
            }
            catch (Exception ex) { AppInfo.Log("OnMetrics: " + ex.Message); }
        }

        /// <summary>"Shfaq vetem gjate lojes" - hides the card on the desktop and brings it back in game.</summary>
        private void ApplyGameRule(MetricsSnapshot s)
        {
            if (!_settings.ShowOnlyInGame)
            {
                if (_hiddenByGameRule)
                {
                    _hiddenByGameRule = false;
                    if (_settings.WidgetVisible && !IsVisible) Show();
                }
                return;
            }

            bool inGame = s.Fps > 0;
            if (!_settings.WidgetVisible) return;

            if (inGame && !IsVisible)
            {
                _hiddenByGameRule = false;
                Show();
                WidgetPlacement.Apply(this, _settings);
            }
            else if (!inGame && IsVisible)
            {
                _hiddenByGameRule = true;
                Hide();
            }
        }

        private string BuildCpuDetail(MetricsSnapshot s)
        {
            var parts = new List<string>();
            if (_settings.ShowCpuTemp && s.CpuTemp > 0) parts.Add(Temp(s.CpuTemp));
            if (_settings.ShowCpuClock && s.CpuClockMhz > 0)
                parts.Add((s.CpuClockMhz / 1000.0).ToString("0.0", CultureInfo.InvariantCulture) + " GHz");
            return string.Join("  ", parts);
        }

        private static string Shorten(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            return s.Length <= 14 ? s : s.Substring(0, 13) + "\u2026";
        }

        private void Paint(MetricRowVm r, double pct)
        {
            Color c;
            if (_settings.ColorizeByLoad) c = pct >= 90 ? DangerC : pct >= 75 ? WarnC : Accent;
            else c = Accent;

            r.BarBrush = Solid(c);
            r.ValueBrush = Solid(_settings.ColorizeByLoad && pct >= 90 ? c : TextC);
        }

        // ================================================================== interaction

        private void OnDragStart(object sender, MouseButtonEventArgs e)
        {
            if (_settings.Locked) return;
            try { DragMove(); } catch { }
        }

        private void OnDragEnd(object sender, MouseButtonEventArgs e)
        {
            if (_settings.Locked) return;
            WidgetPlacement.StoreCurrent(this, _settings);
            SettingsStore.Save(_settings);
        }

        private void Settings_Click(object sender, RoutedEventArgs e) => SettingsRequested?.Invoke(this, EventArgs.Empty);

        private void Hide_Click(object sender, RoutedEventArgs e)
        {
            _settings.WidgetVisible = false;
            SettingsStore.Save(_settings);
            Hide();
        }

        private void Site_Click(object sender, MouseButtonEventArgs e) => AppInfo.OpenUrl(AppInfo.Website);
    }
}
