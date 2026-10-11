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
        public GlassMode Glass = GlassMode.Soft;
        public bool Rule = false;
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

        // ---- optional, null means "do not touch what the user already has"
        public bool? Colorize = null;           // numbers turning orange/red with load
        public bool? BrandDot = null;           // the little pulsing dot in the header
        public double ShadowAmt = -1;           // 0..1, how heavy the drop shadow sits

        /// <summary>Set only by presets that are designed for one specific spot on the screen.</summary>
        public WidgetPosition? Pos = null;

        /// <summary>
        /// The card behaves like an iPhone Dynamic Island: a small pill that rolls through
        /// the readings by itself and opens when the pointer arrives. One preset uses this.
        /// </summary>
        public bool Island = false;

        /// <summary>
        /// A match strip instead of a card: one flat angular line pinned to the top edge,
        /// small labels, big numbers, nothing else. Built for shooters like Counter-Strike 2.
        /// </summary>
        public bool MatchBar = false;

        /// <summary>
        /// Every reading gets its own pill with its own colour and its own glow, the way
        /// an RGB build looks. Changes the shape of the card, not just its colours.
        /// </summary>
        public bool Capsule = false;

        /// <summary>
        /// Name of the colour set used when Capsule is on, so two capsule themes do not
        /// end up looking like the same widget. Empty means the plain accent for all rows.
        /// </summary>
        public string Palette = "";

        /// <summary>A small moving graph of the last readings, drawn at the right of each row.</summary>
        public bool Spark = false;

        /// <summary>Short Albanian line shown on the theme card. Goes through Lang.T.</summary>
        public string Note = null;
    }

    public static class ThemeLibrary
    {
        public const string Default = "Midnight Glass";

        // group keys, in the order the filter chips appear
        public const string GLoja = "Lojëra";
        public const string GPune = "Punë";
        public const string GMinimal = "Minimal";
        public const string GClassic = "Klasike";
        public const string GLight = "Dritë";
        public const string GKosova = "Kosova";

        /// <summary>
        /// Every reading in its own coloured pill. These do not look like the other
        /// groups at all, which is the reason the group exists.
        /// </summary>
        public const string GCapsule = "Kapsula";

        /// <summary>
        /// Frosted panels. The wallpaper stays visible through the card, blurred, with
        /// a bright hairline on the top edge. Nothing here uses an accent ring.
        /// </summary>
        public const string GGlass = "Qelq";

        public static readonly string[] GroupOrder =
        { GGlass, GCapsule, GLoja, GPune, GMinimal, GClassic, GLight, GKosova };

        public static readonly ThemePreset[] All =
        {
            // ==========================================================================
            //  LOJËRA - the strip first, then the worlds people know, then the loud ones.
            // ==========================================================================
            // ==========================================================================
            //  LOJËRA - each one borrows the type, the icons and the palette of a world
            //  people already know. They are meant to look nothing like each other.
            // ==========================================================================
            // the match strip. First in the group because it is the one made for CS2.
            new ThemePreset { Name="Match Bar", Group=GLoja, Note="shirit i hollë në krye, për CS2",
                MatchBar=true,
                Accent="#DE9B35", Accent2="#F0A040",
                BgTop="#0B0E13", BgBottom="#06080B", Border="#00000000", Opacity=0.97, Radius=0,
                BorderThickness=0, Text="#FFFFFF", Label="#6E7886", Detail="#5C6674", Track="#39414D",
                Warn="#E0A23C", Danger="#E0523C", Glow=false, TextShadow=false, Gradient=false,
                Shadow=true, ShadowAmt=0.7,
                Font="Bahnschrift Condensed, Bahnschrift, Segoe UI", Icons=IconStyle.None,
                Bars=false, BarStyle=BarStyle.None, Upper=true,
                PadH=0, PadV=0, RowSpace=0, ValueOff=0, LabelOff=-4,
                ValueWeight="Bold", LabelWeight="Bold",
                BrandDot=false, Pos=WidgetPosition.TopCenter },

            new ThemePreset { Name="Night City", Group=GLoja, Note="verdhë neoni, qoshe të prera",
                Accent="#FCEE0A", Accent2="#00F0FF",
                BgTop="#101214", BgBottom="#030405", Border="#A0FCEE0A", Opacity=0.9, Radius=0,
                BorderThickness=1.8, Text="#FBFFE0", Label="#8FA3A8", Detail="#5D6E72", Track="#1D2124",
                Warn="#FF9F1C", Danger="#FF003C", Glow=true, TextShadow=true, Gradient=true,
                Font="Bahnschrift, DIN, Segoe UI", Icons=IconStyle.Solid, IconOff=7,
                BarStyle=BarStyle.Segmented, BarH=6, Upper=true,
                ValueOff=5, LabelOff=-3, RowSpace=4, PadH=15, PadV=11,
                ValueWeight="Black", LabelWeight="Bold" },

            new ThemePreset { Name="Dust", Group=GLoja, Note="rërë dhe blu, rreshta të ngjeshur",
                Accent="#E8A33C", Accent2="#4B9CD3",
                BgTop="#1A1C20", BgBottom="#0D0F12", Border="#66E8A33C", Opacity=0.94, Radius=2,
                BorderThickness=1.2, Text="#EDE7DB", Label="#A49C8E", Detail="#726B5F", Track="#262A30",
                Warn="#E0B050", Danger="#D4452F", Glow=false, Gradient=false, ShadowAmt=0.55,
                Font="Bahnschrift SemiCondensed, Bahnschrift, Segoe UI",
                Icons=IconStyle.Hairline, IconOff=2, BarStyle=BarStyle.Stripes, BarH=3, Upper=true,
                ValueOff=2.5, LabelOff=-2.5, RowSpace=2.5, PadH=12, PadV=9,
                ValueWeight="Bold", LabelWeight="SemiBold" },

            new ThemePreset { Name="Raid", Group=GLoja, Note="ushtarake, shkronja makine shkrimi",
                Accent="#C8A85A", Accent2="#6E7A4F",
                BgTop="#1B1D17", BgBottom="#0C0D09", Border="#45C8A85A", Opacity=0.95, Radius=1,
                BorderThickness=1.2, Text="#DED9C5", Label="#8E8A74", Detail="#62604E", Track="#2A2C20",
                Warn="#D8A531", Danger="#C0392B", Glow=false, Gradient=false, ShadowAmt=0.6,
                Font="Consolas, Lucida Console, Courier New",
                Icons=IconStyle.Outline, IconOff=1, BarStyle=BarStyle.Stripes, BarH=3, Upper=true,
                ValueOff=1.5, LabelOff=-2.5, RowSpace=2, PadH=11, PadV=8,
                ValueWeight="SemiBold", LabelWeight="Medium" },

            new ThemePreset { Name="Los Santos", Group=GLoja, Note="jeshile dhe perëndim dielli",
                Accent="#59B847", Accent2="#F2A33C",
                BgTop="#121719", BgBottom="#06090A", Border="#5559B847", Opacity=0.88, Radius=9,
                BorderThickness=1.2, Text="#F4F7F2", Label="#9BB09A", Detail="#6A7F6A", Track="#1A2320",
                Warn="#F2A33C", Danger="#E0466E", Glow=false, Gradient=true, ShadowAmt=0.7,
                Font="Franklin Gothic Medium, Franklin Gothic, Segoe UI",
                Icons=IconStyle.ThreeD, IconOff=6, BarStyle=BarStyle.Rounded, BarH=4, Upper=true,
                ValueOff=4, LabelOff=-3, RowSpace=4, PadH=14, PadV=11,
                ValueWeight="Bold", LabelWeight="SemiBold" },

            new ThemePreset { Name="Vice", Group=GLoja, Note="rozë synthwave, shkëlqim i butë",
                Accent="#FF2E9A", Accent2="#00E0FF",
                BgTop="#231046", BgBottom="#0B0418", Border="#90FF2E9A", Opacity=0.87, Radius=12,
                BorderThickness=1.4, Text="#FFEAF7", Label="#B49AD6", Detail="#7E6AA0", Track="#2E1555",
                Warn="#FFC247", Danger="#FF3B5C", Glow=true, TextShadow=true, Gradient=true,
                Font="Bahnschrift Light, Bahnschrift, Segoe UI",
                Icons=IconStyle.Outline, IconOff=5, BarStyle=BarStyle.Rounded, BarH=5, Upper=true,
                ValueOff=4.5, LabelOff=-3, RowSpace=4.5, PadH=15, PadV=12,
                ValueWeight="Black", LabelWeight="Medium" },

            new ThemePreset { Name="Overworld", Group=GLoja, Note="blloqe dhe ngjyra pikseli",
                Accent="#5FBF4A", Accent2="#8C6239",
                BgTop="#303030", BgBottom="#1B1B1B", Border="#705FBF4A", Opacity=0.92, Radius=0,
                BorderThickness=2, Text="#ECECEC", Label="#A8A8A8", Detail="#7A7A7A", Track="#3E3E3E",
                Warn="#E0B33A", Danger="#C7452F", Glow=false, Gradient=false, TextShadow=true,
                Font="Lucida Console, Consolas, Courier New",
                Icons=IconStyle.Solid, IconOff=4, BarStyle=BarStyle.Dots, BarH=6, Upper=true,
                ValueOff=3, LabelOff=-3, RowSpace=3.5, PadH=13, PadV=10,
                ValueWeight="Bold", LabelWeight="Bold" },

            new ThemePreset { Name="Ashen", Group=GLoja, Note="ar i vjetër, shkronja me serif",
                Accent="#C9A227", Accent2="#8A6F1E",
                BgTop="#17140F", BgBottom="#0A0908", Border="#55C9A227", Opacity=0.91, Radius=4,
                BorderThickness=1.2, Text="#EFE6CF", Label="#A3977A", Detail="#726A55", Track="#241F17",
                Warn="#D9A441", Danger="#A8321F", Glow=true, Gradient=false, ShadowAmt=0.8,
                Font="Sitka Banner, Sitka Display, Georgia, Segoe UI",
                Icons=IconStyle.Hairline, IconOff=3, BarStyle=BarStyle.Rounded, BarH=3, Upper=false,
                ValueOff=2.5, LabelOff=-2, RowSpace=5, PadH=15, PadV=12,
                ValueWeight="SemiBold", LabelWeight="Normal" },

            new ThemePreset { Name="Corpo", Group=GLoja, Note="e kuqe korporate, pa shkëlqim",
                Accent="#FF2B4E", Accent2="#7A0E20",
                BgTop="#0C0C0E", BgBottom="#000000", Border="#8CFF2B4E", Opacity=0.93, Radius=0,
                BorderThickness=2, Text="#F2F3F5", Label="#8E9298", Detail="#5C6066", Track="#1A1B1E",
                Warn="#FF7A1A", Danger="#FF2B4E", Glow=false, Gradient=false, ShadowAmt=0.75,
                Font="Bahnschrift SemiBold Condensed, Bahnschrift Condensed, Bahnschrift, Segoe UI",
                Icons=IconStyle.Outline, IconOff=4, BarStyle=BarStyle.Square, BarH=4, Upper=true,
                ValueOff=4, LabelOff=-3, RowSpace=3.5, PadH=14, PadV=10,
                ValueWeight="Bold", LabelWeight="SemiBold" },

            // ==========================================================================
            //  GAMING - loud, segmented bars, uppercase labels, oversized numbers
            // ==========================================================================
            new ThemePreset { Name="Apex", Group=GLoja, Accent="#00E5FF", Accent2="#2563EB",
                BgTop="#0B1524", BgBottom="#04070D", Border="#7000E5FF", Opacity=0.88, Radius=8,
                BorderThickness=1.4, Text="#EAF8FF", Label="#7FA6C4", Detail="#4E6E8A", Track="#122235",
                Upper=true, BarStyle=BarStyle.Segmented, BarH=5, ValueOff=4.5, LabelOff=-3.5,
                IconOff=8, RowSpace=4, PadH=14, PadV=11, ValueWeight="Black" },

            new ThemePreset { Name="Overdrive", Group=GLoja, Accent="#FF3B1F", Accent2="#FFA319",
                BgTop="#1F0A06", BgBottom="#0B0302", Border="#70FF3B1F", Opacity=0.89, Radius=6,
                BorderThickness=1.5, Text="#FFEDE6", Label="#C2887A", Detail="#8A5A4C", Track="#301009",
                Warn="#FFA319", Danger="#FF1744",
                Upper=true, BarStyle=BarStyle.Stripes, BarH=5.5, ValueOff=5, LabelOff=-3.5,
                IconOff=8, RowSpace=4, PadH=14, PadV=11, ValueWeight="Black" },

            new ThemePreset { Name="Reactor", Group=GLoja, Accent="#FFC400", Accent2="#FF6B00",
                BgTop="#1C1403", BgBottom="#090600", Border="#70FFC400", Opacity=0.9, Radius=3,
                BorderThickness=1.6, Text="#FFF4D6", Label="#C0A855", Detail="#8A7536", Track="#2B2007",
                Warn="#FF6B00", Danger="#FF2D2D",
                Upper=true, BarStyle=BarStyle.Stripes, BarH=6, ValueOff=5, LabelOff=-3.5,
                IconOff=8, RowSpace=4.5, PadH=15, PadV=11, ValueWeight="Black" },

            new ThemePreset { Name="Venom", Group=GLoja, Accent="#39FF14", Accent2="#07A317",
                BgTop="#09180B", BgBottom="#020602", Border="#7039FF14", Opacity=0.9, Radius=5,
                BorderThickness=1.4, Text="#DFFFD6", Label="#6FB265", Detail="#487A42", Track="#0F2A12",
                Warn="#D4FF00", Danger="#FF2D2D", Icons=IconStyle.Solid,
                Upper=true, BarStyle=BarStyle.Segmented, BarH=5, ValueOff=4.5, LabelOff=-3.5,
                IconOff=8, RowSpace=4, PadH=14, PadV=11, ValueWeight="Black" },

            new ThemePreset { Name="Phantom", Group=GLoja, Accent="#B14AFF", Accent2="#FF3DCB",
                BgTop="#150B26", BgBottom="#06030E", Border="#70B14AFF", Opacity=0.87, Radius=10,
                BorderThickness=1.4, Text="#F4E8FF", Label="#A68BC4", Detail="#715C8A", Track="#221339",
                Upper=true, BarStyle=BarStyle.Segmented, BarH=5, ValueOff=4.5, LabelOff=-3.5,
                IconOff=8, RowSpace=4, PadH=14, PadV=11, ValueWeight="Black" },

            new ThemePreset { Name="Cyberdeck", Group=GLoja, Accent="#FCEE0A", Accent2="#FF003C",
                BgTop="#1A0B2E", BgBottom="#0A0119", Border="#70FCEE0A", Opacity=0.88, Radius=4,
                BorderThickness=1.5, Text="#FBFFE3", Label="#A896CF", Detail="#6F5F96", Track="#281049",
                Warn="#FF9F1C", Danger="#FF003C",
                Upper=true, BarStyle=BarStyle.Segmented, BarH=5.5, ValueOff=5, LabelOff=-3.5,
                IconOff=8, RowSpace=4, PadH=14, PadV=11, ValueWeight="Black" },

            new ThemePreset { Name="Frostbite", Group=GLoja, Accent="#4FD6FF", Accent2="#7C83FF",
                BgTop="#08192B", BgBottom="#02070D", Border="#704FD6FF", Opacity=0.86, Radius=10,
                BorderThickness=1.3, Text="#E6F7FF", Label="#84ADC8", Detail="#55788F", Track="#0F2638",
                Upper=true, BarStyle=BarStyle.Segmented, BarH=5, ValueOff=4.5, LabelOff=-3.5,
                IconOff=8, RowSpace=4, PadH=14, PadV=11, ValueWeight="Black" },

            new ThemePreset { Name="Bloodline", Group=GLoja, Accent="#FF2740", Accent2="#FF7A8A",
                BgTop="#1E050B", BgBottom="#0A0103", Border="#70FF2740", Opacity=0.89, Radius=7,
                BorderThickness=1.4, Text="#FFE6EA", Label="#C07E88", Detail="#8A525B", Track="#2E0A12",
                Upper=true, BarStyle=BarStyle.Segmented, BarH=5, ValueOff=4.5, LabelOff=-3.5,
                IconOff=8, RowSpace=4, PadH=14, PadV=11, ValueWeight="Black" },


            // ==========================================================================
            //  PUNË - a desk, a rack, an editor. Terminal first because the IT profile uses it.
            // ==========================================================================
            // ==========================================================================
            //  DEV - editor palettes
            // ==========================================================================
            new ThemePreset { Name="Terminal", Group=GPune, Accent="#3DDC97", Accent2="#37B6FF",
                BgTop="#080C10", BgBottom="#060A0D", Border="#263DDC97", Opacity=0.95, Radius=6,
                Glow=false, Gradient=false, Text="#DFF5E9", Label="#7E9A8E", Detail="#4F6B60",
                Track="#16FFFFFF", Warn="#E3B341", Danger="#F85149",
                Icons=IconStyle.Outline, IconOff=1, BarStyle=BarStyle.Square, BarH=3,
                PadH=11, PadV=8, RowSpace=2, Font="Cascadia Mono, Consolas, Segoe UI",
                Upper=true, ValueOff=0, LabelOff=-2, ValueWeight="SemiBold", LabelWeight="Medium",
                ShadowAmt=0.6 },

            // ==========================================================================
            //  IT - the other half of the split. Eight work looks, and none of them
            //  is another palette on the same card: the type, the icons, the bars and
            //  the corners all move.
            // ==========================================================================
            new ThemePreset { Name="Server Room", Group=GPune, Note="LED-at e rackut mbi çelik të ftohtë",
                Accent="#F5A524", Accent2="#38BDF8",
                BgTop="#121820", BgBottom="#080B0F", Border="#55F5A524", Opacity=0.94, Radius=2,
                BorderThickness=1.2, Text="#DCE6F0", Label="#8296AC", Detail="#5A6E82", Track="#1C2530",
                Warn="#F5A524", Danger="#F43F5E", Glow=false, Gradient=false, ShadowAmt=0.6,
                Font="Consolas, Lucida Console, Courier New",
                Icons=IconStyle.Outline, IconOff=2, BarStyle=BarStyle.Stripes, BarH=5, Upper=true,
                ValueOff=2, LabelOff=-2.5, RowSpace=3, PadH=12, PadV=9,
                ValueWeight="Bold", LabelWeight="SemiBold" },

            new ThemePreset { Name="Blueprint", Group=GPune, Note="vizatim teknik, vetëm vija të holla",
                Accent="#7DD3FC", Accent2="#BAE6FD",
                BgTop="#0B2545", BgBottom="#061A33", Border="#667DD3FC", Opacity=0.93, Radius=0,
                BorderThickness=1.2, Text="#E8F4FF", Label="#8FB6D9", Detail="#5F87AC", Track="#143158",
                Warn="#FBBF24", Danger="#FB7185", Glow=false, Gradient=false,
                Font="Corbel, Candara, Segoe UI",
                Icons=IconStyle.Hairline, IconOff=2, BarStyle=BarStyle.Stripes, BarH=2, Upper=true,
                ValueOff=2, LabelOff=-2.5, RowSpace=4, PadH=14, PadV=11,
                ValueWeight="SemiBold", LabelWeight="Normal" },

            new ThemePreset { Name="Dynamic Island", Group=GPune, Island=true,
                Note="pilulë e zezë që rrotullon matjet dhe hapet kur i afrohesh",
                Accent="#30D158", Accent2="#0A84FF",
                BgTop="#060607", BgBottom="#000000", Border="#00000000", Opacity=1, Radius=26,
                BorderThickness=0, Text="#FFFFFF", Label="#98989D", Detail="#636366",
                Track="#1F1F22", Warn="#FF9F0A", Danger="#FF453A",
                Glow=false, Gradient=false, Shadow=true, ShadowAmt=0.8,
                Font="Segoe UI Variable Display, Segoe UI",
                Icons=IconStyle.Hairline, IconOff=1, BarStyle=BarStyle.Rounded, BarH=3,
                Upper=false, ValueOff=1.5, LabelOff=-2, RowSpace=5, PadH=17, PadV=11,
                ValueWeight="SemiBold", LabelWeight="Medium",
                Pos=WidgetPosition.TopCenter, BrandDot=true },

            new ThemePreset { Name="Phosphor", Group=GPune, Note="ekran i vjetër me fosfor qelibar",
                Accent="#FFB000", Accent2="#FF7A00",
                BgTop="#0C0A06", BgBottom="#040302", Border="#55FFB000", Opacity=0.95, Radius=0,
                BorderThickness=1.2, Glow=true, Gradient=false, TextShadow=true,
                Text="#FFCC66", Label="#C79036", Detail="#8A6424", Track="#241A08",
                Warn="#FFB000", Danger="#FF4D2D",
                Font="Lucida Console, Consolas, Courier New",
                Icons=IconStyle.Solid, IconOff=3, BarStyle=BarStyle.Dots, BarH=4, Upper=true,
                ValueOff=2.5, LabelOff=-2.5, RowSpace=3, PadH=12, PadV=9,
                ValueWeight="Bold", LabelWeight="Bold" },

            new ThemePreset { Name="Night Shift", Group=GPune, Note="vjollcë e butë, për natën vonë",
                Accent="#818CF8", Accent2="#38BDF8",
                BgTop="#1E1B33", BgBottom="#120F22", Border="#40818CF8", Opacity=0.9, Radius=10,
                BorderThickness=1, Glow=false, Gradient=true, Shadow=true, ShadowAmt=0.5,
                Text="#E9E7FB", Label="#9A96C4", Detail="#6D6894", Track="#2A2648",
                Warn="#FBBF24", Danger="#FB7185",
                Font="Candara, Calibri, Segoe UI",
                Icons=IconStyle.ThreeD, IconOff=5, BarStyle=BarStyle.Dots, BarH=3, Upper=false,
                ValueOff=2, LabelOff=-2, RowSpace=5, PadH=14, PadV=11,
                ValueWeight="SemiBold", LabelWeight="Normal" },

            new ThemePreset { Name="Nord", Group=GPune, Accent="#88C0D0", Accent2="#81A1C1",
                BgTop="#3B4252", BgBottom="#2E3440", Border="#4088C0D0", Opacity=0.93,
                Text="#ECEFF4", Label="#AEB7C8", Detail="#7A869B", Track="#434C5E", Warn="#EBCB8B", Danger="#BF616A" },

            new ThemePreset { Name="Dracula", Group=GPune, Accent="#BD93F9", Accent2="#FF79C6",
                BgTop="#343746", BgBottom="#282A36", Border="#40BD93F9", Opacity=0.93,
                Text="#F8F8F2", Label="#A9B1C9", Detail="#6272A4", Track="#44475A", Warn="#F1FA8C", Danger="#FF5555" },

            new ThemePreset { Name="Tokyo Night", Group=GPune, Accent="#7AA2F7", Accent2="#BB9AF7",
                BgTop="#24283B", BgBottom="#1A1B26", Border="#407AA2F7", Opacity=0.93,
                Text="#C0CAF5", Label="#9AA5CE", Detail="#565F89", Track="#2F334D", Warn="#E0AF68", Danger="#F7768E" },

            new ThemePreset { Name="One Dark", Group=GPune, Accent="#61AFEF", Accent2="#C678DD",
                BgTop="#2C313A", BgBottom="#21252B", Border="#4061AFEF", Opacity=0.93,
                Text="#ABB2BF", Label="#8A94A6", Detail="#5C6370", Track="#353B45", Warn="#E5C07B", Danger="#E06C75" },

            new ThemePreset { Name="Catppuccin", Group=GPune, Accent="#CBA6F7", Accent2="#89B4FA",
                BgTop="#2B2B40", BgBottom="#1E1E2E", Border="#40CBA6F7", Opacity=0.93,
                Text="#CDD6F4", Label="#A6ADC8", Detail="#6C7086", Track="#313244", Warn="#F9E2AF", Danger="#F38BA8" },

            new ThemePreset { Name="Gruvbox", Group=GPune, Accent="#FABD2F", Accent2="#FE8019",
                BgTop="#3C3836", BgBottom="#282828", Border="#40FABD2F", Opacity=0.94,
                Text="#EBDBB2", Label="#BDAE93", Detail="#928374", Track="#504945", Warn="#FE8019", Danger="#FB4934" },

            new ThemePreset { Name="Monokai", Group=GPune, Accent="#A6E22E", Accent2="#F92672",
                BgTop="#3E3D32", BgBottom="#272822", Border="#40A6E22E", Opacity=0.94,
                Text="#F8F8F2", Label="#BCBCB0", Detail="#75715E", Track="#49483E", Warn="#E6DB74", Danger="#F92672" },

            new ThemePreset { Name="Everforest", Group=GPune, Accent="#A7C080", Accent2="#7FBBB3",
                BgTop="#374145", BgBottom="#2B3339", Border="#40A7C080", Opacity=0.94,
                Text="#D3C6AA", Label="#A6B0A0", Detail="#859289", Track="#404C51", Warn="#DBBC7F", Danger="#E67E80" },

            new ThemePreset { Name="Solarized", Group=GPune, Accent="#2AA198", Accent2="#B58900",
                BgTop="#073642", BgBottom="#002B36", Border="#402AA198", Opacity=0.94,
                Text="#EEE8D5", Label="#93A1A1", Detail="#657B83", Track="#0C4A58", Warn="#B58900", Danger="#DC322F" },

            new ThemePreset { Name="Ledger", Group=GPune, Note="fletë e bardhë zyre, jeshile tabele",
                Accent="#217346", Accent2="#2B579A",
                BgTop="#FFFFFF", BgBottom="#F3F4F6", Border="#1A000000", Opacity=0.96, Radius=3,
                BorderThickness=1, Glow=false, Gradient=false, Shadow=true, ShadowAmt=0.3,
                Text="#1F2328", Label="#5B6470", Detail="#8A929D", Track="#1A000000",
                Warn="#B45309", Danger="#B91C1C", Colorize=false, BrandDot=false,
                Font="Calibri, Carlito, Segoe UI",
                Icons=IconStyle.Outline, IconOff=2, BarStyle=BarStyle.Square, BarH=3, Upper=false,
                ValueOff=1.5, LabelOff=-2, RowSpace=5, PadH=14, PadV=11,
                ValueWeight="Bold", LabelWeight="Normal" },

            new ThemePreset { Name="Helpdesk", Group=GPune, Note="e lehtë dhe e qetë, për tavolinë pune",
                Accent="#2563EB", Accent2="#06B6D4",
                BgTop="#F8FAFC", BgBottom="#E9EFF7", Border="#14000000", Opacity=0.95, Radius=14,
                BorderThickness=0, Blur=true, Glow=false, Gradient=true, Shadow=true, ShadowAmt=0.3,
                Text="#0F172A", Label="#64748B", Detail="#94A3B8", Track="#16000000",
                Warn="#D97706", Danger="#DC2626", Colorize=false,
                Font="Trebuchet MS, Tahoma, Segoe UI",
                Icons=IconStyle.ThreeD, IconOff=5, BarStyle=BarStyle.Dots, BarH=4, Upper=false,
                ValueOff=2, LabelOff=-2, RowSpace=6, PadH=15, PadV=12,
                ValueWeight="Bold", LabelWeight="Medium" },

            new ThemePreset { Name="Memo", Group=GPune, Note="letër e shtypur, bojë e zezë, vijë e kuqe",
                Accent="#B91C1C", Accent2="#78716C",
                BgTop="#FAF7F0", BgBottom="#F0EBE0", Border="#22000000", Opacity=0.96, Radius=1,
                BorderThickness=1, Glow=false, Gradient=false, Shadow=true, ShadowAmt=0.28,
                Text="#1A1A1A", Label="#57534E", Detail="#8C837A", Track="#1F000000",
                Warn="#A16207", Danger="#B91C1C", Colorize=false, BrandDot=false,
                Font="Georgia, Constantia, Cambria",
                Icons=IconStyle.Hairline, IconOff=2, BarStyle=BarStyle.Dots, BarH=2, Upper=false,
                ValueOff=1.5, LabelOff=-2, RowSpace=5, PadH=15, PadV=12,
                ValueWeight="SemiBold", LabelWeight="Normal" },

            new ThemePreset { Name="E-Ink", Group=GPune, Note="vetëm bardhë e zi, si lexuesi i librave",
                Accent="#111111", Accent2="#57534E",
                BgTop="#F5F5F4", BgBottom="#E7E5E4", Border="#33000000", Opacity=1, Radius=0,
                BorderThickness=1.6, Glow=false, Gradient=false, Shadow=false,
                Text="#111111", Label="#44403C", Detail="#78716C", Track="#26000000",
                Warn="#44403C", Danger="#111111", Colorize=false, BrandDot=false,
                Font="Verdana, Tahoma, Segoe UI",
                Icons=IconStyle.Outline, IconOff=2, BarStyle=BarStyle.Dots, BarH=3, Upper=false,
                ValueOff=1, LabelOff=-2, RowSpace=5, PadH=13, PadV=10,
                ValueWeight="Bold", LabelWeight="Normal" },


            // ==========================================================================
            //  MINIMAL - quiet looks: flat, glass, or no card at all.
            // ==========================================================================
            // ==========================================================================
            //  QELQ / GLASS
            // ==========================================================================
            new ThemePreset { Name="Midnight Glass", Group=GMinimal, Accent="#00E5FF", Accent2="#7C4DFF",
                BgTop="#151C2B", BgBottom="#0A0D14", Border="#4D00E5FF", Blur=true, Opacity=0.72 },

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

            new ThemePreset { Name="Slate", Group=GMinimal, Accent="#94A3B8", Accent2="#64748B",
                BgTop="#1E293B", BgBottom="#0F172A", Border="#2694A3B8", Opacity=0.9,
                Glow=false, Label="#94A3B8", Detail="#64748B", Track="#293548" },

            new ThemePreset { Name="Graphite", Group=GMinimal, Accent="#60A5FA", Accent2="#3B82F6",
                BgTop="#1C1F26", BgBottom="#121419", Border="#2660A5FA", Opacity=0.93, Glow=false,
                Label="#8C97A8", Detail="#5C6675", Track="#242830" },

            new ThemePreset { Name="Mono", Group=GMinimal, Accent="#E5E7EB", Accent2="#9CA3AF",
                BgTop="#0C0C0E", BgBottom="#060607", Border="#14FFFFFF", Opacity=0.90, Radius=8,
                Glow=false, Bars=false, BarStyle=BarStyle.None, Icons=IconStyle.None,
                Label="#7A7F88", Detail="#4E535B" },

            new ThemePreset { Name="Clear", Group=GMinimal, Accent="#9FE8FF", Accent2="#FFFFFF",
                BgTop="#1A2434", BgBottom="#0E141F", Border="#1FFFFFFF", Blur=true, Opacity=0.38,
                Radius=16, Glow=false, Icons=IconStyle.Outline },

            new ThemePreset { Name="Frost", Group=GMinimal, Accent="#BFEAFF", Accent2="#7DD3FC",
                BgTop="#142433", BgBottom="#0A131C", Border="#55BFEAFF", Blur=true, Opacity=0.62,
                Label="#A7C0D6", Detail="#6B8299", Track="#1E2E3E" },

            new ThemePreset { Name="Smoke", Group=GMinimal, Accent="#CBD5E1", Accent2="#94A3B8",
                BgTop="#1E222A", BgBottom="#101318", Border="#26FFFFFF", Blur=true, Opacity=0.58,
                Glow=false, Icons=IconStyle.Outline, Label="#9AA6B5", Track="#262C36" },

            new ThemePreset { Name="Aurora", Group=GMinimal, Accent="#5EEAD4", Accent2="#A78BFA",
                BgTop="#10243A", BgBottom="#140E26", Border="#555EEAD4", Blur=true, Opacity=0.70,
                Label="#9FB8C8", Track="#1C2C3C" },

            // ==========================================================================
            //  PA SFOND - no card at all, the numbers float straight on the desktop.
            //  Every glyph carries its own shadow so it stays readable on any wallpaper.
            // ==========================================================================
            new ThemePreset { Name="Overlay", Group=GMinimal, Accent="#FFFFFF", Accent2="#D7DEE8",
                BgTop="#000000", BgBottom="#000000", Border="#00000000", Opacity=0, Radius=0,
                BorderThickness=0, Shadow=false, Glow=false, Gradient=false, TextShadow=true,
                Text="#FFFFFF", Label="#D2DAE4", Detail="#A8B2BE", Track="#00000000",
                Warn="#FFC861", Danger="#FF7A7A",
                Icons=IconStyle.None, Bars=false, BarStyle=BarStyle.None,
                PadH=3, PadV=2, RowSpace=1.5, ValueOff=2.5, LabelOff=-2,
                ValueWeight="Bold", LabelWeight="Medium", Pos=WidgetPosition.TopLeft },

            new ThemePreset { Name="Overlay Neon", Group=GMinimal, Accent="#00E5FF", Accent2="#7C4DFF",
                BgTop="#000000", BgBottom="#000000", Border="#00000000", Opacity=0, Radius=0,
                BorderThickness=0, Shadow=false, Glow=false, Gradient=false, TextShadow=true,
                Text="#FFFFFF", Label="#9FD9EA", Detail="#7FB2C4", Track="#00000000",
                Warn="#FFC861", Danger="#FF6B7A",
                Icons=IconStyle.None, Bars=false, BarStyle=BarStyle.None,
                PadH=3, PadV=2, RowSpace=1.5, ValueOff=2.5, LabelOff=-2,
                ValueWeight="Bold", LabelWeight="Medium", Pos=WidgetPosition.TopLeft },

            new ThemePreset { Name="Overlay Amber", Group=GMinimal, Accent="#FFC247", Accent2="#FF9A3C",
                BgTop="#000000", BgBottom="#000000", Border="#00000000", Opacity=0, Radius=0,
                BorderThickness=0, Shadow=false, Glow=false, Gradient=false, TextShadow=true,
                Text="#FFE8B8", Label="#D8B377", Detail="#AE8C55", Track="#00000000",
                Warn="#FF9A3C", Danger="#FF6B5B",
                Icons=IconStyle.None, Bars=false, BarStyle=BarStyle.None,
                PadH=3, PadV=2, RowSpace=1.5, ValueOff=2.5, LabelOff=-2,
                ValueWeight="Bold", LabelWeight="Medium", Pos=WidgetPosition.TopLeft },

            new ThemePreset { Name="Overlay Strip", Group=GMinimal, Accent="#FFFFFF", Accent2="#D7DEE8",
                BgTop="#000000", BgBottom="#000000", Border="#00000000", Opacity=0, Radius=0,
                BorderThickness=0, Shadow=false, Glow=false, Gradient=false, TextShadow=true,
                Text="#FFFFFF", Label="#D2DAE4", Detail="#A8B2BE", Track="#00000000",
                Warn="#FFC861", Danger="#FF7A7A",
                Layout=WidgetLayout.Horizontal,
                Icons=IconStyle.None, Bars=false, BarStyle=BarStyle.None,
                PadH=3, PadV=2, RowSpace=0, ValueOff=2, LabelOff=-2,
                ValueWeight="Bold", LabelWeight="Medium", Pos=WidgetPosition.TopLeft },


            // ==========================================================================
            //  KLASIKE - the first LIKAsys look and its variants.
            // ==========================================================================
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
            //  DRITË - for a bright desktop.
            // ==========================================================================
            // The quiet one. White frosted glass, no border at all, no glow, hairline icons
            // and sentence-case labels - an IT widget that behaves like a macOS one.
            new ThemePreset { Name="Apple Clean", Group=GLight, Accent="#0A84FF", Accent2="#5AC8FA",
                BgTop="#FFFFFF", BgBottom="#F2F4F8", Border="#14000000", Opacity=0.95, Radius=18,
                BorderThickness=0, Blur=true, Glow=false, Gradient=false, Shadow=true, ShadowAmt=0.35,
                Text="#1D1D1F", Label="#6E6E73", Detail="#9A9AA0", Track="#14000000",
                Warn="#FF9F0A", Danger="#FF3B30", Colorize=false, BrandDot=false,
                Icons=IconStyle.Hairline, IconOff=2, BarStyle=BarStyle.Rounded, BarH=3,
                PadH=16, PadV=13, RowSpace=6,
                Font="Segoe UI Variable Display, Segoe UI Variable, Segoe UI",
                Upper=false, ValueOff=1, LabelOff=-2, ValueWeight="Medium", LabelWeight="Normal" },

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


            // ==========================================================================
            //  KOSOVA.
            // ==========================================================================
            // ==========================================================================
            //  KOSOVA
            // ==========================================================================
            new ThemePreset { Name="Kosova", Group=GKosova, Accent="#D4A747", Accent2="#2449A4",
                BgTop="#17264A", BgBottom="#0B1226", Border="#66D4A747", Opacity=0.88,
                Text="#FFFFFF", Label="#AEBCDB", Detail="#73819E", Track="#1E2E58", Warn="#F0C040", Danger="#E04444" },

            // ==========================================================================
            //  KAPSULA - one pill per reading, one colour per reading. Nothing in the
            //  other groups is built like this, so they never blend together.
            // ==========================================================================
            // ==========================================================================
            //  QELQ - frosted panels. The blur is the background, the hairlines are the
            //  frame, and the type is quiet on purpose.
            // ==========================================================================
            new ThemePreset { Name="Apple Glass", Group=GGlass, Note="qelq i ngrirë, i errët",
                Blur=true, Glass=GlassMode.Apple, Rule=true,
                Accent="#0A84FF", Accent2="#5AC8FA",
                BgTop="#1C1C1E", BgBottom="#1C1C1E", Border="#3CFFFFFF",
                Opacity=0.62, Radius=20, BorderThickness=1, Glow=false, Shadow=false, Gradient=false,
                Text="#F5F5F7", Label="#A1A1A6", Detail="#86868B", Track="#26FFFFFF",
                Warn="#FF9F0A", Danger="#FF453A", Colorize=false, BrandDot=false,
                Icons=IconStyle.Hairline, IconOff=2, BarStyle=BarStyle.Rounded, BarH=3,
                PadH=16, PadV=13, RowSpace=7, Font="Segoe UI Variable Display, Segoe UI Variable, Segoe UI",
                Upper=false, ValueOff=1.5, LabelOff=-2, ValueWeight="Medium", LabelWeight="Normal" },

            new ThemePreset { Name="Apple Glass Light", Group=GGlass, Note="i njëjti qelq, por i ndritshëm",
                Blur=true, Glass=GlassMode.Apple, Rule=true,
                Accent="#007AFF", Accent2="#34C759",
                BgTop="#FFFFFF", BgBottom="#F2F2F7", Border="#26000000",
                Opacity=0.6, Radius=20, BorderThickness=1, Glow=false, Shadow=false, Gradient=false,
                Text="#1D1D1F", Label="#6E6E73", Detail="#8E8E93", Track="#18000000",
                Warn="#FF9500", Danger="#FF3B30", Colorize=false, BrandDot=false,
                Icons=IconStyle.Hairline, IconOff=2, BarStyle=BarStyle.Rounded, BarH=3,
                PadH=16, PadV=13, RowSpace=7, Font="Segoe UI Variable Display, Segoe UI Variable, Segoe UI",
                Upper=false, ValueOff=1.5, LabelOff=-2, ValueWeight="Medium", LabelWeight="Normal" },

            new ThemePreset { Name="Graphite Glass", Group=GGlass, Note="qelq pa asnjë ngjyrë",
                Blur=true, Glass=GlassMode.Apple, Rule=true,
                Accent="#98989D", Accent2="#C7C7CC",
                BgTop="#2C2C2E", BgBottom="#2C2C2E", Border="#34FFFFFF",
                Opacity=0.66, Radius=18, BorderThickness=1, Glow=false, Shadow=false, Gradient=false,
                Text="#F2F2F7", Label="#AEAEB2", Detail="#8E8E93", Track="#22FFFFFF",
                Warn="#D1D1D6", Danger="#FF453A", Colorize=false, BrandDot=false,
                Icons=IconStyle.Hairline, IconOff=1, BarStyle=BarStyle.Rounded, BarH=2.5,
                PadH=15, PadV=12, RowSpace=7, Font="Segoe UI Variable Display, Segoe UI Variable, Segoe UI",
                Upper=false, ValueOff=1, LabelOff=-2, ValueWeight="Medium", LabelWeight="Normal" },

            new ThemePreset { Name="Glass Pills", Group=GGlass, Note="qelq me kapsula pastel",
                Blur=true, Glass=GlassMode.Apple, Rule=false,
                Capsule=true, Palette="pastel", Spark=true,
                Accent="#64D2FF", Accent2="#BF5AF2",
                BgTop="#1C1C1E", BgBottom="#1C1C1E", Border="#32FFFFFF",
                Opacity=0.55, Radius=24, BorderThickness=1, Glow=false, Shadow=false, Gradient=false,
                Text="#FFFFFF", Label="#C7C7CC", Detail="#98989D", Track="#22FFFFFF",
                Warn="#FFD60A", Danger="#FF453A", Colorize=false, BrandDot=false,
                Icons=IconStyle.Hairline, IconOff=3, BarStyle=BarStyle.Rounded, BarH=3,
                PadH=12, PadV=11, RowSpace=5, Font="Segoe UI Variable Display, Segoe UI Variable, Segoe UI",
                Upper=false, ValueOff=2, LabelOff=-2.5, ValueWeight="Medium", LabelWeight="Normal" },

            new ThemePreset { Name="Clear Glass", Group=GGlass, Note="sa më pak qelq, veç numrat",
                Blur=true, Glass=GlassMode.Apple, Rule=false,
                Accent="#FFFFFF", Accent2="#D1D1D6",
                BgTop="#101014", BgBottom="#101014", Border="#2AFFFFFF",
                Opacity=0.34, Radius=22, BorderThickness=1, Glow=false, Shadow=false, Gradient=false,
                Text="#FFFFFF", Label="#D8D8DC", Detail="#AEAEB2", Track="#1EFFFFFF",
                Warn="#FFD60A", Danger="#FF453A", Colorize=false, BrandDot=false, TextShadow=true,
                Icons=IconStyle.Hairline, IconOff=1, Bars=false, BarStyle=BarStyle.None,
                PadH=15, PadV=12, RowSpace=6, Font="Segoe UI Variable Display, Segoe UI Variable, Segoe UI",
                Upper=false, ValueOff=2, LabelOff=-2, ValueWeight="Medium", LabelWeight="Normal" },

            new ThemePreset { Name="Neon Pills", Group=GCapsule, Note="secila matje me ngjyrën e vet",
                Capsule=true, Palette="neon", Spark=true,
                Accent="#2E9BFF", Accent2="#B14BFF",
                BgTop="#00000000", BgBottom="#00000000", Border="#00000000",
                Opacity=0.0, Radius=0, BorderThickness=0, Glow=true, Shadow=false, Gradient=false,
                Text="#FFFFFF", Label="#93A6BE", Detail="#7E8FA6", Track="#1A2230",
                Warn="#FFB020", Danger="#FF4D5E",
                Font="Bahnschrift, Segoe UI", Icons=IconStyle.Outline, IconOff=6,
                BarStyle=BarStyle.Rounded, BarH=3, Upper=true,
                ValueOff=4, LabelOff=-3, RowSpace=7, PadH=0, PadV=0,
                ValueWeight="Bold", LabelWeight="Bold", BrandDot=false },

            new ThemePreset { Name="Neon Pills Dark", Group=GCapsule, Note="kapsula mbi kartelë të zezë",
                Capsule=true, Palette="neon", Spark=true,
                Accent="#2E9BFF", Accent2="#FF49C3",
                BgTop="#0C1018", BgBottom="#05070B", Border="#22FFFFFF",
                Opacity=0.92, Radius=18, BorderThickness=1, Glow=true, Shadow=true, Gradient=false,
                Text="#FFFFFF", Label="#93A6BE", Detail="#7E8FA6", Track="#161D29",
                Font="Bahnschrift, Segoe UI", Icons=IconStyle.Outline, IconOff=6,
                BarStyle=BarStyle.Rounded, BarH=3, Upper=true,
                ValueOff=4, LabelOff=-3, RowSpace=6, PadH=11, PadV=10,
                ValueWeight="Bold", LabelWeight="Bold" },

            new ThemePreset { Name="Ice Pills", Group=GCapsule, Note="vetëm të ftohta, pa ylber",
                Capsule=true, Palette="ice", Spark=true,
                Accent="#5AC8FF", Accent2="#7E9BFF",
                BgTop="#00000000", BgBottom="#00000000", Border="#00000000",
                Opacity=0.0, Radius=0, BorderThickness=0, Glow=true, Shadow=false, Gradient=false,
                Text="#EAF6FF", Label="#9FBDD6", Detail="#7E99B4", Track="#17222E",
                Font="Segoe UI Variable, Segoe UI", Icons=IconStyle.Hairline, IconOff=5,
                BarStyle=BarStyle.Rounded, BarH=3, Upper=false,
                ValueOff=3.5, LabelOff=-3, RowSpace=7, PadH=0, PadV=0,
                ValueWeight="SemiBold", LabelWeight="Medium", BrandDot=false },

            new ThemePreset { Name="Fire Pills", Group=GCapsule, Note="bakër dhe ambër, pa gjelbër",
                Capsule=true, Palette="fire", Spark=true,
                Accent="#FF8A3D", Accent2="#FFC247",
                BgTop="#00000000", BgBottom="#00000000", Border="#00000000",
                Opacity=0.0, Radius=0, BorderThickness=0, Glow=true, Shadow=false, Gradient=false,
                Text="#FFF3E6", Label="#C9A88E", Detail="#A8886E", Track="#241A12",
                Font="Bahnschrift, Segoe UI", Icons=IconStyle.Solid, IconOff=6,
                BarStyle=BarStyle.Stripes, BarH=5, Upper=true,
                ValueOff=4, LabelOff=-3, RowSpace=7, PadH=0, PadV=0,
                ValueWeight="Black", LabelWeight="Bold", BrandDot=false },

            new ThemePreset { Name="Pastel Pills", Group=GCapsule, Note="ngjyra të buta, pa shkëlqim",
                Capsule=true, Palette="pastel", Spark=false,
                Accent="#7FB6F2", Accent2="#C0A4F0",
                BgTop="#00000000", BgBottom="#00000000", Border="#00000000",
                Opacity=0.0, Radius=0, BorderThickness=0, Glow=false, Shadow=false, Gradient=false,
                Text="#F2F6FB", Label="#A9BACD", Detail="#8C9DB2", Track="#1C2330",
                Font="Segoe UI Variable, Segoe UI", Icons=IconStyle.Hairline, IconOff=5,
                BarStyle=BarStyle.Dots, BarH=3, Upper=false,
                ValueOff=3, LabelOff=-3, RowSpace=6, PadH=0, PadV=0,
                ValueWeight="SemiBold", LabelWeight="Normal", BrandDot=false },

            new ThemePreset { Name="Mono Pills", Group=GCapsule, Note="një ngjyrë, pa zhurmë",
                Capsule=true, Palette="mono", Spark=false,
                Accent="#E8F1FF", Accent2="#A9C0E0",
                BgTop="#00000000", BgBottom="#00000000", Border="#00000000",
                Opacity=0.0, Radius=0, BorderThickness=0, Glow=false, Shadow=false, Gradient=false,
                Text="#FFFFFF", Label="#AFC1D6", Detail="#8DA0B6", Track="#1B2330",
                Font="Cascadia Mono, Consolas", Icons=IconStyle.Hairline, IconOff=4,
                Bars=false, BarStyle=BarStyle.None, Upper=true,
                ValueOff=2.5, LabelOff=-3, RowSpace=6, PadH=0, PadV=0,
                ValueWeight="SemiBold", LabelWeight="Medium", BrandDot=false },

            new ThemePreset { Name="Pills Compact", Group=GCapsule, Note="kapsula pa shirita, vetëm numra",
                Capsule=true, Palette="neon", Spark=false,
                Accent="#2BE07A", Accent2="#2E9BFF",
                BgTop="#00000000", BgBottom="#00000000", Border="#00000000",
                Opacity=0.0, Radius=0, BorderThickness=0, Glow=true, Shadow=false, Gradient=false,
                Text="#FFFFFF", Label="#93A6BE", Detail="#7E8FA6", Track="#1A2230",
                Font="Bahnschrift Condensed, Bahnschrift, Segoe UI", Icons=IconStyle.Outline, IconOff=3,
                Layout=WidgetLayout.Compact, Bars=false, BarStyle=BarStyle.None, Upper=true,
                ValueOff=3, LabelOff=-3.5, RowSpace=5, PadH=0, PadV=0,
                ValueWeight="Bold", LabelWeight="Bold", BrandDot=false },

            new ThemePreset { Name="Pills Bar", Group=GCapsule, Note="kapsula në një rresht, lart në ekran",
                Capsule=true, Palette="neon", Spark=false, Pos=WidgetPosition.TopCenter,
                Accent="#19E8FF", Accent2="#B14BFF",
                BgTop="#00000000", BgBottom="#00000000", Border="#00000000",
                Opacity=0.0, Radius=0, BorderThickness=0, Glow=true, Shadow=false, Gradient=false,
                Text="#FFFFFF", Label="#93A6BE", Detail="#7E8FA6", Track="#1A2230",
                Font="Bahnschrift, Segoe UI", Icons=IconStyle.Outline, IconOff=4,
                Layout=WidgetLayout.Horizontal, Bars=false, BarStyle=BarStyle.None, Upper=true,
                ValueOff=3, LabelOff=-3.5, RowSpace=5, PadH=0, PadV=0,
                ValueWeight="Bold", LabelWeight="Bold", BrandDot=false },

            new ThemePreset { Name="Kosova Pills", Group=GCapsule, Note="blu dhe ar, kapsula",
                Capsule=true, Palette="kosova", Spark=true,
                Accent="#2456A6", Accent2="#D4A017",
                BgTop="#0B1220", BgBottom="#05080F", Border="#2A2456A6",
                Opacity=0.9, Radius=16, BorderThickness=1, Glow=true, Shadow=true, Gradient=false,
                Text="#F4F8FF", Label="#9FB2CC", Detail="#7E91AC", Track="#162032",
                Font="Segoe UI Variable, Segoe UI", Icons=IconStyle.Outline, IconOff=6,
                BarStyle=BarStyle.Rounded, BarH=3, Upper=true,
                ValueOff=4, LabelOff=-3, RowSpace=6, PadH=11, PadV=10,
                ValueWeight="Bold", LabelWeight="Bold" },

            new ThemePreset { Name="Pills Glass", Group=GCapsule, Note="kapsula mbi xham të turbullt",
                Capsule=true, Palette="ice", Spark=true, Blur=true,
                Accent="#49E0E8", Accent2="#7E9BFF",
                BgTop="#141C2A", BgBottom="#0A0F18", Border="#24FFFFFF",
                Opacity=0.55, Radius=20, BorderThickness=1, Glow=true, Shadow=true, Gradient=false,
                Text="#F2F8FF", Label="#A6BCD2", Detail="#8599B0", Track="#1A2432",
                Font="Segoe UI Variable, Segoe UI", Icons=IconStyle.Outline, IconOff=6,
                BarStyle=BarStyle.Rounded, BarH=3, Upper=false,
                ValueOff=3.5, LabelOff=-3, RowSpace=6, PadH=12, PadV=11,
                ValueWeight="SemiBold", LabelWeight="Medium" },

            new ThemePreset { Name="Dardania", Group=GKosova, Accent="#E63946", Accent2="#1D3557",
                BgTop="#1B2437", BgBottom="#0B0F1A", Border="#66E63946", Opacity=0.9,
                Text="#F1FAEE", Label="#A8B6C8", Detail="#6E7C90", Track="#232F45" },

        };

        public static ThemePreset Find(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return All.First(t => t.Name == Default);
            return All.FirstOrDefault(t => string.Equals(t.Name, name, StringComparison.OrdinalIgnoreCase))
                   ?? All.First(t => t.Name == Default);
        }

        public static string[] Groups =>
            GroupOrder.Where(g => All.Any(t => t.Group == g)).ToArray();

        /// <summary>
        /// True when the preset draws its own shape and the three layouts do nothing.
        /// The Dynamic Island is a capsule and the Match Bar is a strip: both ignore rows.
        /// </summary>
        public static bool FixedShape(ThemePreset t) => t != null && (t.Island || t.MatchBar);

        /// <summary>One-line description of what the preset does to the shape, for the theme card.</summary>
        public static string Describe(ThemePreset t)
        {
            if (t == null) return "";
            if (!string.IsNullOrWhiteSpace(t.Note)) return Lang.T(t.Note);
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
                s.Glass = t.Glass;
                s.RowRule = t.Rule;
                s.GlowEffect = t.Glow;
                s.ShadowEnabled = t.Shadow;
                s.AccentGradient = t.Gradient;
                s.TextShadow = t.TextShadow;
                s.PaddingH = t.PadH;
                s.PaddingV = t.PadV;
                s.RowSpacing = t.RowSpace;

                s.Capsule = t.Capsule;
                s.Palette = t.Palette ?? "";
                s.RowSpark = t.Spark;

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

                if (t.Colorize.HasValue) s.ColorizeByLoad = t.Colorize.Value;
                if (t.BrandDot.HasValue) s.ShowBrandDot = t.BrandDot.Value;
                if (t.ShadowAmt >= 0) s.ShadowStrength = t.ShadowAmt;

                s.Island = t.Island;
                s.MatchBar = t.MatchBar;

                // the strip is a top-edge object and it must never eat a click mid-round
                if (t.MatchBar)
                {
                    s.MarginX = 0;
                    s.MarginY = 0;
                    s.ClickThrough = true;
                    s.ShowFps = true;
                    s.ShowFpsLow = true;
                    s.ShowCpu = true;
                    s.ShowGpu = true;
                    s.ShowPing = true;
                }

                // the capsule belongs against the top edge of the screen, the way the
                // iPhone one sits in the bezel, so it takes the margins down with it
                if (t.Island) { s.MarginX = 0; s.MarginY = 0; }

                if (t.Pos.HasValue) s.Position = t.Pos.Value;
            }
            finally { s.EndBatch(); }
        }
    }
}
