#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
Build-time safety net for the IT pointer packs.

Replacing the Windows cursor is a system-wide change. Two ways it can go wrong:

  1. A BROKEN OR MISSING .cur. LoadImage just returns NULL and the user picks a
     style that silently does nothing.
  2. A MISSING RESTORE. If the app exits without handing the pointer back, the
     machine keeps our arrow until the user logs off. That must never ship.

Both are checked here. Run from the repo root: python3 scripts/check-cursors.py
"""
import os, struct, sys

ROOT   = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
CURDIR = os.path.join(ROOT, "src", "LIKAsys", "assets", "cursors")
STYLES = ["neon", "terminal", "carbon", "blueprint"]
KINDS  = ["arrow", "ibeam", "link", "cross"]
MIN_SIZES = 4

fail = []

def check_cur(path):
    with open(path, "rb") as f:
        b = f.read()
    if len(b) < 22:
        return "skedar bosh"
    res, typ, n = struct.unpack_from("<HHH", b, 0)
    if res != 0 or typ != 2:
        return "nuk eshte .cur (type=%d)" % typ
    if n < MIN_SIZES:
        return "vetem %d madhesi, duhen te pakten %d" % (n, MIN_SIZES)
    seen = set()
    for i in range(n):
        w, h, _, _, hx, hy, size, off = struct.unpack_from("<BBBBHHII", b, 6 + 16 * i)
        w = w or 256
        h = h or 256
        if off + size > len(b):
            return "kuadrati %d del jashte skedarit" % i
        if hx > w or hy > h:
            return "hotspot %d,%d jashte %dx%d" % (hx, hy, w, h)
        bits = struct.unpack_from("<H", b, off + 14)[0]
        if bits != 32:
            return "kuadrati %dpx nuk eshte 32-bit" % w
        seen.add(w)
    if 32 not in seen:
        return "mungon kuadrati 32px"
    return None

if not os.path.isdir(CURDIR):
    fail.append("mungon dosja %s" % CURDIR)

for st in STYLES:
    d = os.path.join(CURDIR, st)
    if not os.path.isdir(d):
        fail.append("mungon stili '%s'" % st)
        continue
    for k in KINDS:
        p = os.path.join(d, k + ".cur")
        if not os.path.isfile(p):
            fail.append("%s/%s.cur mungon" % (st, k))
            continue
        try:
            err = check_cur(p)
        except Exception as ex:
            err = "nuk lexohet: %s" % ex
        if err:
            fail.append("%s/%s.cur :: %s" % (st, k, err))
    if not os.path.isfile(os.path.join(d, "preview.png")):
        fail.append("%s/preview.png mungon (cilesimet e perdorin)" % st)

# ------------------------------------------------- the restore must be wired up
app = os.path.join(ROOT, "src", "LIKAsys", "App.xaml.cs")
try:
    src = open(app, encoding="utf-8-sig").read()
except Exception as ex:
    src = ""
    fail.append("nuk lexohet App.xaml.cs: %s" % ex)

def body(text, header):
    """The method only, never the one after it: a short method would otherwise
    borrow the next method's Restore() and hide a real hole."""
    i = text.find(header)
    if i < 0:
        return ""
    j = text.find("\n        }", i)
    return text[i:j] if j > i else text[i:i + 520]

if src:
    # every way out of the process has to hand the pointer back
    for header, label in (("private void ExitApp()", "ExitApp"),
                          ("protected override void OnExit(", "OnExit")):
        if "MouseCursors.Restore()" not in body(src, header):
            fail.append("App.xaml.cs :: %s duhet te thirre MouseCursors.Restore()" % label)
    import re as _re
    for hook in ("ProcessExit", "UnhandledException"):
        pat = _re.compile(r"CurrentDomain\.%s\s*\+=.{0,260}?MouseCursors\.Restore\(\)" % hook, _re.S)
        if not pat.search(src):
            fail.append("App.xaml.cs :: %s duhet te rikthejne kursorin" % hook)
    if "MouseCursors.Sync(_settings)" not in src:
        fail.append("App.xaml.cs :: mungon MouseCursors.Sync(_settings)")

core = os.path.join(ROOT, "src", "LIKAsys", "Core", "MouseCursors.cs")
try:
    cs = open(core, encoding="utf-8-sig").read()
except Exception as ex:
    cs = ""
    fail.append("nuk lexohet MouseCursors.cs: %s" % ex)

if cs:
    # SetSystemCursor destroys the handle it is given, so a fresh one per apply
    if "SPI_SETCURSORS" not in cs:
        fail.append("MouseCursors.cs :: rikthimi duhet te perdore SPI_SETCURSORS")
    if "s.Profile != UiProfile.It" not in cs:
        fail.append("MouseCursors.cs :: kursoret duhet te vlejne vetem per profilin IT")
    if "DestroyCursor" not in cs:
        fail.append("MouseCursors.cs :: handle-i i deshtuar duhet liruar me DestroyCursor")

csproj = os.path.join(ROOT, "src", "LIKAsys", "LIKAsys.csproj")
try:
    pj = open(csproj, encoding="utf-8-sig").read()
except Exception:
    pj = ""
if "assets/cursors" not in pj:
    fail.append("LIKAsys.csproj :: kursoret nuk kopjohen ne dalje (mungon Content)")

if fail:
    print("KONTROLLI I KURSOREVE DESHTOI")
    for f in fail:
        print("    " + f)
    sys.exit(1)

total = len(STYLES) * len(KINDS)
print("kursoret ok - %d stile, %d skedare .cur, rikthimi i lidhur" % (len(STYLES), total))
