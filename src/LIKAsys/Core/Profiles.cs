using System;

namespace LIKAsys.Core
{
    /// <summary>Who the widget is for. Picked at install time, changeable any time after.</summary>
    public enum UiProfile
    {
        Gaming = 0,
        It = 1
    }

    /// <summary>
    /// The two personalities of LIKAsys.
    ///
    /// Gaming keeps the eye on frames: FPS, the 1% lows, frame time, how hard the card is
    /// pushed. Big numbers, neon cyan, the 3D icon set.
    ///
    /// IT keeps the eye on the machine: storage, throughput, latency, how long it has been
    /// up. Tighter rows, a calmer green, the flat technical icon set.
    ///
    /// Applying a profile only touches what the profile is about - the user's position,
    /// language, font family and startup choices are deliberately left alone, so switching
    /// profiles never throws away personal setup.
    /// </summary>
    public static class Profiles
    {
        public static string Name(UiProfile p) => p == UiProfile.It ? "IT" : "Gaming";

        public static void Apply(AppSettings s, UiProfile p)
        {
            if (s == null) return;

            s.BeginBatch();
            try
            {
                s.Profile = p;
                s.ProfileChosen = true;

                if (p == UiProfile.It)
                {
                    // --- what is measured
                    s.ShowCpu = true;
                    s.ShowGpu = true;
                    s.ShowVram = false;
                    s.ShowRam = true;
                    s.ShowDisk = true;
                    s.ShowDiskIo = true;
                    s.ShowNet = true;
                    s.ShowPing = true;
                    s.ShowUptime = true;
                    s.ShowFps = false;
                    s.ShowFpsLow = false;
                    s.ShowFrameTime = false;

                    // --- how it looks
                    s.Accent = "#FF3DDC97";
                    s.IconStyle = IconStyle.Outline;
                    s.GlowEffect = false;
                    s.BarStyle = BarStyle.Square;
                    s.BarHeight = 3;
                    s.FontSize = 12;
                    s.ValueSizeOffset = 1;
                    s.LabelSizeOffset = -1;
                    s.RowSpacing = 3;
                    s.ShowUnits = true;
                    s.UpperCaseLabels = true;
                    s.TrayIconMode = TrayIconMode.Cpu;
                }
                else
                {
                    s.ShowCpu = true;
                    s.ShowGpu = true;
                    s.ShowVram = true;
                    s.ShowRam = true;
                    s.ShowDisk = false;
                    s.ShowDiskIo = false;
                    s.ShowNet = false;
                    s.ShowPing = false;
                    s.ShowUptime = false;
                    s.ShowFps = true;
                    s.ShowFpsLow = true;
                    s.ShowFrameTime = false;

                    s.Accent = "#FF00E5FF";
                    s.IconStyle = IconStyle.ThreeD;
                    s.GlowEffect = true;
                    s.BarStyle = BarStyle.Rounded;
                    s.BarHeight = 4;
                    s.FontSize = 13;
                    s.ValueSizeOffset = 2;
                    s.LabelSizeOffset = -1;
                    s.RowSpacing = 4;
                    s.ShowUnits = true;
                    s.UpperCaseLabels = true;
                    s.TrayIconMode = TrayIconMode.Fps;
                }
            }
            finally { s.EndBatch(); }
        }
    }
}
