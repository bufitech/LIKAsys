#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
Gjeneron preview/LIKAsys-preview.html duke lexuar temat direkt nga
src/LIKAsys/Core/ThemeLibrary.cs  (keshtu pamja eshte gjithmone e njejte me aplikacionin).
"""
import os, re, html

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
SRC = os.path.join(ROOT, "src", "LIKAsys", "Core", "ThemeLibrary.cs")
OUT = os.path.join(ROOT, "preview", "LIKAsys-preview.html")
VERSION = open(os.path.join(ROOT, "VERSION")).read().strip()

text = open(SRC, encoding="utf-8").read()

# ---------------------------------------------------------------- defaults
head = text.split("public static class ThemeLibrary")[0]
defaults = {}
for m in re.finditer(r'public\s+(?:string|double|bool|IconStyle)\s+(\w+)\s*=\s*([^;]+);', head):
    k, v = m.group(1), m.group(2).strip()
    defaults[k] = v.strip('"')

# ---------------------------------------------------------------- presets
presets = []
for m in re.finditer(r'new ThemePreset\s*\{(.*?)\}\s*,', text, re.S):
    body = m.group(1)
    t = dict(defaults)
    for p in re.finditer(r'(\w+)\s*=\s*(?:"([^"]*)"|([A-Za-z0-9_.]+))', body):
        t[p.group(1)] = p.group(2) if p.group(2) is not None else p.group(3)
    presets.append(t)


def truth(v):
    return str(v).strip().lower() == "true"


def css(hexstr, fallback="#000000"):
    """#RRGGBB ose #AARRGGBB -> ngjyre CSS."""
    s = (hexstr or "").strip().lstrip("#")
    if len(s) == 8:
        a = int(s[0:2], 16) / 255.0
        return "rgba(%d,%d,%d,%.3f)" % (int(s[2:4], 16), int(s[4:6], 16), int(s[6:8], 16), a)
    if len(s) == 6:
        return "#" + s
    return fallback


def rgba(hexstr, alpha):
    s = (hexstr or "").strip().lstrip("#")
    if len(s) == 8:
        s = s[2:]
    if len(s) != 6:
        return "rgba(0,0,0,%.3f)" % alpha
    return "rgba(%d,%d,%d,%.3f)" % (int(s[0:2], 16), int(s[2:4], 16), int(s[4:6], 16), alpha)


ROWS = [
    ("CPU", "cpu", "42%", 0.42, "58\u00b0C  \u00b7  4.4 GHz"),
    ("GPU", "gpu", "78%", 0.78, "64\u00b0C"),
    ("VRAM", "ram", "6.1 GB", 0.51, "nga 12 GB"),
    ("RAM", "ram", "14.2 GB", 0.44, "nga 32 GB"),
    ("FPS", "fps", "144", 0.72, "Cyberpunk2077"),
]

ICONS = {
    "cpu": '<rect x="4.5" y="4.5" width="11" height="11" rx="2"/><rect x="7.5" y="7.5" width="5" height="5" rx="1"/>'
           '<path d="M8 1.6v2.9M12 1.6v2.9M8 15.5v2.9M12 15.5v2.9M1.6 8h2.9M1.6 12h2.9M15.5 8h2.9M15.5 12h2.9"/>',
    "gpu": '<rect x="2" y="5" width="16" height="10" rx="2"/><circle cx="7.5" cy="10" r="2.6"/><path d="M12 8.5h3.5M12 11.5h3.5"/>',
    "ram": '<rect x="2" y="6" width="16" height="8" rx="1.6"/><path d="M5.5 9v2.6M8.5 9v2.6M11.5 9v2.6M14.5 9v2.6"/>',
    "fps": '<path d="M3 13.5l3.8-4.6 3 2.8L14 6"/><path d="M11.2 6H14v2.8"/>',
}


def widget(t, scale=1.0, rows=ROWS, footer=True, show_header=True):
    acc = css(t["Accent"], "#00E5FF")
    acc2 = css(t["Accent2"], "#7C4DFF")
    txt = css(t["Text"], "#EAF2FF")
    lab = css(t["Label"], "#93A6BE")
    det = css(t["Detail"], "#5D6E85")
    trk = css(t["Track"], "#1B2433")
    bd = css(t["Border"], "rgba(255,255,255,.2)")
    op = float(t["Opacity"])
    rad = float(t["Radius"])
    bt = float(t["BorderThickness"])
    blur = truth(t["Blur"])
    glow = truth(t["Glow"])
    bars = truth(t["Bars"])
    icons = str(t["Icons"]).split(".")[-1]

    bg = "linear-gradient(160deg,%s %s,%s 100%%)" % (
        rgba(t["BgTop"], op), "0%", rgba(t["BgBottom"], op))
    extra = "backdrop-filter:blur(22px) saturate(1.25);-webkit-backdrop-filter:blur(22px) saturate(1.25);" if blur else ""
    shadow = "box-shadow:0 16px 34px rgba(0,0,0,.55);" if truth(t["Shadow"]) else ""

    out = ['<div class="w" style="--acc:%s;--acc2:%s;--txt:%s;--lab:%s;--det:%s;--trk:%s;'
           'background:%s;border:%spx solid %s;border-radius:%spx;%s%sfont-size:%.1fpx">'
           % (acc, acc2, txt, lab, det, trk, bg, bt, bd, rad, extra, shadow, 13 * scale)]

    if show_header:
        dot = ('<i class="dot" style="background:%s;%s"></i>' % (acc, "box-shadow:0 0 9px %s;" % acc if glow else ""))
        out.append('<div class="hd">%s<span class="bn">LIKASYS</span>'
                   '<span class="hb"></span><span class="hb"></span></div>' % dot)

    for name, kind, val, pct, detail in rows:
        ic = ""
        if icons != "None":
            style = ""
            if icons == "Solid":
                style = 'fill="%s" stroke="none"' % acc
            elif icons == "Outline":
                style = 'fill="none" stroke="%s" stroke-width="1.4"' % acc
            else:  # ThreeD
                style = 'fill="none" stroke="%s" stroke-width="1.7"' % acc
            shade = ' filter:drop-shadow(0 0 5px %s);' % rgba(t["Accent"], .55) if glow else ""
            ic = ('<svg class="ic" viewBox="0 0 20 20" style="%s" %s stroke-linecap="round" '
                  'stroke-linejoin="round">%s</svg>' % (shade, style, ICONS[kind]))
        bar = ""
        if bars:
            bar = ('<div class="bar"><i style="width:%d%%;background:linear-gradient(90deg,%s,%s)"></i></div>'
                   % (int(pct * 100), acc, acc2))
        out.append('<div class="row">%s<div class="mid"><div class="l1">'
                   '<span class="lb">%s</span><span class="vl">%s</span></div>%s'
                   '<div class="dt">%s</div></div></div>'
                   % (ic, name, val, bar, html.escape(detail)))

    if footer:
        out.append('<div class="ft"><span>LIKAsys, Made in Kosovo with '
                   '<span style="color:#FF4D6A">\u2665</span></span>'
                   '<span class="site">Likaapps.com</span></div>')
    out.append("</div>")
    return "".join(out)


groups = []
for t in presets:
    if t["Group"] not in groups:
        groups.append(t["Group"])

GROUP_NOTE = {
    "Qelq": "me blur akrilik \u2014 sfondi pas widget-it turbullohet",
    "Thjeshta": "pa zhurm\u00eb, vet\u00ebm ajo q\u00eb duhet",
    "Klasike": "paletat e njohura t\u00eb programuesve",
    "Gaming": "ngjyra t\u00eb forta p\u00ebr set-up gaming",
    "Special": "t\u00eb bera p\u00ebr Kosov\u00ebn",
    "Drite": "p\u00ebr desktop t\u00eb ndrit\u00ebsh\u00ebm",
}

hero = next(t for t in presets if t["Name"] == "Midnight Glass")
kosova = next(t for t in presets if t["Name"] == "Kosova")
cyber = next(t for t in presets if t["Name"] == "Cyberpunk")

parts = []
TPL = """<!DOCTYPE html>
<html lang="sq"><head><meta charset="utf-8" />
<title>LIKAsys v@@VER@@ &mdash; pamja</title>
<style>
*{box-sizing:border-box;margin:0;padding:0}
body{font-family:"Segoe UI",system-ui,-apple-system,Roboto,Arial,sans-serif;
  background:#05070b;color:#EAF2FF;padding:30px 24px 70px}
.top{display:flex;align-items:center;gap:14px;flex-wrap:wrap}
.logo{width:42px;height:42px;border-radius:12px;display:grid;place-items:center;
  background:linear-gradient(145deg,#16203200,#0b0f18);border:1px solid #27374f;
  box-shadow:0 0 0 3px rgba(0,229,255,.1)}
h1{font-size:23px;font-weight:700}
.ver{font-size:12px;color:#00E5FF;border:1px solid rgba(0,229,255,.35);
  background:rgba(0,229,255,.08);border-radius:20px;padding:4px 12px}
.sub{color:#6B7E96;font-size:13px;margin-top:6px}
h2{font-size:14px;letter-spacing:1.6px;color:#7F93AD;font-weight:700;margin:42px 0 4px}
.note{color:#54657C;font-size:12px;margin-bottom:16px}

/* ---------- desktop mock ---------- */
.desk{position:relative;margin-top:24px;height:430px;border-radius:18px;overflow:hidden;
  border:1px solid #1b2535;
  background:
    radial-gradient(900px 430px at 16% 10%,#17344f 0%,transparent 60%),
    radial-gradient(760px 520px at 88% 90%,#2d1745 0%,transparent 62%),
    linear-gradient(160deg,#0a111c 0%,#070a11 55%,#0b0f18 100%)}
.desk::after{content:"";position:absolute;inset:0;opacity:.5;
  background:repeating-linear-gradient(115deg,rgba(255,255,255,.015) 0 2px,transparent 2px 9px)}
.bar{z-index:3}
.deskbar{position:absolute;left:0;right:0;bottom:0;height:40px;background:rgba(8,11,17,.88);
  border-top:1px solid #182130;display:flex;align-items:center;gap:14px;padding:0 18px;z-index:4}
.tray{margin-left:auto;display:flex;align-items:center;gap:12px;color:#7f91a8;font-size:11px}
.trayicon{width:18px;height:18px;border-radius:5px;display:grid;place-items:center;
  background:linear-gradient(145deg,#141c2a,#0a0e16);border:1px solid #2a3850;
  box-shadow:0 0 0 2px rgba(0,229,255,.16);color:#00E5FF;font-size:8px;font-weight:700}
.pin{position:absolute;z-index:3}

/* ---------- widget ---------- */
.w{width:244px;padding:11px 13px 9px;color:var(--txt);line-height:1.25;position:relative}
.w .hd{display:flex;align-items:center;gap:7px;margin-bottom:9px}
.w .dot{width:7px;height:7px;border-radius:50%;display:block}
.w .bn{font-size:.78em;letter-spacing:2.1px;font-weight:700;color:var(--lab)}
.w .hb{width:11px;height:11px;border-radius:3px;background:rgba(255,255,255,.07);margin-left:auto}
.w .hb+.hb{margin-left:4px}
.w .row{display:flex;align-items:center;gap:9px;padding:3px 0}
.w .ic{width:1.45em;height:1.45em;flex:none}
.w .mid{flex:1;min-width:0}
.w .l1{display:flex;align-items:baseline;gap:8px}
.w .lb{font-size:.74em;letter-spacing:1.3px;font-weight:600;color:var(--lab)}
.w .vl{margin-left:auto;font-size:1.1em;font-weight:700;font-variant-numeric:tabular-nums}
.w .bar{height:3px;border-radius:2px;background:var(--trk);margin-top:4px;overflow:hidden}
.w .bar i{display:block;height:100%;border-radius:2px}
.w .dt{font-size:.66em;color:var(--det);margin-top:3px}
.w .ft{display:flex;gap:8px;margin-top:9px;padding-top:7px;font-size:.62em;color:var(--det);
  border-top:1px solid rgba(255,255,255,.07)}
.w .site{margin-left:auto;color:var(--acc)}

/* ---------- gallery ---------- */
.gal{display:flex;flex-wrap:wrap;gap:18px}
.cell{width:244px}
.cap{display:flex;align-items:center;gap:7px;margin-top:9px;font-size:11.5px;color:#90A3BA}
.cap b{font-weight:600;color:#C8D6E8}
.tag{margin-left:auto;font-size:9px;letter-spacing:.6px;color:#00E5FF;
  border:1px solid rgba(0,229,255,.3);border-radius:10px;padding:1px 7px}
.feat{display:flex;flex-wrap:wrap;gap:12px;margin-top:6px}
.f{flex:1 1 260px;background:#0b1018;border:1px solid #18202e;border-radius:12px;padding:14px 16px}
.f b{display:block;font-size:12.5px;margin-bottom:5px;color:#DCE8F7}
.f span{font-size:11.5px;color:#6E8098;line-height:1.5}
footer{margin-top:46px;color:#48596F;font-size:11.5px;text-align:center}
</style></head><body>

<div class="top">
  <div class="logo"><svg width="22" height="22" viewBox="0 0 20 20" fill="none" stroke="#00E5FF"
    stroke-width="1.7" stroke-linecap="round" stroke-linejoin="round">
    <rect x="4.5" y="4.5" width="11" height="11" rx="2"/><rect x="7.5" y="7.5" width="5" height="5" rx="1"/>
    <path d="M8 1.6v2.9M12 1.6v2.9M8 15.5v2.9M12 15.5v2.9M1.6 8h2.9M1.6 12h2.9M15.5 8h2.9M15.5 12h2.9"/></svg></div>
  <div><h1>LIKAsys</h1><div class="sub">CPU &middot; GPU &middot; VRAM &middot; RAM &middot; FPS \u2014 n\u00eb cep t\u00eb ekranit, gjithmon\u00eb sip\u00ebr</div></div>
  <span class="ver">v@@VER@@</span>
</div>

<div class="desk">
  <div class="pin" style="top:26px;right:26px">@@HERO@@</div>
  <div class="pin" style="bottom:72px;left:26px">@@CYBER@@</div>
  <div class="deskbar">
    <div style="width:16px;height:16px;border-radius:4px;background:#1b2636"></div>
    <div style="width:16px;height:16px;border-radius:4px;background:#1b2636"></div>
    <div class="tray"><div class="trayicon">LS</div>
      <span>LIKAsys \u2014 kliko dy her\u00eb p\u00ebr ta fshehur</span>
      <span style="color:#4d5d72">14:08</span></div>
  </div>
</div>
"""

parts.append(TPL.replace("@@VER@@", VERSION)
                .replace("@@HERO@@", widget(hero))
                .replace("@@CYBER@@", widget(cyber, rows=[ROWS[0], ROWS[1], ROWS[4]], footer=False)))

parts.append('<h2>ÇKA \u00cbSHT\u00cb E RE N\u00cb v1.1.0</h2><div class="feat">')
for b, s in [
    ("31 tema t\u00eb gatshme", "Nj\u00eb klik dhe gjith\u00e7ka ndryshon: qelq, gaming, klasike, Kosova, t\u00eb ndritshme."),
    ("Blur i v\u00ebrtet\u00eb (akrilik)", "Sfondi pas widget-it turbullohet si n\u00eb Windows 11, me cepa t\u00eb sakt\u00eb."),
    ("11 ngjyra t\u00eb ndara", "HEX ose paleta e plot\u00eb e Windows-it p\u00ebr secil\u00ebn pjes\u00eb t\u00eb widget-it."),
    ("Cil\u00ebsime maksimale", "8 skeda: pamja, fonti, shiritat, ikonat, pozicioni, metrikat, sistemi."),
    ("Metrika t\u00eb reja", "GHz i CPU-s\u00eb, emri i loj\u00ebs te FPS, Celsius ose Fahrenheit."),
    ("Shfaqe vet\u00ebm n\u00eb loj\u00eb", "Widget-i fshihet n\u00eb desktop dhe shfaqet vet\u00ebm kur luan."),
]:
    parts.append('<div class="f"><b>%s</b><span>%s</span></div>' % (b, s))
parts.append("</div>")

for g in groups:
    items = [t for t in presets if t["Group"] == g]
    parts.append('<h2>%s &mdash; %d tema</h2><div class="note">%s</div><div class="gal">'
                 % (g.upper(), len(items), GROUP_NOTE.get(g, "")))
    for t in items:
        rows = [ROWS[0], ROWS[1], ROWS[3], ROWS[4]]
        parts.append('<div class="cell">%s<div class="cap"><b>%s</b>%s</div></div>'
                     % (widget(t, rows=rows),
                        html.escape(t["Name"]),
                        '<span class="tag">QELQ</span>' if truth(t["Blur"]) else ""))
    parts.append("</div>")

parts.append('<footer>LIKAsys v%s \u2014 Made in Kosovo with \u2665 \u2014 Likaapps.com<br>'
             'Kjo faqe gjenerohet nga ThemeLibrary.cs, prandaj ngjyrat jan\u00eb saktaz\u00eb si n\u00eb aplikacion.</footer>'
             '</body></html>' % VERSION)

os.makedirs(os.path.dirname(OUT), exist_ok=True)
open(OUT, "w", encoding="utf-8").write("\n".join(parts))
print("preview -> %s  (%d tema)" % (OUT, len(presets)))
