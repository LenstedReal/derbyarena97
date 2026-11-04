#!/usr/bin/env python3
"""
DERBY ARENA 2.1 texture forge: tileable PBR-ish textures (albedo + normal) for the arenas, numpy + Pillow only.

Everything is periodic by construction (spectral noise, wrapped Worley cells, drawing on a 3x3 tiled canvas),
so the textures tile seamlessly at any scale.
Run: python3 scripts/texforge.py <out_dir>   -> writes <name>_albedo.jpg / <name>_normal.png (+ preview sheet)
"""
import math, os, sys
import numpy as np
from PIL import Image, ImageDraw, ImageFilter

# ----------------------------------------------------------------------------- periodic noise
def fbm(size, beta=2.0, seed=0, lo=1, hi=None):
    """Spectral 1/f^beta noise, zero mean, unit std, periodic."""
    rng = np.random.default_rng(seed)
    f = np.fft.fftfreq(size) * size
    fx, fy = np.meshgrid(f, f)
    fr = np.hypot(fx, fy)
    amp = np.where(fr > 0, 1.0 / np.maximum(fr, 1e-9) ** (beta / 2.0), 0.0)
    amp[fr < lo] = 0
    if hi: amp[fr > hi] = 0
    phase = np.exp(2j * np.pi * rng.random((size, size)))
    spec = amp * phase
    img = np.fft.ifft2(spec).real
    img -= img.mean(); img /= (img.std() + 1e-9)
    return img

def worley(size, cells, seed=0, jitter=1.0):
    """Periodic Worley: distance to the nearest feature point (in cell units) and the cell id. 3x3 neighbour search."""
    rng = np.random.default_rng(seed)
    pts = (np.indices((cells, cells)).transpose(1, 2, 0) + 0.5 + (rng.random((cells, cells, 2)) - 0.5) * jitter)   # (i=y, j=x) in cell units
    y, x = np.mgrid[0:size, 0:size] / size * cells
    ci = np.floor(y).astype(int); cj = np.floor(x).astype(int)
    best = np.full((size, size), 9.0); cid = np.zeros((size, size), int)
    for di in (-1, 0, 1):
        for dj in (-1, 0, 1):
            ni = (ci + di) % cells; nj = (cj + dj) % cells
            py = pts[ni, nj, 0] + (ci + di - ni)    # unwrap across the tile border
            px = pts[ni, nj, 1] + (cj + dj - nj)
            d = np.hypot(x - px, y - py)
            m = d < best
            best[m] = d[m]; cid[m] = (ni * cells + nj)[m]
    return best, cid

def tiled_draw(size, draw_fn, mode='L'):
    """Draw with PIL on a 3x3 tiled canvas and crop the centre tile (periodic strokes)."""
    canvas = Image.new(mode, (size * 3, size * 3), 0)
    d = ImageDraw.Draw(canvas)
    for ox in (0, size, 2 * size):
        for oy in (0, size, 2 * size):
            draw_fn(d, ox, oy)
    return np.asarray(canvas.crop((size, size, 2 * size, 2 * size))).astype(float) / 255.0

def cracks(size, seed, count=6, length=0.6, width=2, wiggle=0.35):
    rng = np.random.default_rng(seed)
    paths = []
    for _ in range(count):
        x, y = rng.random(2) * size; a = rng.uniform(0, 2 * math.pi); pts = [(x, y)]
        for _ in range(int(length * size / 6)):
            a += rng.normal(0, wiggle); x += 6 * math.cos(a); y += 6 * math.sin(a); pts.append((x, y))
            if rng.random() < 0.08: a += rng.choice([-1, 1]) * 0.9
        paths.append(pts)
    def fn(d, ox, oy):
        for pts in paths:
            d.line([(px + ox, py + oy) for px, py in pts], fill=255, width=width)
    return tiled_draw(size, fn)

def blur(img, r):
    """Gaussian blur that wraps (2.1.1): padded with the opposite edges so the result stays periodic."""
    pad = int(math.ceil(r * 4)) + 1
    src = np.pad(np.clip(img, 0, 1), pad, mode='wrap')
    out = np.asarray(Image.fromarray((src * 255).astype(np.uint8)).filter(ImageFilter.GaussianBlur(r))).astype(float) / 255.0
    return out[pad:-pad, pad:-pad]

def normal_map(height, strength=3.0, size=None):
    h = height
    if size and size != h.shape[0]:
        h = np.asarray(Image.fromarray((np.clip(h * 0.5 + 0.5, 0, 1) * 255).astype(np.uint8)).resize((size, size), Image.BILINEAR)).astype(float) / 255.0 * 2 - 1
    gx = (np.roll(h, -1, 1) - np.roll(h, 1, 1)) * 0.5
    gy = (np.roll(h, -1, 0) - np.roll(h, 1, 0)) * 0.5
    n = np.stack([-gx * strength, -gy * strength, np.ones_like(h)], -1)
    n /= np.linalg.norm(n, axis=-1, keepdims=True)
    return ((n * 0.5 + 0.5) * 255).astype(np.uint8)

def to_rgb(r, g, b):
    return (np.clip(np.stack([r, g, b], -1), 0, 1) * 255).astype(np.uint8)

def tint(lum, color, sat_noise=None):
    c = np.asarray(color)[None, None, :]
    img = lum[..., None] * c
    if sat_noise is not None: img = img * (1 + sat_noise[..., None])
    return (np.clip(img, 0, 1) * 255).astype(np.uint8)

# ----------------------------------------------------------------------------- textures
def tex_sand(size=1024, seed=11):
    """Arena dirt: sand base, damp patches, pebbles, tyre ruts, debris. One tile ~6 m in the colosseum."""
    rng = np.random.default_rng(seed)
    base = 0.62 + 0.07 * fbm(size, 2.3, seed) + 0.03 * fbm(size, 1.4, seed + 1) + rng.normal(0, 0.015, (size, size))
    damp = np.clip(fbm(size, 2.8, seed + 2) * 0.8, 0, 1) ** 1.5
    base *= 1 - damp * 0.16
    # pebbles: wrapped Worley cells, only a fraction of cells are stones
    d, cid = worley(size, 48, seed + 3)
    keep = (rng.random(48 * 48) < 0.16)[cid]
    stone = np.clip(1 - d / 0.4, 0, 1) * keep
    stone_shape = np.sqrt(np.clip(stone, 0, 1))
    tone = (rng.random(48 * 48) * 0.7 + 0.3)[cid]
    base = base * (1 - stone_shape * 0.85) + stone_shape * tone * 0.72
    # tyre ruts: three curved bands with parallel ridges
    y, x = np.mgrid[0:size, 0:size] / size
    rut_h = np.zeros((size, size)); rut_c = np.zeros((size, size))
    for k in range(3):
        cx = (x + 0.33 * k + 0.08 * np.sin(2 * np.pi * (y * 1.0 + rng.random()))) % 1.0
        for off in (-0.028, 0.028):
            band = np.exp(-((cx - 0.5 - off) ** 2) / (2 * 0.014 ** 2))
            ridges = 0.5 + 0.5 * np.cos(2 * np.pi * (cx - 0.5 - off) * 90)
            rut_c += band * (0.22 + 0.1 * ridges); rut_h -= band * (0.6 + 0.4 * ridges)
    base *= 1 - np.clip(rut_c, 0, 0.42)
    # debris specks
    speck = tiled_draw(size, lambda d, ox, oy: [d.ellipse([px + ox - r, py + oy - r, px + ox + r, py + oy + r], fill=255) for px, py, r in [(rng.random() * size, rng.random() * size, rng.integers(1, 3)) for _ in range(160)]])
    base *= 1 - speck * 0.45
    lum = np.clip(base, 0, 1)
    warm = fbm(size, 2.5, seed + 7) * 0.05
    albedo = to_rgb(lum * (0.98 + warm), lum * (0.86 + warm * 0.5), lum * (0.66 - warm * 0.3))
    height = 0.6 * stone_shape + 0.35 * rut_h + 0.08 * fbm(size, 1.6, seed + 8) + 0.03 * fbm(size, 1.0, seed + 9) * 0.5 - damp * 0.1
    return albedo, normal_map(height, 2.6, 1024)

def blocks(size, cols, rows, seed, mortar=0.035, chip=0.5):
    """Block wall mask: per-block id, mortar mask, and edge-chip height."""
    rng = np.random.default_rng(seed)
    y, x = np.mgrid[0:size, 0:size] / size
    row = np.floor(y * rows); off = (row % 2) * 0.5 / cols
    col = np.floor((x + off) * cols)
    u = ((x + off) * cols) % 1.0; v = (y * rows) % 1.0
    jitter = (fbm(size, 1.2, seed) * 0.02)
    edge = np.minimum(np.minimum(u, 1 - u) * cols / rows, np.minimum(v, 1 - v)) + jitter
    mortar_m = np.clip(1 - edge / mortar, 0, 1)
    bid = (row * cols + (col % cols)).astype(int) + 7
    btone = (rng.random(int(rows * cols) + 20))[np.clip(bid, 0, int(rows * cols) + 19)]
    chips = np.clip(1 - edge / (mortar * 3), 0, 1) * (fbm(size, 1.3, seed + 5) > 0.9) * chip
    return mortar_m, btone, chips, edge

def tex_stone(size=1024, seed=21, cols=8, rows=6, base_col=(0.80, 0.72, 0.58), dark=False):
    rng = np.random.default_rng(seed)
    mortar, btone, chips, edge = blocks(size, cols, rows, seed, mortar=0.03 if not dark else 0.04)
    lum = 0.72 + (btone - 0.5) * 0.22 + 0.07 * fbm(size, 2.2, seed + 1) + 0.04 * fbm(size, 1.2, seed + 2) + rng.normal(0, 0.012, (size, size))
    streak = np.clip(fbm(size, 2.6, seed + 3), 0, 2) * 0.5
    streak = np.asarray(Image.fromarray((np.clip(streak, 0, 1) * 255).astype(np.uint8)).resize((size // 8, size), Image.BILINEAR).resize((size, size), Image.BILINEAR)).astype(float) / 255.0
    lum *= 1 - streak * 0.25
    lum = lum * (1 - mortar * 0.42) - chips * 0.25
    crack = cracks(size, seed + 4, count=4, length=0.4, width=1)
    lum *= 1 - crack * 0.5
    lum = np.clip(lum, 0, 1)
    c = np.asarray(base_col) if not dark else np.asarray((0.36, 0.37, 0.4))
    albedo = to_rgb(lum * c[0] * (1 + 0.04 * fbm(size, 2.5, seed + 6)), lum * c[1], lum * c[2])
    height = -mortar * 1.0 - chips * 0.8 - crack * 0.4 + 0.08 * fbm(size, 1.5, seed + 7) + 0.03 * fbm(size, 0.8, seed + 8)
    return albedo, normal_map(height, 2.8, 512)

def tex_concrete(size=1024, seed=31):
    rng = np.random.default_rng(seed)
    lum = 0.58 + 0.07 * fbm(size, 2.4, seed) + 0.03 * fbm(size, 1.3, seed + 1) + rng.normal(0, 0.012, (size, size))
    d, cid = worley(size, 90, seed + 2)
    pits = np.clip(1 - d / 0.25, 0, 1) * ((rng.random(90 * 90) < 0.12)[cid])
    lum *= 1 - pits * 0.45
    crack = cracks(size, seed + 3, count=7, length=0.7, width=2)
    lum *= 1 - crack * 0.55
    # form-tie holes grid
    holes = tiled_draw(size, lambda d, ox, oy: [d.ellipse([gx + ox - 9, gy + oy - 9, gx + ox + 9, gy + oy + 9], fill=255) for gx in (size * 0.17, size * 0.5, size * 0.83) for gy in (size * 0.25, size * 0.75)])
    lum *= 1 - holes * 0.6
    # panel seams every half tile
    y, x = np.mgrid[0:size, 0:size] / size
    seam = (np.abs(((x * 2) % 1.0) - 0.5) > 0.495) | (np.abs(((y * 2) % 1.0) - 0.5) > 0.495)
    lum *= np.where(seam, 0.6, 1.0)
    stain = np.clip(fbm(size, 2.8, seed + 5) * 0.7, 0, 1) ** 2
    lum = np.clip(lum, 0, 1)
    albedo = to_rgb(lum * (0.96 + stain * 0.1), lum * (0.95 + stain * 0.02), lum * (0.93 - stain * 0.08))
    height = -pits * 0.8 - crack * 0.6 - holes * 1.0 - seam * 0.5 + 0.1 * fbm(size, 1.8, seed + 6)
    return albedo, normal_map(height, 2.5, 512)

def tex_asphalt(size=1024, seed=41):
    rng = np.random.default_rng(seed)
    d, cid = worley(size, 160, seed)
    grain = np.clip(1 - d / 0.6, 0, 1) * (rng.random(160 * 160) * 0.8 + 0.2)[cid]
    lum = 0.17 + 0.16 * grain + 0.035 * fbm(size, 2.0, seed + 1) + rng.normal(0, 0.012, (size, size))
    # repair patches: darker rectangles
    y, x = np.mgrid[0:size, 0:size] / size
    patch = np.zeros((size, size))
    for _ in range(3):
        px, py = rng.random(2); w, h = rng.uniform(0.15, 0.35, 2)
        dx = (x - px + 0.5) % 1.0 - 0.5; dy = (y - py + 0.5) % 1.0 - 0.5
        patch = np.maximum(patch, ((np.abs(dx) < w / 2) & (np.abs(dy) < h / 2)).astype(float))
    lum *= 1 - patch * 0.22
    crack = cracks(size, seed + 2, count=5, length=0.8, width=2)
    lum *= 1 - crack * 0.5
    oil = np.clip(fbm(size, 3.0, seed + 3) * 0.9 - 0.2, 0, 1) ** 1.5
    lum *= 1 - oil * 0.35
    lum = np.clip(lum, 0, 1)
    albedo = to_rgb(lum * 1.0, lum * 1.0, lum * 1.03)
    height = 0.5 * grain - crack * 0.6 - patch * 0.15 + 0.05 * fbm(size, 1.5, seed + 4)
    return albedo, normal_map(height, 2.2, 512)

def tex_steel(size=1024, seed=51):
    rng = np.random.default_rng(seed)
    brushed = np.cumsum(rng.normal(0, 1, (size, size)), 1); brushed -= brushed.mean(1, keepdims=True); brushed /= brushed.std() + 1e-9
    brushed = (brushed - brushed.mean()) * 0.012
    lum = 0.42 + brushed + 0.05 * fbm(size, 2.2, seed) + rng.normal(0, 0.01, (size, size))
    # rivet rows + plates
    y, x = np.mgrid[0:size, 0:size] / size
    seam = (np.abs(((y * 3) % 1.0) - 0.5) > 0.49) | (np.abs(((x * 2) % 1.0) - 0.5) > 0.49)
    riv = np.zeros((size, size)); rust = np.zeros((size, size))
    pts = [(gx, gy) for gy in np.linspace(0, 1, 7)[:-1] + 1 / 12 for gx in np.linspace(0, 1, 13)[:-1] + 1 / 24]
    def fn(d, ox, oy):
        for gx, gy in pts:
            cx, cy = gx * size + ox, gy * size + oy
            d.ellipse([cx - 7, cy - 7, cx + 7, cy + 7], fill=255)
    riv = tiled_draw(size, fn)
    rivh = blur(riv, 1.5)
    # rust streaks running down from rivets
    def fr(d, ox, oy):
        for gx, gy in pts:
            if rng.random() < 0.35:
                cx, cy = gx * size + ox, gy * size + oy
                L = rng.integers(30, 160); w = rng.integers(4, 12)
                d.polygon([(cx - w, cy), (cx + w, cy), (cx + w * 0.4, cy + L), (cx - w * 0.4, cy + L)], fill=255)
    rust = blur(tiled_draw(size, fr), 3) * np.clip(0.5 + fbm(size, 1.8, seed + 2), 0, 1)
    scratches = tiled_draw(size, lambda d, ox, oy: [d.line([(px + ox, py + oy), (px + ox + L * math.cos(a), py + oy + L * math.sin(a))], fill=255, width=1) for px, py, L, a in [(rng.random() * size, rng.random() * size, rng.integers(20, 120), rng.uniform(0, math.pi)) for _ in range(40)]])
    lum = lum * (1 - seam * 0.5) + rivh * 0.12 + scratches * 0.15
    lum = np.clip(lum, 0, 1)
    r = lum * (1 - rust) + rust * 0.42; g = lum * (1 - rust) + rust * 0.2; b = lum * 1.04 * (1 - rust) + rust * 0.08
    albedo = to_rgb(r, g, b)
    height = rivh * 1.2 - seam * 0.8 + scratches * 0.1 + 0.03 * fbm(size, 2.0, seed + 3)
    return albedo, normal_map(height, 2.5, 512)

def tex_hazard(size=512, seed=61):
    """Barrier metal: rusty steel on the upper half, yellow/black chevrons on the lower half (keeps the 1.x UV layout)."""
    rng = np.random.default_rng(seed)
    lum = 0.5 + 0.05 * fbm(size, 2.2, seed) + rng.normal(0, 0.012, (size, size))
    rust = np.clip(fbm(size, 2.6, seed + 1) * 0.9 - 0.1, 0, 1) ** 1.3
    y, x = np.mgrid[0:size, 0:size] / size
    stripe = (((x + y) * 6) % 1.0) < 0.5
    lower = y > 0.5
    r = np.where(lower, np.where(stripe, 0.9, 0.08), lum * (1 - rust) + rust * 0.45)
    g = np.where(lower, np.where(stripe, 0.72, 0.08), lum * (1 - rust) + rust * 0.22)
    b = np.where(lower, np.where(stripe, 0.1, 0.09), lum * 1.03 * (1 - rust) + rust * 0.1)
    wear = np.clip(fbm(size, 2.0, seed + 2) * 0.5 + 0.1, 0, 1) * lower
    r = r * (1 - wear * 0.35) + wear * 0.3; g = g * (1 - wear * 0.35) + wear * 0.25; b = b * (1 - wear * 0.35) + wear * 0.2
    albedo = to_rgb(r, g, b)
    height = 0.06 * fbm(size, 1.6, seed + 3) - rust * 0.15
    return albedo, normal_map(height, 2.0, 512)

def tex_rubber(size=512, seed=71):
    rng = np.random.default_rng(seed)
    y, x = np.mgrid[0:size, 0:size] / size
    img = np.full((size, size), 0.11) + rng.normal(0, 0.012, (size, size))
    tread = (y > 0.34) & (y < 0.66)
    blocks_ = ((np.floor(x * 24) + np.floor((y - 0.34) / 0.32 * 3)) % 2 == 0)
    groove = (np.abs(((x * 24) % 1.0) - 0.5) < 0.08) | (np.abs(((y - 0.34) / 0.32 * 3 % 1.0) - 0.5) < 0.1)
    img = np.where(tread, np.where(blocks_, 0.17, 0.13), img)
    img = np.where(tread & groove, 0.05, img)
    img = img + ((np.abs(y - 0.2) < 0.012) | (np.abs(y - 0.8) < 0.012)) * 0.05
    albedo = to_rgb(img, img, img * 1.02)
    height = np.where(tread & groove, -1.0, np.where(tread, 0.3, 0.0)) + 0.04 * fbm(size, 1.5, seed)
    return albedo, normal_map(height, 2.0, 512)

def tex_clouds(w=2048, h=512, seed=81):
    """Grayscale cloud panorama for the sky shader: periodic in U (longitude), thin at the zenith, cut at the horizon.
    Low-frequency billow noise gives rounded cumulus clumps with clear gaps; fine detail is kept subtle."""
    base = fbm(w, 2.6, seed, hi=40)[:h, :]
    bil = np.abs(fbm(w, 2.3, seed + 1, hi=80)[:h, :])
    fine = np.abs(fbm(w, 1.8, seed + 2, hi=400)[:h, :])
    v = np.linspace(0, 1, h)[:, None]        # 0 = zenith, 1 = horizon
    field = base * 0.8 + 0.5 - bil * 0.6 - fine * 0.12 + 0.3 * np.sin(np.pi * v) ** 2
    cover = np.clip((field - 0.5) * 2.0, 0, 1) ** 1.4
    cover = blur(cover, 2.5)
    edge = np.clip(0.8 + 0.2 * fbm(w, 1.6, seed + 3, hi=300)[:h, :], 0.5, 1.1)
    img = np.clip(cover * edge, 0, 1)
    img *= np.clip((1 - v) * 1.7, 0, 1) ** 0.6 * np.clip(v * 2.4, 0, 1)
    return (np.clip(img, 0, 1) * 255).astype(np.uint8)

SETS = {
    'sand': tex_sand, 'stone': tex_stone, 'basalt': lambda: tex_stone(seed=22, cols=5, rows=4, dark=True),
    'concrete': tex_concrete, 'asphalt': tex_asphalt, 'steel': tex_steel, 'hazard': tex_hazard, 'rubber': tex_rubber,
}

def main():
    out = sys.argv[1] if len(sys.argv) > 1 else '/tmp/texforge'
    os.makedirs(out, exist_ok=True)
    previews = []
    for name, fn in SETS.items():
        albedo, normal = fn()
        Image.fromarray(albedo).save(os.path.join(out, name + '_albedo.jpg'), quality=90)
        Image.fromarray(normal).save(os.path.join(out, name + '_normal.png'), optimize=True)
        previews.append((name, albedo, normal))
        print(name, albedo.shape[0], normal.shape[0])
    clouds = tex_clouds()
    Image.fromarray(clouds).save(os.path.join(out, 'clouds.png'), optimize=True)
    sheet = Image.new('RGB', (256 * len(previews), 512))
    for i, (name, a, n) in enumerate(previews):
        sheet.paste(Image.fromarray(a).resize((256, 256)), (i * 256, 0))
        sheet.paste(Image.fromarray(n).resize((256, 256)), (i * 256, 256))
    sheet.save(os.path.join(out, 'preview.jpg'), quality=85)

if __name__ == '__main__':
    main()
