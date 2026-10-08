#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
LIKAsys - gjeneron kursoret e mouse-it per profilin IT.

Kater stile, secili me kater kursore (shigjeta, teksti, linku, shenjestra).
Cdo .cur mban pese madhesi (32/48/64/96/128) qe te jete i qarte edhe ne 4K.
Dalja:  src/LIKAsys/assets/cursors/<stili>/{arrow,ibeam,link,cross}.cur + preview.png
"""
import os, struct
from PIL import Image, ImageDraw, ImageFilter

HERE  = os.path.dirname(os.path.abspath(__file__))
OUT   = os.path.join(HERE, "..", "src", "LIKAsys", "assets", "cursors")
SIZES = (128, 96, 64, 48, 32)
C     = 512                      # canvas i punes
K     = C / 32.0                 # njesite e dizajnit jane ne rrjet 32 px

def u(v):  return v * K          # design units -> canvas px

# --------------------------------------------------------------- gjeometria
ARROW = [(0,0),(0,21.5),(5.4,16.6),(9.0,25.3),(12.6,23.6),(9.1,15.4),(16.0,15.4)]
SLIM  = [(0,0),(0,19.6),(4.7,15.3),(7.9,23.0),(10.9,21.7),(7.8,14.3),(13.8,14.3)]

def place(pts, ox, oy):
    return [(u(x)+u(ox), u(y)+u(oy)) for x, y in pts]

def ring(cx, cy, r):
    return [u(cx-r), u(cy-r), u(cx+r), u(cy+r)]

def ibeam_pts(cx, cy, hw, hh, cap):
    """Shkronja I: trup vertikal me dy kapake."""
    return [
        (cx-hw, cy-hh), (cx+hw, cy-hh), (cx+hw, cy-hh+cap), (cx+cap*0.55, cy-hh+cap),
        (cx+cap*0.55, cy+hh-cap), (cx+hw, cy+hh-cap), (cx+hw, cy+hh), (cx-hw, cy+hh),
        (cx-hw, cy+hh-cap), (cx-cap*0.55, cy+hh-cap), (cx-cap*0.55, cy-hh+cap),
        (cx-hw, cy-hh+cap),
    ]

def new(): return Image.new("RGBA", (C, C), (0,0,0,0))

def stroke(d, pts, col, w, close=True):
    p = list(pts) + ([pts[0]] if close else [])
    d.line(p, fill=col, width=int(round(u(w))), joint="curve")

def glow(shape_img, col, radius, strength=1.0):
    """Perhap driten e nje siluete."""
    a = shape_img.split()[3].filter(ImageFilter.GaussianBlur(u(radius)))
    g = Image.new("RGBA", (C, C), col + (0,))
    g.putalpha(a.point(lambda v: int(min(255, v * strength))))
    return g

def over(base, top): return Image.alpha_composite(base, top)

def shadow(shape_img, radius=0.9, opacity=140, dx=0.0, dy=0.35):
    a = shape_img.split()[3].filter(ImageFilter.GaussianBlur(u(radius)))
    s = Image.new("RGBA", (C, C), (0,0,0,0)); s.putalpha(a.point(lambda v: v*opacity//255))
    return s.transform((C,C), Image.AFFINE, (1,0,-u(dx),0,1,-u(dy)))

def grad(poly_img, top, bot):
    """Mbush siluete me gradient vertikal."""
    g = Image.new("RGBA", (C, C))
    dg = ImageDraw.Draw(g)
    for y in range(C):
        t = y / (C-1.0)
        dg.line([(0,y),(C,y)], fill=tuple(int(top[i]+(bot[i]-top[i])*t) for i in range(3)) + (255,))
    g.putalpha(poly_img.split()[3])
    return g

# =============================================================== 1. NEON
# Xham i erret me buze cian qe ndricon. Shkon me "Dark Glass + neon".
CY  = (0x00, 0xE5, 0xFF)
CY2 = (0x7A, 0xF4, 0xFF)

def neon(kind):
    img = new()
    sil = new(); ds = ImageDraw.Draw(sil)
    d   = ImageDraw.Draw(img)

    if kind == "arrow" or kind == "link":
        pts = place(ARROW, 2.2, 1.6)
        ds.polygon(pts, fill=(255,255,255,255))
        img = over(img, glow(sil, CY, 1.5, 0.75))
        img = over(img, glow(sil, CY, 0.5, 0.9))
        d = ImageDraw.Draw(img)
        d.polygon(pts, fill=(0x0A,0x0E,0x14,0xF0))
        stroke(d, pts, CY + (255,), 1.25)
        if kind == "link":
            d.ellipse(ring(21.5, 20.5, 4.2), outline=CY + (255,), width=int(u(1.15)))
            d.ellipse(ring(21.5, 20.5, 1.3), fill=CY2 + (255,))
        return img, (2.2/32.0, 1.6/32.0)

    if kind == "ibeam":
        pts = [(u(x), u(y)) for x, y in ibeam_pts(16, 16, 3.4, 10.5, 1.5)]
        ds.polygon(pts, fill=(255,255,255,255))
        img = over(img, glow(sil, CY, 1.3, 0.8))
        d = ImageDraw.Draw(img)
        d.polygon(pts, fill=(0x0A,0x0E,0x14,0xE6))
        stroke(d, pts, CY + (255,), 1.2)
        return img, (0.5, 0.5)

    # cross
    ds.line([(u(16),u(3)),(u(16),u(29))], fill=(255,255,255,255), width=int(u(1.5)))
    ds.line([(u(3),u(16)),(u(29),u(16))], fill=(255,255,255,255), width=int(u(1.5)))
    img = over(img, glow(sil, CY, 1.2, 0.85))
    d = ImageDraw.Draw(img)
    for a, b in (((16,3),(16,12.3)), ((16,19.7),(16,29)), ((3,16),(12.3,16)), ((19.7,16),(29,16))):
        d.line([(u(a[0]),u(a[1])),(u(b[0]),u(b[1]))], fill=CY + (255,), width=int(u(1.35)))
    d.ellipse(ring(16,16,3.1), outline=CY + (200,), width=int(u(0.9)))
    d.ellipse(ring(16,16,0.7), fill=(255,255,255,255))
    return img, (0.5, 0.5)

# =============================================================== 2. TERMINAL
# Piksel te trashe jeshil fosfori, si konsola e vjeter.
GR  = (0x3C, 0xFF, 0x8A)
GR2 = (0x0B, 0x6B, 0x3A)

# Shigjeta klasike e piksel-artit, 16 rreshta. E shkruar me dore qe forma te mos
# prishet nga kuantizimi automatik.
PIX_ARROW = [
    "X...............",
    "XX..............",
    "XXX.............",
    "XXXX............",
    "XXXXX...........",
    "XXXXXX..........",
    "XXXXXXX.........",
    "XXXXXXXX........",
    "XXXXXXXXX.......",
    "XXXXXXXXXX......",
    "XXXXXXXXXXX.....",
    "XXXXXX..........",
    "XXX.XXX.........",
    "XX..XXX.........",
    "X....XXX........",
    ".....XXX........",
]
CELL, PX0, PY0 = 1.62, 2.0, 1.3      # njesi dizajni per piksel

def stamp(d, col, row, cols=1, rows=1, cell=CELL, ox=PX0, oy=PY0, fill=255):
    d.rectangle([u(ox+col*cell), u(oy+row*cell),
                 u(ox+(col+cols)*cell), u(oy+(row+rows)*cell)], fill=fill)

def pixmap(rows, **kw):
    img = Image.new("L", (C, C), 0); d = ImageDraw.Draw(img)
    for r, line in enumerate(rows):
        for c, ch in enumerate(line):
            if ch == "X": stamp(d, c, r, **kw)
    return img

def terminal(kind):
    if kind in ("arrow", "link"):
        px = pixmap(PIX_ARROW)
        hot = (PX0/32.0, PY0/32.0)
        if kind == "link":
            d = ImageDraw.Draw(px)
            for i in range(3):
                stamp(d, 12 + i*1.6, 12.6, 1, 1)
    elif kind == "ibeam":
        # blloku i konsoles, jo shkronja I
        px = Image.new("L", (C, C), 0); d = ImageDraw.Draw(px)
        stamp(d, 0, 0, 6, 12, cell=1.62, ox=11.1, oy=6.3)
    else:
        rows = []
        for r in range(16):
            line = ""
            for c in range(16):
                v = (7 <= c <= 8 and not 7 <= r <= 8) or (7 <= r <= 8 and not 7 <= c <= 8)
                line += "X" if v else "."
            rows.append(line)
        px = pixmap(rows, cell=1.72, ox=2.2, oy=2.2)
    if kind == "ibeam" or kind == "cross": hot = (0.5, 0.5)
    sil = Image.new("RGBA", (C, C), GR + (0,)); sil.putalpha(px)

    img = new()
    img = over(img, glow(sil, GR, 1.8, 0.55))
    img = over(img, glow(sil, GR, 0.6, 0.8))

    # trupi: jeshile me buze te erret, dhe vija skanimi
    body = Image.new("RGBA", (C, C), GR + (255,))
    body.putalpha(px)
    scan = Image.new("RGBA", (C, C), (0,0,0,0)); dsc = ImageDraw.Draw(scan)
    step = int(u(1.8))
    for y in range(0, C, max(2, step)):
        dsc.line([(0,y),(C,y)], fill=(0,0,0,70), width=max(1, step//3))
    scan.putalpha(Image.composite(scan.split()[3], Image.new("L",(C,C),0), px.point(lambda v: 255 if v>0 else 0)))
    body = over(body, scan)

    edge = px.filter(ImageFilter.FIND_EDGES).point(lambda v: 255 if v > 40 else 0)
    ed = Image.new("RGBA", (C, C), GR2 + (0,)); ed.putalpha(edge.point(lambda v: v*200//255))

    img = over(img, body)
    img = over(img, ed)

    return img, hot

# =============================================================== 3. CARBON
# Grafit i ngurte me nje fije te bardhe rreth e rrotull. Pa drite, vetem hije.
def carbon(kind):
    img = new()
    sh  = new(); dsh = ImageDraw.Draw(sh)
    d   = ImageDraw.Draw(img)

    if kind in ("arrow", "link"):
        pts = place(SLIM, 2.0, 1.5)
        dsh.polygon(pts, fill=(255,255,255,255))
        img = over(img, shadow(sh, 1.0, 150, 0, 0.45))
        fill = new(); ImageDraw.Draw(fill).polygon(pts, fill=(255,255,255,255))
        img = over(img, grad(fill, (0x7C,0x85,0x95), (0x1A,0x1D,0x23)))
        d = ImageDraw.Draw(img)
        stroke(d, pts, (0xFF,0xFF,0xFF,0xF5), 1.15)
        if kind == "link":
            d.ellipse(ring(18.6,18.4,3.9), outline=(0xFF,0xFF,0xFF,0xF2), width=int(u(1.1)))
            d.ellipse(ring(18.6,18.4,1.9), fill=(0x1A,0x1D,0x22,0xFF))
        return img, (2.0/32.0, 1.5/32.0)

    if kind == "ibeam":
        pts = [(u(x), u(y)) for x, y in ibeam_pts(16, 16, 3.1, 10.2, 1.35)]
        dsh.polygon(pts, fill=(255,255,255,255))
        img = over(img, shadow(sh, 0.9, 150, 0, 0.4))
        fill = new(); ImageDraw.Draw(fill).polygon(pts, fill=(255,255,255,255))
        img = over(img, grad(fill, (0x7C,0x85,0x95), (0x1A,0x1D,0x23)))
        ImageDraw.Draw(img).line([(u(x),u(y)) for x,y in pts] + [(u(pts[0][0]/K),u(pts[0][1]/K))][:0],
                                 fill=(0xFF,0xFF,0xFF,0xF2), width=int(u(1.1)), joint="curve")
        stroke(ImageDraw.Draw(img), pts, (0xFF,0xFF,0xFF,0xF2), 1.1)
        return img, (0.5, 0.5)

    bar = new(); dbar = ImageDraw.Draw(bar)
    dbar.rounded_rectangle([u(14.9),u(3.4),u(17.1),u(28.6)], radius=u(1.1), fill=(255,255,255,255))
    dbar.rounded_rectangle([u(3.4),u(14.9),u(28.6),u(17.1)], radius=u(1.1), fill=(255,255,255,255))
    img = over(img, shadow(bar, 0.9, 150, 0, 0.4))
    img = over(img, grad(bar, (0x7C,0x85,0x95), (0x1A,0x1D,0x23)))
    d = ImageDraw.Draw(img)
    d.rounded_rectangle([u(14.9),u(3.4),u(17.1),u(28.6)], radius=u(1.1), outline=(0xFF,0xFF,0xFF,0xF2), width=int(u(1.0)))
    d.rounded_rectangle([u(3.4),u(14.9),u(28.6),u(17.1)], radius=u(1.1), outline=(0xFF,0xFF,0xFF,0xF2), width=int(u(1.0)))
    d.ellipse(ring(16,16,1.5), fill=(0x12,0x14,0x18,0xFF), outline=(0xFF,0xFF,0xFF,0xF2), width=int(u(1.0)))
    return img, (0.5, 0.5)

# =============================================================== 4. BLUEPRINT
# Vetem vija, si vizatim teknik. Brendia gati bosh, me shenja matjeje.
BP  = (0xDC, 0xEB, 0xFF)
BP2 = (0x2E, 0x9BFF >> 8 & 0xFF, 0xFF)
BP2 = (0x2E, 0x9B, 0xFF)

def hatch(pts):
    """Vija diagonale brenda formes, si ne vizatimet teknike."""
    lay = Image.new("RGBA", (C, C), (0,0,0,0)); dl = ImageDraw.Draw(lay)
    step = u(2.3)
    x = -C
    while x < C*2:
        dl.line([(x, C), (x + C, 0)], fill=BP2 + (120,), width=int(u(0.45)))
        x += step
    m = Image.new("L", (C, C), 0); ImageDraw.Draw(m).polygon(pts, fill=255)
    m = m.filter(ImageFilter.MinFilter(9))
    lay.putalpha(Image.composite(lay.split()[3], Image.new("L", (C, C), 0), m))
    return lay

def blueprint(kind):
    img = new()
    sh  = new()
    d   = ImageDraw.Draw(img)

    if kind in ("arrow", "link"):
        pts = place(ARROW, 2.4, 1.8)
        ImageDraw.Draw(sh).polygon(pts, fill=(255,255,255,255))
        img = over(img, shadow(sh, 1.1, 120, 0, 0.3))
        d = ImageDraw.Draw(img)
        d.polygon(pts, fill=(0x0C,0x1A,0x2E,0x7A))
        img = over(img, hatch(pts))
        d = ImageDraw.Draw(img)
        stroke(d, pts, BP + (255,), 1.1)
        if kind == "link":
            d.ellipse(ring(21.8,20.8,4.4), outline=BP2 + (255,), width=int(u(1.0)))
            d.line([(u(21.8),u(18.4)),(u(21.8),u(23.2))], fill=BP + (255,), width=int(u(0.8)))
            d.line([(u(19.4),u(20.8)),(u(24.2),u(20.8))], fill=BP + (255,), width=int(u(0.8)))
        return img, (2.4/32.0, 1.8/32.0)

    if kind == "ibeam":
        pts = [(u(x), u(y)) for x, y in ibeam_pts(16, 16, 3.6, 10.6, 1.5)]
        ImageDraw.Draw(sh).polygon(pts, fill=(255,255,255,255))
        img = over(img, shadow(sh, 1.0, 120, 0, 0.3))
        d = ImageDraw.Draw(img)
        d.polygon(pts, fill=(0x0C,0x1A,0x2E,0x66))
        stroke(d, pts, BP + (255,), 1.05)
        d.line([(u(16),u(11.0)),(u(16),u(21.0))], fill=BP2 + (200,), width=int(u(0.7)))
        return img, (0.5, 0.5)

    d.ellipse(ring(16,16,10.6), outline=BP2 + (190,), width=int(u(0.85)))
    d.ellipse(ring(16,16,5.2),  outline=BP + (235,), width=int(u(0.9)))
    for a, b in (((16,1.6),(16,10.0)), ((16,22.0),(16,30.4)), ((1.6,16),(10.0,16)), ((22.0,16),(30.4,16))):
        d.line([(u(a[0]),u(a[1])),(u(b[0]),u(b[1]))], fill=BP + (255,), width=int(u(1.0)))
    for a, b in (((16,4.6),(16,7.2)), ((16,24.8),(16,27.4)), ((4.6,16),(7.2,16)), ((24.8,16),(27.4,16))):
        d.line([(u(a[0]),u(a[1])),(u(b[0]),u(b[1]))], fill=BP2 + (255,), width=int(u(2.4)))
    d.ellipse(ring(16,16,0.85), fill=BP + (255,))
    return img, (0.5, 0.5)

# =============================================================== shkrimi i .cur
def write_cur(path, draw_fn, kind):
    img, hot = draw_fn(kind)
    blobs, entries = [], []
    for s in SIZES:
        frame = img.resize((s, s), Image.LANCZOS)
        px = frame.load()
        xor = bytearray()
        for y in range(s-1, -1, -1):
            for x in range(s):
                r, g, b, a = px[x, y]
                xor += bytes((b, g, r, a))
        stride = ((s + 31) // 32) * 4
        andmask = bytes(stride * s)
        bih = struct.pack("<IiiHHIIiiII", 40, s, s*2, 1, 32, 0, len(xor)+len(andmask), 0, 0, 0, 0)
        blobs.append(bih + bytes(xor) + andmask)
        entries.append((s, max(0, min(s-1, int(round(hot[0]*s)))), max(0, min(s-1, int(round(hot[1]*s))))))

    head = struct.pack("<HHH", 0, 2, len(blobs))
    off = 6 + 16*len(blobs)
    dirs = b""
    for (s, hx, hy), blob in zip(entries, blobs):
        dirs += struct.pack("<BBBBHHII", s if s < 256 else 0, s if s < 256 else 0, 0, 0, hx, hy, len(blob), off)
        off += len(blob)
    with open(path, "wb") as f:
        f.write(head + dirs + b"".join(blobs))
    return img

STYLES = [("neon", neon), ("terminal", terminal), ("carbon", carbon), ("blueprint", blueprint)]
KINDS  = ("arrow", "ibeam", "link", "cross")

def main():
    for name, fn in STYLES:
        folder = os.path.join(OUT, name)
        os.makedirs(folder, exist_ok=True)
        shots = []
        for kind in KINDS:
            shots.append(write_cur(os.path.join(folder, kind + ".cur"), fn, kind))
        # preview per cilesimet dhe faqen: kater kursoret ne rresht
        cell, pad = 54, 6
        pv = Image.new("RGBA", (cell*4 + pad*3, cell), (0,0,0,0))
        for i, s in enumerate(shots):
            pv.paste(s.resize((cell, cell), Image.LANCZOS), (i*(cell+pad), 0))
        pv.save(os.path.join(folder, "preview.png"))
        print("  " + name + " -> 4 kursore + preview")
    print("kursoret ok -> " + os.path.normpath(OUT))

if __name__ == "__main__":
    main()
