using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using LIKAsys.Core;

namespace LIKAsys.Ui
{
    /// <summary>
    /// The simple page. One screen, big buttons, no words a gamer has to look up.
    /// Everything here also exists in the advanced tabs, this is just the short way in.
    /// </summary>
    public partial class SettingsWindow
    {
        // the eight that cover most tastes, in the order they are shown
        private static readonly string[] QuickThemes =
        {
            "Midnight Glass", "Night City", "Terminal", "Dynamic Island",
            "Match Bar", "Apple Glass", "Nord", "Pure Black"
        };

        private static readonly double[] QuickSizes = { 0.85, 1.0, 1.25, 1.5 };

        private static readonly string[][] QuickMetrics =
        {
            new[] { "ShowCpu", "CPU" },
            new[] { "ShowCpuTemp", "Temp. CPU" },
            new[] { "ShowGpu", "GPU" },
            new[] { "ShowGpuTemp", "Temp. GPU" },
            new[] { "ShowRam", "RAM" },
            new[] { "ShowVram", "VRAM" },
            new[] { "ShowFps", "FPS" },
            new[] { "ShowFpsLow", "1% LOW" },
            new[] { "ShowDisk", "Disku" },
            new[] { "ShowNet", "Rrjeti" },
            new[] { "ShowPing", "Ping" },
            new[] { "ShowUptime", "Koha ndezur" },
        };

        private readonly List<Border> _sTheme = new List<Border>();
        private readonly List<Border> _sProfile = new List<Border>();
        private readonly List<Border> _sSize = new List<Border>();
        private readonly List<Border> _sPos = new List<Border>();
        private readonly List<Border> _sMetric = new List<Border>();
        private bool _simpleBuilt;

        // ---------------------------------------------------------------- mode

        /// <summary>Simple hides the tab list and shows one page. Advanced is the old window.</summary>
        private void ApplyMode()
        {
            bool adv = _settings.Advanced;
            try
            {
                if (!adv && !_simpleBuilt) BuildSimple();

                SideCol.Width = new GridLength(adv ? 196 : 0);
                SideBar.Visibility = adv ? Visibility.Visible : Visibility.Collapsed;
                PanelSimple.Visibility = adv ? Visibility.Collapsed : Visibility.Visible;

                foreach (var n in PanelNames)
                    if (FindName(n) is UIElement el)
                        el.Visibility = adv && n == CurrentTabPanel() ? Visibility.Visible : Visibility.Collapsed;

                SetPageHead(adv ? CurrentTabPanel() : null);
                ModeSimple.IsChecked = !adv;
                ModeAdvanced.IsChecked = adv;

                MinWidth = adv ? 640 : 560;
                if (WindowState == WindowState.Normal)
                {
                    Width = Math.Max(MinWidth, adv ? 892 : 720);
                    Height = Math.Max(MinHeight, adv ? 668 : 704);
                }

                if (!adv) SyncSimple();
                try { Scroller.ScrollToTop(); } catch { }
            }
            catch { }

            // Failsafe. If the simple page did not come up for any reason, fall back to
            // the tabs rather than showing an empty window.
            try
            {
                if (!_settings.Advanced && PanelSimple.Children.Count == 0)
                {
                    _settings.Advanced = true;
                    SideCol.Width = new GridLength(196);
                    SideBar.Visibility = Visibility.Visible;
                    PanelSimple.Visibility = Visibility.Collapsed;
                    if (FindName("PanelThemes") is UIElement pt) pt.Visibility = Visibility.Visible;
                    ModeAdvanced.IsChecked = true;
                }
            }
            catch { }
        }

        private string CurrentTabPanel()
        {
            foreach (var n in PanelNames)
                if (FindName("Tab" + n.Substring(5)) is RadioButton rb && rb.IsChecked == true)
                    return n;
            return "PanelThemes";
        }

        private void Mode_Checked(object sender, RoutedEventArgs e)
        {
            if (_loading) return;
            var rb = sender as RadioButton;
            if (rb == null) return;

            bool adv = Equals(rb.Tag, "adv");
            if (adv == _settings.Advanced) return;

            _settings.Advanced = adv;
            if (adv)
            {
                TabThemes.IsChecked = true;
                foreach (var n in PanelNames)
                    if (FindName(n) is UIElement el)
                        el.Visibility = n == "PanelThemes" ? Visibility.Visible : Visibility.Collapsed;
            }
            ApplyMode();
        }

        // ---------------------------------------------------------------- build

        private void BuildSimple()
        {
            _simpleBuilt = true;
            PanelSimple.Children.Clear();
            _sTheme.Clear(); _sProfile.Clear(); _sSize.Clear(); _sPos.Clear(); _sMetric.Clear();

            // 1 ------------------------------------------------ what it is for
            PanelSimple.Children.Add(Head("PËR ÇKA E PËRDOR"));
            var profs = new UniformGrid { Columns = 2, Margin = new Thickness(0, 0, 0, 4) };
            profs.Children.Add(ProfileTile(UiProfile.Gaming, "Lojëra",
                "FPS, 1% low, VRAM dhe temperaturat"));
            profs.Children.Add(ProfileTile(UiProfile.It, "Punë",
                "Disku, rrjeti, ping dhe koha ndezur"));
            PanelSimple.Children.Add(profs);

            // 2 ------------------------------------------------------ the look
            PanelSimple.Children.Add(Head("SI DUKET", 20));
            var themes = new WrapPanel();
            foreach (var name in QuickThemes)
            {
                var t = ThemeLibrary.Find(name);
                if (t != null) themes.Children.Add(ThemeTile(t));
            }
            PanelSimple.Children.Add(themes);

            var more = new Button
            {
                Style = (Style)FindResource("GhostButton"),
                Content = Lang.T("Të gjitha temat") + ", " + ThemeLibrary.All.Length + " sosh",
                HorizontalAlignment = HorizontalAlignment.Left,
                Margin = new Thickness(0, 2, 0, 0)
            };
            more.Click += (s, e) =>
            {
                _settings.Advanced = true;
                TabThemes.IsChecked = true;
                ApplyMode();
            };
            PanelSimple.Children.Add(more);

            // 3 --------------------------------------------- size and position
            var two = new Grid { Margin = new Thickness(0, 20, 0, 0) };
            two.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            two.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var left = new StackPanel();
            left.Children.Add(Head("SA I MADH", 0));
            var sizes = new WrapPanel();
            string[] sizeNames = { "E vogël", "Normale", "E madhe", "Shumë e madhe" };
            for (int i = 0; i < QuickSizes.Length; i++)
                sizes.Children.Add(SizeTile(QuickSizes[i], sizeNames[i]));
            left.Children.Add(sizes);
            left.Children.Add(new TextBlock
            {
                Style = (Style)FindResource("Caption"),
                Margin = new Thickness(0, 6, 16, 0),
                TextWrapping = TextWrapping.Wrap,
                Text = Lang.T("Mund ta tërheqësh edhe nga qoshja e poshtme e djathtë e kartelës.")
            });
            Grid.SetColumn(left, 0);
            two.Children.Add(left);

            var right = new StackPanel();
            right.Children.Add(Head("KU TË RRIJË", 0));
            var pos = new UniformGrid { Rows = 3, Columns = 3, HorizontalAlignment = HorizontalAlignment.Left };
            foreach (WidgetPosition wp in new[]
            {
                WidgetPosition.TopLeft, WidgetPosition.TopCenter, WidgetPosition.TopRight,
                WidgetPosition.MiddleLeft, WidgetPosition.Center, WidgetPosition.MiddleRight,
                WidgetPosition.BottomLeft, WidgetPosition.BottomCenter, WidgetPosition.BottomRight
            })
                pos.Children.Add(PosTile(wp));
            right.Children.Add(pos);
            Grid.SetColumn(right, 1);
            two.Children.Add(right);
            PanelSimple.Children.Add(two);

            // 4 -------------------------------------------------- what is shown
            PanelSimple.Children.Add(Head("ÇKA SHIHET", 20));
            PanelSimple.Children.Add(new TextBlock
            {
                Style = (Style)FindResource("Caption"),
                Margin = new Thickness(0, 0, 0, 9),
                TextWrapping = TextWrapping.Wrap,
                Text = Lang.T("Kliko për ta ndezur ose fikur një rresht.")
            });
            var chips = new WrapPanel();
            foreach (var m in QuickMetrics)
                chips.Children.Add(MetricTile(m[0], m[1]));
            PanelSimple.Children.Add(chips);

            // 5 ------------------------------------------------------ behaviour
            PanelSimple.Children.Add(Head("SJELLJA", 20));
            PanelSimple.Children.Add(Switch("Gjithmonë sipër të gjitha dritareve", "AlwaysOnTop"));
            PanelSimple.Children.Add(Switch("Nisu bashkë me Windows", "StartWithWindows"));
            PanelSimple.Children.Add(Switch("Blloko pozicionin, që të mos lëvizë gabimisht", "Locked"));
            PanelSimple.Children.Add(Switch("Mausi kalon përtej kartelës", "ClickThrough"));

            // 6 --------------------------------------------------------- footer
            PanelSimple.Children.Add(new Border
            {
                Height = 1,
                Background = new SolidColorBrush(Color.FromArgb(0x14, 0xFF, 0xFF, 0xFF)),
                Margin = new Thickness(0, 22, 0, 14)
            });

            var foot = new StackPanel { Orientation = Orientation.Horizontal };
            var upd = new Button
            {
                Style = (Style)FindResource("GhostButton"),
                Content = Lang.T("Kontrollo për update")
            };
            upd.Click += (s, e) => CheckUpdatesRequested?.Invoke(this, EventArgs.Empty);
            foot.Children.Add(upd);
            foot.Children.Add(new TextBlock
            {
                Style = (Style)FindResource("Caption"),
                Margin = new Thickness(14, 0, 0, 0),
                VerticalAlignment = VerticalAlignment.Center,
                Text = "LIKAsys " + AppInfo.VersionText + "  ·  Likaapps.com"
            });
            PanelSimple.Children.Add(foot);

            PanelSimple.Children.Add(new TextBlock
            {
                Style = (Style)FindResource("Caption"),
                Margin = new Thickness(0, 12, 0, 0),
                TextWrapping = TextWrapping.Wrap,
                Text = Lang.T("Çdo ndryshim ruhet vetvetiu. Për ngjyrat, fontin dhe pjesën tjetër, kalo te Avancuar lart.")
            });
        }

        // ---------------------------------------------------------------- tiles

        private TextBlock Head(string text, double top = 0)
        {
            return new TextBlock
            {
                Style = (Style)FindResource("H2"),
                Text = Lang.T(text),
                Margin = new Thickness(0, top, 0, 9)
            };
        }

        private static Border Shell(double w, double h, Thickness pad)
        {
            return new Border
            {
                Width = w > 0 ? w : double.NaN,
                Height = h > 0 ? h : double.NaN,
                CornerRadius = new CornerRadius(10),
                Margin = new Thickness(0, 0, 9, 9),
                Padding = pad,
                BorderThickness = new Thickness(1.6),
                BorderBrush = new SolidColorBrush(Color.FromArgb(0x24, 0xFF, 0xFF, 0xFF)),
                Background = new SolidColorBrush(Color.FromArgb(0xFF, 0x13, 0x1A, 0x26)),
                Cursor = Cursors.Hand
            };
        }

        private Border ProfileTile(UiProfile p, string title, string sub)
        {
            var card = Shell(0, 0, new Thickness(15, 13, 15, 13));
            card.Tag = p;

            var sp = new StackPanel();
            var top = new StackPanel { Orientation = Orientation.Horizontal };
            top.Children.Add(new System.Windows.Shapes.Path
            {
                Data = (Geometry)TryFindResource(p == UiProfile.Gaming ? "IconFps" : "IconDisk"),
                Width = 18,
                Height = 18,
                Stretch = Stretch.Uniform,
                Margin = new Thickness(0, 0, 9, 0),
                VerticalAlignment = VerticalAlignment.Center,
                Stroke = new SolidColorBrush(Color.FromRgb(0x93, 0xA6, 0xBE)),
                StrokeThickness = 1.7
            });
            top.Children.Add(new TextBlock
            {
                Text = Lang.T(title),
                FontSize = 15,
                FontWeight = FontWeights.SemiBold,
                VerticalAlignment = VerticalAlignment.Center,
                Foreground = new SolidColorBrush(Color.FromRgb(0xEA, 0xF2, 0xFF))
            });
            sp.Children.Add(top);
            sp.Children.Add(new TextBlock
            {
                Style = (Style)FindResource("Caption"),
                Text = Lang.T(sub),
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 6, 0, 0)
            });
            card.Child = sp;

            card.MouseLeftButtonUp += (s, e) => { if (!_loading) { PickProfile(p); SyncSimple(); } };
            _sProfile.Add(card);
            return card;
        }

        private Border ThemeTile(ThemePreset t)
        {
            var card = Shell(136, 74, new Thickness(11, 9, 11, 9));
            card.Tag = t;
            card.Background = Brush(t.BgBottom, 0x0E, 0x13, 0x1D);

            var sp = new StackPanel();
            var top = new StackPanel { Orientation = Orientation.Horizontal };
            top.Children.Add(new Ellipse
            {
                Width = 8,
                Height = 8,
                Fill = Brush(t.Accent),
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 7, 0)
            });
            top.Children.Add(new TextBlock
            {
                Text = t.Name,
                FontSize = 11.5,
                TextTrimming = TextTrimming.CharacterEllipsis,
                VerticalAlignment = VerticalAlignment.Center,
                Foreground = Brush(t.Text, 0xEA, 0xF2, 0xFF)
            });
            sp.Children.Add(top);
            sp.Children.Add(MiniBar(t, 0.72, t.Accent, 9));
            sp.Children.Add(MiniBar(t, 0.44, t.Accent2, 5));
            card.Child = sp;

            card.MouseLeftButtonUp += (s, e) => { if (!_loading) { PickTheme(t); SyncSimple(); } };
            _sTheme.Add(card);
            return card;
        }

        private Border SizeTile(double scale, string name)
        {
            var card = Shell(0, 0, new Thickness(13, 9, 13, 9));
            card.Tag = scale;
            card.Child = new TextBlock
            {
                Text = Lang.T(name),
                FontSize = 12,
                Foreground = new SolidColorBrush(Color.FromRgb(0xC5, 0xD3, 0xE6))
            };
            card.MouseLeftButtonUp += (s, e) =>
            {
                if (_loading) return;
                _settings.Scale = scale;
                SyncSimple();
            };
            _sSize.Add(card);
            return card;
        }

        private Border PosTile(WidgetPosition wp)
        {
            var card = Shell(38, 30, new Thickness(0));
            card.Tag = wp;
            card.Margin = new Thickness(0, 0, 5, 5);
            card.CornerRadius = new CornerRadius(6);

            string n = wp.ToString();
            var dot = new Border
            {
                Width = 12,
                Height = 7,
                CornerRadius = new CornerRadius(2),
                Background = new SolidColorBrush(Color.FromRgb(0x5D, 0x6E, 0x85)),
                Margin = new Thickness(5),
                HorizontalAlignment = n.Contains("Left") ? HorizontalAlignment.Left
                    : n.Contains("Right") ? HorizontalAlignment.Right : HorizontalAlignment.Center,
                VerticalAlignment = n.StartsWith("Top") ? VerticalAlignment.Top
                    : n.StartsWith("Bottom") ? VerticalAlignment.Bottom : VerticalAlignment.Center
            };
            card.Child = dot;
            card.ToolTip = Lang.T(PosName(wp));

            card.MouseLeftButtonUp += (s, e) =>
            {
                if (_loading) return;
                _settings.Position = wp;
                SyncSimple();
            };
            _sPos.Add(card);
            return card;
        }

        private static string PosName(WidgetPosition wp)
        {
            switch (wp)
            {
                case WidgetPosition.TopLeft: return "Lart majtas";
                case WidgetPosition.TopCenter: return "Lart në mes";
                case WidgetPosition.TopRight: return "Lart djathtas";
                case WidgetPosition.MiddleLeft: return "Në mes majtas";
                case WidgetPosition.Center: return "Në qendër";
                case WidgetPosition.MiddleRight: return "Në mes djathtas";
                case WidgetPosition.BottomLeft: return "Poshtë majtas";
                case WidgetPosition.BottomCenter: return "Poshtë në mes";
                default: return "Poshtë djathtas";
            }
        }

        private Border MetricTile(string prop, string label)
        {
            var card = Shell(0, 0, new Thickness(12, 7, 12, 7));
            card.Tag = prop;
            card.Child = new TextBlock
            {
                Text = Lang.T(label),
                FontSize = 12,
                Foreground = new SolidColorBrush(Color.FromRgb(0xC5, 0xD3, 0xE6))
            };
            card.MouseLeftButtonUp += (s, e) =>
            {
                if (_loading) return;
                var pi = Prop(prop);
                if (pi == null) return;
                bool now = pi.GetValue(_settings) is bool v && v;
                pi.SetValue(_settings, !now);
                SyncSimple();
            };
            _sMetric.Add(card);
            return card;
        }

        private CheckBox Switch(string text, string prop)
        {
            var cb = new CheckBox
            {
                Style = (Style)FindResource("Switch"),
                Content = Lang.T(text),
                Tag = prop
            };
            var pi = Prop(prop);
            if (pi != null) cb.IsChecked = pi.GetValue(_settings) is bool v && v;

            RoutedEventHandler flip = (s, e) =>
            {
                if (_loading) return;
                var p = Prop((string)cb.Tag);
                if (p != null) p.SetValue(_settings, cb.IsChecked == true);
            };
            cb.Checked += flip;
            cb.Unchecked += flip;
            _sSwitches.Add(cb);
            return cb;
        }

        private readonly List<CheckBox> _sSwitches = new List<CheckBox>();

        // ----------------------------------------------------------------- sync

        /// <summary>Repaints every tile so the page always matches the settings,
        /// even when something else changed them, like the tray menu.</summary>
        private void SyncSimple()
        {
            if (!_simpleBuilt) return;
            try
            {
                foreach (var c in _sProfile)
                    Mark(c, (UiProfile)c.Tag == _settings.Profile, null);

                foreach (var c in _sTheme)
                {
                    var t = (ThemePreset)c.Tag;
                    Mark(c, string.Equals(t.Name, _settings.ThemeName, StringComparison.OrdinalIgnoreCase), t.Accent);
                }

                foreach (var c in _sSize)
                    Mark(c, Math.Abs((double)c.Tag - _settings.Scale) < 0.02, null);

                foreach (var c in _sPos)
                {
                    bool on = (WidgetPosition)c.Tag == _settings.Position;
                    Mark(c, on, null);
                    if (c.Child is Border d)
                        d.Background = new SolidColorBrush(on
                            ? Color.FromRgb(0x3D, 0xDC, 0x97)
                            : Color.FromRgb(0x5D, 0x6E, 0x85));
                }

                foreach (var c in _sMetric)
                {
                    var pi = Prop((string)c.Tag);
                    bool on = pi != null && pi.GetValue(_settings) is bool v && v;

                    // soft fill rather than a heavy outline, because a dozen chips
                    // with thick green borders is harder to read than no chips at all
                    c.BorderThickness = new Thickness(1.6);
                    c.BorderBrush = new SolidColorBrush(on
                        ? Color.FromArgb(0x66, 0x3D, 0xDC, 0x97)
                        : Color.FromArgb(0x24, 0xFF, 0xFF, 0xFF));
                    c.Background = new SolidColorBrush(on
                        ? Color.FromArgb(0x22, 0x3D, 0xDC, 0x97)
                        : Color.FromArgb(0xFF, 0x13, 0x1A, 0x26));
                    if (c.Child is TextBlock tb)
                        tb.Foreground = new SolidColorBrush(on
                            ? Color.FromRgb(0xBF, 0xF0, 0xDB)
                            : Color.FromRgb(0x6E, 0x7E, 0x95));
                }

                foreach (var cb in _sSwitches)
                {
                    var pi = Prop((string)cb.Tag);
                    if (pi != null) cb.IsChecked = pi.GetValue(_settings) is bool v && v;
                }
            }
            catch { }
        }

        private static void Mark(Border c, bool on, string accent)
        {
            Color a = Color.FromRgb(0x3D, 0xDC, 0x97);
            if (on && !string.IsNullOrEmpty(accent))
            {
                try
                {
                    var o = ColorConverter.ConvertFromString(accent);
                    if (o is Color pc) a = pc;
                }
                catch { }
            }
            c.BorderBrush = new SolidColorBrush(on ? a : Color.FromArgb(0x24, 0xFF, 0xFF, 0xFF));
            c.BorderThickness = new Thickness(on ? 2.4 : 1.6);
        }
    }
}
