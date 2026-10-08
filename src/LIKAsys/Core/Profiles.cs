using System;

namespace LIKAsys.Core
{
    /// <summary>Who the widget is for. Picked at install time, changeable any time after.</summary>
    public enum UiProfile
    {
        Gaming = 0,
        It = 1,
        Apple = 2
    }

    /// <summary>How the widget arrives on screen.</summary>
    public enum RevealAnimation
    {
        None = 0,
        FromTop = 1,
        FromRight = 2,
        FromLeft = 3,
        FromBottom = 4,
        Fade = 5
    }

    /// <summary>
    /// The three personalities of LIKAsys. A profile is not a colour swap - it rewrites the
    /// whole look: which rows exist, which icon family draws them, the typeface, the sizes,
    /// the corner radius, the bars, the background and how the card arrives on screen.
    ///
    ///   Gaming  - eyes on frames. Dark glass, neon cyan, 3D icons, big numbers.
    ///   IT      - eyes on the machine. Monospace, tight rows, technical icons, green.
    ///   Apple   - eyes on nothing. Near-white frosted glass, hairline icons, no borders,
    ///             no glow, nothing shouting. It drops in from the top and gets out of the way.
    ///
    /// Position, monitor, language and startup choices are deliberately never touched, so
    /// switching profiles can't throw away personal setup.
    /// </summary>
    public static class Profiles
    {
        public static string Name(UiProfile p) =>
            p == UiProfile.It ? "IT" : p == UiProfile.Apple ? "IT Apple" : "Gaming";

        /// <summary>Suffix appended to an icon resource key to find this profile's variant.</summary>
        public static string IconSuffix(UiProfile p) =>
            p == UiProfile.It ? "It" : p == UiProfile.Apple ? "Apple" : "";

        public static void Apply(AppSettings s, UiProfile p)
        {
            if (s == null) return;

            s.BeginBatch();
            try
            {
                s.Profile = p;
                s.ProfileChosen = true;

                switch (p)
                {
                    case UiProfile.It: ApplyIt(s); break;
                    case UiProfile.Apple: ApplyApple(s); break;
                    default: ApplyGaming(s); break;
                }
            }
            finally { s.EndBatch(); }
        }

        // ===================================================================== gaming

        private static void ApplyGaming(AppSettings s)
        {
            Rows(s, cpu: true, gpu: true, vram: true, ram: true,
                    disk: false, io: false, uptime: false,
                    fps: true, low: true, frame: false, net: false, ping: false);

            s.ThemeName = "Midnight Glass";

            s.Accent = "#FF00E5FF";
            s.Accent2 = "#FF7C4DFF";
            s.BgTop = "#F00E1420";
            s.BgBottom = "#F00A0F18";
            s.BorderColor = "#2600E5FF";
            s.TextColor = "#FFEAF2FF";
            s.LabelColor = "#FF8EA2BD";
            s.DetailColor = "#FF5D6E85";
            s.TrackColor = "#1AFFFFFF";
            s.WarnColor = "#FFFFB020";
            s.DangerColor = "#FFFF4D6A";
            s.AccentGradient = true;
            s.ColorizeByLoad = true;

            s.FontFamily = "Segoe UI";
            s.FontSize = 13;
            s.ValueSizeOffset = 2;
            s.LabelSizeOffset = -1;
            s.ValueWeight = "Bold";
            s.LabelWeight = "SemiBold";
            s.UpperCaseLabels = true;
            s.ShowUnits = true;
            s.Decimals = 0;
            s.TextShadow = true;

            s.IconStyle = IconStyle.ThreeD;
            s.IconSizeOffset = 3;
            s.GlowEffect = true;
            s.ShowBrandDot = true;

            s.ShowBars = true;
            s.BarStyle = BarStyle.Rounded;
            s.BarHeight = 4;

            s.CornerRadius = 14;
            s.BorderThickness = 1;
            s.PaddingH = 13;
            s.PaddingV = 10;
            s.RowSpacing = 4;
            s.BackgroundOpacity = 0.94;
            s.Blur = false;
            s.ShadowEnabled = true;
            s.ShadowStrength = 0.75;

            s.Layout = WidgetLayout.Vertical;
            s.Reveal = RevealAnimation.Fade;
            s.TrayIconMode = TrayIconMode.Fps;
        }

        // ========================================================================= it

        private static void ApplyIt(AppSettings s)
        {
            Rows(s, cpu: true, gpu: true, vram: false, ram: true,
                    disk: true, io: true, uptime: true,
                    fps: false, low: false, frame: false, net: true, ping: true);

            s.ThemeName = "Terminal";

            s.Accent = "#FF3DDC97";
            s.Accent2 = "#FF37B6FF";
            s.BgTop = "#F2080C10";
            s.BgBottom = "#F2060A0D";
            s.BorderColor = "#263DDC97";
            s.TextColor = "#FFDFF5E9";
            s.LabelColor = "#FF7E9A8E";
            s.DetailColor = "#FF4F6B60";
            s.TrackColor = "#16FFFFFF";
            s.WarnColor = "#FFE3B341";
            s.DangerColor = "#FFF85149";
            s.AccentGradient = false;
            s.ColorizeByLoad = true;

            // a monospace face keeps every column of digits in the same place
            s.FontFamily = "Cascadia Mono, Consolas, Segoe UI";
            s.FontSize = 12;
            s.ValueSizeOffset = 0;
            s.LabelSizeOffset = -1;
            s.ValueWeight = "SemiBold";
            s.LabelWeight = "Normal";
            s.UpperCaseLabels = true;
            s.ShowUnits = true;
            s.Decimals = 0;
            s.TextShadow = false;

            s.IconStyle = IconStyle.Outline;
            s.IconSizeOffset = 1;
            s.GlowEffect = false;
            s.ShowBrandDot = true;

            s.ShowBars = true;
            s.BarStyle = BarStyle.Square;
            s.BarHeight = 3;

            s.CornerRadius = 6;
            s.BorderThickness = 1;
            s.PaddingH = 11;
            s.PaddingV = 8;
            s.RowSpacing = 2;
            s.BackgroundOpacity = 0.95;
            s.Blur = false;
            s.ShadowEnabled = true;
            s.ShadowStrength = 0.6;

            s.Layout = WidgetLayout.Vertical;
            s.Reveal = RevealAnimation.FromRight;
            s.TrayIconMode = TrayIconMode.Cpu;
        }

        // ====================================================================== apple

        private static void ApplyApple(AppSettings s)
        {
            Rows(s, cpu: true, gpu: true, vram: false, ram: true,
                    disk: true, io: false, uptime: false,
                    fps: false, low: false, frame: false, net: true, ping: false);

            s.ThemeName = "Apple Clean";

            // light, frosted, almost no colour - the numbers are the only thing with weight
            s.Accent = "#FF0A84FF";
            s.Accent2 = "#FF5AC8FA";
            s.BgTop = "#F2FFFFFF";
            s.BgBottom = "#F2F2F4F8";
            s.BorderColor = "#14000000";
            s.TextColor = "#FF1D1D1F";
            s.LabelColor = "#FF6E6E73";
            s.DetailColor = "#FF9A9AA0";
            s.TrackColor = "#1A000000";
            s.WarnColor = "#FFFF9F0A";
            s.DangerColor = "#FFFF3B30";
            s.AccentGradient = false;
            s.ColorizeByLoad = false;     // nothing changes colour on its own; it stays calm

            // the closest thing Windows has to San Francisco, with graceful fallbacks
            s.FontFamily = "Segoe UI Variable Display, Segoe UI Variable, Segoe UI";
            s.FontSize = 13;
            s.ValueSizeOffset = 1;
            s.LabelSizeOffset = -2;
            s.ValueWeight = "Medium";
            s.LabelWeight = "Normal";
            s.UpperCaseLabels = false;
            s.ShowUnits = true;
            s.Decimals = 0;
            s.TextShadow = false;

            s.IconStyle = IconStyle.Outline;
            s.IconSizeOffset = 2;
            s.GlowEffect = false;
            s.ShowBrandDot = false;       // no blinking dot, no branding noise

            s.ShowBars = true;
            s.BarStyle = BarStyle.Rounded;
            s.BarHeight = 3;

            s.CornerRadius = 18;          // the big soft radius of a macOS widget
            s.BorderThickness = 0;
            s.PaddingH = 16;
            s.PaddingV = 13;
            s.RowSpacing = 6;             // air between rows is the whole point
            s.BackgroundOpacity = 0.95;
            s.Blur = true;
            s.ShadowEnabled = true;
            s.ShadowStrength = 0.35;      // a soft lift, not a drop shadow

            s.Layout = WidgetLayout.Vertical;
            s.Reveal = RevealAnimation.FromTop;   // it drops in from above
            s.TrayIconMode = TrayIconMode.Cpu;
        }

        // ===================================================================== helper

        private static void Rows(AppSettings s, bool cpu, bool gpu, bool vram, bool ram,
                                 bool disk, bool io, bool uptime,
                                 bool fps, bool low, bool frame, bool net, bool ping)
        {
            s.ShowCpu = cpu;
            s.ShowGpu = gpu;
            s.ShowVram = vram;
            s.ShowRam = ram;
            s.ShowDisk = disk;
            s.ShowDiskIo = io;
            s.ShowUptime = uptime;
            s.ShowFps = fps;
            s.ShowFpsLow = low;
            s.ShowFrameTime = frame;
            s.ShowNet = net;
            s.ShowPing = ping;
        }
    }
}
