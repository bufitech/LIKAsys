#!/usr/bin/env python3
"""
Build-time safety net for ThemeLibrary.

A theme is pure data, so a typo in it never breaks the compiler - it breaks the
widget at runtime, in front of the user. These checks run before every build:

  1. DUPLICATE NAMES.  Theme.Of() looks presets up by name; two presets with the
     same name means one of them can never be selected again.
  2. BROKEN COLOURS.   Every colour must be #RRGGBB or #AARRGGBB. WPF throws on
     anything else, and it throws while the window is being drawn.
  3. UNKNOWN ENUMS.    Icons / BarStyle / Layout / Pos must name a real member.
  4. UNKNOWN GROUP.    Every Group must be one of the G* constants and must
     appear in GroupOrder, otherwise the preset is invisible in the settings.
  5. MISSING NOTE.     Every Note must have an Albanian -> English entry in Lang.

Run from the repo root:  python3 scripts/check-themes.py
"""
import io
import os
import re
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
SRC = os.path.join(ROOT, "src", "LIKAsys")
LIB = os.path.join(SRC, "Core", "ThemeLibrary.cs")
SET = os.path.join(SRC, "Core", "AppSettings.cs")
LANG = os.path.join(SRC, "Core", "Lang.cs")

COLOUR_FIELDS = ("Accent", "Accent2", "BgTop", "BgBottom", "Border", "Text",
                 "Label", "Detail", "Track", "Warn", "Danger")
HEX = re.compile(r"^#(?:[0-9A-Fa-f]{6}|[0-9A-Fa-f]{8})$")


def read(path):
    return io.open(path, encoding="utf-8-sig").read()


def enum_members(text, name):
    m = re.search(r"enum\s+" + name + r"\s*\{([^}]*)\}", text)
    if not m:
        return None
    return [x.strip() for x in m.group(1).split(",") if x.strip()]


def main():
    lib, aset, lang = read(LIB), read(SET), read(LANG)
    errors = []

    enums = {}
    for e in ("IconStyle", "BarStyle", "WidgetLayout", "WidgetPosition"):
        members = enum_members(aset, e)
        if members is None:
            errors.append("enum %s not found in AppSettings.cs" % e)
        enums[e] = members or []

    groups = dict(re.findall(r'public const string (G\w+)\s*=\s*"([^"]+)"', lib))
    order = re.search(r"GroupOrder\s*=\s*\{([^}]*)\}", lib)
    in_order = [g.strip() for g in order.group(1).split(",") if g.strip()] if order else []

    lang_keys = set(re.findall(r'\{\s*"((?:[^"\\]|\\.)*)"\s*,\s*"(?:[^"\\]|\\.)*"\s*\}', lang))

    # one preset per "new ThemePreset { ... }" block, nesting-free by construction
    blocks = []
    for m in re.finditer(r"new ThemePreset\s*\{", lib):
        i = m.end()
        depth = 1
        while i < len(lib) and depth:
            if lib[i] == "{":
                depth += 1
            elif lib[i] == "}":
                depth -= 1
            i += 1
        blocks.append(lib[m.end():i - 1])

    if not blocks:
        errors.append("no presets found at all")

    seen = {}
    for b in blocks:
        name = re.search(r'Name\s*=\s*"([^"]*)"', b)
        name = name.group(1) if name else "<pa emer>"
        if name in seen:
            errors.append('%s: duplicate name' % name)
        seen[name] = True

        for field in COLOUR_FIELDS:
            for value in re.findall(field + r'\s*=\s*"([^"]*)"', b):
                if not HEX.match(value):
                    errors.append("%s: %s=%s is not #RRGGBB or #AARRGGBB" % (name, field, value))

        for field, enum in (("Icons", "IconStyle"), ("BarStyle", "BarStyle"),
                            ("Layout", "WidgetLayout"), ("Pos", "WidgetPosition")):
            for value in re.findall(field + r"\s*=\s*\w+\.(\w+)", b):
                if enums.get(enum) and value not in enums[enum]:
                    errors.append("%s: %s.%s does not exist" % (name, enum, value))

        g = re.search(r"Group\s*=\s*(G\w+)", b)
        if not g:
            errors.append("%s: no Group" % name)
        elif g.group(1) not in groups:
            errors.append("%s: group constant %s does not exist" % (name, g.group(1)))
        elif g.group(1) not in in_order:
            errors.append("%s: group %s is missing from GroupOrder" % (name, g.group(1)))

        note = re.search(r'Note\s*=\s*"([^"]*)"', b)
        if note and note.group(1) not in lang_keys:
            errors.append('%s: Note "%s" has no entry in Lang.Map' % (name, note.group(1)))

        if re.search(r"\bOpacity\s*=\s*([01](?:\.\d+)?)", b):
            o = float(re.search(r"\bOpacity\s*=\s*([01](?:\.\d+)?)", b).group(1))
            if not 0 <= o <= 1:
                errors.append("%s: Opacity %s is outside 0..1" % (name, o))

    default = re.search(r'public const string Default\s*=\s*"([^"]+)"', lib)
    if default and default.group(1) not in seen:
        errors.append("the default theme (%s) does not exist" % default.group(1))

    if errors:
        print("temat kane probleme:")
        for e in errors:
            print("  - " + e)
        return 1

    per_group = {}
    for b in blocks:
        g = re.search(r"Group\s*=\s*(G\w+)", b)
        per_group[groups[g.group(1)]] = per_group.get(groups[g.group(1)], 0) + 1
    summary = ", ".join("%s %d" % (g, per_group[g]) for g in
                        [groups[k] for k in in_order if k in groups] if g in per_group)
    print("temat ok - %d presete, pa perseritje (%s)" % (len(blocks), summary))
    return 0


if __name__ == "__main__":
    sys.exit(main())
