using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace LIKAsys.Core
{
    public enum WidgetPosition
    {
        TopLeft, TopCenter, TopRight,
        MiddleLeft, Center, MiddleRight,
        BottomLeft, BottomCenter, BottomRight,
        Custom
    }

    public enum WidgetLayout { Vertical, Horizontal, Compact }

    public enum IconStyle { ThreeD, Outline, Hairline, Solid, None }

    public enum BarStyle { Rounded, Square, Segmented, None }

    public enum ValueStyle { Right, Inline }

    /// <summary>What the tray icon itself draws. Logo = the plain LIKAsys icon.</summary>
    public enum TrayIconMode { Logo, Cpu, CpuTemp, Gpu, GpuTemp, Ram, Fps }

    /// <summary>How the widget comes up when LIKAsys starts.</summary>
    public enum StartView { Remember, Full, Minimized }

    public class AppSettings : INotifyPropertyChanged
    {
        // ======================================================= theme
        private string _themeName = "Midnight Glass";

        // ======================================================= colours
        private string _accent = "#00E5FF";
        private string _accent2 = "#7C4DFF";
        private string _bgTop = "#151C2B";
        private string _bgBottom = "#0A0D14";
        private string _borderColor = "#4D00E5FF";
        private string _textColor = "#EAF2FF";
        private string _labelColor = "#93A6BE";
        private string _detailColor = "#5D6E85";
        private string _trackColor = "#1B2433";
        private string _warnColor = "#FFB020";
        private string _dangerColor = "#FF4D5E";
        private bool _colorizeByLoad = true;
        private bool _accentGradient = true;

        // ======================================================= card
        private double _cornerRadius = 14;
        private double _borderThickness = 1;
        private bool _shadowEnabled = true;
        private double _shadowStrength = 0.6;
        private bool _blur = true;
        private double _backgroundOpacity = 0.72;
        private double _paddingH = 13;
        private double _paddingV = 10;
        private double _rowSpacing = 3;
        private double _barHeight = 3;
        private BarStyle _barStyle = BarStyle.Rounded;
        private double _scale = 1.0;
        private WidgetLayout _layout = WidgetLayout.Vertical;
        private bool _showBars = true;
        private bool _showBrandDot = true;
        private bool _glowEffect = true;
        private bool _animations = true;
        private bool _island;
        private MouseCursorStyle _mouseCursor = MouseCursorStyle.None;
        private bool _textShadow = false;
        private IconStyle _iconStyle = IconStyle.ThreeD;
        private double _iconSizeOffset = 6;

        // ======================================================= text
        private string _fontFamily = "Segoe UI";
        private double _fontSize = 13;
        private string _valueWeight = "Bold";
        private string _labelWeight = "Medium";
        private bool _upperCaseLabels = false;
        private bool _showUnits = true;
        private int _decimals = 0;
        private double _labelSizeOffset = -2.5;
        private double _valueSizeOffset = 1.5;

        // ======================================================= position
        private WidgetPosition _position = WidgetPosition.TopRight;
        private int _marginX = 18, _marginY = 18;
        private int _customX = 100, _customY = 100;
        private int _monitorIndex = 0;
        private bool _alwaysOnTop = true;
        private bool _clickThrough = false;
        private bool _locked = false;
        private bool _snapToCorners = true;
        private bool _showOnlyInGame = false;

        // ======================================================= metrics
        private bool _showCpu = true, _showCpuTemp = true, _showGpu = true, _showGpuTemp = true;
        private bool _showVram = true, _showRam = true, _showFps = true;
        private bool _showCpuClock = true;
        private bool _showNet = false, _showPing = false;
        private bool _showDisk = false, _showDiskIo = false, _showUptime = false;
        private UiProfile _profile = UiProfile.Gaming;
        private bool _profileChosen = false;
        private bool _minimized = false;
        private StartView _startView = StartView.Remember;
        private RevealAnimation _reveal = RevealAnimation.Fade;
        private string _pingHost = "1.1.1.1";
        private bool _showFpsApp = true;
        private bool _showFpsLow = false, _showFrameTime = false;
        private string _tempUnit = "C";
        private int _refreshMs = 1000;
        private bool _advancedSensors = true;
        private bool _fpsEnabled = true;
        private int _fpsBarMax = 0;          // 0 = automatic

        // ======================================================= system
        private string _language = "sq";
        private TrayIconMode _trayIconMode = TrayIconMode.Logo;
        private bool _startWithWindows = false;
        private bool _autoCheckUpdates = true;
        private bool _widgetVisible = true;
        private bool _firstRunDone = false;

        // ------------------------------------------------------- batching
        [JsonIgnore] private int _batch;
        [JsonIgnore] private bool _dirtyDuringBatch;

        public void BeginBatch() => _batch++;

        public void EndBatch()
        {
            if (_batch > 0) _batch--;
            if (_batch == 0 && _dirtyDuringBatch)
            {
                _dirtyDuringBatch = false;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(string.Empty));
            }
        }

        // ------------------------------------------------------- properties
        public string ThemeName { get => _themeName; set => Set(ref _themeName, value); }

        public string Accent { get => _accent; set => Set(ref _accent, value); }
        public string Accent2 { get => _accent2; set => Set(ref _accent2, value); }
        public string BgTop { get => _bgTop; set => Set(ref _bgTop, value); }
        public string BgBottom { get => _bgBottom; set => Set(ref _bgBottom, value); }
        public string BorderColor { get => _borderColor; set => Set(ref _borderColor, value); }
        public string TextColor { get => _textColor; set => Set(ref _textColor, value); }
        public string LabelColor { get => _labelColor; set => Set(ref _labelColor, value); }
        public string DetailColor { get => _detailColor; set => Set(ref _detailColor, value); }
        public string TrackColor { get => _trackColor; set => Set(ref _trackColor, value); }
        public string WarnColor { get => _warnColor; set => Set(ref _warnColor, value); }
        public string DangerColor { get => _dangerColor; set => Set(ref _dangerColor, value); }
        public bool ColorizeByLoad { get => _colorizeByLoad; set => Set(ref _colorizeByLoad, value); }
        public bool AccentGradient { get => _accentGradient; set => Set(ref _accentGradient, value); }

        public double CornerRadius { get => _cornerRadius; set => Set(ref _cornerRadius, Clamp(value, 0, 30)); }
        public double BorderThickness { get => _borderThickness; set => Set(ref _borderThickness, Clamp(value, 0, 4)); }
        public bool ShadowEnabled { get => _shadowEnabled; set => Set(ref _shadowEnabled, value); }
        public double ShadowStrength { get => _shadowStrength; set => Set(ref _shadowStrength, Clamp(value, 0, 1)); }
        public bool Blur { get => _blur; set => Set(ref _blur, value); }
        public double BackgroundOpacity { get => _backgroundOpacity; set => Set(ref _backgroundOpacity, Clamp(value, 0, 1)); }
        public double PaddingH { get => _paddingH; set => Set(ref _paddingH, Clamp(value, 2, 40)); }
        public double PaddingV { get => _paddingV; set => Set(ref _paddingV, Clamp(value, 2, 40)); }
        public double RowSpacing { get => _rowSpacing; set => Set(ref _rowSpacing, Clamp(value, 0, 16)); }
        public double BarHeight { get => _barHeight; set => Set(ref _barHeight, Clamp(value, 1, 14)); }
        public BarStyle BarStyle { get => _barStyle; set => Set(ref _barStyle, value); }
        public double Scale { get => _scale; set => Set(ref _scale, Math.Round(Clamp(value, 0.6, 2.5), 2)); }
        public WidgetLayout Layout { get => _layout; set => Set(ref _layout, value); }
        public bool ShowBars { get => _showBars; set => Set(ref _showBars, value); }
        // --- branding is permanent -------------------------------------------------
        // The LIKAsys name and "Made in Kosovo with love" are part of the product, not a
        // preference: the setters accept any value and keep returning true, so neither an old
        // settings.json nor a future code path can ever hide them.
        public bool ShowHeader { get => true; set { /* locked on */ } }
        public bool ShowFooter { get => true; set { /* locked on */ } }
        public bool ShowBrandDot { get => _showBrandDot; set => Set(ref _showBrandDot, value); }
        public bool GlowEffect { get => _glowEffect; set => Set(ref _glowEffect, value); }

        /// <summary>
        /// Motion: bars that glide to their new value, rows that arrive one after the other,
        /// a card that lifts under the pointer. Everything is a transform or an opacity, so
        /// it runs on the render thread and never re-measures the window. Off means instant.
        /// </summary>
        public bool Animations { get => _animations; set => Set(ref _animations, value); }

        /// <summary>
        /// Dynamic Island behaviour: the card sits as a small black pill that cycles through
        /// the readings on its own, and opens up when the pointer comes near. Set by the
        /// theme, not by a checkbox, because only one theme is drawn for it.
        /// </summary>
        public bool Island { get => _island; set => Set(ref _island, value); }

        /// <summary>Pointer pack. Only honoured in the IT profile; Gaming leaves the mouse alone.</summary>
        public MouseCursorStyle MouseCursor { get => _mouseCursor; set => Set(ref _mouseCursor, value); }

        /// <summary>Soft black shadow behind every glyph - makes a background-less widget readable on any wallpaper.</summary>
        public bool TextShadow { get => _textShadow; set => Set(ref _textShadow, value); }
        public IconStyle IconStyle { get => _iconStyle; set => Set(ref _iconStyle, value); }
        public double IconSizeOffset { get => _iconSizeOffset; set => Set(ref _iconSizeOffset, Clamp(value, -2, 22)); }

        public string FontFamily { get => _fontFamily; set => Set(ref _fontFamily, value); }
        public double FontSize { get => _fontSize; set => Set(ref _fontSize, Math.Round(Clamp(value, 8, 34), 1)); }
        public string ValueWeight { get => _valueWeight; set => Set(ref _valueWeight, value); }
        public string LabelWeight { get => _labelWeight; set => Set(ref _labelWeight, value); }
        public bool UpperCaseLabels { get => _upperCaseLabels; set => Set(ref _upperCaseLabels, value); }
        public bool ShowUnits { get => _showUnits; set => Set(ref _showUnits, value); }
        public int Decimals { get => _decimals; set => Set(ref _decimals, (int)Clamp(value, 0, 2)); }
        public double LabelSizeOffset { get => _labelSizeOffset; set => Set(ref _labelSizeOffset, Clamp(value, -6, 4)); }
        public double ValueSizeOffset { get => _valueSizeOffset; set => Set(ref _valueSizeOffset, Clamp(value, -4, 12)); }

        public WidgetPosition Position { get => _position; set => Set(ref _position, value); }
        public int MarginX { get => _marginX; set => Set(ref _marginX, value); }
        public int MarginY { get => _marginY; set => Set(ref _marginY, value); }
        public int CustomX { get => _customX; set => Set(ref _customX, value); }
        public int CustomY { get => _customY; set => Set(ref _customY, value); }
        public int MonitorIndex { get => _monitorIndex; set => Set(ref _monitorIndex, value); }
        public bool AlwaysOnTop { get => _alwaysOnTop; set => Set(ref _alwaysOnTop, value); }
        public bool ClickThrough { get => _clickThrough; set => Set(ref _clickThrough, value); }
        public bool Locked { get => _locked; set => Set(ref _locked, value); }
        public bool SnapToCorners { get => _snapToCorners; set => Set(ref _snapToCorners, value); }
        public bool ShowOnlyInGame { get => _showOnlyInGame; set => Set(ref _showOnlyInGame, value); }

        public bool ShowCpu { get => _showCpu; set => Set(ref _showCpu, value); }
        public bool ShowCpuTemp { get => _showCpuTemp; set => Set(ref _showCpuTemp, value); }
        public bool ShowCpuClock { get => _showCpuClock; set => Set(ref _showCpuClock, value); }
        public bool ShowGpu { get => _showGpu; set => Set(ref _showGpu, value); }
        public bool ShowGpuTemp { get => _showGpuTemp; set => Set(ref _showGpuTemp, value); }
        public bool ShowVram { get => _showVram; set => Set(ref _showVram, value); }
        public bool ShowRam { get => _showRam; set => Set(ref _showRam, value); }
        public bool ShowFps { get => _showFps; set => Set(ref _showFps, value); }
        public bool ShowFpsApp { get => _showFpsApp; set => Set(ref _showFpsApp, value); }
        public bool ShowFpsLow { get => _showFpsLow; set => Set(ref _showFpsLow, value); }
        public bool ShowFrameTime { get => _showFrameTime; set => Set(ref _showFrameTime, value); }
        public bool ShowDisk { get => _showDisk; set => Set(ref _showDisk, value); }
        public bool ShowDiskIo { get => _showDiskIo; set => Set(ref _showDiskIo, value); }
        public bool ShowUptime { get => _showUptime; set => Set(ref _showUptime, value); }
        public bool ShowNet { get => _showNet; set => Set(ref _showNet, value); }
        public bool ShowPing { get => _showPing; set => Set(ref _showPing, value); }
        public string PingHost { get => _pingHost; set => Set(ref _pingHost, string.IsNullOrWhiteSpace(value) ? "1.1.1.1" : value.Trim()); }
        public string TempUnit { get => _tempUnit; set => Set(ref _tempUnit, value); }
        public int RefreshMs { get => _refreshMs; set => Set(ref _refreshMs, (int)Clamp(value, 250, 5000)); }
        public bool AdvancedSensors { get => _advancedSensors; set => Set(ref _advancedSensors, value); }
        public bool FpsEnabled { get => _fpsEnabled; set => Set(ref _fpsEnabled, value); }
        public int FpsBarMax { get => _fpsBarMax; set => Set(ref _fpsBarMax, (int)Clamp(value, 0, 1000)); }

        /// <summary>"sq" (default) or "en".</summary>
        public string Language
        {
            get => _language;
            set => Set(ref _language, string.Equals(value, "en", StringComparison.OrdinalIgnoreCase) ? "en" : "sq");
        }

        public TrayIconMode TrayIconMode { get => _trayIconMode; set => Set(ref _trayIconMode, value); }

        /// <summary>Gaming or IT. Picked during setup, changeable in the settings at any time.</summary>
        public UiProfile Profile { get => _profile; set => Set(ref _profile, value); }
        public bool ProfileChosen { get => _profileChosen; set => Set(ref _profileChosen, value); }

        /// <summary>Collapsed to a small bar. Survives restarts unless StartView overrides it.</summary>
        public bool Minimized { get => _minimized; set => Set(ref _minimized, value); }
        public StartView StartView { get => _startView; set => Set(ref _startView, value); }

        /// <summary>How the card arrives: a drop from the top, a slide from a side, or nothing.</summary>
        public RevealAnimation Reveal { get => _reveal; set => Set(ref _reveal, value); }

        public bool StartWithWindows { get => _startWithWindows; set => Set(ref _startWithWindows, value); }
        public bool AutoCheckUpdates { get => _autoCheckUpdates; set => Set(ref _autoCheckUpdates, value); }
        public bool WidgetVisible { get => _widgetVisible; set => Set(ref _widgetVisible, value); }
        public bool FirstRunDone { get => _firstRunDone; set => Set(ref _firstRunDone, value); }

        // ------------------------------------------------------- infra
        public event PropertyChangedEventHandler PropertyChanged;

        private static double Clamp(double v, double min, double max) => v < min ? min : (v > max ? max : v);

        private void Set<T>(ref T field, T value, [CallerMemberName] string name = null)
        {
            if (Equals(field, value)) return;
            field = value;
            if (_batch > 0) { _dirtyDuringBatch = true; return; }
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public void RaiseAll() => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(string.Empty));
    }
}
