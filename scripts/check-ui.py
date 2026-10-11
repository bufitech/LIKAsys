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
