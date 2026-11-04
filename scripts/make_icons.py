"""DERBY ARENA launcher icons from the key art (unchanged design), fitted to Android adaptive-icon safe zone.
Usage: python scripts/make_icons.py <keyart_1024.png> unity-game/Assets/DerbyArena/Icons unity-game/AndroidIcons/res"""
import math, sys
from PIL import Image, ImageDraw, ImageFilter, ImageChops
ART = sys.argv[1]; OUT = sys.argv[2]
art = Image.open(ART).convert('RGB')

def radial_mask(size, fin, fout):
    m = Image.new('L', (size, size), 0); px = m.load(); c = size / 2
    for y in range(size):
        for x in range(size):
            d = math.hypot(x + 0.5 - c, y + 0.5 - c)
            px[x, y] = int(255 * (1.0 - min(1.0, max(0.0, (d - fin) / max(1.0, fout - fin)))))
    return m

def background(size):
    # the art itself, zoomed and blurred, darkened: any mask shape shows arena texture, never a hard edge
    a = art.resize((int(size * 1.35), int(size * 1.35)), Image.LANCZOS).filter(ImageFilter.GaussianBlur(size * 0.03))
    bg = Image.new('RGB', (size, size)); bg.paste(a, ((size - a.width) // 2, (size - a.height) // 2))
    return ImageChops.multiply(bg, Image.new('RGB', (size, size), (150, 140, 135)))

def foreground(size, art_px, fin, fout):
    layer = Image.new('RGBA', (size, size), (0, 0, 0, 0))
    a = art.resize((art_px, art_px), Image.LANCZOS).convert('RGBA')
    layer.paste(a, ((size - art_px) // 2, (size - art_px) // 2))
    r, g, b, al = layer.split()
    return Image.merge('RGBA', (r, g, b, ImageChops.multiply(al, radial_mask(size, fin, fout))))

def composite(size, art_px, fin, fout):
    return Image.alpha_composite(background(size).convert('RGBA'), foreground(size, art_px, fin, fout))

# adaptive (432 px = 108 dp): visible circle 288 px, safe zone 264 px -> the art's inscribed circle sits inside the safe zone
S = 432
background(S).save(f'{OUT}/icon_adaptive_bg.png')
foreground(S, 258, 118, 146).save(f'{OUT}/icon_adaptive_fg.png')
legacy = composite(1024, 860, 440, 540)
m = Image.new('L', (1024, 1024), 0); ImageDraw.Draw(m).rounded_rectangle((0, 0, 1023, 1023), radius=180, fill=255)
legacy.putalpha(m); legacy.save(f'{OUT}/icon_legacy.png')
rnd = composite(512, 400, 196, 250)
m = Image.new('L', (512, 512), 0); ImageDraw.Draw(m).ellipse((0, 0, 511, 511), fill=255)
rnd.putalpha(m); rnd.save(f'{OUT}/icon_round.png')
composite(512, 448, 236, 276).convert('RGB').save(f'{OUT}/play_icon_512.png')
print('ok')

# Android res folders: legacy + round (48 dp) and adaptive layers (108 dp) per density, rendered at exact size
import os
DENS = {'mdpi': 1, 'hdpi': 1.5, 'xhdpi': 2, 'xxhdpi': 3, 'xxxhdpi': 4}
res = sys.argv[3] if len(sys.argv) > 3 else None
if res:
    for name, k in DENS.items():
        d = f'{res}/mipmap-{name}'; os.makedirs(d, exist_ok=True)
        L = int(108 * k); I = int(48 * k); f = L / 432.0
        background(L).save(f'{d}/app_icon_background.png')
        foreground(L, int(258 * f), 118 * f, 146 * f).save(f'{d}/app_icon_foreground.png')
        lg = composite(I, int(I * 0.84), I * 0.43, I * 0.53)
        m = Image.new('L', (I, I), 0); ImageDraw.Draw(m).rounded_rectangle((0, 0, I - 1, I - 1), radius=int(I * 0.18), fill=255)
        lg.putalpha(m); lg.save(f'{d}/app_icon.png')
        rd = composite(I, int(I * 0.78), I * 0.38, I * 0.49)
        m = Image.new('L', (I, I), 0); ImageDraw.Draw(m).ellipse((0, 0, I - 1, I - 1), fill=255)
        rd.putalpha(m); rd.save(f'{d}/app_icon_round.png')
    print('res ok')
