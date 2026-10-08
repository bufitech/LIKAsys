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
