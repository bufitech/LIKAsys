#!/usr/bin/env python3
"""
Build-time safety net for the translation table.

Two things are checked, and either one fails the build:

  1. DUPLICATE KEYS.  Lang.Map is written by hand with a collection initialiser.
     A repeated Albanian key used to throw ArgumentException inside the static
     constructor - the app then died before drawing a single pixel, with no
     window and no message.  That shipped once.  Never again.

  2. MISSING TRANSLATIONS.  Every Albanian literal that reaches the user through
     Lang.T(...) or Tr(...) must have an entry, otherwise English users read
     Albanian.

Run from the repo root:  python3 scripts/check-lang.py
"""
import collections
import glob
import os
import re
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
SRC = os.path.join(ROOT, "src", "LIKAsys")
LANG = os.path.join(SRC, "Core", "Lang.cs")

# brand names, acronyms and other strings that are the same in both languages
SKIP = {
    "LIKAsys", "Likaapps.com", "Made in Kosovo with", "FPS", "Gaming", "IT",
    "WidthAndHeight", "Midnight Glass", "Terminal", "Apple Clean",
    # identical on purpose: units, proper nouns and the language names themselves
    "Celsius (\u00b0C)", "Fahrenheit (\u00b0F)", "Shqip", "English",
    "Classic", "Dev", "Minimal", "Kosova",
}

fail = []

# ----------------------------------------------------------------- the table
src = open(LANG, encoding="utf-8").read()
keys = re.findall(r'\{\s*"((?:[^"\\]|\\.)*)"\s*,\s*"', src)
dupes = sorted(k for k, c in collections.Counter(keys).items() if c > 1)
if dupes:
    fail.append("celesa te perseritur ne Lang.cs (do ta vrisnin aplikacionin ne nisje):")
    fail += ["    " + d for d in dupes]

same = sorted(set(re.findall(r'\{\s*"((?:[^"\\]|\\.)*)"\s*,\s*"\1"\s*\}', src)) - SKIP)
if same:
    fail.append("perkthime qe jane identike me shqipen:")
    fail += ["    " + d for d in same]

known = set(keys)

# ------------------------------------------------- every string shown to a user
for path in sorted(glob.glob(os.path.join(SRC, "**", "*.cs"), recursive=True)):
    if os.sep + "obj" + os.sep in path or os.sep + "bin" + os.sep in path:
        continue
    if path == LANG:
        continue
    text = open(path, encoding="utf-8").read()
    rel = os.path.relpath(path, ROOT)

    for m in re.finditer(r'Lang\.T\(\s*"((?:[^"\\]|\\.)*)"', text):
        if m.group(1) not in known and m.group(1) not in SKIP:
            fail.append("    %s :: Lang.T(\"%s\")" % (rel, m.group(1)))

    for pat in (r'\bTr\(([^;]*?)\)\s*;', r'AddProfileCard\(([^;]*?)\);'):
        for m in re.finditer(pat, text, re.S):
            for lit in re.findall(r'"((?:[^"\\]|\\.)*)"', m.group(1)):
                if lit and lit not in known and lit not in SKIP:
                    fail.append("    %s :: \"%s\"" % (rel, lit))

# ------------------------------------------------------- duplicate resource keys
for path in sorted(glob.glob(os.path.join(SRC, "**", "*.xaml"), recursive=True)):
    text = open(path, encoding="utf-8").read()
    ks = re.findall(r'x:Key="([^"]+)"', text)
    d = sorted(k for k, c in collections.Counter(ks).items() if c > 1)
    if d:
        fail.append("x:Key i perseritur ne %s: %s" % (os.path.relpath(path, ROOT), ", ".join(d)))

if fail:
    print("KONTROLLI I GJUHES DESHTOI")
    for line in fail:
        print(line)
    sys.exit(1)

print("gjuha ok - %d hyrje, pa perseritje, pa boshlleqe" % len(keys))
