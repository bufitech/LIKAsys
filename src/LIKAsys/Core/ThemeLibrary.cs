using System;
using System.Linq;

namespace LIKAsys.Core
{
    /// <summary>
    /// A complete look: colours AND shape. A theme is not just a palette - it decides whether
    /// the widget has icons, bars, uppercase labels, how big the numbers are and whether the
    /// rows stack vertically or run across in a strip. That is what makes "Gaming" feel
    /// nothing like "Classic".
    /// </summary>
    public sealed class ThemePreset
    {
        public string Name;
        public string Group;

        // ---- colours
        public string Accent = "#00E5FF";
        public string Accent2 = "#7C4DFF";
        public string BgTop = "#151C2B";
        public string BgBottom = "#0A0D14";
        public string Border = "#4000E5FF";
        public string Text = "#EAF2FF";
        public string Label = "#93A6BE";
        public string Detail = "#5D6E85";
        public string Track = "#1B2433";
        public string Warn = "#FFB020";
        public string Danger = "#FF4D5E";

        // ---- card
        public double Opacity = 0.85;
        public double Radius = 14;
        public double BorderThickness = 1;
        public bool Blur = false;
        public bool Glow = true;
        public bool Shadow = true;
        public bool Gradient = true;
        public bool TextShadow = false;
        public double PadH = 13, PadV = 10;
        public double RowSpace = 3;

        // ---- structure
        public WidgetLayout Layout = WidgetLayout.Vertical;
        public IconStyle Icons = IconStyle.ThreeD;
        public double IconOff = 6;
        public bool Bars = true;
        public BarStyle BarStyle = BarStyle.Rounded;
        public double BarH = 3;

        // ---- type
        public string Font = null;              // null = keep whatever the user picked
        public bool Upper = false;
        public double ValueOff = 1.5;
        public double LabelOff = -2.5;
        public string ValueWeight = "Bold";
        public string LabelWeight = "Medium";

        /// <summary>Set only by presets that are designed for one specific spot on the screen.</summary>
        public WidgetPosition? Pos = null;
    }

    public static class ThemeLibrary
    {
        public const string Default = "Midnight Glass";

        // group keys, in the order the filter chips appear
        public const string GGaming = "Gaming";
        public const string GClassic = "Classic";
        public const string GGlass = "Qelq";
        public const string GMinimal = "Minimal";
        public const string GDev = "Dev";
        public const string GKosova = "Kosova";
        public const string GLight = "Dritë";
        public const string GBare = "Pa sfond";

        public static readonly string[] GroupOrder =
        { GBare, GGaming, GClassic, GGlass, GMinimal, GDev, GKosova, GLight };

        public static readonly ThemePreset[] All =
        {
            // ==========================================================================
            //  PA SFOND - no card at all, the numbers float straight on the desktop.
            //  Every glyph carries its own shadow so it stays readable on any wallpaper.
            // ==========================================================================
            new ThemePreset { Name="Overlay", Group=GBare, Accent="#FFFFFF", Accent2="#D7DEE8",
                BgTop="#000000", BgBottom="#000000", Border="#00000000", Opacity=0, Radius=0,
                BorderThickness=0, Shadow=false, Glow=false, Gradient=false, TextShadow=true,
                Text="#FFFFFF", Label="#D2DAE4", Detail="#A8B2BE", Track="#00000000",
                Warn="#FFC861", Danger="#FF7A7A",
                Icons=IconStyle.None, Bars=false, BarStyle=BarStyle.None,
                PadH=3, PadV=2, RowSpace=1.5, ValueOff=2.5, LabelOff=-2,
                ValueWeight="Bold", LabelWeight="Medium", Pos=WidgetPosition.TopLeft },

            new ThemePreset { Name="Overlay Neon", Group=GBare, Accent="#00E5FF", Accent2="#7C4DFF",
                BgTop="#000000", BgBottom="#000000", Border="#00000000", Opacity=0, Radius=0,
                BorderThickness=0, Shadow=false, Glow=false, Gradient=false, TextShadow=true,
                Text="#FFFFFF", Label="#9FD9EA", Detail="#7FB2C4", Track="#00000000",
                Warn="#FFC861", Danger="#FF6B7A",
                Icons=IconStyle.None, Bars=false, BarStyle=BarStyle.None,
                PadH=3, PadV=2, RowSpace=1.5, ValueOff=2.5, LabelOff=-2,
                ValueWeight="Bold", LabelWeight="Medium", Pos=WidgetPosition.TopLeft },

            new ThemePreset { Name="Overlay Amber", Group=GBare, Accent="#FFC247", Accent2="#FF9A3C",
                BgTop="#000000", BgBottom="#000000", Border="#00000000", Opacity=0, Radius=0,
                BorderThickness=0, Shadow=false, Glow=false, Gradient=false, TextShadow=true,
                Text="#FFE8B8", Label="#D8B377", Detail="#AE8C55", Track="#00000000",
                Warn="#FF9A3C", Danger="#FF6B5B",
                Icons=IconStyle.None, Bars=false, BarStyle=BarStyle.None,
                PadH=3, PadV=2, RowSpace=1.5, ValueOff=2.5, LabelOff=-2,
                ValueWeight="Bold", LabelWeight="Medium", Pos=WidgetPosition.TopLeft },

            new ThemePreset { Name="Overlay Strip", Group=GBare, Accent="#FFFFFF", Accent2="#D7DEE8",
                BgTop="#000000", BgBottom="#000000", Border="#00000000", Opacity=0, Radius=0,
                BorderThickness=0, Shadow=false, Glow=false, Gradient=false, TextShadow=true,
                Text="#FFFFFF", Label="#D2DAE4", Detail="#A8B2BE", Track="#00000000",
                Warn="#FFC861", Danger="#FF7A7A",
                Layout=WidgetLayout.Horizontal,
                Icons=IconStyle.None, Bars=false, BarStyle=BarStyle.None,
                PadH=3, PadV=2, RowSpace=0, ValueOff=2, LabelOff=-2,
                ValueWeight="Bold", LabelWeight="Medium", Pos=WidgetPosition.TopLeft },

            // ==========================================================================
            //  GAMING - loud, segmented bars, uppercase labels, oversized numbers
            // ==========================================================================
            new ThemePreset { Name="Apex", Group=GGaming, Accent="#00E5FF", Accent2="#2563EB",
                BgTop="#0B1524", BgBottom="#04070D", Border="#7000E5FF", Opacity=0.88, Radius=8,
                BorderThickness=1.4, Text="#EAF8FF", Label="#7FA6C4", Detail="#4E6E8A", Track="#122235",
                Upper=true, BarStyle=BarStyle.Segmented, BarH=5, ValueOff=4.5, LabelOff=-3.5,
                IconOff=8, RowSpace=4, PadH=14, PadV=11, ValueWeight="Black" },

            new ThemePreset { Name="Overdrive", Group=GGaming, Accent="#FF3B1F", Accent2="#FFA319",
                BgTop="#1F0A06", BgBottom="#0B0302", Border="#70FF3B1F", Opacity=0.89, Radius=6,
                BorderThickness=1.5, Text="#FFEDE6", Label="#C2887A", Detail="#8A5A4C", Track="#301009",
                Warn="#FFA319", Danger="#FF1744",
                Upper=true, BarStyle=BarStyle.Segmented, BarH=5.5, ValueOff=5, LabelOff=-3.5,
                IconOff=8, RowSpace=4, PadH=14, PadV=11, ValueWeight="Black" },

            new ThemePreset { Name="Venom", Group=GGaming, Accent="#39FF14", Accent2="#07A317",
                BgTop="#09180B", BgBottom="#020602", Border="#7039FF14", Opacity=0.9, Radius=5,
                BorderThickness=1.4, Text="#DFFFD6", Label="#6FB265", Detail="#487A42", Track="#0F2A12",
                Warn="#D4FF00", Danger="#FF2D2D", Icons=IconStyle.Solid,
                Upper=true, BarStyle=BarStyle.Segmented, BarH=5, ValueOff=4.5, LabelOff=-3.5,
                IconOff=8, RowSpace=4, PadH=14, PadV=11, ValueWeight="Black" },

            new ThemePreset { Name="Phantom", Group=GGaming, Accent="#B14AFF", Accent2="#FF3DCB",
                BgTop="#150B26", BgBottom="#06030E", Border="#70B14AFF", Opacity=0.87, Radius=10,
                BorderThickness=1.4, Text="#F4E8FF", Label="#A68BC4", Detail="#715C8A", Track="#221339",
                Upper=true, BarStyle=BarStyle.Segmented, BarH=5, ValueOff=4.5, LabelOff=-3.5,
                IconOff=8, RowSpace=4, PadH=14, PadV=11, ValueWeight="Black" },

            new ThemePreset { Name="Reactor", Group=GGaming, Accent="#FFC400", Accent2="#FF6B00",
                BgTop="#1C1403", BgBottom="#090600", Border="#70FFC400", Opacity=0.9, Radius=3,
                BorderThickness=1.6, Text="#FFF4D6", Label="#C0A855", Detail="#8A7536", Track="#2B2007",
                Warn="#FF6B00", Danger="#FF2D2D",
                Upper=true, BarStyle=BarStyle.Segmented, BarH=6, ValueOff=5, LabelOff=-3.5,
                IconOff=8, RowSpace=4.5, PadH=15, PadV=11, ValueWeight="Black" },

            new ThemePreset { Name="Cyberdeck", Group=GGaming, Accent="#FCEE0A", Accent2="#FF003C",
                BgTop="#1A0B2E", BgBottom="#0A0119", Border="#70FCEE0A", Opacity=0.88, Radius=4,
                BorderThickness=1.5, Text="#FBFFE3", Label="#A896CF", Detail="#6F5F96", Track="#281049",
                Warn="#FF9F1C", Danger="#FF003C",
                Upper=true, BarStyle=BarStyle.Segmented, BarH=5.5, ValueOff=5, LabelOff=-3.5,
                IconOff=8, RowSpace=4, PadH=14, PadV=11, ValueWeight="Black" },

            new ThemePreset { Name="Frostbite", Group=GGaming, Accent="#4FD6FF", Accent2="#7C83FF",
                BgTop="#08192B", BgBottom="#02070D", Border="#704FD6FF", Opacity=0.86, Radius=10,
                BorderThickness=1.3, Text="#E6F7FF", Label="#84ADC8", Detail="#55788F", Track="#0F2638",
                Upper=true, BarStyle=BarStyle.Segmented, BarH=5, ValueOff=4.5, LabelOff=-3.5,
                IconOff=8, RowSpace=4, PadH=14, PadV=11, ValueWeight="Black" },

            new ThemePreset { Name="Bloodline", Group=GGaming, Accent="#FF2740", Accent2="#FF7A8A",
                BgTop="#1E050B", BgBottom="#0A0103", Border="#70FF2740", Opacity=0.89, Radius=7,
                BorderThickness=1.4, Text="#FFE6EA", Label="#C07E88", Detail="#8A525B", Track="#2E0A12",
                Upper=true, BarStyle=BarStyle.Segmented, BarH=5, ValueOff=4.5, LabelOff=-3.5,
                IconOff=8, RowSpace=4, PadH=14, PadV=11, ValueWeight="Black" },

            // ==========================================================================
            //  CLASSIC - nothing but the words and the numbers
            // ==========================================================================
            new ThemePreset { Name="Classic", Group=GClassic, Accent="#FFFFFF", Accent2="#C9CDD4",
                BgTop="#0D0F12", BgBottom="#07080A", Border="#22FFFFFF", Opacity=0.92, Radius=7,
                Text="#FFFFFF", Label="#8A9099", Detail="#5A6069", Track="#191C21",
                Icons=IconStyle.None, Bars=false, BarStyle=BarStyle.None, Glow=false, Gradient=false,
                ValueOff=2, LabelOff=-2, RowSpace=2.5, PadH=13, PadV=9,
                ValueWeight="SemiBold", LabelWeight="Normal" },

            new ThemePreset { Name="Classic Light", Group=GClassic, Accent="#111827", Accent2="#4B5563",
                BgTop="#FFFFFF", BgBottom="#F1F4F8", Border="#2A0F172A", Opacity=0.94, Radius=7,
                Text="#0F172A", Label="#5A6675", Detail="#93A0B0", Track="#E2E8F0",
                Warn="#C2410C", Danger="#B91C1C",
                Icons=IconStyle.None, Bars=false, BarStyle=BarStyle.None, Glow=false, Shadow=true,
                Gradient=false, ValueOff=2, LabelOff=-2, RowSpace=2.5, PadH=13, PadV=9,
                ValueWeight="SemiBold", LabelWeight="Normal" },

            new ThemePreset { Name="Classic Mono", Group=GClassic, Accent="#D4D4D4", Accent2="#A3A3A3",
                BgTop="#101010", BgBottom="#070707", Border="#1FFFFFFF", Opacity=0.93, Radius=4,
                Text="#E8E8E8", Label="#8A8A8A", Detail="#5C5C5C", Track="#1C1C1C",
                Font="Consolas", Icons=IconStyle.None, Bars=false, BarStyle=BarStyle.None,
                Glow=false, Gradient=false, ValueOff=1.5, LabelOff=-1.5, RowSpace=2, PadH=13, PadV=9,
                ValueWeight="Bold", LabelWeight="Normal" },

            new ThemePreset { Name="Classic Amber", Group=GClassic, Accent="#FFB000", Accent2="#C98A00",
                BgTop="#120D03", BgBottom="#070500", Border="#33FFB000", Opacity=0.94, Radius=4,
                Text="#FFD479", Label="#A97F2E", Detail="#775820", Track="#241A06",
                Warn="#FF8A00", Danger="#FF4433",
                Font="Consolas", Icons=IconStyle.None, Bars=false, BarStyle=BarStyle.None,
                Glow=false, Gradient=false, ValueOff=1.5, LabelOff=-1.5, RowSpace=2, PadH=13, PadV=9,
                ValueWeight="Bold", LabelWeight="Normal" },

            new ThemePreset { Name="Classic Strip", Group=GClassic, Accent="#9AE6FF", Accent2="#67A9C9",
                BgTop="#0C1118", BgBottom="#070A0E", Border="#22FFFFFF", Opacity=0.92, Radius=7,
                Text="#E8F3FA", Label="#7E8C9A", Detail="#56626E", Track="#161D26",
                Layout=WidgetLayout.Horizontal,
                Icons=IconStyle.None, Bars=false, BarStyle=BarStyle.None, Glow=false, Gradient=false,
                ValueOff=2, LabelOff=-2, RowSpace=0, PadH=13, PadV=8,
                ValueWeight="SemiBold", LabelWeight="Normal" },

            new ThemePreset { Name="Classic Strip Dark", Group=GClassic, Accent="#E5E7EB", Accent2="#9CA3AF",
                BgTop="#000000", BgBottom="#000000", Border="#1AFFFFFF", Opacity=1.0, Radius=5,
                Text="#FFFFFF", Label="#86868B", Detail="#5A5A5F", Track="#17171A",
                Layout=WidgetLayout.Horizontal,
                Icons=IconStyle.None, Bars=false, BarStyle=BarStyle.None, Glow=false, Shadow=false,
                Gradient=false, ValueOff=1.5, LabelOff=-2, RowSpace=0, PadH=12, PadV=7,
                ValueWeight="SemiBold", LabelWeight="Normal" },

            // ==========================================================================
            //  QELQ / GLASS
            // ==========================================================================
            new ThemePreset { Name="Midnight Glass", Group=GGlass, Accent="#00E5FF", Accent2="#7C4DFF",
                BgTop="#151C2B", BgBottom="#0A0D14", Border="#4D00E5FF", Blur=true, Opacity=0.72 },

            new ThemePreset { Name="Clear", Group=GGlass, Accent="#9FE8FF", Accent2="#FFFFFF",
                BgTop="#1A2434", BgBottom="#0E141F", Border="#1FFFFFFF", Blur=true, Opacity=0.38,
                Radius=16, Glow=false, Icons=IconStyle.Outline },

            new ThemePreset { Name="Frost", Group=GGlass, Accent="#BFEAFF", Accent2="#7DD3FC",
                BgTop="#142433", BgBottom="#0A131C", Border="#55BFEAFF", Blur=true, Opacity=0.62,
                Label="#A7C0D6", Detail="#6B8299", Track="#1E2E3E" },

            new ThemePreset { Name="Aurora", Group=GGlass, Accent="#5EEAD4", Accent2="#A78BFA",
                BgTop="#10243A", BgBottom="#140E26", Border="#555EEAD4", Blur=true, Opacity=0.70,
                Label="#9FB8C8", Track="#1C2C3C" },

            new ThemePreset { Name="Smoke", Group=GGlass, Accent="#CBD5E1", Accent2="#94A3B8",
                BgTop="#1E222A", BgBottom="#101318", Border="#26FFFFFF", Blur=true, Opacity=0.58,
                Glow=false, Icons=IconStyle.Outline, Label="#9AA6B5", Track="#262C36" },

            // ==========================================================================
            //  MINIMAL
            // ==========================================================================
            new ThemePreset { Name="Obsidian", Group=GMinimal, Accent="#9CA3AF", Accent2="#6B7280",
                BgTop="#121214", BgBottom="#0A0A0C", Border="#1FFFFFFF", Opacity=0.94,
                Glow=false, Icons=IconStyle.Outline, Label="#8A8F99", Detail="#5A6068", Track="#1E1F23" },

            new ThemePreset { Name="Pure Black", Group=GMinimal, Accent="#FFFFFF", Accent2="#A3A3A3",
                BgTop="#000000", BgBottom="#000000", Border="#1AFFFFFF", Opacity=1.0, Radius=10,
                Glow=false, Shadow=false, Icons=IconStyle.Outline,
                Text="#FFFFFF", Label="#8E8E93", Detail="#5A5A5F", Track="#1C1C1E" },

            new ThemePreset { Name="Carbon", Group=GMinimal, Accent="#8B95A5", Accent2="#5B6472",
                BgTop="#17191E", BgBottom="#0E1013", Border="#1AFFFFFF", Opacity=0.92,
                Glow=false, Bars=false, BarStyle=BarStyle.None, Icons=IconStyle.Outline,
                Label="#7D8796", Track="#22252B" },

            new ThemePreset { Name="Mono", Group=GMinimal, Accent="#E5E7EB", Accent2="#9CA3AF",
                BgTop="#0C0C0E", BgBottom="#060607", Border="#14FFFFFF", Opacity=0.90, Radius=8,
                Glow=false, Bars=false, BarStyle=BarStyle.None, Icons=IconStyle.None,
                Label="#7A7F88", Detail="#4E535B" },

            new ThemePreset { Name="Slate", Group=GMinimal, Accent="#94A3B8", Accent2="#64748B",
                BgTop="#1E293B", BgBottom="#0F172A", Border="#2694A3B8", Opacity=0.9,
                Glow=false, Label="#94A3B8", Detail="#64748B", Track="#293548" },

            new ThemePreset { Name="Graphite", Group=GMinimal, Accent="#60A5FA", Accent2="#3B82F6",
                BgTop="#1C1F26", BgBottom="#121419", Border="#2660A5FA", Opacity=0.93, Glow=false,
                Label="#8C97A8", Detail="#5C6675", Track="#242830" },

            // ==========================================================================
            //  DEV - editor palettes
            // ==========================================================================
            new ThemePreset { Name="Nord", Group=GDev, Accent="#88C0D0", Accent2="#81A1C1",
                BgTop="#3B4252", BgBottom="#2E3440", Border="#4088C0D0", Opacity=0.93,
                Text="#ECEFF4", Label="#AEB7C8", Detail="#7A869B", Track="#434C5E", Warn="#EBCB8B", Danger="#BF616A" },

            new ThemePreset { Name="Dracula", Group=GDev, Accent="#BD93F9", Accent2="#FF79C6",
                BgTop="#343746", BgBottom="#282A36", Border="#40BD93F9", Opacity=0.93,
                Text="#F8F8F2", Label="#A9B1C9", Detail="#6272A4", Track="#44475A", Warn="#F1FA8C", Danger="#FF5555" },

            new ThemePreset { Name="Tokyo Night", Group=GDev, Accent="#7AA2F7", Accent2="#BB9AF7",
                BgTop="#24283B", BgBottom="#1A1B26", Border="#407AA2F7", Opacity=0.93,
                Text="#C0CAF5", Label="#9AA5CE", Detail="#565F89", Track="#2F334D", Warn="#E0AF68", Danger="#F7768E" },

            new ThemePreset { Name="Catppuccin", Group=GDev, Accent="#CBA6F7", Accent2="#89B4FA",
                BgTop="#2B2B40", BgBottom="#1E1E2E", Border="#40CBA6F7", Opacity=0.93,
                Text="#CDD6F4", Label="#A6ADC8", Detail="#6C7086", Track="#313244", Warn="#F9E2AF", Danger="#F38BA8" },

            new ThemePreset { Name="Gruvbox", Group=GDev, Accent="#FABD2F", Accent2="#FE8019",
                BgTop="#3C3836", BgBottom="#282828", Border="#40FABD2F", Opacity=0.94,
                Text="#EBDBB2", Label="#BDAE93", Detail="#928374", Track="#504945", Warn="#FE8019", Danger="#FB4934" },

            new ThemePreset { Name="Solarized", Group=GDev, Accent="#2AA198", Accent2="#B58900",
                BgTop="#073642", BgBottom="#002B36", Border="#402AA198", Opacity=0.94,
                Text="#EEE8D5", Label="#93A1A1", Detail="#657B83", Track="#0C4A58", Warn="#B58900", Danger="#DC322F" },

            new ThemePreset { Name="One Dark", Group=GDev, Accent="#61AFEF", Accent2="#C678DD",
                BgTop="#2C313A", BgBottom="#21252B", Border="#4061AFEF", Opacity=0.93,
                Text="#ABB2BF", Label="#8A94A6", Detail="#5C6370", Track="#353B45", Warn="#E5C07B", Danger="#E06C75" },

            new ThemePreset { Name="Monokai", Group=GDev, Accent="#A6E22E", Accent2="#F92672",
                BgTop="#3E3D32", BgBottom="#272822", Border="#40A6E22E", Opacity=0.94,
                Text="#F8F8F2", Label="#BCBCB0", Detail="#75715E", Track="#49483E", Warn="#E6DB74", Danger="#F92672" },

            new ThemePreset { Name="Everforest", Group=GDev, Accent="#A7C080", Accent2="#7FBBB3",
                BgTop="#374145", BgBottom="#2B3339", Border="#40A7C080", Opacity=0.94,
                Text="#D3C6AA", Label="#A6B0A0", Detail="#859289", Track="#404C51", Warn="#DBBC7F", Danger="#E67E80" },

            // ==========================================================================
            //  KOSOVA
            // ==========================================================================
            new ThemePreset { Name="Kosova", Group=GKosova, Accent="#D4A747", Accent2="#2449A4",
                BgTop="#17264A", BgBottom="#0B1226", Border="#66D4A747", Opacity=0.88,
                Text="#FFFFFF", Label="#AEBCDB", Detail="#73819E", Track="#1E2E58", Warn="#F0C040", Danger="#E04444" },

            new ThemePreset { Name="Dardania", Group=GKosova, Accent="#E63946", Accent2="#1D3557",
                BgTop="#1B2437", BgBottom="#0B0F1A", Border="#66E63946", Opacity=0.9,
                Text="#F1FAEE", Label="#A8B6C8", Detail="#6E7C90", Track="#232F45" },

            // ==========================================================================
            //  DRITË / LIGHT
            // ==========================================================================
            new ThemePreset { Name="Snow", Group=GLight, Accent="#0EA5E9", Accent2="#6366F1",
                BgTop="#FFFFFF", BgBottom="#EEF2F7", Border="#330F172A", Opacity=0.92, Glow=false,
                Icons=IconStyle.Outline, Text="#0F172A", Label="#475569", Detail="#94A3B8",
                Track="#DEE5EE", Warn="#D97706", Danger="#DC2626" },

            new ThemePreset { Name="Paper", Group=GLight, Accent="#B45309", Accent2="#92400E",
                BgTop="#FBF8F1", BgBottom="#EFE9DC", Border="#331C1917", Opacity=0.94, Glow=false,
                Icons=IconStyle.Outline, Text="#1C1917", Label="#57534E", Detail="#A8A29E",
                Track="#E3DBCB", Warn="#CA8A04", Danger="#B91C1C" },

            new ThemePreset { Name="Light Glass", Group=GLight, Accent="#2563EB", Accent2="#7C3AED",
                BgTop="#FFFFFF", BgBottom="#DDE6F2", Border="#330F172A", Blur=true, Opacity=0.55,
                Glow=false, Icons=IconStyle.Outline, Text="#0F172A", Label="#475569", Detail="#8494A8",
                Track="#CBD5E1", Warn="#D97706", Danger="#DC2626" },
        };

        public static ThemePreset Find(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return All.First(t => t.Name == Default);
            return All.FirstOrDefault(t => string.Equals(t.Name, name, StringComparison.OrdinalIgnoreCase))
                   ?? All.First(t => t.Name == Default);
        }

        public static string[] Groups =>
            GroupOrder.Where(g => All.Any(t => t.Group == g)).ToArray();

        /// <summary>One-line description of what the preset does to the shape, for the theme card.</summary>
        public static string Describe(ThemePreset t)
        {
            if (t == null) return "";
            if (t.Opacity <= 0.001 && t.BorderThickness <= 0)
                return Lang.IsEnglish ? "no background at all" : "fare pa sfond";
            if (t.Icons == IconStyle.None && !t.Bars)
                return Lang.IsEnglish ? "text + numbers" : "vetëm tekst dhe numra";
            if (t.BarStyle == BarStyle.Segmented)
                return Lang.IsEnglish ? "segmented bars" : "shirita të segmentuar";
            if (t.Blur)
                return Lang.IsEnglish ? "frosted glass" : "qelq i turbullt";
            if (!t.Bars)
                return Lang.IsEnglish ? "no bars" : "pa shirita";
            return Lang.IsEnglish ? "bars + icons" : "shirita dhe ikona";
        }

        /// <summary>Copies a preset into the live settings - colours and shape together.</summary>
        public static void Apply(ThemePreset t, AppSettings s)
        {
            if (t == null || s == null) return;
            s.BeginBatch();
            try
            {
                s.ThemeName = t.Name;

                s.Accent = t.Accent;
                s.Accent2 = t.Accent2;
                s.BgTop = t.BgTop;
                s.BgBottom = t.BgBottom;
                s.BorderColor = t.Border;
                s.TextColor = t.Text;
                s.LabelColor = t.Label;
                s.DetailColor = t.Detail;
                s.TrackColor = t.Track;
                s.WarnColor = t.Warn;
                s.DangerColor = t.Danger;

                s.BackgroundOpacity = t.Opacity;
                s.CornerRadius = t.Radius;
                s.BorderThickness = t.BorderThickness;
                s.Blur = t.Blur;
                s.GlowEffect = t.Glow;
                s.ShadowEnabled = t.Shadow;
                s.AccentGradient = t.Gradient;
                s.TextShadow = t.TextShadow;
                s.PaddingH = t.PadH;
                s.PaddingV = t.PadV;
                s.RowSpacing = t.RowSpace;

                s.Layout = t.Layout;
                s.IconStyle = t.Icons;
                s.IconSizeOffset = t.IconOff;
                s.ShowBars = t.Bars;
                s.BarStyle = t.BarStyle;
                s.BarHeight = t.BarH;

                if (!string.IsNullOrWhiteSpace(t.Font)) s.FontFamily = t.Font;
                s.UpperCaseLabels = t.Upper;
                s.ValueSizeOffset = t.ValueOff;
                s.LabelSizeOffset = t.LabelOff;
                s.ValueWeight = t.ValueWeight;
                s.LabelWeight = t.LabelWeight;

                if (t.Pos.HasValue) s.Position = t.Pos.Value;
            }
            finally { s.EndBatch(); }
        }
    }
}
