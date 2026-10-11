using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using LIKAsys.Core;

namespace LIKAsys.Ui
{
    /// <summary>
    /// The monitor picker. It draws the real desktop arrangement to scale, the way the
    /// Windows display page does, so picking a screen is pointing at it rather than
    /// guessing which one "2" is.
    /// </summary>
    public partial class SettingsWindow
    {
        private readonly List<Border> _modeCards = new List<Border>();
        private readonly List<Border> _screenTiles = new List<Border>();

        /// <summary>Called from the app when Windows reports monitors coming or going.</summary>
        public void RefreshScreens()
        {
            try
            {
                Screens.Invalidate();
                BuildScreens();
                if (_simpleBuilt) BuildSimple();
            }
            catch { }
        }

        private void BuildScreens()
        {
            if (ScreenHost == null) return;
            ScreenHost.Children.Clear();
            _modeCards.Clear();
            _screenTiles.Clear();

            // --- how the monitor is chosen
            var modes = new WrapPanel { Margin = new Thickness(0, 0, 0, 12) };
            modes.Children.Add(ModeCard(MonitorMode.Primary, "Ekrani kryesor",
                "Aty ku Windows e ka desktopin kryesor"));
            modes.Children.Add(ModeCard(MonitorMode.Game, "Aty ku është loja",
                "Ndjek dritaren që është përpara"));
            modes.Children.Add(ModeCard(MonitorMode.Fixed, "Një ekran i zgjedhur",
                "Zgjidhe më poshtë dhe rri aty"));
            ScreenHost.Children.Add(modes);

            // --- the desktop, drawn to scale
            var all = Screens.All();
            ScreenHost.Children.Add(ScreenMap(all));

            var line = string.Join("   ·   ", all.Select(s =>
                s.Caption + (s.Primary ? " " + Lang.T("(kryesor)") : "")));
            ScreenHost.Children.Add(new TextBlock
            {
                Style = (Style)FindResource("Caption"),
                Margin = new Thickness(0, 10, 0, 0),
                TextWrapping = TextWrapping.Wrap,
                Text = (all.Count == 1
                    ? Lang.T("U gjet 1 ekran.")
                    : string.Format(Lang.T("U gjetën {0} ekrane."), all.Count)) + "   " + line
            });

            ScreenHost.Children.Add(new TextBlock
            {
                Style = (Style)FindResource("Caption"),
                Margin = new Thickness(0, 6, 0, 0),
                TextWrapping = TextWrapping.Wrap,
                Text = Lang.T("Nëse një ekran hiqet, kartela kthehet vetvetiu në një ekran që ekziston.")
            });

            SyncScreens();
        }

        private Border ModeCard(MonitorMode mode, string title, string sub)
        {
            var card = new Border
            {
                Width = 184,
                CornerRadius = new CornerRadius(10),
                Margin = new Thickness(0, 0, 9, 0),
                Padding = new Thickness(13, 10, 13, 10),
                BorderThickness = new Thickness(1.6),
                BorderBrush = new SolidColorBrush(Color.FromArgb(0x24, 0xFF, 0xFF, 0xFF)),
                Background = new SolidColorBrush(Color.FromArgb(0xFF, 0x13, 0x1A, 0x26)),
                Cursor = Cursors.Hand,
                Tag = mode
            };

            var sp = new StackPanel();
            sp.Children.Add(new TextBlock
            {
                Text = Lang.T(title),
                FontSize = 12.5,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(Color.FromRgb(0xEA, 0xF2, 0xFF))
            });
            sp.Children.Add(new TextBlock
            {
                Style = (Style)FindResource("Caption"),
                Text = Lang.T(sub),
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 4, 0, 0)
            });
            card.Child = sp;

            card.MouseLeftButtonUp += (s, e) =>
            {
                if (_loading) return;
                _settings.MonitorMode = mode;
                if (mode == MonitorMode.Fixed && string.IsNullOrEmpty(_settings.MonitorId))
                {
                    var cur = Screens.All().FirstOrDefault(x => x.Index == _settings.MonitorIndex)
                              ?? Screens.Primary();
                    _settings.MonitorId = cur.Id;
                    _settings.MonitorIndex = cur.Index;
                }
                SyncScreens();
            };
            _modeCards.Add(card);
            return card;
        }

        /// <summary>
        /// The monitors as rectangles, in their real relative positions, shrunk to fit.
        /// A small block inside each one shows where the widget would sit.
        /// </summary>
        private FrameworkElement ScreenMap(List<ScreenInfo> all)
        {
            const double MapW = 470, MapH = 190, Gap = 6;

            int minX = all.Min(s => s.Left), minY = all.Min(s => s.Top);
            int maxX = all.Max(s => s.Right), maxY = all.Max(s => s.Bottom);
            double spanX = Math.Max(1, maxX - minX), spanY = Math.Max(1, maxY - minY);
            double k = Math.Min((MapW - Gap * 2) / spanX, (MapH - Gap * 2) / spanY);

            var host = new Canvas
            {
                Width = MapW,
                Height = Math.Min(MapH, spanY * k + Gap * 2),
                HorizontalAlignment = HorizontalAlignment.Left,
                Background = new SolidColorBrush(Color.FromArgb(0x30, 0x06, 0x09, 0x0F))
            };

            double offX = (MapW - spanX * k) / 2;

            foreach (var s in all)
            {
                double w = Math.Max(26, s.Width * k), h = Math.Max(20, s.Height * k);
                var tile = new Border
                {
                    Width = w,
                    Height = h,
                    CornerRadius = new CornerRadius(5),
                    BorderThickness = new Thickness(1.6),
                    BorderBrush = new SolidColorBrush(Color.FromArgb(0x30, 0xFF, 0xFF, 0xFF)),
                    Background = new SolidColorBrush(Color.FromArgb(0xFF, 0x12, 0x18, 0x24)),
                    Cursor = Cursors.Hand,
                    Tag = s,
                    ToolTip = s.Caption
                };

                var inner = new Grid();
                inner.Children.Add(new TextBlock
                {
                    Text = (s.Index + 1).ToString(),
                    FontSize = Math.Min(30, h * 0.5),
                    FontWeight = FontWeights.Bold,
                    Opacity = 0.5,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    Foreground = new SolidColorBrush(Color.FromRgb(0x5D, 0x6E, 0x85))
                });

                // where the card would land on this monitor
                var dot = new Border
                {
                    Width = Math.Max(10, w * 0.26),
                    Height = Math.Max(5, h * 0.13),
                    CornerRadius = new CornerRadius(2),
                    Margin = new Thickness(4),
                    Background = new SolidColorBrush(Color.FromRgb(0x3D, 0xDC, 0x97)),
                    HorizontalAlignment = HAlign(_settings.Position),
                    VerticalAlignment = VAlign(_settings.Position)
                };
                inner.Children.Add(dot);

                if (s.Primary)
                    inner.Children.Add(new TextBlock
                    {
                        Text = Lang.T("kryesor"),
                        FontSize = 8.5,
                        Margin = new Thickness(0, 0, 4, 3),
                        HorizontalAlignment = HorizontalAlignment.Right,
                        VerticalAlignment = VerticalAlignment.Bottom,
                        Foreground = new SolidColorBrush(Color.FromRgb(0x5D, 0x6E, 0x85))
                    });

                tile.Child = inner;
                Canvas.SetLeft(tile, offX + (s.Left - minX) * k);
                Canvas.SetTop(tile, Gap + (s.Top - minY) * k);

                tile.MouseLeftButtonUp += (snd, e) =>
                {
                    if (_loading) return;
                    _settings.MonitorMode = MonitorMode.Fixed;
                    _settings.MonitorId = s.Id;
                    _settings.MonitorIndex = s.Index;
                    SyncScreens();
                };

                host.Children.Add(tile);
                _screenTiles.Add(tile);
            }

            return new Border
            {
                CornerRadius = new CornerRadius(10),
                BorderThickness = new Thickness(1),
                BorderBrush = new SolidColorBrush(Color.FromArgb(0x14, 0xFF, 0xFF, 0xFF)),
                Background = new SolidColorBrush(Color.FromArgb(0xFF, 0x0A, 0x0E, 0x16)),
                Padding = new Thickness(8),
                HorizontalAlignment = HorizontalAlignment.Left,
                Child = host
            };
        }

        private static HorizontalAlignment HAlign(WidgetPosition p)
        {
            string n = p.ToString();
            if (n.Contains("Left")) return HorizontalAlignment.Left;
            if (n.Contains("Right")) return HorizontalAlignment.Right;
            return HorizontalAlignment.Center;
        }

        private static VerticalAlignment VAlign(WidgetPosition p)
        {
            string n = p.ToString();
            if (n.StartsWith("Top")) return VerticalAlignment.Top;
            if (n.StartsWith("Bottom")) return VerticalAlignment.Bottom;
            return VerticalAlignment.Center;
        }

        private void SyncScreens()
        {
            try
            {
                foreach (var c in _modeCards)
                {
                    bool on = (MonitorMode)c.Tag == _settings.MonitorMode;
                    c.BorderBrush = new SolidColorBrush(on
                        ? Color.FromRgb(0x3D, 0xDC, 0x97)
                        : Color.FromArgb(0x24, 0xFF, 0xFF, 0xFF));
                    c.BorderThickness = new Thickness(on ? 2.4 : 1.6);
                }

                var live = Screens.Pick(_settings);
                foreach (var t in _screenTiles)
                {
                    var s = (ScreenInfo)t.Tag;
                    bool on = live != null && s.Id == live.Id;
                    t.BorderBrush = new SolidColorBrush(on
                        ? Color.FromRgb(0x3D, 0xDC, 0x97)
                        : Color.FromArgb(0x30, 0xFF, 0xFF, 0xFF));
                    t.BorderThickness = new Thickness(on ? 2.4 : 1.6);
                    t.Background = new SolidColorBrush(on
                        ? Color.FromArgb(0xFF, 0x14, 0x24, 0x20)
                        : Color.FromArgb(0xFF, 0x12, 0x18, 0x24));

                    if (t.Child is Grid g)
                        foreach (var ch in g.Children)
                            if (ch is Border d && d.CornerRadius.TopLeft < 3)
                            {
                                d.HorizontalAlignment = HAlign(_settings.Position);
                                d.VerticalAlignment = VAlign(_settings.Position);
                                d.Opacity = on ? 1 : 0.35;
                            }
                }
            }
            catch { }
        }
    }
}
