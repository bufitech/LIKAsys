#!/usr/bin/env python3
"""
Build-time safety net for the widget markup.

XAML bindings are resolved at runtime, not by the compiler. A renamed property
does not break the build: it breaks one row of the widget, silently, on the
user's machine. Worse, a row that binds its Opacity to a property that no longer
exists draws nothing at all, and the card looks empty.

Three checks, any of which fails the build:

  1. DANGLING BINDINGS. Every {Binding Foo} in the widget markup must match a
     public property on MetricRowVm or AppSettings.
  2. THE MOTION PROPERTIES. Fill, RowOpacity and RowShift have to exist on both
     sides, because the entrance animation starts the rows at zero opacity.
  3. THE FAILSAFE. StaggerIn must still contain the guard timer that forces the
     rows back to full opacity. Without it an animation bug means a blank card.
  4. THE ISLAND LET-GO. The Dynamic Island clamps the height of the rows to zero
     while it is a pill. Leaving that theme has to lift the clamp, and a failed
     measurement has to skip it, or the card opens to nothing.

Run from the repo root:  python3 scripts/check-ui.py
"""
import io
import os
import re
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
SRC = os.path.join(ROOT, "src", "LIKAsys")
XAML = os.path.join(SRC, "Ui", "WidgetWindow.xaml")
CODE = os.path.join(SRC, "Ui", "WidgetWindow.xaml.cs")
VM = os.path.join(SRC, "Ui", "ViewModels.cs")
SETTINGS = os.path.join(SRC, "Core", "AppSettings.cs")

# names that come from somewhere other than a view model property
KNOWN = {"IsMouseOver", "RelativeSource", "Path", "ElementName", "Source"}

MOTION = ("Fill", "RowOpacity", "RowShift")


def read(path):
    return io.open(path, encoding="utf-8-sig").read()


def props(text):
    return set(re.findall(r"public\s+[\w<>?.\[\]]+\s+(\w+)\s*(?:=>|\{)", text))


def main():
    xaml, code = read(XAML), read(CODE)
    known = props(read(VM)) | props(read(SETTINGS)) | KNOWN
    errors = []

    for name in sorted(set(re.findall(r"\{Binding\s+([A-Za-z_]\w*)", xaml))):
        if name not in known:
            errors.append("{Binding %s} has no matching property" % name)

    for name in MOTION:
        if name not in known:
            errors.append("the motion property %s is gone from the view model" % name)
        if "{Binding %s}" % name not in xaml:
            errors.append("the widget markup no longer binds %s" % name)

    body = code[code.find("private void StaggerIn()"):]
    body = body[:body.find("private void MotionTick")] if "private void MotionTick" in body else body
    if "DispatcherTimer" not in body or "RowOpacity = 1" not in body:
        errors.append("StaggerIn lost its failsafe: rows could stay invisible")

    if "if (!busy) _motion.Stop();" not in code:
        errors.append("the motion timer no longer stops itself, the widget would spin forever")

    off = code[code.find("private void IslandMode"):]
    off = off[:off.find("private void IslandMeasure")] if "private void IslandMeasure" in off else off
    if "RowsHost.MaxHeight = double.PositiveInfinity" not in off:
        errors.append("leaving the island theme no longer lifts the height clamp")
    if "_isleRowsH < 0" not in code:
        errors.append("the island no longer has its 'could not measure, open anyway' escape")
    if xaml.count("IslandSwap") != 1:
        errors.append("the markup has %d IslandSwap blocks, it needs exactly one"
                      % xaml.count("IslandSwap"))

    lib = read(os.path.join(SRC, "Core", "ThemeLibrary.cs"))
    if 'Name="Dynamic Island"' not in lib or "Island=true" not in lib:
        errors.append("the Dynamic Island preset is gone or no longer sets Island=true")

    # 5. THE MATCH BAR GIVE-BACK. The CS2 strip hides the header, the rows and the
    #    footer and clips the card into an angular silhouette. If leaving the theme
    #    forgets any one of those, the next theme draws an empty sliver and the user
    #    has a widget with nothing in it.
    off = code[code.find("private void MatchMode"):]
    off = off[:off.find("private void MatchClipHandler")] if "private void MatchClipHandler" in off else off
    for need, msg in (
        ("RowsHost.Visibility = Visibility.Visible", "the rows"),
        ("FooterRow.Visibility = Visibility.Visible", "the footer"),
        ("HeaderRow.Visibility = Visibility.Visible", "the header"),
        ("Card.ClearValue(ClipProperty)", "the angular clip"),
    ):
        if need not in off:
            errors.append("leaving the match bar no longer restores " + msg)
    if xaml.count("MatchHost") != 1:
        errors.append("the markup has %d MatchHost blocks, it needs exactly one"
                      % xaml.count("MatchHost"))
    if "MatchRefresh" not in code or "if (_mbOn) MatchRefresh(s);" not in code:
        errors.append("the match bar is never refreshed from the metrics tick")
    if 'Name="Match Bar"' not in lib or "MatchBar=true" not in lib:
        errors.append("the Match Bar preset is gone or no longer sets MatchBar=true")

    # 6. THE SIZE. Three ways in (grip, Ctrl+wheel, tray) and one clamp. If the clamp
    #    goes, a slip of the wheel can shrink the widget to nothing or blow it off screen.
    for need, msg in (
        ("private void Grip_Down", "the drag grip"),
        ("private void Grip_Move", "the drag move"),
        ("private void Grip_Up", "the drag release"),
        ("private void Widget_Wheel", "Ctrl and the wheel"),
        ("Math.Max(ScaleMin, Math.Min(ScaleMax, value))", "the size clamp"),
    ):
        if need not in code:
            errors.append("resizing lost " + msg)
    if xaml.count('x:Name="SizeGrip"') != 1:
        errors.append("the markup has %d SizeGrip blocks, it needs exactly one"
                      % xaml.count('x:Name="SizeGrip"'))

    # 7. THE ICON FAMILIES. Two new sets, eleven icons each, plus the switch that
    #    reaches them. A missing geometry silently falls back and the set looks dead.
    ico = read(os.path.join(SRC, "Ui", "Icons.xaml"))
    for fam in ("Badge", "Ring"):
        n = sum(1 for m in ("Cpu", "Gpu", "Ram", "Disk", "DiskIo", "Fps",
                            "Low", "Frame", "Net", "Ping", "Uptime")
                if 'x:Key="Icon%s%s"' % (m, fam) in ico)
        if n != 11:
            errors.append("icon family %s has %d of 11 icons" % (fam, n))
    for need in ("case IconSet.Badge:", "case IconSet.Ring:", "case IconSet.Tech:"):
        if need not in code:
            errors.append("Ico() no longer reaches " + need.strip("case :"))

    # 8. THE LAYOUT CARDS. They must say which arrangement the theme was made for.
    sw = read(os.path.join(SRC, "Ui", "SettingsWindow.xaml.cs"))
    if "ThemeLibrary.FixedShape" not in sw:
        errors.append("the layout cards no longer check for themes with a fixed shape")
    if "BuildIconSets" not in sw:
        errors.append("the icon family picker is not built")

    # 9. THE WINDOW FRAME. The settings window draws its own, so minimise, maximise
    #    and the eight resize handles all have to be present and wired by hand.
    simple = read(os.path.join(SRC, "Ui", "SettingsWindow.Simple.cs"))
    sx = read(os.path.join(SRC, "Ui", "SettingsWindow.xaml"))
    for need, msg in (
        ("Min_Click", "the minimise button"),
        ("Max_Click", "the maximise button"),
        ("WM_GETMINMAXINFO", "the taskbar guard when maximised"),
        ("Edge_Down", "the resize handles"),
    ):
        if need not in sw:
            errors.append("the settings window lost " + msg)
    grips = sx.count('MouseLeftButtonDown="Edge_Down"')
    if grips != 8:
        errors.append("the settings window has %d resize handles, it needs 8" % grips)
    if 'ResizeMode="CanResize"' not in sx:
        errors.append("the settings window is not resizable")

    # 10. SIMPLE MODE. One page, and a way back to the tabs. If the switch or the
    #     failsafe goes, people land in a window with nothing in it.
    for need, msg in (
        ("private void ApplyMode", "the mode switch"),
        ("private void BuildSimple", "the simple page"),
        ("private void SyncSimple", "the refresh for the simple page"),
        ("PanelSimple.Children.Count == 0", "the failsafe back to the tabs"),
    ):
        if need not in simple:
            errors.append("simple mode lost " + msg)
    if 'x:Name="PanelSimple"' not in sx:
        errors.append("the simple page has no host in the markup")
    if sx.count('Style="{StaticResource ModeTab}"') != 2:
        errors.append("the simple and advanced switch is not two buttons")
    if "ApplyMode();" not in read(os.path.join(SRC, "Ui", "SettingsWindow.xaml.cs")):
        errors.append("the mode is never applied when the window opens")

    # 11. THE SCREENS. Monitors get unplugged. If the lookup or the watcher goes, the
    #     card ends up at coordinates that no longer belong to any screen.
    scr = read(os.path.join(SRC, "Core", "Screens.cs"))
    for need, msg in (
        ("public static ScreenInfo Pick", "the monitor lookup"),
        ("public static void Watch", "the watcher for monitors coming and going"),
        ("public static ScreenInfo Foreground", "follow the game"),
        ("public static bool OffScreen", "the off screen test"),
        ("DisplaySettingsChanged", "the Windows notification"),
    ):
        if need not in scr:
            errors.append("screen detection lost " + msg)

    plc = read(os.path.join(SRC, "Ui", "WidgetPlacement.cs"))
    if "Screens.Pick(s)" not in plc:
        errors.append("placement no longer asks which monitor to use")
    if "public static bool IsStranded" not in plc:
        errors.append("placement lost the stranded card test")

    app = read(os.path.join(SRC, "App.xaml.cs"))
    for need, msg in (
        ("WatchScreens();", "the call that starts watching the monitors"),
        ("IsStranded", "the rescue when a monitor disappears"),
        ("private void FollowForeground()", "the follow the game move"),
        ("            FollowForeground();", "the call that follows the game"),
    ):
        if need not in app:
            errors.append("the app lost " + msg)

    # 12. THE CAPSULES. Eleven colours per set, or a reading silently falls back to the
    #     accent and the theme stops looking like itself.
    pal = read(os.path.join(SRC, "Core", "Palettes.cs"))
    sets = re.findall(r'\["(\w+)"\] = new\[\]\s*\{(.*?)\}', pal, re.S)
    if len(sets) < 6:
        errors.append("only %d colour sets, there should be at least 6" % len(sets))
    for name, body in sets:
        n = len(re.findall(r'"#[0-9A-Fa-f]{6}"', body))
        if n != 11:
            errors.append("colour set %s has %d colours, it needs 11" % (name, n))

    if "private void StyleCapsule" not in code:
        errors.append("the capsule styling is gone")
    if "r.PushHistory" not in code:
        errors.append("nothing feeds the small graph any more")
    if 'CornerRadius="{Binding CapsuleRadius}"' not in xaml:
        errors.append("the row markup lost the capsule wrapper")
    if 'Data="{Binding Spark}"' not in xaml:
        errors.append("the row markup lost the small graph")

    # 13. THE ADVANCED WINDOW. The search is the whole reason it stopped being a wall
    #     of tabs, so it is worth a guard.
    pro = read(os.path.join(SRC, "Ui", "SettingsWindow.Pro.cs"))
    for need, msg in (
        ("private void Search_Changed", "the search"),
        ("private void BuildIndex", "the index the search reads"),
        ("private void JumpTo", "jumping to a found setting"),
        ("private void SetPageHead", "the page header"),
        ("private void BuildPalettes", "the colour set picker"),
    ):
        if need not in pro:
            errors.append("the advanced window lost " + msg)
    if 'x:Name="SearchBox"' not in sx:
        errors.append("the search box is not in the markup")
    if 'x:Name="PageTitle"' not in sx:
        errors.append("the page header is not in the markup")

    # 14. THE FINISHING TOUCHES. Each one is a few lines that are easy to lose in a
    #     refactor, and each one is the difference between a card that looks lit and
    #     a card that looks flat.
    for need, msg in (
        ("private void ChromeGlass", "the rim and the grain"),
        ("private static ImageBrush GrainBrush", "the grain tile"),
        ("private static Brush DotMask", "the dotted bar"),
        ("private static Brush StripeMask", "the slanted bar"),
        ("private Brush BarPaint", "the gradient bar"),
        ("private void UpdatePulse", "the breathing of a hot row"),
        ("_settings.ShadowTint", "the shadow that takes the theme colour"),
    ):
        if need not in code:
            errors.append("the card lost " + msg)

    if 'x:Name="Rim"' not in xaml or 'x:Name="Grain"' not in xaml:
        errors.append("the rim or the grain is not in the markup")
    if 'Opacity="{Binding BarPulse}"' not in xaml:
        errors.append("the bar no longer follows the pulse")

    # the live repaint runs every tick. If it stops reading the capsule colour, every
    # capsule theme silently falls back to one accent as soon as the first reading lands.
    if "r.RowColor ?? Accent" not in code:
        errors.append("the repaint no longer keeps the capsule colour")

    if "Dots, Stripes" not in read(SETTINGS):
        errors.append("the two new bar shapes are gone from the list")
    bars = re.search(r"BarCombo\.ItemsSource = Tr\((.*?)\);", read(os.path.join(SRC, "Ui", "SettingsWindow.xaml.cs")))
    if bars is None or bars.group(1).count('"') // 2 != 6:
        errors.append("the bar picker does not offer all six shapes")

    # 15. THE FROSTED GLASS. The material is four pieces that only work together: the
    #     milky sheen, the hairline round the edge, the bright rim with its dark
    #     bottom, and the grain. Lose one and the card stops reading as glass.
    for need, msg in (
        ("_settings.Glass == GlassMode.Apple", "the frosted material"),
        ("private bool LightMaterial", "the test for a light panel"),
        ("RimGlassDark", "the frosted rim"),
        ("private static Brush BuildRimGlass", "the builder of the frosted rim"),
        ("r.RuleBrush = ruleBrush", "the hairline between the rows"),
    ):
        if need not in code:
            errors.append("the glass lost " + msg)

    if 'Visibility="{Binding RuleVisibility}"' not in xaml:
        errors.append("the row hairline is not in the markup")

    sw = read(os.path.join(SRC, "Ui", "SettingsWindow.xaml.cs"))
    if "GlassCombo.SelectedIndex = (int)_settings.Glass" not in sw:
        errors.append("the glass picker never shows what is selected")
    if "_settings.Glass = (GlassMode)GlassCombo.SelectedIndex" not in sw:
        errors.append("the glass picker changes nothing")

    tl = read(os.path.join(SRC, "Core", "ThemeLibrary.cs"))
    if "s.Glass = t.Glass" not in tl or "s.RowRule = t.Rule" not in tl:
        errors.append("a theme can no longer carry its glass")
    if tl.count("Glass=GlassMode.Apple") < 5:
        errors.append("the frosted themes are gone")

    if errors:
        print("pamja e widget-it ka probleme:")
        for e in errors:
            print("  - " + e)
        return 1

    n = len(set(re.findall(r"\{Binding\s+([A-Za-z_]\w*)", xaml)))
    print("pamja ok - %d lidhje te verifikuara, animacionet me rrjete sigurie" % n)
    return 0


if __name__ == "__main__":
    sys.exit(main())
