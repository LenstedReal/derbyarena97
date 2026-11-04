#!/usr/bin/env python3
"""
DERBY ARENA 4.0 UI forge: 9-slice panel/button sprites, a tileable carbon pattern and ~40 white icons for the
code-built UI (UiKit loads them from Resources/Derby/UI). Everything is drawn 4x supersampled with Pillow and
downsampled, white with alpha so Image.color tints it. Usage: uiforge.py [out_dir]
"""
import math, os, sys
from PIL import Image, ImageDraw, ImageFilter, ImageChops

SS = 4  # supersample

def canvas(w, h):
    return Image.new('RGBA', (w * SS, h * SS), (0, 0, 0, 0))

def down(img, w, h):
    return img.resize((w, h), Image.LANCZOS)

def S(v):
    return v * SS

# ----------------------------------------------------------------------------- 9-slice sprites
def rounded(w, h, radius, fill=255, alpha=255, border=0, border_alpha=0, gradient=None, gloss=0.0, shadow=0):
    """White rounded rect. gradient=(top, bottom) luminance 0..1; gloss adds a brighter top half; shadow darkens
    the bottom band; border draws a ring of the given luminance/alpha outside the fill."""
    img = canvas(w, h)
    d = ImageDraw.Draw(img)
    box = (S(0), S(0), S(w) - 1, S(h) - 1)
    d.rounded_rectangle(box, radius=S(radius), fill=(fill, fill, fill, alpha))
    if gradient is not None:
        top, bot = gradient
        grad = Image.new('L', (1, S(h)))
        for y in range(S(h)):
            t = y / max(1, S(h) - 1)
            grad.putpixel((0, y), int(255 * (top + (bot - top) * t)))
        grad = grad.resize((S(w), S(h)))
        lum = Image.merge('RGBA', (grad, grad, grad, img.split()[3]))
        img = lum
        d = ImageDraw.Draw(img)
    if gloss > 0:
        g = canvas(w, h)
        gd = ImageDraw.Draw(g)
        gd.rounded_rectangle((S(2), S(2), S(w) - S(2), S(h) * 0.48), radius=S(radius - 2), fill=(255, 255, 255, int(255 * gloss)))
        mask = Image.new('L', g.size, 0)
        ImageDraw.Draw(mask).rounded_rectangle(box, radius=S(radius), fill=255)
        g.putalpha(ImageChops.multiply(g.split()[3], mask))
        img = Image.alpha_composite(img, g)
    if shadow > 0:
        sh = canvas(w, h)
        sd = ImageDraw.Draw(sh)
        sd.rounded_rectangle((S(0), S(h) - S(shadow) - S(1), S(w), S(h)), radius=S(radius), fill=(0, 0, 0, 90))
        mask = Image.new('L', sh.size, 0)
        ImageDraw.Draw(mask).rounded_rectangle(box, radius=S(radius), fill=255)
        sh.putalpha(ImageChops.multiply(sh.split()[3], mask))
        img = Image.alpha_composite(img, sh)
    if border > 0:
        bd = ImageDraw.Draw(img)
        bd.rounded_rectangle(box, radius=S(radius), outline=(border, border, border, border_alpha), width=S(2))
    return down(img, w, h)

def ring(w, h, radius, width=2, lum=255):
    img = canvas(w, h)
    d = ImageDraw.Draw(img)
    d.rounded_rectangle((S(1), S(1), S(w) - S(1) - 1, S(h) - S(1) - 1), radius=S(radius), outline=(lum, lum, lum, 255), width=S(width))
    return down(img, w, h)

def carbon(size=128):
    """Tileable diagonal weave for side panels (used tiled, tinted dark)."""
    img = Image.new('L', (size, size), 0)
    px = img.load()
    for y in range(size):
        for x in range(size):
            u = (x + y) % 16
            v = (x - y) % 16
            a = 120 if u < 8 else 60
            if v < 2 or u < 1: a += 40
            px[x, y] = a
    out = Image.merge('RGBA', (Image.new('L', (size, size), 255), Image.new('L', (size, size), 255), Image.new('L', (size, size), 255), img))
    return out

def radial_glow(size=256):
    img = Image.new('L', (size, size), 0)
    px = img.load()
    r = size / 2
    for y in range(size):
        for x in range(size):
            d = math.hypot(x + 0.5 - r, y + 0.5 - r) / r
            px[x, y] = int(255 * max(0.0, 1 - d) ** 2.2)
    return Image.merge('RGBA', (Image.new('L', (size, size), 255),) * 3 + (img,))

def hstripe(w=256, h=16):
    """Diagonal hazard stripe strip (white/transparent) for headers."""
    img = canvas(w, h)
    d = ImageDraw.Draw(img)
    for x in range(-S(h), S(w) + S(h), S(24)):
        d.polygon([(x, S(h)), (x + S(12), S(h)), (x + S(12) + S(h), 0), (x + S(h), 0)], fill=(255, 255, 255, 255))
    return down(img, w, h)

# ----------------------------------------------------------------------------- icons (128 px, white)
def icon(draw_fn, size=128):
    img = canvas(size, size)
    d = ImageDraw.Draw(img)
    draw_fn(d, S(size))
    return down(img, size, size)

W = (255, 255, 255, 255)

def poly_star(cx, cy, r_out, r_in, n=5, rot=-math.pi / 2):
    pts = []
    for i in range(n * 2):
        r = r_out if i % 2 == 0 else r_in
        a = rot + i * math.pi / n
        pts.append((cx + math.cos(a) * r, cy + math.sin(a) * r))
    return pts

def ic_star(d, s):
    d.polygon(poly_star(s / 2, s / 2 + s * 0.03, s * 0.46, s * 0.2), fill=W)

def ic_star_outline(d, s):
    pts = poly_star(s / 2, s / 2 + s * 0.03, s * 0.46, s * 0.2)
    d.polygon(pts, outline=W, width=int(s * 0.06))

def ic_scrap(d, s):
    # gear with a bolt hole: the scrap currency
    cx = cy = s / 2
    teeth = 8
    pts = []
    for i in range(teeth * 2):
        r = s * 0.46 if i % 2 == 0 else s * 0.36
        a0 = i * math.pi / teeth
        for da in (-0.14, 0.14):
            a = a0 + da
            pts.append((cx + math.cos(a) * r, cy + math.sin(a) * r))
    d.polygon(pts, fill=W)
    d.ellipse((cx - s * 0.16, cy - s * 0.16, cx + s * 0.16, cy + s * 0.16), fill=(0, 0, 0, 0))

def ic_lock(d, s):
    d.rounded_rectangle((s * 0.22, s * 0.44, s * 0.78, s * 0.9), radius=s * 0.08, fill=W)
    d.arc((s * 0.3, s * 0.1, s * 0.7, s * 0.6), 180, 360, fill=W, width=int(s * 0.1))
    d.rectangle((s * 0.3, s * 0.34, s * 0.3 + s * 0.1, s * 0.46), fill=W)
    d.rectangle((s * 0.7 - s * 0.1, s * 0.34, s * 0.7, s * 0.46), fill=W)
    d.ellipse((s * 0.45, s * 0.58, s * 0.55, s * 0.68), fill=(0, 0, 0, 0))
    d.rectangle((s * 0.47, s * 0.63, s * 0.53, s * 0.78), fill=(0, 0, 0, 0))

def ic_trophy(d, s):
    d.rounded_rectangle((s * 0.28, s * 0.12, s * 0.72, s * 0.52), radius=s * 0.06, fill=W)
    d.arc((s * 0.1, s * 0.14, s * 0.4, s * 0.5), 90, 270, fill=W, width=int(s * 0.07))
    d.arc((s * 0.6, s * 0.14, s * 0.9, s * 0.5), 270, 90, fill=W, width=int(s * 0.07))
    d.polygon([(s * 0.28, s * 0.5), (s * 0.72, s * 0.5), (s * 0.56, s * 0.68), (s * 0.44, s * 0.68)], fill=W)
    d.rectangle((s * 0.45, s * 0.66, s * 0.55, s * 0.8), fill=W)
    d.rounded_rectangle((s * 0.3, s * 0.8, s * 0.7, s * 0.9), radius=s * 0.03, fill=W)

def ic_skull(d, s):
    d.ellipse((s * 0.2, s * 0.1, s * 0.8, s * 0.7), fill=W)
    d.rounded_rectangle((s * 0.32, s * 0.55, s * 0.68, s * 0.86), radius=s * 0.06, fill=W)
    for x in (0.37, 0.63):
        d.ellipse((s * (x - 0.09), s * 0.32, s * (x + 0.09), s * 0.5), fill=(0, 0, 0, 0))
    d.polygon([(s * 0.5, s * 0.5), (s * 0.45, s * 0.6), (s * 0.55, s * 0.6)], fill=(0, 0, 0, 0))
    for x in (0.42, 0.5, 0.58):
        d.rectangle((s * (x - 0.025), s * 0.72, s * (x + 0.025), s * 0.86), fill=(0, 0, 0, 0))

def ic_reactor(d, s):
    cx = cy = s / 2
    for ang in (0, 60, 120):
        d.ellipse((cx - s * 0.42, cy - s * 0.17, cx + s * 0.42, cy + s * 0.17), outline=W, width=int(s * 0.06))
        # rotate by drawing on a rotated copy is complex; draw three ellipses via polygon approximation
    # replace: draw rotated ellipses via polygons
def ic_reactor(d, s):
    cx = cy = s / 2
    for ang in (0, 60, 120):
        pts = []
        for i in range(72):
            t = i / 72 * 2 * math.pi
            x, y = math.cos(t) * s * 0.42, math.sin(t) * s * 0.16
            a = math.radians(ang)
            pts.append((cx + x * math.cos(a) - y * math.sin(a), cy + x * math.sin(a) + y * math.cos(a)))
        d.line(pts + [pts[0]], fill=W, width=int(s * 0.055), joint='curve')
    d.ellipse((cx - s * 0.1, cy - s * 0.1, cx + s * 0.1, cy + s * 0.1), fill=W)

def ic_flag(d, s):
    d.rectangle((s * 0.2, s * 0.1, s * 0.27, s * 0.9), fill=W)
    d.polygon([(s * 0.27, s * 0.12), (s * 0.82, s * 0.12), (s * 0.7, s * 0.33), (s * 0.82, s * 0.54), (s * 0.27, s * 0.54)], fill=W)

def ic_shield(d, s):
    d.polygon([(s * 0.5, s * 0.08), (s * 0.86, s * 0.22), (s * 0.82, s * 0.56), (s * 0.5, s * 0.92), (s * 0.18, s * 0.56), (s * 0.14, s * 0.22)], fill=W)

def ic_heart(d, s):
    d.ellipse((s * 0.12, s * 0.16, s * 0.52, s * 0.56), fill=W)
    d.ellipse((s * 0.48, s * 0.16, s * 0.88, s * 0.56), fill=W)
    d.polygon([(s * 0.14, s * 0.42), (s * 0.86, s * 0.42), (s * 0.5, s * 0.9)], fill=W)

def ic_crosshair(d, s):
    cx = cy = s / 2
    d.ellipse((cx - s * 0.36, cy - s * 0.36, cx + s * 0.36, cy + s * 0.36), outline=W, width=int(s * 0.07))
    for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
        d.line((cx + dx * s * 0.22, cy + dy * s * 0.22, cx + dx * s * 0.48, cy + dy * s * 0.48), fill=W, width=int(s * 0.07))
    d.ellipse((cx - s * 0.07, cy - s * 0.07, cx + s * 0.07, cy + s * 0.07), fill=W)

def ic_crown(d, s):
    d.polygon([(s * 0.12, s * 0.3), (s * 0.32, s * 0.52), (s * 0.5, s * 0.18), (s * 0.68, s * 0.52), (s * 0.88, s * 0.3), (s * 0.8, s * 0.76), (s * 0.2, s * 0.76)], fill=W)
    d.rectangle((s * 0.2, s * 0.8, s * 0.8, s * 0.88), fill=W)

def ic_gear(d, s):
    cx = cy = s / 2
    teeth = 10
    pts = []
    for i in range(teeth * 2):
        r = s * 0.46 if i % 2 == 0 else s * 0.35
        a0 = i * math.pi / teeth
        for da in (-0.11, 0.11):
            a = a0 + da
            pts.append((cx + math.cos(a) * r, cy + math.sin(a) * r))
    d.polygon(pts, fill=W)
    d.ellipse((cx - s * 0.14, cy - s * 0.14, cx + s * 0.14, cy + s * 0.14), fill=(0, 0, 0, 0))

def ic_wrench(d, s):
    d.line((s * 0.3, s * 0.7, s * 0.72, s * 0.28), fill=W, width=int(s * 0.16))
    d.ellipse((s * 0.56, s * 0.08, s * 0.92, s * 0.44), fill=W)
    d.rectangle((s * 0.74, s * 0.08, s * 0.92, s * 0.26), fill=(0, 0, 0, 0))
    d.ellipse((s * 0.1, s * 0.58, s * 0.42, s * 0.9), fill=W)
    d.rectangle((s * 0.1, s * 0.74, s * 0.26, s * 0.9), fill=(0, 0, 0, 0))

def ic_play(d, s):
    d.polygon([(s * 0.3, s * 0.15), (s * 0.85, s * 0.5), (s * 0.3, s * 0.85)], fill=W)

def ic_back(d, s):
    d.line((s * 0.62, s * 0.18, s * 0.32, s * 0.5, s * 0.62, s * 0.82), fill=W, width=int(s * 0.12), joint='curve')

def ic_flame(d, s):
    d.polygon([(s * 0.5, s * 0.06), (s * 0.7, s * 0.34), (s * 0.78, s * 0.6), (s * 0.66, s * 0.9), (s * 0.34, s * 0.9), (s * 0.22, s * 0.6), (s * 0.34, s * 0.3), (s * 0.42, s * 0.48)], fill=W)
    d.polygon([(s * 0.5, s * 0.5), (s * 0.6, s * 0.7), (s * 0.5, s * 0.84), (s * 0.4, s * 0.7)], fill=(0, 0, 0, 0))

def ic_rocket(d, s):
    d.polygon([(s * 0.5, s * 0.06), (s * 0.64, s * 0.3), (s * 0.64, s * 0.66), (s * 0.36, s * 0.66), (s * 0.36, s * 0.3)], fill=W)
    d.polygon([(s * 0.36, s * 0.5), (s * 0.2, s * 0.72), (s * 0.36, s * 0.72)], fill=W)
    d.polygon([(s * 0.64, s * 0.5), (s * 0.8, s * 0.72), (s * 0.64, s * 0.72)], fill=W)
    d.polygon([(s * 0.42, s * 0.7), (s * 0.58, s * 0.7), (s * 0.5, s * 0.92)], fill=W)
    d.ellipse((s * 0.44, s * 0.3, s * 0.56, s * 0.42), fill=(0, 0, 0, 0))

def ic_bullets(d, s):
    for i, x in enumerate((0.3, 0.5, 0.7)):
        d.rounded_rectangle((s * (x - 0.07), s * 0.3, s * (x + 0.07), s * 0.85), radius=s * 0.04, fill=W)
        d.ellipse((s * (x - 0.07), s * 0.16, s * (x + 0.07), s * 0.44), fill=W)

def ic_gauge(d, s):
    cx, cy = s / 2, s * 0.62
    d.pieslice((cx - s * 0.42, cy - s * 0.42, cx + s * 0.42, cy + s * 0.42), 180, 360, fill=W)
    d.pieslice((cx - s * 0.3, cy - s * 0.3, cx + s * 0.3, cy + s * 0.3), 180, 360, fill=(0, 0, 0, 0))
    d.line((cx, cy, cx + s * 0.28, cy - s * 0.3), fill=W, width=int(s * 0.07))
    d.ellipse((cx - s * 0.07, cy - s * 0.07, cx + s * 0.07, cy + s * 0.07), fill=W)

def ic_steering(d, s):
    cx = cy = s / 2
    d.ellipse((cx - s * 0.44, cy - s * 0.44, cx + s * 0.44, cy + s * 0.44), outline=W, width=int(s * 0.1))
    d.ellipse((cx - s * 0.12, cy - s * 0.12, cx + s * 0.12, cy + s * 0.12), fill=W)
    for a in (0, 120, 240):
        r = math.radians(a + 90)
        d.line((cx, cy, cx + math.cos(r) * s * 0.4, cy + math.sin(r) * s * 0.4), fill=W, width=int(s * 0.08))

def ic_bulb(d, s):
    d.ellipse((s * 0.26, s * 0.08, s * 0.74, s * 0.56), fill=W)
    d.polygon([(s * 0.36, s * 0.5), (s * 0.64, s * 0.5), (s * 0.6, s * 0.72), (s * 0.4, s * 0.72)], fill=W)
    d.rounded_rectangle((s * 0.4, s * 0.74, s * 0.6, s * 0.86), radius=s * 0.03, fill=W)
    for a in (-60, -30, 0, 30, 60):
        r = math.radians(a - 90)
        d.line((s * 0.5 + math.cos(r) * s * 0.3, s * 0.32 + math.sin(r) * s * 0.3, s * 0.5 + math.cos(r) * s * 0.44, s * 0.32 + math.sin(r) * s * 0.44), fill=W, width=int(s * 0.04))

def ic_drop(d, s):
    d.polygon([(s * 0.5, s * 0.06), (s * 0.78, s * 0.5), (s * 0.5, s * 0.5), (s * 0.22, s * 0.5)], fill=W)
    d.ellipse((s * 0.22, s * 0.36, s * 0.78, s * 0.92), fill=W)
    d.ellipse((s * 0.36, s * 0.6, s * 0.46, s * 0.7), fill=(0, 0, 0, 0))

def ic_wheel(d, s):
    cx = cy = s / 2
    d.ellipse((cx - s * 0.46, cy - s * 0.46, cx + s * 0.46, cy + s * 0.46), fill=W)
    d.ellipse((cx - s * 0.34, cy - s * 0.34, cx + s * 0.34, cy + s * 0.34), fill=(0, 0, 0, 0))
    d.ellipse((cx - s * 0.1, cy - s * 0.1, cx + s * 0.1, cy + s * 0.1), fill=W)
    for a in range(0, 360, 72):
        r = math.radians(a - 90)
        d.line((cx, cy, cx + math.cos(r) * s * 0.34, cy + math.sin(r) * s * 0.34), fill=W, width=int(s * 0.07))

def ic_stripe(d, s):
    d.rounded_rectangle((s * 0.16, s * 0.14, s * 0.84, s * 0.86), radius=s * 0.1, outline=W, width=int(s * 0.06))
    d.rectangle((s * 0.4, s * 0.14, s * 0.47, s * 0.86), fill=W)
    d.rectangle((s * 0.53, s * 0.14, s * 0.6, s * 0.86), fill=W)

def ic_roof(d, s):
    d.rounded_rectangle((s * 0.14, s * 0.44, s * 0.86, s * 0.56), radius=s * 0.04, fill=W)
    for x in (0.26, 0.42, 0.58, 0.74):
        d.ellipse((s * (x - 0.07), s * 0.26, s * (x + 0.07), s * 0.4), fill=W)
    d.rectangle((s * 0.2, s * 0.56, s * 0.26, s * 0.76), fill=W)
    d.rectangle((s * 0.74, s * 0.56, s * 0.8, s * 0.76), fill=W)

def ic_spoiler(d, s):
    d.polygon([(s * 0.1, s * 0.34), (s * 0.9, s * 0.26), (s * 0.9, s * 0.38), (s * 0.1, s * 0.46)], fill=W)
    d.rectangle((s * 0.26, s * 0.42, s * 0.32, s * 0.72), fill=W)
    d.rectangle((s * 0.68, s * 0.38, s * 0.74, s * 0.72), fill=W)
    d.rounded_rectangle((s * 0.14, s * 0.7, s * 0.86, s * 0.84), radius=s * 0.04, fill=W)

def ic_bumper(d, s):
    d.rounded_rectangle((s * 0.1, s * 0.36, s * 0.9, s * 0.48), radius=s * 0.05, fill=W)
    d.rounded_rectangle((s * 0.22, s * 0.56, s * 0.78, s * 0.66), radius=s * 0.05, fill=W)
    d.rectangle((s * 0.3, s * 0.44, s * 0.36, s * 0.6), fill=W)
    d.rectangle((s * 0.64, s * 0.44, s * 0.7, s * 0.6), fill=W)
    for x in (0.2, 0.5, 0.8):
        d.polygon([(s * (x - 0.05), s * 0.36), (s * (x + 0.05), s * 0.36), (s * x, s * 0.2)], fill=W)

def ic_exhaust(d, s):
    d.rounded_rectangle((s * 0.1, s * 0.42, s * 0.74, s * 0.58), radius=s * 0.08, fill=W)
    d.ellipse((s * 0.64, s * 0.36, s * 0.9, s * 0.64), outline=W, width=int(s * 0.06))
    for i in range(3):
        d.ellipse((s * (0.2 + i * 0.16), s * 0.18, s * (0.3 + i * 0.16), s * 0.3), outline=W, width=int(s * 0.035))

def ic_mission(d, s):
    d.rounded_rectangle((s * 0.2, s * 0.14, s * 0.8, s * 0.9), radius=s * 0.06, outline=W, width=int(s * 0.06))
    d.rounded_rectangle((s * 0.38, s * 0.08, s * 0.62, s * 0.2), radius=s * 0.03, fill=W)
    for y in (0.4, 0.55, 0.7):
        d.line((s * 0.32, s * y, s * 0.68, s * y), fill=W, width=int(s * 0.05))

def ic_check(d, s):
    d.line((s * 0.2, s * 0.52, s * 0.42, s * 0.74, s * 0.82, s * 0.28), fill=W, width=int(s * 0.12), joint='curve')

def ic_close(d, s):
    d.line((s * 0.24, s * 0.24, s * 0.76, s * 0.76), fill=W, width=int(s * 0.12))
    d.line((s * 0.76, s * 0.24, s * 0.24, s * 0.76), fill=W, width=int(s * 0.12))

def ic_sound(d, s):
    d.polygon([(s * 0.14, s * 0.38), (s * 0.3, s * 0.38), (s * 0.5, s * 0.2), (s * 0.5, s * 0.8), (s * 0.3, s * 0.62), (s * 0.14, s * 0.62)], fill=W)
    d.arc((s * 0.4, s * 0.3, s * 0.72, s * 0.7), 300, 60, fill=W, width=int(s * 0.06))
    d.arc((s * 0.44, s * 0.16, s * 0.9, s * 0.84), 300, 60, fill=W, width=int(s * 0.06))

def ic_vibrate(d, s):
    d.rounded_rectangle((s * 0.34, s * 0.12, s * 0.66, s * 0.88), radius=s * 0.06, outline=W, width=int(s * 0.06))
    for x, sign in ((0.22, -1), (0.78, 1)):
        d.line((s * x, s * 0.34, s * x, s * 0.66), fill=W, width=int(s * 0.05))
        d.line((s * (x + sign * 0.1), s * 0.26, s * (x + sign * 0.1), s * 0.74), fill=W, width=int(s * 0.05))

def ic_globe(d, s):
    cx = cy = s / 2
    d.ellipse((cx - s * 0.42, cy - s * 0.42, cx + s * 0.42, cy + s * 0.42), outline=W, width=int(s * 0.06))
    d.ellipse((cx - s * 0.18, cy - s * 0.42, cx + s * 0.18, cy + s * 0.42), outline=W, width=int(s * 0.05))
    d.line((cx - s * 0.42, cy, cx + s * 0.42, cy), fill=W, width=int(s * 0.05))
    d.line((cx, cy - s * 0.42, cx, cy + s * 0.42), fill=W, width=int(s * 0.05))

def ic_car(d, s):
    d.rounded_rectangle((s * 0.08, s * 0.44, s * 0.92, s * 0.7), radius=s * 0.08, fill=W)
    d.polygon([(s * 0.26, s * 0.46), (s * 0.36, s * 0.26), (s * 0.68, s * 0.26), (s * 0.8, s * 0.46)], fill=W)
    for x in (0.28, 0.72):
        d.ellipse((s * (x - 0.1), s * 0.6, s * (x + 0.1), s * 0.8), fill=W)
        d.ellipse((s * (x - 0.045), s * 0.655, s * (x + 0.045), s * 0.745), fill=(0, 0, 0, 0))

def ic_tank(d, s):
    d.rounded_rectangle((s * 0.08, s * 0.54, s * 0.92, s * 0.78), radius=s * 0.12, fill=W)
    d.rounded_rectangle((s * 0.3, s * 0.34, s * 0.66, s * 0.56), radius=s * 0.06, fill=W)
    d.rectangle((s * 0.62, s * 0.4, s * 0.96, s * 0.46), fill=W)
    for x in (0.22, 0.36, 0.5, 0.64, 0.78):
        d.ellipse((s * (x - 0.05), s * 0.6, s * (x + 0.05), s * 0.7), fill=(0, 0, 0, 0))

def ic_calendar(d, s):
    d.rounded_rectangle((s * 0.14, s * 0.2, s * 0.86, s * 0.88), radius=s * 0.06, outline=W, width=int(s * 0.06))
    d.rectangle((s * 0.14, s * 0.2, s * 0.86, s * 0.38), fill=W)
    for x in (0.3, 0.7):
        d.rectangle((s * (x - 0.03), s * 0.1, s * (x + 0.03), s * 0.28), fill=W)
    for yy in (0.5, 0.66):
        for xx in (0.3, 0.5, 0.7):
            d.rectangle((s * (xx - 0.05), s * (yy - 0.04), s * (xx + 0.05), s * (yy + 0.04)), fill=W)

def ic_info(d, s):
    cx = cy = s / 2
    d.ellipse((cx - s * 0.42, cy - s * 0.42, cx + s * 0.42, cy + s * 0.42), outline=W, width=int(s * 0.06))
    d.ellipse((cx - s * 0.06, cy - s * 0.26, cx + s * 0.06, cy - s * 0.14), fill=W)
    d.rounded_rectangle((cx - s * 0.06, cy - s * 0.06, cx + s * 0.06, cy + s * 0.28), radius=s * 0.03, fill=W)

def ic_ad(d, s):
    d.rounded_rectangle((s * 0.1, s * 0.2, s * 0.9, s * 0.74), radius=s * 0.08, outline=W, width=int(s * 0.06))
    d.polygon([(s * 0.42, s * 0.34), (s * 0.64, s * 0.47), (s * 0.42, s * 0.6)], fill=W)
    d.rectangle((s * 0.3, s * 0.8, s * 0.7, s * 0.88), fill=W)

def ic_cart(d, s):
    d.line((s * 0.08, s * 0.2, s * 0.22, s * 0.2, s * 0.34, s * 0.64, s * 0.82, s * 0.64), fill=W, width=int(s * 0.06), joint='curve')
    d.polygon([(s * 0.26, s * 0.3), (s * 0.9, s * 0.3), (s * 0.82, s * 0.56), (s * 0.32, s * 0.56)], fill=W)
    for x in (0.4, 0.74):
        d.ellipse((s * (x - 0.07), s * 0.7, s * (x + 0.07), s * 0.84), fill=W)

def ic_clock(d, s):
    cx = cy = s / 2
    d.ellipse((cx - s * 0.42, cy - s * 0.42, cx + s * 0.42, cy + s * 0.42), outline=W, width=int(s * 0.07))
    d.line((cx, cy, cx, cy - s * 0.26), fill=W, width=int(s * 0.06))
    d.line((cx, cy, cx + s * 0.18, cy + s * 0.1), fill=W, width=int(s * 0.06))

def ic_skid(d, s):
    # drift: a tyre with motion marks
    cx, cy = s * 0.58, s * 0.5
    d.ellipse((cx - s * 0.3, cy - s * 0.3, cx + s * 0.3, cy + s * 0.3), outline=W, width=int(s * 0.1))
    for yy in (0.36, 0.5, 0.64):
        d.line((s * 0.06, s * yy, s * 0.22, s * yy), fill=W, width=int(s * 0.05))

def ic_brake(d, s):
    cx = cy = s / 2
    d.ellipse((cx - s * 0.3, cy - s * 0.3, cx + s * 0.3, cy + s * 0.3), outline=W, width=int(s * 0.09))
    d.arc((cx - s * 0.44, cy - s * 0.44, cx + s * 0.44, cy + s * 0.44), 120, 240, fill=W, width=int(s * 0.07))
    d.arc((cx - s * 0.44, cy - s * 0.44, cx + s * 0.44, cy + s * 0.44), 300, 60, fill=W, width=int(s * 0.07))

def ic_pause(d, s):
    d.rounded_rectangle((s * 0.26, s * 0.18, s * 0.42, s * 0.82), radius=s * 0.04, fill=W)
    d.rounded_rectangle((s * 0.58, s * 0.18, s * 0.74, s * 0.82), radius=s * 0.04, fill=W)

def ic_tire_mark(d, s):
    ic_skid(d, s)

ICONS = {
    'star': ic_star, 'star_outline': ic_star_outline, 'scrap': ic_scrap, 'lock': ic_lock, 'trophy': ic_trophy,
    'skull': ic_skull, 'reactor': ic_reactor, 'flag': ic_flag, 'shield': ic_shield, 'heart': ic_heart,
    'crosshair': ic_crosshair, 'crown': ic_crown, 'gear': ic_gear, 'wrench': ic_wrench, 'play': ic_play,
    'back': ic_back, 'flame': ic_flame, 'rocket': ic_rocket, 'bullets': ic_bullets, 'gauge': ic_gauge,
    'steering': ic_steering, 'bulb': ic_bulb, 'drop': ic_drop, 'wheel': ic_wheel, 'stripe': ic_stripe,
    'roof': ic_roof, 'spoiler': ic_spoiler, 'bumper': ic_bumper, 'exhaust': ic_exhaust, 'mission': ic_mission,
    'check': ic_check, 'close': ic_close, 'sound': ic_sound, 'vibrate': ic_vibrate, 'globe': ic_globe,
    'car': ic_car, 'tank': ic_tank, 'calendar': ic_calendar, 'info': ic_info, 'ad': ic_ad, 'cart': ic_cart,
    'clock': ic_clock, 'skid': ic_skid, 'brake': ic_brake, 'pause': ic_pause,
}

def main():
    out = sys.argv[1] if len(sys.argv) > 1 and not sys.argv[1].startswith('--') else os.path.join(os.path.dirname(os.path.abspath(__file__)), '..', 'unity-game', 'Assets', 'Resources', 'Derby', 'UI')
    os.makedirs(out, exist_ok=True)
    rounded(96, 96, 22, fill=255, alpha=255).save(os.path.join(out, 'panel.png'))
    ring(96, 96, 22, width=2).save(os.path.join(out, 'panel_ring.png'))
    rounded(96, 96, 20, gradient=(1.0, 0.74), gloss=0.16, shadow=6).save(os.path.join(out, 'button.png'))
    rounded(96, 96, 20, gradient=(1.0, 0.62), gloss=0.22, shadow=8).save(os.path.join(out, 'button_primary.png'))
    rounded(96, 96, 46, gradient=(1.0, 0.8), gloss=0.12).save(os.path.join(out, 'pill.png'))
    rounded(96, 96, 14, gradient=(1.0, 0.86)).save(os.path.join(out, 'card.png'))
    ring(96, 96, 14, width=3).save(os.path.join(out, 'card_ring.png'))
    rounded(96, 96, 10, gradient=(1.0, 0.9)).save(os.path.join(out, 'tab.png'))
    carbon().save(os.path.join(out, 'carbon.png'))
    radial_glow().save(os.path.join(out, 'glow.png'))
    hstripe().save(os.path.join(out, 'hazard.png'))
    for name, fn in ICONS.items():
        icon(fn).save(os.path.join(out, 'ic_' + name + '.png'))
    # contact sheet (only with --sheet <file>)
    if '--sheet' not in sys.argv:
        print('ui: %d sprites + %d icons -> %s' % (11, len(ICONS), out)); return
    names = sorted(ICONS)
    cols = 9
    sheet = Image.new('RGBA', (cols * 72, ((len(names) + cols - 1) // cols) * 72), (30, 32, 40, 255))
    for i, n in enumerate(names):
        im = Image.open(os.path.join(out, 'ic_' + n + '.png')).resize((64, 64))
        sheet.alpha_composite(im, ((i % cols) * 72 + 4, (i // cols) * 72 + 4))
    sheet.save(sys.argv[sys.argv.index('--sheet') + 1])
    print('ui: %d sprites + %d icons -> %s' % (11, len(ICONS), out))

if __name__ == '__main__':
    main()
