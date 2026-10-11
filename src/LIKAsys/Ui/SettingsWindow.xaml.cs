using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using LIKAsys.Core;
using LIKAsys.Monitoring;
using Forms = System.Windows.Forms;

namespace LIKAsys.Ui
{
    public partial class SettingsWindow : Window
    {
        private readonly AppSettings _settings;
        private readonly MetricsService _metrics;
        private bool _loading = true;

        // Opening the list on "all" means 63 cards at once and nobody finds anything.
        // Start on the group that holds the theme already in use.
        private string _group = ThemeLibrary.GLoja;
        private bool _groupPicked;
        private readonly List<Border> _cards = new List<Border>();
        private readonly List<Action> _colorRefresh = new List<Action>();

        public event EventHandler CheckUpdatesRequested;
        public event EventHandler LanguageChanged;
        public event EventHandler ResetRequested;

        private static readonly string[] PanelNames =
        {
            "PanelThemes", "PanelColors", "PanelLook", "PanelFont", "PanelMouse",
            "PanelPos", "PanelMetrics", "PanelSystem", "PanelLang", "PanelAbout"
        };

        private static readonly string[] WeightKeys =
        { "Light", "Normal", "Medium", "SemiBold", "Bold", "Black" };

        private static readonly string[] WeightNamesSq =
        { "E hollë", "Normale", "Mesatare", "Gjysmë e trashë", "E trashë", "Shumë e trashë" };

        private static string[] WeightNames => WeightNamesSq.Select(Lang.T).ToArray();

        // property name -> label shown in the Ngjyrat tab
        private static readonly string[][] ColorFields =
        {
            new[] { "Accent",      "Ngjyra kryesore" },
            new[] { "Accent2",     "Ngjyra dytësore" },
            new[] { "BgTop",       "Sfondi (lart)" },
            new[] { "BgBottom",    "Sfondi (poshtë)" },
            new[] { "BorderColor", "Korniza" },
            new[] { "TextColor",   "Vlerat / teksti" },
            new[] { "LabelColor",  "Etiketat" },
            new[] { "DetailColor", "Detajet e vogla" },
            new[] { "TrackColor",  "Shiriti bosh" },
            new[] { "WarnColor",   "Paralajmërim (mbi 75%)" },
            new[] { "DangerColor", "Rrezik (mbi 90%)" },
        };

        public SettingsWindow(AppSettings settings, MetricsService metrics)
        {
            InitializeComponent();
            _settings = settings;
            _metrics = metrics;
            DataContext = settings;

            Icon = TryIcon();

            Lang.Set(_settings.Language);
            Lang.Localize(this);

            BuildThemeTab();
            BuildColorTab();
            BuildLangTab();
            BuildMouseTab();
            BuildProfileTab();
            FillCombos();
            SyncFromSettings();

            _settings.PropertyChanged += OnExternalChange;
            Closed += (s, e) => { try { _settings.PropertyChanged -= OnExternalChange; } catch { } };

            _loading = false;
        }

        private ImageSource TryIcon()
        {
            // An .ico holds many frames and WPF grabs the first one, which is 16x16 - that is
            // what made the window icon look chewed up. Always hand back the largest frame.
            try
            {
                var uri = new Uri("pack://application:,,,/assets/LIKAsys.ico", UriKind.Absolute);
                var dec = new System.Windows.Media.Imaging.IconBitmapDecoder(
                    uri,
                    System.Windows.Media.Imaging.BitmapCreateOptions.None,
                    System.Windows.Media.Imaging.BitmapCacheOption.OnLoad);

                System.Windows.Media.Imaging.BitmapFrame best = null;
                foreach (var f in dec.Frames)
                    if (best == null || f.PixelWidth > best.PixelWidth) best = f;
                return best;
            }
            catch { return null; }
        }

        // ============================================================ helpers

        private static Color Col(string hex, Color fallback)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(hex)) return fallback;
                var o = ColorConverter.ConvertFromString(hex.Trim());
                if (o is Color c) return c;
            }
            catch { }
            return fallback;
        }

        private static SolidColorBrush Brush(string hex, byte fr = 0x80, byte fg = 0x80, byte fb = 0x80)
            => new SolidColorBrush(Col(hex, Color.FromRgb(fr, fg, fb)));

        private static string Normalize(string input, string current)
        {
            if (string.IsNullOrWhiteSpace(input)) return current;
            var t = input.Trim().TrimStart('#').Replace(" ", "");
            if (t.Length == 3)
                t = string.Concat(t[0], t[0], t[1], t[1], t[2], t[2]);
            if (t.Length != 6 && t.Length != 8) return current;
            foreach (var ch in t)
                if (!Uri.IsHexDigit(ch)) return current;
            return "#" + t.ToUpperInvariant();
        }

        private PropertyInfo Prop(string name) => typeof(AppSettings).GetProperty(name);

        // ============================================================ theme tab

        private void BuildThemeTab()
        {
            if (!_groupPicked)
                _group = ThemeLibrary.Find(_settings.ThemeName).Group;

            GroupFilter.Children.Clear();
            AddChip(Lang.T("Të gjitha"), "*", _group == "*");
            foreach (var g in ThemeLibrary.Groups) AddChip(Lang.T(g), g, _group == g);

            BuildLayoutQuick();
            RenderThemes();
        }

        /// <summary>Vertical / horizontal / compact, right next to the themes - Classic needs it most.</summary>
        private void BuildLayoutQuick()
        {
            while (LayoutQuick.Children.Count > 1) LayoutQuick.Children.RemoveAt(1);

            var names = new[] { "Vertikale", "Horizontale", "Kompakte" };
            for (int i = 0; i < names.Length; i++)
            {
                var layout = (WidgetLayout)i;
                var rb = new RadioButton
                {
                    Style = (Style)FindResource("Chip"),
                    Content = Lang.T(names[i]),
                    GroupName = "layoutquick",
                    IsChecked = _settings.Layout == layout
                };
                rb.Checked += (s2, e2) => { if (!_loading) _settings.Layout = layout; };
                LayoutQuick.Children.Add(rb);
            }
        }

        private void AddChip(string text, string key, bool on)
        {
            var rb = new RadioButton
            {
                Style = (Style)FindResource("Chip"),
                Content = text,
                Tag = key,
                GroupName = "grp",
                IsChecked = on
            };
            rb.Checked += (s, e) =>
            {
                _group = key;
                _groupPicked = true;
                RenderThemes();
                try { Scroller.ScrollToTop(); } catch { }
            };
            GroupFilter.Children.Add(rb);
        }

        private void RenderThemes()
        {
            ThemeHost.Children.Clear();
            _cards.Clear();
            foreach (var t in ThemeLibrary.All)
            {
                if (_group != "*" && t.Group != _group) continue;
                ThemeHost.Children.Add(Card(t));
            }
            SyncThemeSelection();
        }

        private Border Card(ThemePreset t)
        {
            var card = new Border
            {
                Width = 152,
                Height = 90,
                CornerRadius = new CornerRadius(10),
                Margin = new Thickness(0, 0, 10, 10),
                BorderThickness = new Thickness(1.6),
                BorderBrush = new SolidColorBrush(Color.FromArgb(0x2A, 0xFF, 0xFF, 0xFF)),
                Cursor = Cursors.Hand,
                Tag = t,
                ToolTip = t.Name + "  -  " + Lang.T(t.Group) + "  -  " + ThemeLibrary.Describe(t),
                Background = new LinearGradientBrush(
                    Col(t.BgTop, Color.FromRgb(0x15, 0x1C, 0x2B)),
                    Col(t.BgBottom, Color.FromRgb(0x0A, 0x0D, 0x14)),
                    90)
            };

            var sp = new StackPanel { Margin = new Thickness(11, 10, 11, 9) };

            var head = new StackPanel { Orientation = Orientation.Horizontal };
            head.Children.Add(new Ellipse
            {
                Width = 7,
                Height = 7,
                Fill = Brush(t.Accent),
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 7, 0)
            });
            head.Children.Add(new TextBlock
            {
                Text = t.Name,
                FontSize = 11.5,
                FontWeight = FontWeights.SemiBold,
                Foreground = Brush(t.Text, 0xEA, 0xF2, 0xFF),
                VerticalAlignment = VerticalAlignment.Center,
                TextTrimming = TextTrimming.CharacterEllipsis
            });
            sp.Children.Add(head);

            sp.Children.Add(MiniBar(t, 0.70, t.Accent, 11));
            sp.Children.Add(MiniBar(t, 0.44, t.Accent2, 6));

            sp.Children.Add(new TextBlock
            {
                Text = Lang.T(t.Group).ToUpperInvariant() + "  ·  " + ThemeLibrary.Describe(t),
                FontSize = 8,
                Foreground = Brush(t.Detail, 0x5D, 0x6E, 0x85),
                Margin = new Thickness(0, 9, 0, 0)
            });

            card.Child = sp;
            card.MouseLeftButtonUp += (s, e) => PickTheme(t);
            _cards.Add(card);
            return card;
        }

        private static FrameworkElement MiniBar(ThemePreset t, double pct, string color, double topMargin)
        {
            var track = new Border
            {
                Height = 4,
                Width = 130,
                CornerRadius = new CornerRadius(2),
                HorizontalAlignment = HorizontalAlignment.Left,
                Margin = new Thickness(0, topMargin, 0, 0),
                Background = Brush(t.Track, 0x1B, 0x24, 0x33)
            };
            track.Child = new Border
            {
                Height = 4,
                Width = Math.Round(130 * pct),
                CornerRadius = new CornerRadius(2),
                HorizontalAlignment = HorizontalAlignment.Left,
                Background = Brush(color)
            };
            return track;
        }

        private void PickTheme(ThemePreset t)
        {
            ThemeLibrary.Apply(t, _settings);
            SyncThemeSelection();
            RefreshColors();
            SyncCombos();
        }

        private void SyncThemeSelection()
        {
            foreach (var c in _cards)
            {
                var t = (ThemePreset)c.Tag;
                bool on = string.Equals(t.Name, _settings.ThemeName, StringComparison.OrdinalIgnoreCase);
                c.BorderBrush = on ? Brush(t.Accent) : new SolidColorBrush(Color.FromArgb(0x2A, 0xFF, 0xFF, 0xFF));
                c.BorderThickness = new Thickness(on ? 2.4 : 1.6);
            }
            try { ThemeBadge.Text = _settings.ThemeName; } catch { }
        }

        // ============================================================ colours tab

        private void BuildColorTab()
        {
            ColorHost.Children.Clear();
            _colorRefresh.Clear();

            foreach (var f in ColorFields)
            {
                var pi = Prop(f[0]);
                if (pi == null) continue;

                var grid = new Grid { Margin = new Thickness(0, 5, 0, 5) };
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(176) });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(132) });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

                var lbl = new TextBlock
                {
                    Text = Lang.T(f[1]),
                    Style = (Style)FindResource("FieldLabel"),
                    VerticalAlignment = VerticalAlignment.Center
                };
                Grid.SetColumn(lbl, 0);
                grid.Children.Add(lbl);

                var box = new TextBox { Style = (Style)FindResource("HexBox") };
                Grid.SetColumn(box, 1);
                grid.Children.Add(box);

                var chip = new Border
                {
                    Width = 44,
                    Height = 30,
                    CornerRadius = new CornerRadius(7),
                    Margin = new Thickness(10, 0, 0, 0),
                    Cursor = Cursors.Hand,
                    BorderThickness = new Thickness(1),
                    BorderBrush = new SolidColorBrush(Color.FromArgb(0x33, 0xFF, 0xFF, 0xFF)),
                    ToolTip = Lang.T("Hap paletën e ngjyrave")
                };
                Grid.SetColumn(chip, 2);
                grid.Children.Add(chip);

                Action refresh = () =>
                {
                    var v = pi.GetValue(_settings) as string ?? "";
                    box.Text = v;
                    chip.Background = Brush(v);
                };

                Action<string> commit = raw =>
                {
                    var cur = pi.GetValue(_settings) as string ?? "";
                    var clean = Normalize(raw, cur);
                    if (!string.Equals(clean, cur, StringComparison.OrdinalIgnoreCase))
                    {
                        pi.SetValue(_settings, clean);
                        _settings.ThemeName = "Personale";
                    }
                    refresh();
                    SyncThemeSelection();
                };

                box.LostFocus += (s, e) => commit(box.Text);
                box.KeyDown += (s, e) =>
                {
                    if (e.Key == Key.Enter) { commit(box.Text); Keyboard.ClearFocus(); }
                    else if (e.Key == Key.Escape) refresh();
                };

                chip.MouseLeftButtonUp += (s, e) =>
                {
                    var cur = pi.GetValue(_settings) as string ?? "#FFFFFF";
                    var c = Col(cur, Colors.White);
                    byte alpha = cur.TrimStart('#').Length == 8 ? c.A : (byte)0xFF;

                    using (var dlg = new Forms.ColorDialog())
                    {
                        dlg.FullOpen = true;
                        dlg.AnyColor = true;
                        dlg.Color = System.Drawing.Color.FromArgb(c.R, c.G, c.B);
                        if (dlg.ShowDialog() != Forms.DialogResult.OK) return;

                        var n = dlg.Color;
                        var hex = alpha == 0xFF
                            ? string.Format(CultureInfo.InvariantCulture, "#{0:X2}{1:X2}{2:X2}", n.R, n.G, n.B)
                            : string.Format(CultureInfo.InvariantCulture, "#{0:X2}{1:X2}{2:X2}{3:X2}", alpha, n.R, n.G, n.B);
                        commit(hex);
                    }
                };

                _colorRefresh.Add(refresh);
                refresh();
                ColorHost.Children.Add(grid);
            }
        }

        private void RefreshColors()
        {
            foreach (var a in _colorRefresh)
            {
                try { a(); } catch { }
            }
        }

        // ============================================================ combos

        // ============================================================ language tab

        private readonly List<Border> _langCards = new List<Border>();

        private void BuildLangTab()
        {
            LangHost.Children.Clear();
            _langCards.Clear();

            AddLangCard("sq", "Shqip", Lang.T("Gjuha e parazgjedhur e LIKAsys"), "AL");
            AddLangCard("en", "English", "International / English interface", "EN");

            SyncLangSelection();
        }

        private void AddLangCard(string code, string title, string subtitle, string badge)
        {
            var card = new Border
            {
                CornerRadius = new CornerRadius(10),
                Margin = new Thickness(0, 0, 0, 10),
                Padding = new Thickness(16, 13, 16, 13),
                BorderThickness = new Thickness(1.6),
                BorderBrush = new SolidColorBrush(Color.FromArgb(0x2A, 0xFF, 0xFF, 0xFF)),
                Background = new SolidColorBrush(Color.FromArgb(0xFF, 0x13, 0x1A, 0x26)),
                Cursor = Cursors.Hand,
                Tag = code,
                HorizontalAlignment = HorizontalAlignment.Left,
                Width = 420
            };

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var tag = new Border
            {
                Width = 40,
                Height = 40,
                CornerRadius = new CornerRadius(8),
                Margin = new Thickness(0, 0, 14, 0),
                Background = new SolidColorBrush(Color.FromArgb(0x1F, 0xFF, 0xFF, 0xFF)),
                Child = new TextBlock
                {
                    Text = badge,
                    FontSize = 13,
                    FontWeight = FontWeights.Bold,
                    Foreground = new SolidColorBrush(Color.FromRgb(0xEA, 0xF2, 0xFF)),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                }
            };
            Grid.SetColumn(tag, 0);
            grid.Children.Add(tag);

            var text = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
            text.Children.Add(new TextBlock
            {
                Text = title,
                FontSize = 14,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(Color.FromRgb(0xEA, 0xF2, 0xFF))
            });
            text.Children.Add(new TextBlock
            {
                Text = subtitle,
                FontSize = 11,
                Margin = new Thickness(0, 2, 0, 0),
                Foreground = new SolidColorBrush(Color.FromRgb(0x5D, 0x6E, 0x85))
            });
            Grid.SetColumn(text, 1);
            grid.Children.Add(text);

            var tick = new TextBlock
            {
                Text = "\u2713",
                FontSize = 17,
                FontWeight = FontWeights.Bold,
                VerticalAlignment = VerticalAlignment.Center,
                Visibility = Visibility.Hidden
            };
            Grid.SetColumn(tick, 2);
            grid.Children.Add(tick);

            card.Child = grid;
            card.MouseLeftButtonUp += (s, e) => PickLanguage(code);
            _langCards.Add(card);
            LangHost.Children.Add(card);
        }

        private void PickLanguage(string code)
        {
            if (string.Equals(_settings.Language, code, StringComparison.OrdinalIgnoreCase)) return;
            _settings.Language = code;
            Lang.Set(code);
            ApplyLanguage();
            LanguageChanged?.Invoke(this, EventArgs.Empty);
        }

        private void SyncLangSelection()
        {
            var accent = Brush(_settings.Accent, 0x00, 0xE5, 0xFF);
            foreach (var c in _langCards)
            {
                bool on = string.Equals((string)c.Tag, _settings.Language, StringComparison.OrdinalIgnoreCase);
                c.BorderBrush = on ? accent : new SolidColorBrush(Color.FromArgb(0x2A, 0xFF, 0xFF, 0xFF));
                c.BorderThickness = new Thickness(on ? 2.4 : 1.6);
                if (c.Child is Grid g && g.Children.Count > 2 && g.Children[2] is TextBlock tick)
                {
                    tick.Visibility = on ? Visibility.Visible : Visibility.Hidden;
                    tick.Foreground = accent;
                }
            }
        }

        /// <summary>Re-renders the whole window in the freshly picked language.</summary>
        private void ApplyLanguage()
        {
            bool old = _loading;
            _loading = true;
            try
            {
                Lang.Localize(this);
                BuildThemeTab();
                BuildColorTab();
                BuildLangTab();
                BuildMouseTab();
                BuildProfileTab();
                FillCombos();
            }
            catch { }
            finally { _loading = old; }

            SyncCombos();
            RefreshColors();
            RefreshStatus();
            SyncThemeSelection();
        }

        private static string[] Tr(params string[] sq) => sq.Select(Lang.T).ToArray();

        private void FillCombos()
        {
            try
            {
                var families = Fonts.SystemFontFamilies
                    .Select(f => f.Source)
                    .Where(s => !string.IsNullOrWhiteSpace(s))
                    .Distinct()
                    .OrderBy(s => s, StringComparer.OrdinalIgnoreCase)
                    .ToList();
                FontCombo.ItemsSource = families;
            }
            catch { }

            StartViewCombo.ItemsSource = Tr("Siç e lashë herën e fundit", "Gjithmonë i plotë", "Gjithmonë i minimizuar");
            RevealCombo.ItemsSource = Tr("Pa lëvizje", "Bie nga lart", "Rrëshqet nga e djathta", "Rrëshqet nga e majta", "Ngrihet nga poshtë", "Shfaqet butë");
            IconCombo.ItemsSource = Tr("3D (me thellësi)", "Outline (vija të holla)", "Vijë floku (e qetë)", "Solid (të mbushura)", "Pa ikona");
            BarCombo.ItemsSource = Tr("Të rrumbullakosur", "Katrorë", "Të segmentuar", "Pa shirita");
            LayoutCombo.ItemsSource = Tr("Vertikale (njëra mbi tjetrën)", "Horizontale (në një shirit)", "Kompakte (pa shirita)");
            DecimalsCombo.ItemsSource = Tr("0  -  p.sh. 75%", "1  -  p.sh. 75.4%", "2  -  p.sh. 75.42%");
            TempCombo.ItemsSource = Tr("Celsius (°C)", "Fahrenheit (°F)");
            TrayIconCombo.ItemsSource = Tr("Logoja e LIKAsys", "CPU - ngarkesa %", "CPU - temperatura",
                                           "GPU - ngarkesa %", "GPU - temperatura", "RAM - ngarkesa %", "FPS");
            ValueWeightCombo.ItemsSource = WeightNames;
            LabelWeightCombo.ItemsSource = WeightNames;

            try
            {
                var screens = Forms.Screen.AllScreens;
                var items = new List<string>();
                for (int i = 0; i < screens.Length; i++)
                {
                    var b = screens[i].Bounds;
                    items.Add($"{i + 1} - {b.Width}x{b.Height}{(screens[i].Primary ? " " + Lang.T("(kryesor)") : "")}");
                }
                MonitorCombo.ItemsSource = items;
            }
            catch { }
        }

        private void SyncCombos()
        {
            bool old = _loading;
            _loading = true;
            try
            {
                IconCombo.SelectedIndex = (int)_settings.IconStyle;
                BarCombo.SelectedIndex = (int)_settings.BarStyle;
                LayoutCombo.SelectedIndex = (int)_settings.Layout;
                DecimalsCombo.SelectedIndex = Math.Max(0, Math.Min(2, _settings.Decimals));
                TempCombo.SelectedIndex = string.Equals(_settings.TempUnit, "F", StringComparison.OrdinalIgnoreCase) ? 1 : 0;
                TrayIconCombo.SelectedIndex = (int)_settings.TrayIconMode;
                StartViewCombo.SelectedIndex = (int)_settings.StartView;
                RevealCombo.SelectedIndex = (int)_settings.Reveal;

                int vi = Array.FindIndex(WeightKeys, k => string.Equals(k, _settings.ValueWeight, StringComparison.OrdinalIgnoreCase));
                int li = Array.FindIndex(WeightKeys, k => string.Equals(k, _settings.LabelWeight, StringComparison.OrdinalIgnoreCase));
                ValueWeightCombo.SelectedIndex = vi < 0 ? 4 : vi;
                LabelWeightCombo.SelectedIndex = li < 0 ? 2 : li;

                if (FontCombo.ItemsSource is IEnumerable<string> fams)
                {
                    var list = fams.ToList();
                    var hit = list.FirstOrDefault(f => string.Equals(f, _settings.FontFamily, StringComparison.OrdinalIgnoreCase))
                              ?? list.FirstOrDefault(f => f == "Segoe UI");
                    FontCombo.SelectedItem = hit;
                }

                if (MonitorCombo.Items.Count > 0)
                    MonitorCombo.SelectedIndex = Math.Max(0, Math.Min(MonitorCombo.Items.Count - 1, _settings.MonitorIndex));

                foreach (var rb in PosGrid.Children.OfType<RadioButton>())
                    rb.IsChecked = (string)rb.Tag == _settings.Position.ToString();

                int li2 = (int)_settings.Layout;
                for (int i = 1; i < LayoutQuick.Children.Count; i++)
                    if (LayoutQuick.Children[i] is RadioButton lrb) lrb.IsChecked = (i - 1) == li2;

                SyncLangSelection();
            }
            catch { }
            finally { _loading = old; }
        }

        /// <summary>Full refresh of every control (used after a reset).</summary>
        public void SyncAll()
        {
            try { SyncFromSettings(); } catch { }
        }

        private void SyncFromSettings()
        {
            SyncCombos();
            SyncThemeSelection();
            RefreshColors();
            SyncMouseTab();
            SyncMouseTabVisibility();

            VersionLine.Text = $"LIKAsys {AppInfo.VersionText}";
            AboutVersion.Text = $"{AppInfo.VersionText}  -  {AppInfo.WebsiteShort}";
            RefreshStatus();
        }

        private void OnExternalChange(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (_loading) return;
            var p = e.PropertyName ?? "";
            if (p.Length == 0 || p == nameof(AppSettings.ThemeName))
            {
                Dispatcher.BeginInvoke(new Action(() => { SyncThemeSelection(); RefreshColors(); SyncCombos(); SyncProfileSelection(); SyncMouseTab(); SyncMouseTabVisibility(); }));
            }
            else if (p == nameof(AppSettings.Profile) || p == nameof(AppSettings.MouseCursor))
            {
                Dispatcher.BeginInvoke(new Action(() => { SyncMouseTab(); SyncMouseTabVisibility(); }));
            }
            else if (p == nameof(AppSettings.Position))
            {
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    bool old = _loading; _loading = true;
                    foreach (var rb in PosGrid.Children.OfType<RadioButton>())
                        rb.IsChecked = (string)rb.Tag == _settings.Position.ToString();
                    _loading = old;
                }));
            }
        }

        public void RefreshStatus()
        {
            try
            {
                if (_metrics == null) return;
                FpsStatus.Text = _metrics.FpsAvailable
                    ? (Lang.IsEnglish
                        ? "FPS: active - ETW measurement is running. Open a game and the number appears by itself."
                        : "FPS: aktiv - matja ETW po punon. Hape një lojë dhe numri shfaqet vetvetiu.")
                    : (_metrics.FpsStatus == "admin"
                        ? (Lang.IsEnglish
                            ? "FPS: inactive - LIKAsys must be started as administrator."
                            : "FPS: joaktiv - LIKAsys duhet të nisët si administrator.")
                        : (Lang.IsEnglish
                            ? "FPS: starting up or switched off."
                            : "FPS: duke u nisur ose i çaktivizuar."));

                SensorStatus.Text = _metrics.SensorsAvailable
                    ? (Lang.IsEnglish
                        ? "Sensors: active - temperatures and CPU clock are read from the hardware."
                        : "Sensorët: aktiv - temperaturat dhe shpejtësia e CPU-së lexohen nga hardueri.")
                    : (Lang.IsEnglish
                        ? "Sensors: no temperatures (the sensor driver did not load)."
                        : "Sensorët: pa temperatura (drajveri i sensorëve nuk u ngarkua).");
            }
            catch { }
        }

        public void SetUpdateStatus(string text) { try { UpdateStatus.Text = text; } catch { } }
        public void SetCheckEnabled(bool on) { try { CheckBtn.IsEnabled = on; } catch { } }

        // ============================================================ handlers

        // ============================================================ mouse tab

        private readonly List<Border> _mouseCards = new List<Border>();

        /// <summary>
        /// Pointer packs are an IT-profile idea, so the whole tab disappears in Gaming.
        /// Every card draws the real .cur artwork: the preview strip beside each name is
        /// the same four pointers the machine will get, rendered from the shipped files.
        /// </summary>
        private void BuildMouseTab()
        {
            if (MouseHost == null) return;
            MouseHost.Children.Clear();
            _mouseCards.Clear();

            foreach (var pack in MouseCursors.All) AddMouseCard(pack);

            SyncMouseTab();
        }

        private void AddMouseCard(CursorPack pack)
        {
            var card = new Border
            {
                CornerRadius = new CornerRadius(10),
                Margin = new Thickness(0, 0, 0, 10),
                Padding = new Thickness(14, 11, 16, 11),
                BorderThickness = new Thickness(1.6),
                BorderBrush = new SolidColorBrush(Color.FromArgb(0x2A, 0xFF, 0xFF, 0xFF)),
                Background = new SolidColorBrush(Color.FromArgb(0xFF, 0x13, 0x1A, 0x26)),
                Cursor = Cursors.Hand,
                Tag = pack.Style,
                HorizontalAlignment = HorizontalAlignment.Left,
                Width = 460
            };

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(128) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            UIElement art = null;
            var png = MouseCursors.PreviewPath(pack);
            if (png != null)
            {
                try
                {
                    var bmp = new System.Windows.Media.Imaging.BitmapImage();
                    bmp.BeginInit();
                    bmp.UriSource = new Uri(png, UriKind.Absolute);
                    bmp.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad;
                    bmp.EndInit();
                    bmp.Freeze();
                    art = new Image
                    {
                        Source = bmp,
                        Width = 112,
                        Stretch = Stretch.Uniform,
                        VerticalAlignment = VerticalAlignment.Center,
                        Margin = new Thickness(0, 0, 14, 0)
                    };
                    RenderOptions.SetBitmapScalingMode((Image)art, BitmapScalingMode.HighQuality);
                }
                catch { art = null; }
            }

            if (art == null)
            {
                art = new Border
                {
                    Width = 112,
                    Height = 34,
                    CornerRadius = new CornerRadius(8),
                    Margin = new Thickness(0, 0, 14, 0),
                    Background = new SolidColorBrush(Color.FromArgb(0x16, 0xFF, 0xFF, 0xFF)),
                    Child = new TextBlock
                    {
                        Text = "Windows",
                        FontSize = 11.5,
                        Foreground = new SolidColorBrush(Color.FromRgb(0x8C, 0x9B, 0xB2)),
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center
                    }
                };
            }
            Grid.SetColumn(art, 0);
            grid.Children.Add(art);

            var text = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
            text.Children.Add(new TextBlock
            {
                Text = Lang.T(pack.Name),
                FontSize = 14,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(Color.FromRgb(0xEA, 0xF2, 0xFF))
            });
            text.Children.Add(new TextBlock
            {
                Text = Lang.T(pack.Desc),
                FontSize = 11,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 2, 0, 0),
                Foreground = new SolidColorBrush(Color.FromRgb(0x5D, 0x6E, 0x85))
            });
            Grid.SetColumn(text, 1);
            grid.Children.Add(text);

            var tick = new TextBlock
            {
                Text = "\u2713",
                FontSize = 17,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(10, 0, 0, 0),
                VerticalAlignment = VerticalAlignment.Center,
                Visibility = Visibility.Hidden
            };
            Grid.SetColumn(tick, 2);
            grid.Children.Add(tick);

            card.Child = grid;
            card.MouseLeftButtonUp += (s, e) => PickMouseCursor(pack.Style);
            _mouseCards.Add(card);
            MouseHost.Children.Add(card);
        }

        private void PickMouseCursor(MouseCursorStyle style)
        {
            if (_settings.MouseCursor != style) _settings.MouseCursor = style;
            SyncMouseTab();
        }

        private void SyncMouseTab()
        {
            var accent = ParseBrush(_settings.Accent, Color.FromRgb(0x00, 0xE5, 0xFF));
            foreach (var card in _mouseCards)
            {
                bool on = (MouseCursorStyle)card.Tag == _settings.MouseCursor;
                card.BorderBrush = on ? accent : new SolidColorBrush(Color.FromArgb(0x2A, 0xFF, 0xFF, 0xFF));
                card.Background = new SolidColorBrush(on
                    ? Color.FromArgb(0xFF, 0x16, 0x22, 0x33)
                    : Color.FromArgb(0xFF, 0x13, 0x1A, 0x26));
                var g = card.Child as Grid;
                if (g == null) continue;
                foreach (var child in g.Children)
                {
                    var tb = child as TextBlock;
                    if (tb == null || tb.Text != "\u2713") continue;
                    tb.Visibility = on ? Visibility.Visible : Visibility.Hidden;
                    tb.Foreground = accent;
                }
            }
        }

        private static SolidColorBrush ParseBrush(string hex, Color fallback)
        {
            try
            {
                var c = (Color)ColorConverter.ConvertFromString(hex);
                return new SolidColorBrush(c);
            }
            catch { return new SolidColorBrush(fallback); }
        }

        /// <summary>Hides the Mouse tab outside the IT profile and steps off it if it was open.</summary>
        private void SyncMouseTabVisibility()
        {
            if (TabMouse == null) return;
            bool it = _settings.Profile == UiProfile.It;
            TabMouse.Visibility = it ? Visibility.Visible : Visibility.Collapsed;
            if (!it && TabMouse.IsChecked == true)
            {
                TabMouse.IsChecked = false;
                if (TabThemes != null) TabThemes.IsChecked = true;
            }
        }

        private void Tab_Checked(object sender, RoutedEventArgs e)
        {
            if (!(sender is RadioButton rb) || rb.Tag == null) return;
            var want = (string)rb.Tag;
            foreach (var name in PanelNames)
            {
                if (FindName(name) is UIElement el)
                    el.Visibility = name == want ? Visibility.Visible : Visibility.Collapsed;
            }
            try { Scroller.ScrollToTop(); } catch { }
            if (want == "PanelMetrics") RefreshStatus();
            if (want == "PanelMouse") SyncMouseTab();
        }

        private void Pos_Checked(object sender, RoutedEventArgs e)
        {
            if (_loading || !(sender is RadioButton rb)) return;
            if (Enum.TryParse((string)rb.Tag, out WidgetPosition p)) _settings.Position = p;
        }

        private void FontCombo_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (_loading) return;
            if (FontCombo.SelectedItem is string f) _settings.FontFamily = f;
        }

        private void IconCombo_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (_loading || IconCombo.SelectedIndex < 0) return;
            _settings.IconStyle = (IconStyle)IconCombo.SelectedIndex;
        }

        private void BarCombo_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (_loading || BarCombo.SelectedIndex < 0) return;
            _settings.BarStyle = (BarStyle)BarCombo.SelectedIndex;
        }

        private void LayoutCombo_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (_loading || LayoutCombo.SelectedIndex < 0) return;
            _settings.Layout = (WidgetLayout)LayoutCombo.SelectedIndex;
        }

        private void ValueWeight_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (_loading || ValueWeightCombo.SelectedIndex < 0) return;
            _settings.ValueWeight = WeightKeys[ValueWeightCombo.SelectedIndex];
        }

        private void LabelWeight_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (_loading || LabelWeightCombo.SelectedIndex < 0) return;
            _settings.LabelWeight = WeightKeys[LabelWeightCombo.SelectedIndex];
        }

        private void Decimals_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (_loading || DecimalsCombo.SelectedIndex < 0) return;
            _settings.Decimals = DecimalsCombo.SelectedIndex;
        }


        // ============================================================ profile cards

        private readonly List<Border> _profileCards = new List<Border>();

        private void BuildProfileTab()
        {
            ProfileHost.Children.Clear();
            _profileCards.Clear();

            AddProfileCard(UiProfile.Gaming, "Gaming",
                "FPS, 1% low, VRAM dhe temperatura. Numra të mëdhenj, ikona 3D, theks cyan.");
            AddProfileCard(UiProfile.It, "IT",
                "Disku, I/O, rrjeti, ping dhe uptime. Shkronja monospace, ikona teknike, theks i gjelbër. Për pamje Apple - jashtëzakonisht e pastër, e bardhë, pa korniza - shko te Temat > Dritë > Apple Clean.");

            SyncProfileSelection();
        }

        private void AddProfileCard(UiProfile profile, string title, string subtitle)
        {
            var card = new Border
            {
                CornerRadius = new CornerRadius(10),
                Margin = new Thickness(0, 0, 0, 10),
                Padding = new Thickness(16, 13, 16, 13),
                BorderThickness = new Thickness(1.6),
                BorderBrush = new SolidColorBrush(Color.FromArgb(0x2A, 0xFF, 0xFF, 0xFF)),
                Background = new SolidColorBrush(Color.FromArgb(0xFF, 0x13, 0x1A, 0x26)),
                Cursor = Cursors.Hand,
                Tag = profile,
                HorizontalAlignment = HorizontalAlignment.Left,
                Width = 420
            };

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var glyph = new Border
            {
                Width = 40,
                Height = 40,
                CornerRadius = new CornerRadius(8),
                Margin = new Thickness(0, 0, 14, 0),
                Background = new SolidColorBrush(Color.FromArgb(0x1F, 0xFF, 0xFF, 0xFF)),
                Child = new System.Windows.Shapes.Path
                {
                    Data = Application.Current.TryFindResource(
                        profile == UiProfile.It ? "IconCpuIt" : "IconFps") as Geometry,
                    Stroke = new SolidColorBrush(Color.FromRgb(0xEA, 0xF2, 0xFF)),
                    StrokeThickness = 1.6,
                    Width = 21,
                    Height = 21,
                    Stretch = Stretch.Uniform,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                }
            };
            Grid.SetColumn(glyph, 0);
            grid.Children.Add(glyph);

            var text = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
            text.Children.Add(new TextBlock
            {
                Text = Lang.T(title),
                FontSize = 14,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(Color.FromRgb(0xEA, 0xF2, 0xFF))
            });
            text.Children.Add(new TextBlock
            {
                Text = Lang.T(subtitle),
                FontSize = 11.5,
                Margin = new Thickness(0, 3, 0, 0),
                TextWrapping = TextWrapping.Wrap,
                Foreground = new SolidColorBrush(Color.FromRgb(0x7E, 0x8E, 0xA8))
            });
            Grid.SetColumn(text, 1);
            grid.Children.Add(text);

            card.Child = grid;
            card.MouseLeftButtonUp += (s, e) => PickProfile(profile);

            ProfileHost.Children.Add(card);
            _profileCards.Add(card);
        }

        private void SyncProfileSelection()
        {
            var accent = Brush(_settings.Accent, 0x00, 0xE5, 0xFF);
            foreach (var c in _profileCards)
            {
                bool on = (UiProfile)c.Tag == _settings.Profile;
                c.BorderBrush = on ? accent : new SolidColorBrush(Color.FromArgb(0x2A, 0xFF, 0xFF, 0xFF));
            }
        }

        private void PickProfile(UiProfile p)
        {
            Profiles.Apply(_settings, p);
            SyncProfileSelection();
            ReloadFromSettings();
        }

        /// <summary>Pulls every control back in line after a profile or theme rewrote the settings.</summary>
        private void ReloadFromSettings()
        {
            bool old = _loading;
            _loading = true;
            try
            {
                FillCombos();
            }
            catch { }
            finally { _loading = old; }

            SyncCombos();
            RefreshColors();
            SyncThemeSelection();
            SyncProfileSelection();
        }

        private void Reveal_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (_loading) return;
            int i = RevealCombo.SelectedIndex;
            if (i < 0) return;
            _settings.Reveal = (RevealAnimation)i;
            RevealPreview?.Invoke(this, EventArgs.Empty);
        }

        public event EventHandler RevealPreview;

        private void StartView_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (_loading) return;
            int i = StartViewCombo.SelectedIndex;
            if (i >= 0) _settings.StartView = (StartView)i;
        }

        private void TrayIcon_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (_loading || TrayIconCombo.SelectedIndex < 0) return;
            _settings.TrayIconMode = (TrayIconMode)TrayIconCombo.SelectedIndex;
        }

        private void Temp_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (_loading || TempCombo.SelectedIndex < 0) return;
            _settings.TempUnit = TempCombo.SelectedIndex == 1 ? "F" : "C";
        }

        private void MonitorCombo_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (_loading || MonitorCombo.SelectedIndex < 0) return;
            _settings.MonitorIndex = MonitorCombo.SelectedIndex;
            if (_settings.Position == WidgetPosition.Custom) _settings.Position = WidgetPosition.TopRight;
        }

        private void Check_Click(object sender, RoutedEventArgs e) => CheckUpdatesRequested?.Invoke(this, EventArgs.Empty);

        private void OpenData_Click(object sender, RoutedEventArgs e) => AppInfo.OpenUrl(AppInfo.DataDir);

        private void Reset_Click(object sender, RoutedEventArgs e)
        {
            var r = MessageBox.Show(this, Lang.IsEnglish
                    ? "Are you sure you want to restore every setting to its original state?"
                    : "A je i sigurt që do t'i kthesh të gjitha cilësimet në gjendjen fillestare?",
                "LIKAsys", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (r == MessageBoxResult.Yes) ResetRequested?.Invoke(this, EventArgs.Empty);
        }

        private void Site_Click(object sender, RoutedEventArgs e) => AppInfo.OpenUrl(AppInfo.Website);

        private void Title_Drag(object sender, MouseButtonEventArgs e)
        {
            try { if (e.ButtonState == MouseButtonState.Pressed) DragMove(); } catch { }
        }

        private void Close_Click(object sender, RoutedEventArgs e) => Close();
    }
}
