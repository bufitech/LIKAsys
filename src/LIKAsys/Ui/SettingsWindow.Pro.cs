using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using LIKAsys.Core;

namespace LIKAsys.Ui
{
    /// <summary>
    /// The parts that make the advanced window feel like a real settings app: a page
    /// header on every tab, and a search that finds any single switch in the whole
    /// window and takes you to it.
    /// </summary>
    public partial class SettingsWindow
    {
        private sealed class Found
        {
            public string Panel;
            public string Tab;
            public string Text;
            public FrameworkElement Target;
        }

        private readonly List<Found> _index = new List<Found>();
        private bool _indexed;

        /// <summary>Title and one line of plain explanation for each tab.</summary>
        private static readonly Dictionary<string, string[]> Pages =
            new Dictionary<string, string[]>
        {
            ["PanelThemes"] = new[] { "Temat", "73 pamje të gatshme, të ndara në grupe. Kliko njërën dhe kartela ndryshon menjëherë." },
            ["PanelColors"] = new[] { "Ngjyrat", "Çdo ngjyrë e kartelës veç e veç, nëse asnjë temë nuk të bën punë." },
            ["PanelLook"] = new[] { "Pamja e kartelës", "Forma, ikonat, shiritat dhe sa e tejdukshme është." },
            ["PanelFont"] = new[] { "Fonti dhe teksti", "Shkronjat, madhësia e numrave dhe si rrumbullakohen." },
            ["PanelMouse"] = new[] { "Kursori i mouse-it", "Katër grupe kursorësh. Vlen vetëm derisa kompjuteri të fiket." },
            ["PanelPos"] = new[] { "Pozicioni dhe ekranet", "Ku rri kartela, në cilin ekran, dhe si sillet ndaj dritareve tjera." },
            ["PanelMetrics"] = new[] { "Metrikat", "Profili dhe cilat matje duken në kartelë." },
            ["PanelSystem"] = new[] { "Sistemi dhe update", "Nisja me Windows, sa shpesh matet, dhe kontrolli për version të ri." },
            ["PanelLang"] = new[] { "Gjuha", "Shqip ose anglisht. Ndryshon menjëherë, pa rinisur." },
            ["PanelAbout"] = new[] { "Rreth LIKAsys", "Versioni, licenca dhe ku t'i raportosh problemet." },
        };

        private void SetPageHead(string panel)
        {
            try
            {
                if (panel != null && Pages.TryGetValue(panel, out var p))
                {
                    PageTitle.Text = Lang.T(p[0]);
                    PageNote.Text = Lang.T(p[1]);
                    PageHead.Visibility = Visibility.Visible;
                }
                else PageHead.Visibility = Visibility.Collapsed;
            }
            catch { }
        }

        // ------------------------------------------------------------- the index

        /// <summary>
        /// Walks every panel once and writes down each label it finds, together with the
        /// control that carries it. Built lazily, because nothing needs it until someone
        /// actually types in the box.
        /// </summary>
        private void BuildIndex()
        {
            if (_indexed) return;
            _indexed = true;
            _index.Clear();

            foreach (var name in PanelNames)
            {
                if (!(FindName(name) is DependencyObject root)) continue;
                string tab = Pages.TryGetValue(name, out var p) ? Lang.T(p[0]) : name;
                Walk(root, name, tab);
            }
        }

        private void Walk(DependencyObject node, string panel, string tab, int depth = 0)
        {
            if (depth > 12) return;
            foreach (var ch in LogicalTreeHelper.GetChildren(node))
            {
                if (!(ch is DependencyObject d)) continue;
                Take(d, panel, tab);
                Walk(d, panel, tab, depth + 1);
            }
        }

        private void Take(DependencyObject d, string panel, string tab)
        {
            string text = null;
            if (d is CheckBox cb && cb.Content is string cs) text = cs;
            else if (d is TextBlock tb && !string.IsNullOrWhiteSpace(tb.Text)) text = tb.Text;
            else if (d is Button bt && bt.Content is string bs) text = bs;
            else if (d is RadioButton rb && rb.Content is string rs) text = rs;

            if (string.IsNullOrWhiteSpace(text) || text.Length < 2 || text.Length > 70) return;
            if (!(d is FrameworkElement el)) return;
            if (_index.Any(x => x.Panel == panel && x.Text == text)) return;

            _index.Add(new Found { Panel = panel, Tab = tab, Text = text, Target = el });
        }

        // ------------------------------------------------------------- searching

        private void Search_Changed(object sender, TextChangedEventArgs e)
        {
            string q = (SearchBox.Text ?? "").Trim();
            try { SearchHint.Visibility = q.Length == 0 ? Visibility.Visible : Visibility.Collapsed; } catch { }

            if (q.Length < 2)
            {
                NavList.Visibility = Visibility.Visible;
                SearchList.Visibility = Visibility.Collapsed;
                SearchList.Children.Clear();
                return;
            }

            BuildIndex();
            NavList.Visibility = Visibility.Collapsed;
            SearchList.Visibility = Visibility.Visible;
            SearchList.Children.Clear();

            var hits = _index
                .Where(x => x.Text.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0)
                .OrderBy(x => x.Text.StartsWith(q, StringComparison.OrdinalIgnoreCase) ? 0 : 1)
                .ThenBy(x => x.Text.Length)
                .Take(14)
                .ToList();

            if (hits.Count == 0)
            {
                SearchList.Children.Add(new TextBlock
                {
                    Style = (Style)FindResource("Caption"),
                    Margin = new Thickness(12, 8, 10, 0),
                    TextWrapping = TextWrapping.Wrap,
                    Text = Lang.T("Asgjë nuk u gjet.")
                });
                return;
            }

            foreach (var h in hits)
            {
                var sp = new StackPanel();
                sp.Children.Add(new TextBlock
                {
                    Text = h.Text,
                    FontSize = 12,
                    TextTrimming = TextTrimming.CharacterEllipsis,
                    Foreground = new SolidColorBrush(Color.FromRgb(0xEA, 0xF2, 0xFF))
                });
                sp.Children.Add(new TextBlock
                {
                    Text = h.Tab,
                    FontSize = 10,
                    Margin = new Thickness(0, 2, 0, 0),
                    Foreground = new SolidColorBrush(Color.FromRgb(0x5D, 0x6E, 0x85))
                });

                var btn = new Button { Style = (Style)FindResource("FoundRow"), Content = sp, Tag = h };
                btn.Click += (s, ev) => JumpTo((Found)((Button)s).Tag);
                SearchList.Children.Add(btn);
            }
        }

        /// <summary>Opens the right tab, scrolls the control into view and flashes it.</summary>
        private void JumpTo(Found h)
        {
            if (h == null) return;
            try
            {
                foreach (var rb in NavList.Children.OfType<RadioButton>())
                    if ((string)rb.Tag == h.Panel) { rb.IsChecked = true; break; }

                SearchBox.Text = "";
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    try
                    {
                        h.Target.BringIntoView();
                        Flash(h.Target);
                    }
                    catch { }
                }), System.Windows.Threading.DispatcherPriority.Loaded);
            }
            catch { }
        }

        /// <summary>
        /// A short pulse so the eye lands on the right line. Opacity only, because
        /// anything that changes layout would resize the window mid animation.
        /// </summary>
        private static void Flash(FrameworkElement el)
        {
            try
            {
                var a = new DoubleAnimationUsingKeyFrames { FillBehavior = FillBehavior.Stop };
                a.KeyFrames.Add(new LinearDoubleKeyFrame(0.25, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(120))));
                a.KeyFrames.Add(new LinearDoubleKeyFrame(1.0, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(260))));
                a.KeyFrames.Add(new LinearDoubleKeyFrame(0.25, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(400))));
                a.KeyFrames.Add(new LinearDoubleKeyFrame(1.0, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(560))));
                a.Completed += (s, e) => { try { el.BeginAnimation(UIElement.OpacityProperty, null); el.Opacity = 1; } catch { } };
                el.BeginAnimation(UIElement.OpacityProperty, a);
            }
            catch { }
        }

        // ------------------------------------------------------------- palettes

        private readonly List<Border> _palCards = new List<Border>();

        private static readonly string[][] PalNames =
        {
            new[] { "", "Vetëm theksi" },
            new[] { "neon", "Neon" },
            new[] { "ice", "Akull" },
            new[] { "fire", "Zjarr" },
            new[] { "pastel", "Pastel" },
            new[] { "mono", "Një ngjyrë" },
            new[] { "kosova", "Kosova" },
        };

        /// <summary>
        /// Each palette is shown as the colours it actually paints, not as a word.
        /// A name like "neon" means nothing until the eleven swatches are on screen.
        /// </summary>
        private void BuildPalettes()
        {
            if (PaletteHost == null) return;
            PaletteHost.Children.Clear();
            _palCards.Clear();

            foreach (var pn in PalNames)
            {
                var card = new Border
                {
                    CornerRadius = new CornerRadius(9),
                    Margin = new Thickness(0, 0, 8, 8),
                    Padding = new Thickness(9, 7, 9, 7),
                    BorderThickness = new Thickness(1.6),
                    BorderBrush = new SolidColorBrush(Color.FromArgb(0x24, 0xFF, 0xFF, 0xFF)),
                    Background = new SolidColorBrush(Color.FromArgb(0xFF, 0x13, 0x1A, 0x26)),
                    Cursor = Cursors.Hand,
                    Tag = pn[0]
                };

                var sp = new StackPanel();
                var strip = new StackPanel { Orientation = Orientation.Horizontal };
                foreach (var key in Palettes.Keys.Take(7))
                {
                    string hex = Palettes.Color(pn[0], key);
                    var c = hex == null
                        ? Col(_settings.Accent, Color.FromRgb(0x00, 0xE5, 0xFF))
                        : Col(hex, Color.FromRgb(0x8A, 0xA0, 0xBF));
                    strip.Children.Add(new Border
                    {
                        Width = 11,
                        Height = 11,
                        CornerRadius = new CornerRadius(3),
                        Margin = new Thickness(0, 0, 3, 0),
                        Background = new SolidColorBrush(c)
                    });
                }
                sp.Children.Add(strip);
                sp.Children.Add(new TextBlock
                {
                    Text = Lang.T(pn[1]),
                    FontSize = 10.5,
                    Margin = new Thickness(0, 6, 0, 0),
                    Foreground = new SolidColorBrush(Color.FromRgb(0x8C, 0x9D, 0xB4))
                });
                card.Child = sp;

                string val = pn[0];
                card.MouseLeftButtonUp += (s, e) =>
                {
                    if (_loading) return;
                    _settings.Palette = val;
                    if (val.Length > 0 && !_settings.Capsule) _settings.Capsule = true;
                    SyncPalettes();
                };
                _palCards.Add(card);
                PaletteHost.Children.Add(card);
            }
            SyncPalettes();
        }

        private void SyncPalettes()
        {
            try
            {
                foreach (var c in _palCards)
                {
                    bool on = string.Equals((string)c.Tag, _settings.Palette ?? "", StringComparison.OrdinalIgnoreCase);
                    c.BorderBrush = new SolidColorBrush(on
                        ? Color.FromRgb(0x3D, 0xDC, 0x97)
                        : Color.FromArgb(0x24, 0xFF, 0xFF, 0xFF));
                    c.BorderThickness = new Thickness(on ? 2.4 : 1.6);
                }
            }
            catch { }
        }

        // ------------------------------------------------------------- keyboard

        private void Pro_Keys(object sender, KeyEventArgs e)
        {
            try
            {
                if (e.Key == Key.Escape)
                {
                    if (!string.IsNullOrEmpty(SearchBox.Text)) { SearchBox.Text = ""; e.Handled = true; return; }
                    Close();
                    e.Handled = true;
                }
                else if (e.Key == Key.F && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
                {
                    if (!_settings.Advanced) { _settings.Advanced = true; ApplyMode(); }
                    SearchBox.Focus();
                    e.Handled = true;
                }
            }
            catch { }
        }
    }
}
