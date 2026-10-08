#!/usr/bin/env python3
"""Generates LIKAsys branding assets: app icon (.ico) + NSIS installer bitmaps."""
import os
from PIL import Image, ImageDraw, ImageFilter

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
ASSETS = os.path.join(ROOT, "src", "LIKAsys", "assets")
INST = os.path.join(ROOT, "installer")
os.makedirs(ASSETS, exist_ok=True)
os.makedirs(INST, exist_ok=True)

CYAN = (0, 229, 255)
VIOLET = (124, 77, 255)
BG_TOP = (18, 24, 38)
BG_BOT = (8, 11, 18)


def lerp(a, b, t):
    return tuple(int(round(a[i] + (b[i] - a[i]) * t)) for i in range(3))


def vgrad(size, top, bot):
    w, h = size
    img = Image.new("RGB", size)
    d = ImageDraw.Draw(img)
    for y in range(h):
        d.line([(0, y), (w, y)], fill=lerp(top, bot, y / max(1, h - 1)))
    return img


def dgrad(size, c1, c2):
    """diagonal gradient"""
    w, h = size
    img = Image.new("RGB", size)
    d = ImageDraw.Draw(img)
    for y in range(h):
        for x in range(0, w, 4):
            t = (x / w * 0.6 + y / h * 0.4)
            d.rectangle([x, y, x + 4, y], fill=lerp(c1, c2, min(1.0, t)))
    return img


def rounded_mask(size, radius, scale=1):
    m = Image.new("L", (size[0] * scale, size[1] * scale), 0)
    ImageDraw.Draw(m).rounded_rectangle(
        [0, 0, size[0] * scale - 1, size[1] * scale - 1], radius=radius * scale, fill=255)
    return m.resize(size, Image.LANCZOS)


def build_icon(px=1024):
    S = px
    img = Image.new("RGBA", (S, S), (0, 0, 0, 0))

    # --- body ---
    body = vgrad((S, S), BG_TOP, BG_BOT).convert("RGBA")
    mask = rounded_mask((S, S), int(S * 0.22), scale=2)
    img.paste(body, (0, 0), mask)

    # --- outer gradient ring (3D outline look) ---
    ring = dgrad((S, S), CYAN, VIOLET).convert("RGBA")
    inset = int(S * 0.035)
    thickness = max(2, int(S * 0.030))
    rm = Image.new("L", (S, S), 0)
    rd = ImageDraw.Draw(rm)
    rd.rounded_rectangle([inset, inset, S - inset, S - inset],
                         radius=int(S * 0.195), outline=255, width=thickness)
    img.paste(ring, (0, 0), rm)

    # --- top glass highlight ---
    hl = Image.new("L", (S, S), 0)
    ImageDraw.Draw(hl).ellipse([-int(S * 0.35), -int(S * 0.75), int(S * 1.35), int(S * 0.42)], fill=42)
    hl = hl.filter(ImageFilter.GaussianBlur(S * 0.03))
    hl = Image.composite(hl, Image.new("L", (S, S), 0), mask)
    img.paste(Image.new("RGBA", (S, S), (255, 255, 255, 255)), (0, 0), hl)

    # --- glow behind the monogram ---
    glow = Image.new("RGBA", (S, S), (0, 0, 0, 0))
    gd = ImageDraw.Draw(glow)
    gd.ellipse([int(S * .18), int(S * .18), int(S * .82), int(S * .82)], fill=CYAN + (70,))
    glow = glow.filter(ImageFilter.GaussianBlur(S * 0.09))
    img.alpha_composite(Image.composite(glow, Image.new("RGBA", (S, S), (0, 0, 0, 0)), mask))

    # --- "L" monogram (geometric, crisp at small sizes) ---
    lm = Image.new("L", (S, S), 0)
    ld = ImageDraw.Draw(lm)
    x0, y0 = int(S * 0.285), int(S * 0.235)
    bar = int(S * 0.112)
    bottom = int(S * 0.66)
    right = int(S * 0.70)
    r = int(bar * 0.34)
    ld.rounded_rectangle([x0, y0, x0 + bar, bottom], radius=r, fill=255)           # vertical
    ld.rounded_rectangle([x0, bottom - bar, right, bottom], radius=r, fill=255)    # foot
    mono = dgrad((S, S), (210, 255, 255), CYAN).convert("RGBA")
    img.paste(mono, (0, 0), lm)

    # --- heartbeat / performance pulse line ---
    pm = Image.new("L", (S, S), 0)
    pd = ImageDraw.Draw(pm)
    yb = int(S * 0.785)
    a = int(S * 0.055)
    pts = [(int(S * 0.20), yb), (int(S * 0.34), yb), (int(S * 0.41), yb - a),
           (int(S * 0.50), yb + int(a * 0.85)), (int(S * 0.58), yb - int(a * 1.5)),
           (int(S * 0.66), yb), (int(S * 0.80), yb)]
    pd.line(pts, fill=255, width=int(S * 0.036), joint="curve")
    pulse = dgrad((S, S), CYAN, VIOLET).convert("RGBA")
    pglow = Image.new("RGBA", (S, S), (0, 0, 0, 0))
    pglow.paste(pulse, (0, 0), pm)
    img.alpha_composite(pglow.filter(ImageFilter.GaussianBlur(S * 0.016)))
    img.paste(pulse, (0, 0), pm)

    return img


def main():
    big = build_icon(1024)
    big.resize((512, 512), Image.LANCZOS).save(os.path.join(ASSETS, "logo.png"))
    sizes = [256, 128, 96, 64, 48, 40, 32, 24, 20, 16]
    frames = [big.resize((s, s), Image.LANCZOS) for s in sizes]
    frames[0].save(os.path.join(ASSETS, "LIKAsys.ico"), format="ICO",
                   sizes=[(s, s) for s in sizes], append_images=frames[1:])

    # ---------- NSIS MUI bitmaps (BMP3, no alpha) ----------
    # Welcome / finish page: 164 x 314
    w, h = 164, 314
    wel = vgrad((w, h), (14, 19, 30), (6, 9, 15))
    d = ImageDraw.Draw(wel)
    for i in range(10):
        y = 20 + i * 30
        d.line([(0, y), (w, y - 40)], fill=(16, 26, 42))
    logo = big.resize((108, 108), Image.LANCZOS)
    wel.paste(logo, (28, 46), logo)
    d.line([(24, 182), (140, 182)], fill=CYAN, width=1)
    d.text((28, 196), "LIKAsys", fill=(235, 245, 255))
    d.text((28, 212), "CPU - GPU - RAM - FPS", fill=(110, 130, 150))
    d.text((28, 278), "Made in Kosovo", fill=(0, 190, 215))
    d.text((28, 290), "Likaapps.com", fill=(90, 110, 130))
    wel.save(os.path.join(INST, "wizard.bmp"), format="BMP")

    # Header: 150 x 57
    w, h = 150, 57
    hdr = vgrad((w, h), (16, 22, 34), (10, 14, 22))
    hd = ImageDraw.Draw(hdr)
    lg = big.resize((40, 40), Image.LANCZOS)
    hdr.paste(lg, (8, 8), lg)
    hd.text((56, 16), "LIKAsys", fill=(235, 245, 255))
    hd.text((56, 30), "system widget", fill=(0, 190, 215))
    hdr.save(os.path.join(INST, "header.bmp"), format="BMP")

    print("branding written ->", ASSETS, INST)


if __name__ == "__main__":
    main()
