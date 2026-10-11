using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace LIKAsys.Core
{
    /// <summary>
    /// Tiny runtime localiser. The source language of every string in the XAML is Albanian;
    /// <see cref="T"/> maps it to English when the user switches. Nothing is compiled per
    /// language and no satellite assembly is produced, so the app stays a single exe.
    /// </summary>
    public static class Lang
    {
        public const string Sq = "sq";
        public const string En = "en";

        public static string Code { get; private set; } = Sq;
        public static bool IsEnglish => Code == En;

        public static event EventHandler Changed;

        public static void Set(string code)
        {
            var c = string.Equals(code, En, StringComparison.OrdinalIgnoreCase) ? En : Sq;
            if (c == Code) return;
            Code = c;
            Changed?.Invoke(null, EventArgs.Empty);
        }

        /// <summary>Albanian in, current language out.</summary>
        public static string T(string sq)
        {
            if (!IsEnglish || string.IsNullOrEmpty(sq)) return sq;
            try { return Map.TryGetValue(sq, out var v) ? v : sq; }
            catch { return sq; }
        }

        // ================================================================= tree walker

        /// <summary>
        /// Elements whose text the code rewrites at runtime (version numbers, sensor status,
        /// the active theme name...). The walker must never restore a stale value over them.
        /// </summary>
        private static readonly HashSet<string> Skip = new HashSet<string>(StringComparer.Ordinal)
        {
            "ThemeBadge", "VersionLine", "AboutVersion", "UpdateStatus",
            "FpsStatus", "SensorStatus", "HeadLine", "SubLine", "NotesText", "ProgressText"
        };

        private static readonly System.Runtime.CompilerServices.ConditionalWeakTable<DependencyObject, Dictionary<string, string>>
            Originals = new System.Runtime.CompilerServices.ConditionalWeakTable<DependencyObject, Dictionary<string, string>>();

        /// <summary>
        /// Re-renders every literal string under <paramref name="root"/> in the current language.
        /// The first Albanian value seen is remembered per element, so switching back and forth
        /// is loss-free. Data-bound and runtime-generated text is never touched.
        /// </summary>
        public static void Localize(DependencyObject root)
        {
            if (root == null) return;
            try { Translate(root); } catch { }

            foreach (var child in LogicalTreeHelper.GetChildren(root))
                if (child is DependencyObject d) Localize(d);
        }

        private static void Translate(DependencyObject o)
        {
            if (o is FrameworkElement named && !string.IsNullOrEmpty(named.Name) && Skip.Contains(named.Name))
                return;

            if (o is Window w && !IsBound(w, Window.TitleProperty))
                Swap(o, "Title", w.Title, v => w.Title = v);

            if (o is TextBlock tb && !IsBound(tb, TextBlock.TextProperty))
                Swap(o, "Text", tb.Text, v => tb.Text = v);

            if (o is ContentControl cc && cc.Content is string cs && !IsBound(cc, ContentControl.ContentProperty))
                Swap(o, "Content", cs, v => cc.Content = v);

            if (o is HeaderedContentControl hc && hc.Header is string hs &&
                !IsBound(hc, HeaderedContentControl.HeaderProperty))
                Swap(o, "Header", hs, v => hc.Header = v);

            if (o is FrameworkElement fe && fe.ToolTip is string ts &&
                !IsBound(fe, FrameworkElement.ToolTipProperty))
                Swap(o, "ToolTip", ts, v => fe.ToolTip = v);
        }

        private static void Swap(DependencyObject o, string key, string current, Action<string> set)
        {
            if (string.IsNullOrEmpty(current)) return;

            var map = Originals.GetOrCreateValue(o);
            if (!map.TryGetValue(key, out var original))
            {
                // Only strings that actually live in the dictionary are ever captured. Anything
                // the code produced at runtime is left exactly as it is.
                if (!Map.ContainsKey(current)) return;
                original = current;
                map[key] = original;
            }

            var want = T(original);
            if (!string.Equals(want, current, StringComparison.Ordinal)) set(want);
        }

        private static bool IsBound(DependencyObject o, DependencyProperty p)
        {
            try { return BindingOperations.GetBindingExpressionBase(o, p) != null; }
            catch { return false; }
        }

        // ================================================================= dictionary

        /// <summary>
        /// A dictionary whose Add() overwrites instead of throwing. The map below is written
        /// by hand and grows with every release; a single repeated Albanian key used to throw
        /// inside the static constructor, which killed the app before it drew anything.
        /// Never let a typo in a translation table be fatal.
        /// </summary>
        private sealed class SafeMap : Dictionary<string, string>
        {
            public SafeMap(IEqualityComparer<string> cmp) : base(cmp) { }
            public new void Add(string sq, string en) { this[sq] = en; }
        }

        private static readonly Dictionary<string, string> Map =
            new SafeMap(StringComparer.Ordinal)
        {
            // ---------------------------------------------------- window chrome / tabs
            { "LIKAsys - Cilësimet", "LIKAsys - Settings" },
            { "Cilësimet", "Settings" },
            { "Cilësimet...", "Settings..." },
            { "Mbyll", "Close" },
            { "Temat", "Themes" },
            { "Ngjyrat", "Colours" },
            { "Pamja e kartelës", "Card look" },
            { "Fonti dhe teksti", "Font and text" },
            { "Pozicioni", "Position" },
            { "Metrikat", "Metrics" },
            { "Sistemi dhe update", "System and updates" },
            { "Gjuha", "Language" },
            { "Rreth", "About" },
            { "Rikthe cilesimet", "Reset settings" },
            { "Çdo ndryshim ruhet vetvetiu dhe shfaqet menjëherë në widget.",
              "Every change is saved automatically and shows up in the widget right away." },

            // ---------------------------------------------------- language panel
            { "GJUHA E PROGRAMIT", "APPLICATION LANGUAGE" },
            { "Zgjidh gjuhën", "Choose language" },
            { "Ndryshimi zbatohet menjëherë - nuk duhet rinisur programi. CPU, GPU, RAM dhe FPS mbeten njësoj në të dyja gjuhët.",
              "The change applies instantly - no restart needed. CPU, GPU, RAM and FPS stay the same in both languages." },
            { "Shqip", "Shqip" },
            { "English", "English" },

            // ---------------------------------------------------- themes
            { "ZGJIDH NJË TEMË", "PICK A THEME" },
            { "Secili grup ka vetë karakterin e vet: Gaming është agresiv me shirita të segmentuar, Classic është vetëm shkrim dhe numra. Klikimi e ndryshon widget-in menjëherë.",
              "Each group has its own character: Gaming is aggressive with segmented bars, Classic is text and numbers only. One click changes the widget instantly." },
            { "Të gjitha", "All" },
            { "Renditja e widget-it", "Widget layout" },
            { "Vertikale", "Vertical" },
            { "Horizontale", "Horizontal" },
            { "Kompakte", "Compact" },
            { "Lojëra", "Games" },
            { "Tema e lojës", "Game theme" },
            { "Tema e IT-së", "IT theme" },

            // ---- kursoret e mouse-it (vetem profili IT)
            { "shirit i hollë në krye, për CS2", "a thin strip at the top, for CS2" },
            { "Kursori i mouse-it", "Mouse pointer" },
            { "KURSORI I MOUSE-IT", "MOUSE POINTER" },
            { "Kursori i Windows-it", "Windows pointer" },
            { "Katër kursorë të bërë për profilin IT. Secili ndryshon shigjetën, kursorin e tekstit, kursorin e linkut dhe shenjëstrën.",
              "Four pointers made for the IT profile. Each one changes the arrow, the text cursor, the link cursor and the crosshair." },
            { "Kursori ndryshon në gjithë Windows-in sa kohë LIKAsys është hapur. Kur e mbyll programin, kthehet kursori yt. Asgjë nuk shkruhet në regjistër.",
              "The pointer changes everywhere in Windows while LIKAsys is open. Close the app and your own pointer comes back. Nothing is written to the registry." },
            { "Pa ndryshim. LIKAsys nuk e prek shigjetën.", "No change. LIKAsys leaves the pointer alone." },
            { "Xham i errët me buzë ciani që ndriçon. Shkon me temat e errëta.",
              "Dark glass with a glowing cyan edge. Goes with the dark themes." },
            { "Piksel jeshil fosfori, me vija skanimi. Teksti merr bllokun e konsolës.",
              "Green phosphor pixels with scan lines. Text gets the console block." },
            { "Metal i ngurtë me një fije të bardhë rreth e rrotull. I qetë dhe i pastër.",
              "Solid metal with a white hairline all around it. Quiet and clean." },
            { "Vizatim teknik: vija të holla, hijezim diagonal, shenjëstër blu.",
              "A technical drawing: thin lines, diagonal hatching, a blue target." },
            { "LED-at e rackut mbi çelik të ftohtë", "rack LEDs on cold steel" },
            { "vizatim teknik, vetëm vija të holla", "technical drawing, thin lines only" },
            { "fletë e bardhë zyre, jeshile tabele", "white office sheet, spreadsheet green" },
            { "e lehtë dhe e qetë, për tavolinë pune", "light and calm, for the desk" },
            { "letër e shtypur, bojë e zezë, vijë e kuqe", "printed paper, black ink, red line" },
            { "vetëm bardhë e zi, si lexuesi i librave", "black and white only, like an e-reader" },
            { "ekran i vjetër me fosfor qelibar", "old amber phosphor screen" },
            { "vjollcë e butë, për natën vonë", "soft indigo, for late nights" },
            { "Tema u ndryshua ne ", "Theme changed to " },
            { "verdhë neoni, qoshe të prera", "neon yellow, sharp corners" },
            { "e kuqe korporate, pa shkëlqim", "corporate red, no glow" },
            { "jeshile dhe perëndim dielli", "green and sunset orange" },
            { "rozë synthwave, shkëlqim i butë", "synthwave pink, soft glow" },
            { "rërë dhe blu, rreshta të ngjeshur", "sand and blue, tight rows" },
            { "ushtarake, shkronja makine shkrimi", "military, typewriter type" },
            { "blloqe dhe ngjyra pikseli", "blocks and pixel colours" },
            { "ar i vjetër, shkronja me serif", "old gold, serif type" },
            { "Gaming", "Gaming" },
            { "Classic", "Classic" },
            { "Qelq", "Glass" },
            { "Minimal", "Minimal" },
            { "Dev", "Dev" },
            { "Dritë", "Light" },
            { "Pa sfond", "No background" },
            { "Kosova", "Kosova" },

            // ---------------------------------------------------- colours
            { "NGJYRAT E WIDGET-IT", "WIDGET COLOURS" },
            { "Shkruaj kodin HEX ose kliko katrorin për paletën e plotë të Windows-it.",
              "Type a HEX code or click the square for the full Windows palette." },
            { "SJELLJA E NGJYRAVE", "COLOUR BEHAVIOUR" },
            { "Ngjyros sipas ngarkesës (portokalli mbi 75%, kuqe mbi 90%)",
              "Colour by load (orange above 75%, red above 90%)" },
            { "Gradient midis dy ngjyrave kryesore (ikona dhe korniza)",
              "Gradient between the two main colours (icons and border)" },
            { "Hap paletën e ngjyrave", "Open the colour palette" },
            { "Ngjyra kryesore", "Main colour" },
            { "Ngjyra dytësore", "Secondary colour" },
            { "Sfondi (lart)", "Background (top)" },
            { "Sfondi (poshtë)", "Background (bottom)" },
            { "Korniza", "Border" },
            { "Vlerat / teksti", "Values / text" },
            { "Etiketat", "Labels" },
            { "Detajet e vogla", "Small details" },
            { "Shiriti bosh", "Empty bar" },
            { "Paralajmërim (mbi 75%)", "Warning (above 75%)" },
            { "Rrezik (mbi 90%)", "Danger (above 90%)" },

            // ---------------------------------------------------- card look
            { "QELQ DHE SFOND", "GLASS AND BACKGROUND" },
            { "Blur në sfond (akrilik - sfondi pas widget-it turbullohet)",
              "Background blur (acrylic - whatever is behind the widget goes soft)" },
            { "Tejdukshmëria", "Opacity" },
            { "Hije poshtë kartelës", "Shadow under the card" },
            { "Forca e hijes", "Shadow strength" },
            { "FORMA", "SHAPE" },
            { "Rrumbullakimi i cepave", "Corner rounding" },
            { "Trashësia e kornizës", "Border thickness" },
            { "Përmasa", "Size" },
            { "Hapësira anash", "Side padding" },
            { "Hapësira lart/poshtë", "Top/bottom padding" },
            { "Largësia mes rreshtave", "Space between rows" },
            { "IKONAT", "ICONS" },
            { "Stili i ikonave", "Icon style" },
            { "Madhësia e ikonave", "Icon size" },
            { "Animacione (shiritat rrëshqasin, rreshtat vijnë me radhë)", "Animations (bars glide, rows arrive one by one)" },
            { "Efekt ndriçimi (glow)", "Glow effect" },
            { "Hije pas shkrimit (lexohet mbi çdo sfond)", "Shadow behind the text (readable on any background)" },
            { "SHIRITAT DHE RENDITJA", "BARS AND LAYOUT" },
            { "Stili i shiritave", "Bar style" },
            { "Trashësia e shiritit", "Bar thickness" },
            { "Renditja", "Layout" },
            { "Shfaq shiritat e ngarkesës", "Show load bars" },
            { "Shfaq kreun me emrin LIKAsys", "Show the header with the LIKAsys name" },
            { "Shfaq pikën ndriçuese te kreu", "Show the glowing dot in the header" },
            { "Shfaq fundin 'Made in Kosovo'", "Show the 'Made in Kosovo' footer" },
            { "Emri LIKAsys dhe 'Made in Kosovo' rrinë gjithmonë të ndezura — janë pjesë e identitetit, jo cilësim.",
              "The LIKAsys name and 'Made in Kosovo' stay on for good - they are part of the identity, not a preference." },

            // ---------------------------------------------------- combos
            { "3D (me thellësi)", "3D (with depth)" },
            { "Outline (vija të holla)", "Outline (thin lines)" },
            { "Solid (të mbushura)", "Solid (filled)" },
            { "Pa ikona", "No icons" },
            { "Të rrumbullakosur", "Rounded" },
            { "Katrorë", "Square" },
            { "Të segmentuar", "Segmented" },
            { "Pa shirita", "No bars" },
            { "Vertikale (njëra mbi tjetrën)", "Vertical (stacked)" },
            { "Horizontale (në një shirit)", "Horizontal (one strip)" },
            { "Kompakte (pa shirita)", "Compact (no bars)" },
            { "0  -  p.sh. 75%", "0  -  e.g. 75%" },
            { "1  -  p.sh. 75.4%", "1  -  e.g. 75.4%" },
            { "2  -  p.sh. 75.42%", "2  -  e.g. 75.42%" },
            { "Celsius (°C)", "Celsius (°C)" },
            { "Fahrenheit (°F)", "Fahrenheit (°F)" },
            { "E hollë", "Light" },
            { "Normale", "Normal" },
            { "Mesatare", "Medium" },
            { "Gjysmë e trashë", "Semi bold" },
            { "E trashë", "Bold" },
            { "Shumë e trashë", "Black" },
            { "(kryesor)", "(primary)" },

            // ---------------------------------------------------- font
            { "FONTI", "FONT" },
            { "Lloji i fontit", "Font family" },
            { "Madhësia bazë", "Base size" },
            { "Vlerat më të mëdha", "Bigger values" },
            { "Etiketat më të vogla", "Smaller labels" },
            { "TRASHËSIA", "WEIGHT" },
            { "Vlerat (numrat)", "Values (numbers)" },
            { "Etiketat (CPU, GPU...)", "Labels (CPU, GPU...)" },
            { "FORMATI", "FORMAT" },
            { "Shifra pas presjes", "Decimal places" },
            { "Etiketat me shkronja të mëdha (CPU / cpu)", "Uppercase labels (CPU / cpu)" },
            { "Shfaq njësitë (%, GB, FPS)", "Show units (%, GB, FPS)" },

            // ---------------------------------------------------- position
            { "VENDI NË EKRAN", "PLACE ON SCREEN" },
            { "Monitori", "Monitor" },
            { "Largësia horizontale", "Horizontal margin" },
            { "Largësia vertikale", "Vertical margin" },
            { "SJELLJA", "BEHAVIOUR" },
            { "Gjithmonë sipër të gjitha dritareve", "Always on top of every window" },
            { "Kap qoshet automatikisht kur e tërheq", "Snap to corners while dragging" },
            { "Blloko pozicionin (nuk lëvizet me mouse)", "Lock position (cannot be dragged)" },
            { "Përshkueshme nga klikimi (mausi kalon përtej)", "Click-through (mouse passes behind)" },
            { "Shfaqe vetëm gjatë lojës (fshihet në desktop)", "Show only in game (hidden on the desktop)" },

            // ---------------------------------------------------- metrics
            { "ÇFARË TË SHFAQET", "WHAT TO SHOW" },
            { "CPU - përqindja e përdorimit", "CPU - usage percentage" },
            { "CPU - temperatura", "CPU - temperature" },
            { "CPU - shpejtësia (GHz)", "CPU - clock speed (GHz)" },
            { "GPU - përqindja e përdorimit", "GPU - usage percentage" },
            { "GPU - temperatura", "GPU - temperature" },
            { "VRAM - memoria e kartës grafike", "VRAM - graphics card memory" },
            { "RAM - memoria e sistemit", "RAM - system memory" },
            { "FPS - kuadro në sekondë", "FPS - frames per second" },
            { "FPS - shfaq emrin e lojës", "FPS - show the game name" },
            { "1% LOW - ngecjet (kërkon lojë aktive)", "1% LOW - stutter (needs a running game)" },
            { "FRAME - koha e një kuadri në ms", "FRAME - time of one frame in ms" },
            { "1% LOW tregon sa bien kuadrot në momentet më të këqija. Sa më afër FPS-së mesatare, aq më e qetë loja.",
              "1% LOW shows how far the frame rate drops at its worst. The closer to the average FPS, the smoother the game." },
            { "duke mbledhur", "collecting" },
            { "pa loje", "no game" },
            { "kerkon admin", "needs admin" },
                        // ---- v1.6: Apple profile, reveal motion, quick switch
            { "Disku, I/O, rrjeti, ping dhe uptime. Shkronja monospace, ikona teknike, theks i gjelbër. Për pamje Apple - jashtëzakonisht e pastër, e bardhë, pa korniza - shko te Temat > Dritë > Apple Clean.",
              "Storage, I/O, network, ping and uptime. Monospace type, technical icons, green accent. For the Apple look - extremely clean, white, no borders - go to Themes > Light > Apple Clean." },
            { "Vijë floku (e qetë)", "Hairline (quiet)" },
            { "Si vjen në ekran", "How it arrives" },
            { "Lëvizja zgjat më pak se gjysmë sekonde dhe shihet sa herë widget-i shfaqet ose kthehet nga i minimizuari.",
              "The motion takes under half a second and plays whenever the widget appears or comes back from minimised." },
            { "Pa lëvizje", "No motion" },
            { "Bie nga lart", "Drops from the top" },
            { "Rrëshqet nga e djathta", "Slides in from the right" },
            { "Rrëshqet nga e majta", "Slides in from the left" },
            { "Ngrihet nga poshtë", "Rises from the bottom" },
            { "Shfaqet butë", "Gentle fade" },
            { "Profili", "Profile" },
            { "Profili u ndryshua në ", "Profile switched to " },
            // ---- v1.5: profiles, storage, minimised view
            { "PROFILI", "PROFILE" },
            { "Zgjidh për çfarë e përdor këtë kompjuter. Profili i ndërron njëherësh rreshtat, ikonat dhe ngjyrat. Pastaj mund të ndryshosh çdo gjë veç e veç më poshtë.",
              "Pick what you use this computer for. The profile switches the rows, the icons and the colours all at once. After that you can still change everything one by one below." },
            { "IT", "IT" },
            { "FPS, 1% low, VRAM dhe temperatura. Numra të mëdhenj, ikona 3D, theks cyan.",
              "FPS, 1% low, VRAM and temperatures. Big numbers, 3D icons, cyan accent." },
            { "RUAJTJA", "STORAGE" },
            { "DISK - sa është mbushur disku i sistemit", "DISK - how full the system drive is" },
            { "I/O - lexim dhe shkrim në MB/s", "I/O - read and write in MB/s" },
            { "UPTIME - sa kohë është ndezur kompjuteri", "UPTIME - how long the computer has been on" },
            { "Si të hapet widget-i", "How the widget opens" },
            { "I minimizuar do të thotë një shirit i vetëm me numrat kryesorë - mbetet sipër, por nuk zë vend.",
              "Minimised means a single bar with the headline numbers - it stays on top, but takes no space." },
            { "Siç e lashë herën e fundit", "However I left it" },
            { "Gjithmonë i plotë", "Always full" },
            { "Gjithmonë i minimizuar", "Always minimised" },
            { "Minimizo", "Minimise" },
            { "Hape te plote", "Open it fully" },
            { "lire", "free" },
            // ---- runtime messages (notifications, diagnostics, update window)
            { "Gabim gjate punes", "Something went wrong" },
            { "Cilësimet nuk u lexuan dot - u perdoren ato fillestare.",
              "The settings could not be read - the defaults were used." },
            { "Ikona afer ores nuk u krijua: ", "The icon next to the clock was not created: " },
            { "Matja e sistemit nuk u nis: ", "System monitoring did not start: " },
            { "Widget-i nuk u hap: ", "The widget did not open: " },
            { "LIKAsys eshte gati", "LIKAsys is ready" },
            { "Widget-i u hap ne qoshe te ekranit. Kliko dy here mbi ikonen per ta fshehur ose shfaqur.",
              "The widget opened in the corner of your screen. Double-click the icon to hide or show it." },
            { "LIKAsys u nis, por disa pjese nuk punuan:", "LIKAsys started, but some parts did not work:" },
            { "Detajet e plota jane ketu:", "The full details are here:" },
            { "LIKAsys - diagnostike", "LIKAsys - diagnostics" },
            { "Detajet: ", "Details: " },
            { "Widget-i nuk u rikthye dot", "The widget could not be brought back" },
            { "Gjuha e parazgjedhur e LIKAsys", "The default language of LIKAsys" },
            { "Duke shkarkuar...", "Downloading..." },
            { "Nuk u nis instaluesi. Hape dosjen Downloads dhe nise manualisht.",
              "The installer did not start. Open your Downloads folder and run it manually." },
            { "RRJETI", "NETWORK" },
            { "NET - shpejtësia e shkarkimit dhe ngarkimit", "NET - download and upload speed" },
            { "PING - koha e përgjigjes së rrjetit", "PING - network response time" },
            { "Hosti për ping", "Ping host" },
            { "Matet çdo 5 sekonda, në sfond. Shkruaj 1.1.1.1, 8.8.8.8 ose adresën e serverit tënd.",
              "Measured every 5 seconds, in the background. Enter 1.1.1.1, 8.8.8.8 or your own server address." },
            { "NJËSITË DHE MATJA", "UNITS AND MEASUREMENT" },
            { "Temperatura", "Temperature" },
            { "Rifreskimi", "Refresh rate" },
            { "Shiriti i FPS deri në", "FPS bar maximum" },
            { "0 = automatik (përshtatet me FPS-në më të lartë të parë).",
              "0 = automatic (follows the highest FPS seen so far)." },
            { "Sensorët e avancuar (temperatura, GHz)", "Advanced sensors (temperature, GHz)" },
            { "Matja e FPS-së në lojëra", "FPS measurement in games" },

            // ---------------------------------------------------- system
            { "NISJA", "STARTUP" },
            { "IKONA NË TRAY", "TRAY ICON" },
            { "Çfarë vizaton ikona", "What the icon draws" },
            { "Numri vizatohet direkt mbi ikonën afër orës - lexohet edhe me widget-in e fshehur, pa zënë asnjë piksel.",
              "The number is drawn straight onto the icon next to the clock - readable even with the widget hidden, using no screen space at all." },
            { "Logoja e LIKAsys", "The LIKAsys logo" },
            { "CPU - ngarkesa %", "CPU - load %" },
            { "GPU - ngarkesa %", "GPU - load %" },
            { "RAM - ngarkesa %", "RAM - load %" },
            { "Nis bashkë me Windows (pa dritare UAC)", "Start with Windows (no UAC prompt)" },
            { "PËRDITËSIMET", "UPDATES" },
            { "Kontrollo automatikisht për versione të reja", "Check for new versions automatically" },
            { "Përditësimet merren direkt nga serveri i LIKAsys.", "Updates come straight from the LIKAsys server." },
            { "Kontrollo tani", "Check now" },
            { "MIRËMBAJTJE", "MAINTENANCE" },
            { "Hap dosjen e cilësimeve", "Open the settings folder" },
            { "Rikthe cilësimet fillestare", "Restore default settings" },
            { "Duke kontrolluar...", "Checking..." },
            { "Je në versionin më të ri.", "You are on the latest version." },
            { "Version i ri:", "New version:" },
            { "Nuk u lidh dot me serverin e perditesimeve.", "Could not reach the update server." },

            // ---------------------------------------------------- about
            { "LIKAsys është monitor i lehtë i sistemit për gamer-a: CPU, GPU, VRAM, RAM dhe FPS në një kartelë të vogël në qoshe të ekranit. Falas përgjithmonë, pa reklama, pa pagesa.",
              "LIKAsys is a lightweight system monitor for gamers: CPU, GPU, VRAM, RAM and FPS on a small card in the corner of your screen. Free forever, no ads, no payments." },
            { "Sensorët e temperaturës mundësohen nga LibreHardwareMonitor (MPL-2.0). Matja e FPS-së bazohet në ngjarjet ETW të Windows-it, e njëjta teknikë që përdor PresentMon.",
              "Temperature sensors are powered by LibreHardwareMonitor (MPL-2.0). FPS measurement is based on Windows ETW events, the same technique PresentMon uses." },
            { "(c) 2026 LIKAsys - Prishtinë, Kosovë", "(c) 2026 LIKAsys - Prishtina, Kosovo" },
            { "Made in Kosovo with", "Made in Kosovo with" },

            // ---------------------------------------------------- widget + tray
            { "Fshih (mbetet ne tray)", "Hide (stays in the tray)" },
            { "Hap Likaapps.com", "Open Likaapps.com" },
            { "Shfaq widget-in", "Show the widget" },
            { "Gjithmonë sipër", "Always on top" },
            { "Përshkueshme nga klikimi", "Click-through" },
            { "Blloko pozicionin", "Lock position" },
            { "Lart majtas", "Top left" },
            { "Lart në mes", "Top centre" },
            { "Lart djathtas", "Top right" },
            { "Mes majtas", "Middle left" },
            { "Qendra", "Centre" },
            { "Mes djathtas", "Middle right" },
            { "Poshtë majtas", "Bottom left" },
            { "Poshtë në mes", "Bottom centre" },
            { "Poshtë djathtas", "Bottom right" },
            { "Rikthe widget-in në ekran", "Bring the widget back on screen" },
            { "Kontrollo për update", "Check for updates" },
            { "Hap regjistrin", "Open the log file" },
            { "Rreth LIKAsys", "About LIKAsys" },
            { "pilulë e zezë që rrotullon matjet dhe hapet kur i afrohesh",
              "a black pill that rolls through the readings and opens when you come near" },
            { "Dil", "Exit" },

            // ---------------------------------------------------- update window
            { "LIKAsys - Perditesim", "LIKAsys - Update" },
            { "Version i ri i disponueshem", "A new version is available" },
            { "ÇFARE KA TE RE", "WHAT'S NEW" },
            { "Me vone", "Later" },
            { "Instalo tani", "Install now" },
            { "Duke instaluar... LIKAsys mbyllet dhe rihapet vetvetiu.",
              "Installing... LIKAsys will close and reopen by itself." },
            { "Shkarkimi deshtoi. Provo perseri ose merre nga Likaapps.com.",
              "The download failed. Try again or get it from Likaapps.com." },
        };
    }
}
