using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Media.Animation;
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
        private double _diskScale = 20;
        private bool _animating;
        private bool _ready;
        private bool _hiddenByGameRule;

        public event EventHandler SettingsRequested;
        public event EventHandler MinimizedChanged;
        public event EventHandler<Point> MenuRequested;

        public WidgetWindow(AppSettings settings, MetricsService metrics)
        {
            InitializeComponent();
            _settings = settings;
            _metrics = metrics;

            RowsHost.ItemsSource = _rows;

            MouseEnter += (s, e) => { HoverLift(true); IslandSet(true); };
            MouseLeave += (s, e) => { HoverLift(false); IslandSet(false); };
            MouseLeftButtonDown += OnDragStart;
            MouseLeftButtonUp += OnDragEnd;
            MouseRightButtonUp += (s, e) => MenuRequested?.Invoke(this, PointToScreen(e.GetPosition(this)));
            SizeChanged += (s, e) => { if (_ready && !_animating) { UpdateBlurAndRegion(); WidgetPlacement.Apply(this, _settings); } };

            if (_metrics != null) _metrics.Updated += OnMetrics;
            Localize();
            BuildRows();
            ApplySettings();
            SetMinimized(_settings.Minimized);
        }

        /// <summary>
        /// Collapses the card to a single bar, or opens it back up.
        ///
        /// Minimised is not "hidden": the widget is still there, still on top, still
        /// reading the machine - it just shrinks to one line with the headline numbers
        /// so it can sit over a game or a full screen terminal without taking space.
        /// </summary>
        public void SetMinimized(bool on)
        {
            try
            {
                _settings.Minimized = on;

                if (_isleOn)
                {
                    _isleLocked = on;
                    if (on) IslandSet(false);
                    MinIcon.Data = (Geometry)Application.Current.TryFindResource(on ? "IconRestore" : "IconMinimize");
                    MinBtn.ToolTip = Lang.T(on ? "Hape te plote" : "Minimizo");
                    HeaderButtons.Opacity = 1.0;
                    return;
                }

                bool wasMin = RowsHost.Visibility != Visibility.Visible;
                RowsHost.Visibility = on ? Visibility.Collapsed : Visibility.Visible;
                if (!on && wasMin) StaggerIn();
                FooterRow.Visibility = on ? Visibility.Collapsed : Visibility.Visible;
                MiniLine.Visibility = on ? Visibility.Visible : Visibility.Collapsed;
                HeaderRow.Margin = on ? new Thickness(0) : new Thickness(0, 0, 0, 8);

                MinIcon.Data = (Geometry)Application.Current.TryFindResource(on ? "IconRestore" : "IconMinimize");
                MinBtn.ToolTip = Lang.T(on ? "Hape te plote" : "Minimizo");

                // while collapsed the controls must always be reachable, not hover-only
                if (on) HeaderButtons.Opacity = 1.0;
                else HeaderButtons.ClearValue(OpacityProperty);

                if (_metrics?.Latest != null) OnMetrics(_metrics.Latest);
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    UpdateBlurAndRegion();
                    WidgetPlacement.Apply(this, _settings);
                    if (!on && IsVisible) Reveal();
                }), System.Windows.Threading.DispatcherPriority.Loaded);
            }
            catch (Exception ex) { AppInfo.Log("SetMinimized: " + ex.Message); }
        }

        /// <summary>The one-line summary shown while collapsed: the first few rows, short form.</summary>
        private void UpdateMiniLine()
        {
            if (MiniLine.Visibility != Visibility.Visible) return;
            try
            {
                var parts = new List<string>();
                foreach (var r in _rows)
                {
                    if (parts.Count >= 3) break;
                    if (string.IsNullOrEmpty(r.Value) || r.Value == "--") continue;
                    parts.Add(r.Label + " " + r.Value + (string.IsNullOrEmpty(r.Unit) ? "" : r.Unit));
                }
                MiniLine.Text = parts.Count > 0 ? string.Join("   ", parts) : "";
            }
            catch { }
        }

        /// <summary>
        /// Brings the card in with a motion instead of a hard pop: a drop from above, a
        /// slide from a side, or just a fade. The window itself is moved rather than a
        /// transform, so nothing clips and the final position stays exactly where the
        /// placement logic wants it.
        /// </summary>
        protected override void OnClosed(EventArgs e)
        {
            try { IslandRollStop(); } catch { }
            try { if (_motion != null) _motion.Stop(); } catch { }
            base.OnClosed(e);
        }

        public void Reveal()
        {
            try
            {
                StaggerIn();
                var mode = _settings.Reveal;

                BeginAnimation(OpacityProperty, null);
                BeginAnimation(TopProperty, null);
                BeginAnimation(LeftProperty, null);
                Opacity = 1;

                if (mode == RevealAnimation.None) { _animating = false; return; }

                WidgetPlacement.Apply(this, _settings);
                double tx = Left, ty = Top;
                if (double.IsNaN(tx) || double.IsNaN(ty)) return;

                double dx = 0, dy = 0;
                const double travel = 34;
                switch (mode)
                {
                    case RevealAnimation.FromTop: dy = -travel; break;
                    case RevealAnimation.FromBottom: dy = travel; break;
                    case RevealAnimation.FromLeft: dx = -travel; break;
                    case RevealAnimation.FromRight: dx = travel; break;
                }

                var ease = new CubicEase { EasingMode = EasingMode.EaseOut };
                var dur = new Duration(TimeSpan.FromMilliseconds(mode == RevealAnimation.Fade ? 260 : 420));

                _animating = true;
                Opacity = 0;

                // Failsafe: an animation that never completes would leave the widget fully
                // transparent, which to the user is indistinguishable from "it never opened".
                var rescue = new System.Windows.Threading.DispatcherTimer
                { Interval = TimeSpan.FromMilliseconds(1200) };
                rescue.Tick += (s2, e2) =>
                {
                    rescue.Stop();
                    if (Opacity < 1)
                    {
                        BeginAnimation(OpacityProperty, null);
                        BeginAnimation(TopProperty, null);
                        BeginAnimation(LeftProperty, null);
                        Opacity = 1;
                        _animating = false;
                        WidgetPlacement.Apply(this, _settings);
                    }
                };
                rescue.Start();
                var fade = new DoubleAnimation(0, 1, dur) { EasingFunction = ease };

                if (dx != 0 || dy != 0)
                {
                    Left = tx + dx;
                    Top = ty + dy;

                    var slide = new DoubleAnimation(dy != 0 ? ty + dy : tx + dx, dy != 0 ? ty : tx, dur)
                    { EasingFunction = ease };
                    slide.Completed += (s, e) =>
                    {
                        _animating = false;
                        BeginAnimation(TopProperty, null);
                        BeginAnimation(LeftProperty, null);
                        BeginAnimation(OpacityProperty, null);
                        Top = ty; Left = tx; Opacity = 1;
                    };
                    BeginAnimation(OpacityProperty, fade);
                    BeginAnimation(dy != 0 ? TopProperty : LeftProperty, slide);
                }
                else
                {
                    fade.Completed += (s, e) =>
                    {
                        _animating = false;
                        BeginAnimation(OpacityProperty, null);
                        Opacity = 1;
                    };
                    BeginAnimation(OpacityProperty, fade);
                }
            }
            catch (Exception ex) { _animating = false; Opacity = 1; AppInfo.Log("Reveal: " + ex.Message); }
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


        // =================================================================== motion
        //
        // One timer for the whole card. It only runs while something is actually
        // moving and stops itself the moment everything has settled, so an idle
        // widget costs nothing. Every value it touches is a transform or an
        // opacity: no width, no margin, no font size. That is the difference
        // between a card that glides and a card that re-measures itself 60 times
        // a second inside a SizeToContent window.

        private System.Windows.Threading.DispatcherTimer _motion;
        private int _staggerFrame = -1;
        private bool _hoverUp;

        private bool Motion => _settings != null && _settings.Animations;

        /// <summary>How eagerly the bars chase a new reading. Gaming snaps, IT glides.</summary>
        private double Chase => _settings.Profile == UiProfile.Gaming ? 0.30 : 0.17;

        /// <summary>How far a row travels on its way in.</summary>
        private double EntryShift => _settings.Profile == UiProfile.Gaming ? 14 : 6;

        private void Kick()
        {
            if (!Motion) { Settle(); return; }
            if (_motion == null)
            {
                _motion = new System.Windows.Threading.DispatcherTimer(
                    System.Windows.Threading.DispatcherPriority.Render)
                { Interval = TimeSpan.FromMilliseconds(16) };
                _motion.Tick += MotionTick;
            }
            if (!_motion.IsEnabled) _motion.Start();
        }

        /// <summary>Jump everything to its final state and stop the timer.</summary>
        private void Settle()
        {
            try
            {
                _motion?.Stop();
                _staggerFrame = -1;
                foreach (var r in _rows)
                {
                    r.Fill = r.Percent / 100.0;
                    r.RowOpacity = 1;
                    r.RowShift = 0;
                }
            }
            catch { }
        }

        /// <summary>
        /// Rows fade and slide in one after the other. Called when the card appears.
        ///
        /// They start at zero opacity, so if the timer ever failed to run they would stay
        /// invisible. A one shot failsafe puts everything back on screen after a second
        /// and a half, no matter what happened in between.
        /// </summary>
        private void StaggerIn()
        {
            if (!Motion) { Settle(); return; }
            try
            {
                double shift = EntryShift;
                foreach (var r in _rows) { r.RowOpacity = 0; r.RowShift = shift; r.Fill = 0; }
                _staggerFrame = 0;
                Kick();

                var guard = new System.Windows.Threading.DispatcherTimer
                { Interval = TimeSpan.FromMilliseconds(1500) };
                guard.Tick += (s2, e2) =>
                {
                    guard.Stop();
                    try
                    {
                        foreach (var r in _rows)
                            if (r.RowOpacity < 1) { r.RowOpacity = 1; r.RowShift = 0; }
                    }
                    catch { }
                };
                guard.Start();
            }
            catch { Settle(); }
        }

        private void MotionTick(object sender, EventArgs e)
        {
            try
            {
                bool busy = false;
                double k = Chase;

                if (IslandStep()) busy = true;

                if (_staggerFrame >= 0)
                {
                    double shift = EntryShift;
                    bool done = true;
                    for (int i = 0; i < _rows.Count; i++)
                    {
                        double t = (_staggerFrame - i * 2) / 13.0;      // ~32 ms apart, ~210 ms each
                        if (t <= 0) { done = false; continue; }
                        if (t >= 1) { _rows[i].RowOpacity = 1; _rows[i].RowShift = 0; continue; }
                        double o = 1 - Math.Pow(1 - t, 3);              // ease out cubic
                        _rows[i].RowOpacity = o;
                        _rows[i].RowShift = shift * (1 - o);
                        done = false;
                    }
                    _staggerFrame++;
                    if (done) _staggerFrame = -1; else busy = true;
                }

                foreach (var r in _rows)
                {
                    double target = r.Percent / 100.0;
                    double d = target - r.Fill;
                    if (Math.Abs(d) > 0.0015) { r.Fill = r.Fill + d * k; busy = true; }
                    else if (r.Fill != target) r.Fill = target;
                }

                if (!busy) _motion.Stop();
            }
            catch { try { _motion?.Stop(); } catch { } }
        }

        /// <summary>A small lift under the pointer. Pure render transform, costs nothing.</summary>
        private void HoverLift(bool up)
        {
            if (_hoverUp == up) return;
            _hoverUp = up;
            try
            {
                if (!Motion)
                {
                    if (Card.RenderTransform is ScaleTransform flat) { flat.ScaleX = 1; flat.ScaleY = 1; }
                    return;
                }
                if (!(Card.RenderTransform is ScaleTransform st))
                {
                    st = new ScaleTransform(1, 1);
                    Card.RenderTransformOrigin = new Point(0.5, 0.5);
                    Card.RenderTransform = st;
                }
                double to = up ? 1.018 : 1.0;
                var dur = new Duration(TimeSpan.FromMilliseconds(up ? 150 : 220));
                var ease = new CubicEase { EasingMode = EasingMode.EaseOut };
                st.BeginAnimation(ScaleTransform.ScaleXProperty,
                    new DoubleAnimation(to, dur) { EasingFunction = ease });
                st.BeginAnimation(ScaleTransform.ScaleYProperty,
                    new DoubleAnimation(to, dur) { EasingFunction = ease });
            }
            catch { }
        }

        /// <summary>The brand dot breathes. One animation for the whole app.</summary>
        private void ApplyDotPulse()
        {
            try
            {
                BrandDot.BeginAnimation(OpacityProperty, null);
                BrandDot.Opacity = 1;
                if (!Motion || !_settings.ShowBrandDot) return;
                var a = new DoubleAnimation(1, 0.42,
                    new Duration(TimeSpan.FromMilliseconds(_settings.Profile == UiProfile.Gaming ? 1500 : 2600)))
                {
                    AutoReverse = true,
                    RepeatBehavior = RepeatBehavior.Forever,
                    EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut }
                };
                BrandDot.BeginAnimation(OpacityProperty, a);
            }
            catch { }
        }

        // ================================================================ dynamic island

        // The pill. It is one theme, not a mode the user has to find: "Dynamic Island" in
        // the IT group turns it on. Closed, the card is a small black pill that rolls
        // through the readings by itself. The pointer comes near and it opens.
        //
        // Opening really does change the size of the window, which normally is the one
        // thing to avoid. It is allowed here because it lasts fourteen frames and then
        // stops, and because the window is re-placed on every one of those frames so the
        // pill grows from its own centre instead of sliding off to the right.

        private bool _isleOn;            // the island theme is the one in use
        private bool _isleOpen;          // opened up, showing every row
        private bool _isleLocked;        // minimised: stay a pill even on hover
        private double _isleT;           // 0 pill .. 1 open
        private int _isleDir;            // -1 closing, 0 resting, +1 opening
        private double _isleRowsH = -1, _isleFootH = -1, _isleWidth = -1;
        private int _isleIndex;
        private bool _isleFlip;
        private System.Windows.Threading.DispatcherTimer _isleRoll;

        private const int IsleFrames = 14;

        private bool Island => _settings != null && _settings.Island;

        /// <summary>Ease out with a little overshoot. That snap is most of the effect.</summary>
        private static double IsleEase(double t)
        {
            if (t <= 0) return 0;
            if (t >= 1) return 1;
            const double c = 0.94;
            double u = t - 1;
            return 1 + (c + 1) * u * u * u + c * u * u;
        }

        /// <summary>Turns the pill behaviour on, or puts every borrowed property back.</summary>
        private void IslandMode(bool on)
        {
            try
            {
                if (on == _isleOn && on == false) return;
                _isleOn = on;

                if (!on)
                {
                    IslandRollStop();
                    RowsHost.MaxHeight = double.PositiveInfinity;
                    FooterRow.MaxHeight = double.PositiveInfinity;
                    RowsHost.Opacity = 1;
                    FooterRow.Opacity = 1;
                    BrandText.Opacity = 1;
                    IslandSwap.Visibility = Visibility.Collapsed;
                    Card.ClipToBounds = false;
                    Card.MinWidth = 150;
                    _isleRowsH = _isleFootH = _isleWidth = -1;
                    _isleOpen = false; _isleT = 0; _isleDir = 0;
                    return;
                }

                Card.ClipToBounds = true;
                IslandSwap.Visibility = Visibility.Visible;
                MiniLine.Visibility = Visibility.Collapsed;
                RowsHost.Visibility = Visibility.Visible;
                FooterRow.Visibility = Visibility.Visible;
                _isleOpen = false;
                _isleT = 0;
                _isleDir = 0;

                // the natural size has to be read while nothing is clamped
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    IslandMeasure();
                    IslandDraw(0);
                    IslandRollStart();
                }), System.Windows.Threading.DispatcherPriority.Loaded);
            }
            catch (Exception ex)
            {
                AppInfo.Log("IslandMode: " + ex.Message);
                try { IslandMode(false); } catch { }
            }
        }

        /// <summary>
        /// Reads how tall the rows and the footer want to be, and how wide the open card
        /// is. The width then becomes the pill width too, so opening only moves one way.
        /// </summary>
        private void IslandMeasure()
        {
            try
            {
                Card.MinWidth = 150;                 // let it shrink back before reading it
                RowsHost.MaxHeight = double.PositiveInfinity;
                FooterRow.MaxHeight = double.PositiveInfinity;
                RowsHost.Opacity = 1;
                FooterRow.Opacity = 1;
                HeaderRow.Margin = new Thickness(0, 0, 0, 8);
                UpdateLayout();

                _isleRowsH = RowsHost.ActualHeight;
                _isleFootH = FooterRow.ActualHeight;
                _isleWidth = Card.ActualWidth;

                if (_isleRowsH > 1 && _isleWidth > 40) Card.MinWidth = _isleWidth;
                else { _isleRowsH = _isleFootH = -1; }     // measuring failed, never clamp
            }
            catch { _isleRowsH = _isleFootH = -1; }
        }

        /// <summary>Puts the card at a point between pill and open. t is 0..1.</summary>
        private void IslandDraw(double t)
        {
            if (!_isleOn) return;
            try
            {
                if (_isleRowsH < 0) { RowsHost.Opacity = 1; FooterRow.Opacity = 1; return; }

                double e = IsleEase(t);
                double o = e < 0 ? 0 : (e > 1 ? 1 : e);

                RowsHost.MaxHeight = Math.Max(0, _isleRowsH * e);
                FooterRow.MaxHeight = Math.Max(0, _isleFootH * e);
                RowsHost.Opacity = o;
                FooterRow.Opacity = o;
                HeaderRow.Margin = new Thickness(0, 0, 0, 8 * o);

                BrandText.Opacity = o;
                IslandSwap.Opacity = 1 - o;

                UpdateLayout();
                WidgetPlacement.Apply(this, _settings);
            }
            catch { }
        }

        /// <summary>Opens the pill, or lets it fall back shut.</summary>
        private void IslandSet(bool open)
        {
            if (!_isleOn) return;
            if (open && _isleLocked) return;
            if (open == _isleOpen && _isleDir == 0) return;

            _isleOpen = open;

            if (!Motion || _isleRowsH < 0)
            {
                _isleT = open ? 1 : 0;
                _isleDir = 0;
                IslandDraw(_isleT);
                if (open) { foreach (var r in _rows) { r.RowOpacity = 1; r.RowShift = 0; } }
                return;
            }

            _isleDir = open ? 1 : -1;
            if (open) StaggerIn();
            Kick();
        }

        /// <summary>One frame of the open or close morph. Returns true while it is moving.</summary>
        private bool IslandStep()
        {
            if (_isleDir == 0) return false;
            _isleT += _isleDir / (double)IsleFrames;
            if (_isleT >= 1) { _isleT = 1; _isleDir = 0; }
            else if (_isleT <= 0) { _isleT = 0; _isleDir = 0; }
            IslandDraw(_isleT);
            return _isleDir != 0;
        }

        // ---- the roll: a different reading every few seconds, while the pill is shut

        private void IslandRollStart()
        {
            IslandRollStop();
            if (!_isleOn) return;
            _isleRoll = new System.Windows.Threading.DispatcherTimer
            { Interval = TimeSpan.FromMilliseconds(2700) };
            _isleRoll.Tick += (s, e) => IslandRollOnce();
            _isleRoll.Start();
            IslandShow(IslandText(), false);
        }

        private void IslandRollStop()
        {
            try { if (_isleRoll != null) { _isleRoll.Stop(); _isleRoll = null; } } catch { }
        }

        /// <summary>The next reading in the queue, written the short way.</summary>
        private string IslandText()
        {
            try
            {
                if (_rows.Count == 0) return "LIKAsys";
                for (int i = 0; i < _rows.Count; i++)
                {
                    _isleIndex = (_isleIndex + 1) % _rows.Count;
                    var r = _rows[_isleIndex];
                    if (string.IsNullOrEmpty(r.Value) || r.Value == "--") continue;
                    return r.Label + "  " + r.Value + (string.IsNullOrEmpty(r.Unit) ? "" : r.Unit);
                }
            }
            catch { }
            return "LIKAsys";
        }

        /// <summary>
        /// Rewrites the label that is on screen, leaving the pair alone.
        ///
        /// This is the one that runs every second. It must not touch opacity: once a
        /// property has been handed to an animation, a plain assignment to it is ignored,
        /// so only the text is allowed to change here.
        /// </summary>
        private void IslandRefresh()
        {
            try { (_isleFlip ? IsleB : IsleA).Text = IslandCurrent(); } catch { }
        }

        /// <summary>The reading the pill is on right now, refreshed, without moving along.</summary>
        private string IslandCurrent()
        {
            try
            {
                if (_isleIndex >= 0 && _isleIndex < _rows.Count)
                {
                    var r = _rows[_isleIndex];
                    if (!string.IsNullOrEmpty(r.Value) && r.Value != "--")
                        return r.Label + "  " + r.Value + (string.IsNullOrEmpty(r.Unit) ? "" : r.Unit);
                }
            }
            catch { }
            return "LIKAsys";
        }

        private void IslandRollOnce()
        {
            if (!_isleOn || _isleOpen || _isleDir != 0) return;
            if (!IsVisible) return;
            IslandShow(IslandText(), Motion);
        }

        /// <summary>
        /// Swaps the text. The one leaving rises and fades, the one arriving comes up from
        /// below into the same spot. Two labels take turns so neither ever has to wait.
        /// </summary>
        private void IslandShow(string text, bool animate)
        {
            try
            {
                var front = _isleFlip ? IsleB : IsleA;
                var back = _isleFlip ? IsleA : IsleB;
                var backT = _isleFlip ? IsleAT : IsleBT;
                var frontT = _isleFlip ? IsleBT : IsleAT;

                back.Text = text;

                if (!animate)
                {
                    back.BeginAnimation(OpacityProperty, null);
                    front.BeginAnimation(OpacityProperty, null);
                    backT.BeginAnimation(TranslateTransform.YProperty, null);
                    frontT.BeginAnimation(TranslateTransform.YProperty, null);
                    back.Opacity = 1; backT.Y = 0;
                    front.Opacity = 0; frontT.Y = 0;
                    _isleFlip = !_isleFlip;
                    return;
                }

                var ease = new CubicEase { EasingMode = EasingMode.EaseOut };
                var up = TimeSpan.FromMilliseconds(340);
                var fade = TimeSpan.FromMilliseconds(240);

                backT.BeginAnimation(TranslateTransform.YProperty,
                    new DoubleAnimation(15, 0, new Duration(up)) { EasingFunction = ease });
                back.BeginAnimation(OpacityProperty, new DoubleAnimation(0, 1, new Duration(fade)));

                frontT.BeginAnimation(TranslateTransform.YProperty,
                    new DoubleAnimation(0, -15, new Duration(up)) { EasingFunction = ease });
                front.BeginAnimation(OpacityProperty, new DoubleAnimation(1, 0, new Duration(fade)));

                _isleFlip = !_isleFlip;
            }
            catch { }
        }

        // ================================================================== rows

        public void BuildRows()
        {
            _rows.Clear();
            if (_settings.ShowCpu) _rows.Add(NewRow("cpu", "CPU", Ico("IconCpu")));
            if (_settings.ShowGpu) _rows.Add(NewRow("gpu", "GPU", Ico("IconGpu")));
            if (_settings.ShowVram) _rows.Add(NewRow("vram", "VRAM", Ico("IconGpu")));
            if (_settings.ShowRam) _rows.Add(NewRow("ram", "RAM", Ico("IconRam")));
            if (_settings.ShowDisk) _rows.Add(NewRow("disk", "DISK", Ico("IconDisk")));
            if (_settings.ShowDiskIo) _rows.Add(NewRow("diskio", "I/O", Ico("IconDiskIo")));
            if (_settings.ShowFps) _rows.Add(NewRow("fps", "FPS", Ico("IconFps")));
            if (_settings.ShowFpsLow) _rows.Add(NewRow("fpslow", "1% LOW", Ico("IconLow")));
            if (_settings.ShowFrameTime) _rows.Add(NewRow("frametime", "FRAME", Ico("IconFrame")));
            if (_settings.ShowNet) _rows.Add(NewRow("net", "NET", Ico("IconNet")));
            if (_settings.ShowPing) _rows.Add(NewRow("ping", "PING", Ico("IconPing")));
            if (_settings.ShowUptime) _rows.Add(NewRow("uptime", "UPTIME", Ico("IconUptime")));
            if (_rows.Count == 0) _rows.Add(NewRow("cpu", "CPU", Ico("IconCpu")));
            StyleRows();
            if (_metrics?.Latest != null) OnMetrics(_metrics.Latest);
            StaggerIn();
        }

        /// <summary>
        /// The IT profile draws a completely different icon family - flat, technical,
        /// the kind of thing on a rack diagram - while Gaming keeps the rounded set.
        /// Anything without an IT variant simply falls through to the shared icon.
        /// </summary>
        /// <summary>
        /// Which icon family draws this row. The hairline style carries its own set, so the
        /// Apple Clean theme looks right on either profile; otherwise the profile decides.
        /// </summary>
        private string Ico(string baseKey)
        {
            string suffix = _settings.IconStyle == IconStyle.Hairline
                ? "Apple"
                : Profiles.IconSuffix(_settings.Profile);
            if (suffix.Length == 0) return baseKey;
            try { if (Application.Current.TryFindResource(baseKey + suffix) != null) return baseKey + suffix; } catch { }
            return baseKey;
        }

        /// <summary>
        /// With uppercase labels switched off the row names stop shouting: real acronyms
        /// keep their capitals, everything else drops to sentence case. That is what makes
        /// the quiet themes read like a macOS widget rather than a dashboard.
        /// </summary>
        private string Lbl(string label)
        {
            if (_settings.UpperCaseLabels) return label;
            switch (label)
            {
                case "DISK": return "Disk";
                case "NET": return "Network";
                case "PING": return "Ping";
                case "UPTIME": return "Uptime";
                case "FRAME": return "Frame";
                case "1% LOW": return "1% low";
                default: return label;   // CPU, GPU, RAM, VRAM, FPS, I/O
            }
        }

        private MetricRowVm NewRow(string key, string label, string iconKey)
        {
            Geometry geo = null;
            try { geo = Application.Current.TryFindResource(iconKey) as Geometry; } catch { }
            return new MetricRowVm
            {
                Key = key,
                Label = _settings.UpperCaseLabels ? label.ToUpperInvariant() : Lbl(label),
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

        // One frozen brush per colour, reused for the life of the app.
        //
        // This is not only about the allocations. Two separate brushes of the same colour
        // are not equal to each other, so handing a fresh one to a row every second made
        // the binding fire and the text and the bar repaint even when nothing had changed.
        // Returning the same instance makes that update a no-op, which is most of them.
        private static readonly Dictionary<uint, SolidColorBrush> _brushCache =
            new Dictionary<uint, SolidColorBrush>();

        private static SolidColorBrush Solid(Color c)
        {
            uint key = ((uint)c.A << 24) | ((uint)c.R << 16) | ((uint)c.G << 8) | c.B;
            SolidColorBrush b;
            if (_brushCache.TryGetValue(key, out b)) return b;
            b = new SolidColorBrush(c);
            b.Freeze();
            if (_brushCache.Count < 512) _brushCache[key] = b;
            return b;
        }

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

                // a restyle must never quietly re-open a card the user collapsed
                if (_settings.Minimized)
                {
                    RowsHost.Visibility = Visibility.Collapsed;
                    FooterRow.Visibility = Visibility.Collapsed;
                    MiniLine.Visibility = Visibility.Visible;
                    HeaderRow.Margin = new Thickness(0);
                }

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

                IslandMode(_settings.Island);
                ApplyDotPulse();
                if (!Motion) Settle(); else Kick();
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
                if (r.Key == "uptime") r.BarVisibility = Visibility.Collapsed;
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
                    case IconStyle.Hairline:
                        // the quiet set: one thin stroke, no fill, no glow, no shadow
                        r.IconVisibility = Visibility.Visible;
                        r.ShadowVisibility = Visibility.Collapsed;
                        r.IconStroke = Solid(accent);
                        r.IconFill = null;
                        r.IconThickness = 1.15;
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
                case "disk": return "888";
                case "diskio": return "888.8";
                case "uptime": return "88888";
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
                case "disk": return "8888 GB";
                case "diskio": return "\u2193 888.8";
                case "uptime": return "nnnnnnn";
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

                        case "disk":
                            {
                                if (s.DiskUsedPct >= 0)
                                {
                                    r.Value = s.DiskUsedPct.ToString("0", CultureInfo.InvariantCulture);
                                    r.Detail = s.DiskFreeGb >= 0
                                        ? s.DiskFreeGb.ToString(s.DiskFreeGb < 100 ? "0.0" : "0", CultureInfo.InvariantCulture) + " GB " + Lang.T("lire")
                                        : "";
                                    Paint(r, s.DiskUsedPct);
                                }
                                else
                                {
                                    r.Value = "--";
                                    r.Percent = 0;
                                    r.Detail = "";
                                    r.ValueBrush = Solid(DetailC);
                                    r.BarBrush = Solid(Accent);
                                }
                                r.Unit = "%";
                                break;
                            }

                        case "diskio":
                            {
                                double rd = Math.Max(0, s.DiskReadMbs);
                                double wr = Math.Max(0, s.DiskWriteMbs);
                                double top = Math.Max(rd, wr);
                                if (top > _diskScale) _diskScale = Math.Min(2000, top);
                                else _diskScale = Math.Max(20, _diskScale * 0.995);

                                r.Value = rd.ToString(rd < 100 ? "0.0" : "0", CultureInfo.InvariantCulture);
                                r.Unit = "MB/s";
                                r.Detail = "\u2191 " + wr.ToString(wr < 100 ? "0.0" : "0", CultureInfo.InvariantCulture);
                                r.Percent = _diskScale > 0 ? Math.Min(100.0, top / _diskScale * 100.0) : 0;
                                r.ValueBrush = Solid(Accent);
                                r.BarBrush = Solid(Accent);
                                break;
                            }

                        case "uptime":
                            {
                                if (s.UptimeSec > 0)
                                {
                                    var t = TimeSpan.FromSeconds(s.UptimeSec);
                                    r.Value = t.TotalDays >= 1
                                        ? ((int)t.TotalDays).ToString(CultureInfo.InvariantCulture) + "d " + t.Hours.ToString(CultureInfo.InvariantCulture) + "h"
                                        : t.Hours.ToString(CultureInfo.InvariantCulture) + "h " + t.Minutes.ToString("00", CultureInfo.InvariantCulture) + "m";
                                    r.ValueBrush = Solid(Accent);
                                }
                                else
                                {
                                    r.Value = "--";
                                    r.ValueBrush = Solid(DetailC);
                                }
                                r.Unit = "";
                                r.Detail = "";
                                r.Percent = 0;
                                r.BarBrush = Solid(Accent);
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

                UpdateMiniLine();
                if (_isleOn && !_isleOpen && _isleDir == 0) IslandRefresh();
                if (IsVisible) Kick(); else Settle();
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

        private void Minimize_Click(object sender, RoutedEventArgs e)
        {
            SetMinimized(!_settings.Minimized);
            MinimizedChanged?.Invoke(this, EventArgs.Empty);
        }

        private void Hide_Click(object sender, RoutedEventArgs e)
        {
            _settings.WidgetVisible = false;
            SettingsStore.Save(_settings);
            Hide();
        }

        private void Site_Click(object sender, MouseButtonEventArgs e) => AppInfo.OpenUrl(AppInfo.Website);
    }
}
