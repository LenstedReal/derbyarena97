#!/usr/bin/env python3
"""
DERBY ARENA 4.0 tank forge: Panzerkampfwagen VI Tiger I (Ausf. E) "T-12", the endgame vehicle.

Built at historical proportions (hull 6.3 x 3.56 m, 3.0 m high, 8.8 cm KwK 36 L/56 with the double-baffle muzzle
brake, eight interleaved road wheels per side, front drive sprocket, rear idler) in the car-local design space of
carforge (+Z forward, +Y up, metres; the game shows it at 85%). Three-tone sprayed camouflage, the "T-12" turret
marking with the crescent and star, spare track links, hull tools and log bundle, twin exhausts, a Turkish flag on
the turret antenna (vertex-waved in the game by TankVisual).

Node contract (see VehicleVisual / TankVisual):
  Body, Wheel_FL/FR/RL/RR (empty anchors for the suspension), Turret > Barrel, Track_L / Track_R (UV v in metres
  for scrolling), Road_L1..8 / Road_R1..8, Sprocket_L/R, Idler_L/R (spin about X), Flag (cloth quad, both faces),
  *_LOD1 nodes (simplified far model).
"""
import math, os, sys
import numpy as np
from PIL import Image, ImageDraw, ImageFilter, ImageFont
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import carforge as cf
from carforge import rbox, box, cyl, tube, lathe, loft, fan, quad, Mesh, Glb

# ----------------------------------------------------------------------------- textures
def fbm(size, beta, seed, hi=None):
    rng = np.random.default_rng(seed)
    f = np.fft.fftfreq(size) * size
    fx, fy = np.meshgrid(f, f)
    fr = np.hypot(fx, fy)
    amp = np.where(fr > 0, 1.0 / np.maximum(fr, 1e-9) ** (beta / 2.0), 0.0)
    if hi: amp[fr > hi] = 0
    phase = np.exp(2j * np.pi * rng.random((size, size)))
    img = np.fft.ifft2(amp * phase).real
    img -= img.mean(); img /= (img.std() + 1e-9)
    return img

def smooth(x, lo, hi):
    t = np.clip((x - lo) / max(hi - lo, 1e-6), 0, 1)
    return t * t * (3 - 2 * t)

def tex_camo(size=1024, seed=21):
    """Sprayed three-tone camouflage: dunkelgelb base, olive green bands, red-brown patches, dust and grime."""
    yellow = np.array([0.66, 0.57, 0.36]); green = np.array([0.33, 0.40, 0.24]); brown = np.array([0.42, 0.28, 0.19])
    a = fbm(size, 2.6, seed, hi=7)
    b = fbm(size, 2.4, seed + 1, hi=9)
    g = smooth(a + 0.35 * fbm(size, 1.8, seed + 2, hi=40), 0.25, 0.55)
    r = smooth(b - 0.3 * a, 0.65, 0.95)
    col = yellow[None, None, :] * (1 - g[..., None]) + green[None, None, :] * g[..., None]
    col = col * (1 - r[..., None]) + brown[None, None, :] * r[..., None]
    # dust (light) and grime (dark) at different scales, plus fine grain
    dust = smooth(fbm(size, 2.2, seed + 3, hi=24), 0.4, 1.4) * 0.16
    grime = smooth(-fbm(size, 2.0, seed + 4, hi=60), 0.5, 1.6) * 0.22
    grain = np.random.default_rng(seed + 5).normal(0, 0.02, (size, size))
    lum = 1 + dust - grime + grain
    col = np.clip(col * lum[..., None], 0, 1)
    # scratches down to bare metal
    layer = Image.new('L', (size, size), 0); d = ImageDraw.Draw(layer)
    rng = np.random.default_rng(seed + 6)
    for _ in range(90):
        x, y = rng.integers(0, size, 2); L = rng.integers(20, 160); ang = rng.uniform(0, math.pi)
        d.line([(x, y), (x + L * math.cos(ang), y + L * math.sin(ang))], fill=int(rng.integers(120, 220)), width=int(rng.integers(1, 3)))
    scr = np.asarray(layer).astype(float) / 255.0
    col = col * (1 - scr[..., None] * 0.5) + np.array([0.3, 0.3, 0.32])[None, None, :] * scr[..., None] * 0.5
    height = (0.5 * fbm(size, 2.0, seed + 7, hi=80) * 0.3 - scr * 0.6)
    # weld seams / plate edges: a faint grid every half texture (plates ~1 m) for the normal map
    grid = np.zeros((size, size))
    for k in (0, size // 2):
        grid[k:k + 3, :] = 1; grid[:, k:k + 3] = 1
    height -= grid * 0.4
    return (col * 255).astype(np.uint8), height

def tex_track(size=256, seed=31):
    """Track plate strip: v along the run (1 m = 8 links of 125 mm), cleats and guide horn shadow."""
    rng = np.random.default_rng(seed)
    y = np.arange(size)[:, None] / size
    x = np.arange(size)[None, :] / size
    lum = 0.22 + rng.normal(0, 0.02, (size, size))
    link = (y * 8) % 1.0
    cleat = ((link > 0.12) & (link < 0.34)).astype(float)                 # raised cleat bar
    edge = ((link < 0.05) | (link > 0.95)).astype(float)                  # link gap
    lum += cleat * 0.14 - edge * 0.12
    # wear: the cleat tops are polished (lighter), the centre (guide horn path) darker
    centre = np.exp(-((x - 0.5) ** 2) / 0.01)          # (1, size)
    lum += cleat * 0.1 - centre * 0.08
    lum = np.clip(lum, 0, 1)
    rgb = np.stack([lum, lum * 1.02, lum * 1.05], -1)
    height = cleat * 0.8 - edge * 0.6 + rng.normal(0, 0.05, (size, size))
    return (np.clip(rgb, 0, 1) * 255).astype(np.uint8), height

def tex_wood(size=256, seed=41):
    rng = np.random.default_rng(seed)
    y = np.arange(size)[:, None] / size
    grain = 0.5 + 0.5 * np.sin(y * 60 + 2 * fbm(size, 2.0, seed, hi=30))
    base = np.array([0.42, 0.28, 0.16])
    lum = 0.8 + 0.3 * grain + rng.normal(0, 0.03, (size, size))
    col = np.clip(base[None, None, :] * lum[..., None], 0, 1)
    return (col * 255).astype(np.uint8)

def tex_flag(w=256, h=160):
    """Turkish flag: red field, white crescent and five-pointed star (proportions of the flag law)."""
    img = Image.new('RGB', (w, h), (227, 10, 23))
    d = ImageDraw.Draw(img)
    G = h / 30.0                                          # standard unit: height = 30 G, width = 45 G
    cx, cy = 12 * G, h / 2.0
    d.ellipse((cx - 6 * G, cy - 6 * G, cx + 6 * G, cy + 6 * G), fill=(255, 255, 255))
    cx2 = cx + 1.5 * G
    d.ellipse((cx2 - 4.9 * G, cy - 4.9 * G, cx2 + 4.9 * G, cy + 4.9 * G), fill=(227, 10, 23))
    sx = 20.5 * G; R = 3.0 * G; rin = R * 0.382
    pts = []
    for i in range(10):
        rr = R if i % 2 == 0 else rin
        ang = math.pi + i * math.pi / 5            # a point toward the crescent
        pts.append((sx + math.cos(ang) * rr, cy + math.sin(ang) * rr))
    d.polygon(pts, fill=(255, 255, 255))
    return np.asarray(img)

def tex_marking(w=512, h=256):
    """Turret side decal: "T-12" in white stencil letters with a small flag roundel, on transparent."""
    img = Image.new('RGBA', (w, h), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    font = None
    for path in ('/usr/share/fonts/truetype/google-fonts/Poppins-Bold.ttf', '/usr/share/fonts/truetype/dejavu/DejaVuSans-Bold.ttf'):
        if os.path.exists(path):
            font = ImageFont.truetype(path, 150); break
    if font is None: font = ImageFont.load_default()
    text = 'T-12'
    bbox = d.textbbox((0, 0), text, font=font)
    tw, th = bbox[2] - bbox[0], bbox[3] - bbox[1]
    x0 = w * 0.3 - bbox[0]; y0 = (h - th) / 2 - bbox[1]
    d.text((x0 + 4, y0 + 4), text, font=font, fill=(0, 0, 0, 120))
    d.text((x0, y0), text, font=font, fill=(245, 240, 230, 255))
    # roundel with the crescent and star
    r = h * 0.22; cx, cy = w * 0.15, h / 2
    d.ellipse((cx - r, cy - r, cx + r, cy + r), fill=(227, 10, 23, 255), outline=(245, 240, 230, 255), width=4)
    d.ellipse((cx - r * 0.55, cy - r * 0.55, cx + r * 0.55, cy + r * 0.55), fill=(255, 255, 255, 255))
    d.ellipse((cx - r * 0.3, cy - r * 0.45, cx + r * 0.6, cy + r * 0.45), fill=(227, 10, 23, 255))
    pts = []
    for i in range(10):
        rr = r * 0.28 if i % 2 == 0 else r * 0.11
        ang = math.pi + i * math.pi / 5
        pts.append((cx + r * 0.42 + math.cos(ang) * rr, cy + math.sin(ang) * rr))
    d.polygon(pts, fill=(255, 255, 255, 255))
    # stencil wear
    arr = np.asarray(img).astype(float)
    rng = np.random.default_rng(3)
    wear = smooth(fbm(h, 2.0, 77, hi=30), 0.6, 1.4)
    wear = np.asarray(Image.fromarray((wear * 255).astype(np.uint8)).resize((w, h))).astype(float) / 255.0
    arr[..., 3] *= (1 - wear * 0.7)
    return arr.astype(np.uint8)

# ----------------------------------------------------------------------------- materials
def materials():
    p = 'tiger'
    return {
        'camo':      dict(name=p + '_camo', pbr=dict(baseColorFactor=[1, 1, 1, 1], metallicFactor=0.25, roughnessFactor=0.72, tex='camo', normal='camo_n')),
        'track':     dict(name=p + '_track', pbr=dict(baseColorFactor=[1, 1, 1, 1], metallicFactor=0.55, roughnessFactor=0.6, tex='track', normal='track_n')),
        'dark_steel': dict(name=p + '_dark_steel', pbr=dict(baseColorFactor=[0.3, 0.3, 0.32, 1], metallicFactor=0.6, roughnessFactor=0.55, tex='steel')),
        'steel':     dict(name=p + '_steel', pbr=dict(baseColorFactor=[0.36, 0.36, 0.37, 1], metallicFactor=0.7, roughnessFactor=0.45, tex='steel')),
        'VehicleRubber': dict(name='VehicleRubber', pbr=dict(baseColorFactor=[1, 1, 1, 1], metallicFactor=0.0, roughnessFactor=0.9, tex='rubber')),
        'wood':      dict(name=p + '_wood', pbr=dict(baseColorFactor=[1, 1, 1, 1], metallicFactor=0.0, roughnessFactor=0.85, tex='wood')),
        'flag':      dict(name=p + '_flag', pbr=dict(baseColorFactor=[1, 1, 1, 1], metallicFactor=0.0, roughnessFactor=0.8, tex='flag')),
        'marking':   dict(name=p + '_marking', pbr=dict(baseColorFactor=[1, 1, 1, 1], metallicFactor=0.1, roughnessFactor=0.7, tex='marking'), alphaMode='BLEND'),
        'black':     dict(name=p + '_black', pbr=dict(baseColorFactor=[0.04, 0.04, 0.045, 1], metallicFactor=0.1, roughnessFactor=0.85)),
        'headlight': dict(name=p + '_headlight', pbr=dict(baseColorFactor=[0.9, 0.9, 0.85, 1], metallicFactor=0.0, roughnessFactor=0.3), emissive=[1.0, 0.92, 0.75], strength=2.0),
    }

def textures():
    camo, hc = tex_camo()
    track, ht = tex_track()
    return {
        'camo': camo, 'camo_n': cf.tex_normal(hc, 1.6, 512),
        'track': track, 'track_n': cf.tex_normal(ht, 2.5, 256),
        'steel': cf.tex_steel(), 'rubber': cf.tex_rubber(), 'wood': tex_wood(),
        'flag': tex_flag(), 'marking': tex_marking(),
    }

# ----------------------------------------------------------------------------- geometry helpers
S = np.array([1, 1, 1], float)

def belt(path, width, thick, seg_uv=1.0):
    """Closed track belt along a (z, y) path: outer/inner/side faces, UV v = run length in metres."""
    P = np.array([[0.0, y, z] for z, y in path])
    n = len(P)
    V, N, U, T = [], [], [], []
    # per-station tangent and outward normal (in the zy plane, outward = away from the loop centre)
    centre = P.mean(0)
    tang = np.zeros_like(P); out = np.zeros_like(P)
    for i in range(n):
        t = P[(i + 1) % n] - P[i - 1]
        t = t / (np.linalg.norm(t) + 1e-9)
        tang[i] = t
        o = np.cross(np.array([1.0, 0, 0]), t)       # perpendicular in the zy plane
        if np.dot(o, P[i] - centre) < 0: o = -o
        out[i] = o / (np.linalg.norm(o) + 1e-9)
    s = np.zeros(n + 1)
    for i in range(1, n + 1): s[i] = s[i - 1] + np.linalg.norm(P[i % n] - P[i - 1])
    hw = width / 2; ht = thick / 2
    def face(pa, pb, pc, pd, normal, ua, va, ub, vb):
        b = sum(len(v) for v in V)
        V.append(np.array([pa, pb, pc, pd])); N.append(np.tile(normal, (4, 1)))
        U.append(np.array([[ua, va], [ub, va], [ub, vb], [ua, vb]]))
        tri = np.array([[0, 1, 2], [0, 2, 3]])
        tri = cf._orient(V[-1], tri, np.tile(normal, (2, 1)))
        T.append(tri + b)
    for i in range(n):
        j = (i + 1) % n
        for sign, nm in ((1, 'outer'), (-1, 'inner')):
            a = P[i] + out[i] * ht * sign; c = P[j] + out[j] * ht * sign
            pa = a + np.array([-hw, 0, 0]); pb = a + np.array([hw, 0, 0]); pc = c + np.array([hw, 0, 0]); pd = c + np.array([-hw, 0, 0])
            normal = (out[i] + out[j]) * 0.5 * sign
            face(pa, pb, pc, pd, normal / (np.linalg.norm(normal) + 1e-9), 0.0, s[i] * seg_uv, 1.0, s[i + 1] * seg_uv)
        for xs in (-1, 1):
            a_o = P[i] + out[i] * ht + np.array([xs * hw, 0, 0]); a_i = P[i] - out[i] * ht + np.array([xs * hw, 0, 0])
            c_o = P[j] + out[j] * ht + np.array([xs * hw, 0, 0]); c_i = P[j] - out[j] * ht + np.array([xs * hw, 0, 0])
            face(a_i, a_o, c_o, c_i, np.array([xs, 0, 0], float), 0.0, s[i] * seg_uv, 0.12, s[i + 1] * seg_uv)
    return np.concatenate(V), np.concatenate(N), np.concatenate(U), np.concatenate(T)

def arc(cz, cy, r, a0, a1, n):
    return [(cz + math.cos(math.radians(a)) * r, cy + math.sin(math.radians(a)) * r) for a in np.linspace(a0, a1, n)]

def road_wheel():
    """Interleaved road wheel: rubber tyre ring, dished steel disc, hub with bolts (axis X)."""
    m = Mesh('Wheel')
    w = 0.15
    tyre = [(0.33, -w / 2), (0.40, -w / 2 + 0.01, True), (0.40, w / 2 - 0.01, True), (0.33, w / 2)]
    m.add('VehicleRubber', lathe(tyre, seg=28))
    disc = [(0.0, -0.05), (0.33, -0.05, True), (0.33, 0.05, True), (0.0, 0.05)]
    m.add('dark_steel', lathe(disc, seg=24))
    m.add('dark_steel', cyl((-0.11, 0, 0), (0.11, 0, 0), 0.11, seg=14))
    for i in range(8):
        a = i * 2 * math.pi / 8
        m.add('dark_steel', cyl((0.1, math.cos(a) * 0.2, math.sin(a) * 0.2), (0.125, math.cos(a) * 0.2, math.sin(a) * 0.2), 0.025, seg=6))
    return m

def sprocket():
    m = Mesh('Sprocket')
    disc = [(0.0, -0.08), (0.42, -0.08, True), (0.42, 0.08, True), (0.0, 0.08)]
    m.add('dark_steel', lathe(disc, seg=24))
    m.add('dark_steel', cyl((-0.12, 0, 0), (0.12, 0, 0), 0.14, seg=14))
    for i in range(18):
        a = i * 2 * math.pi / 18
        c = (0, math.cos(a) * 0.45, math.sin(a) * 0.45)
        m.add('dark_steel', tube((c[0] - 0.09, c[1], c[2]), (c[0] + 0.09, c[1], c[2]), 0.05, 0.08, up_hint=(0, math.cos(a), math.sin(a))))
    return m

def idler():
    m = Mesh('Idler')
    disc = [(0.0, -0.08), (0.36, -0.08, True), (0.36, 0.08, True), (0.0, 0.08)]
    m.add('dark_steel', lathe(disc, seg=24))
    m.add('dark_steel', cyl((-0.12, 0, 0), (0.12, 0, 0), 0.12, seg=14))
    return m

def track_links(m, centre, count, axis, size=(0.73, 0.06, 0.14), gap=0.02, yaw=0.0):
    """Spare track links stacked along `axis` (0 x, 1 y, 2 z)."""
    c = np.array(centre, float)
    step = size[axis] + gap
    for i in range(count):
        p = c.copy(); p[axis] += (i - (count - 1) / 2) * step
        m.add('track', box(p, size))

# ----------------------------------------------------------------------------- the tank
def build_hull():
    m = Mesh('Body')
    camo = 'camo'
    # lower hull and pannier (superstructure spanning the tracks)
    m.add(camo, rbox((0, 0.86, 0.0), (2.2, 0.78, 6.0), 0.04, 1))
    m.add(camo, rbox((0, 1.58, -0.1), (3.56, 0.66, 5.9), 0.03, 1))
    # glacis (lower front plate, raked) and the upper front plate detail
    m.add(camo, tube((0, 0.5, 2.78), (0, 1.26, 3.02), 2.2, 0.1, up_hint=(0, 0, 1)))
    m.add('black', box((-0.62, 1.74, 2.86), (0.5, 0.16, 0.06)))               # driver's visor
    m.add(camo, box((-0.62, 1.74, 2.87), (0.6, 0.3, 0.05)))                   # visor armour block
    m.add(camo, cyl((0.62, 1.68, 2.84), (0.62, 1.68, 3.0), 0.17, 0.12, seg=14))   # MG ball mount
    m.add('dark_steel', cyl((0.62, 1.68, 2.98), (0.62, 1.68, 3.28), 0.03, seg=8))  # MG barrel
    # turret ring
    m.add('dark_steel', cyl((0, 1.9, -0.15), (0, 1.98, -0.15), 1.0, seg=36))
    # engine deck: grilles, central hatch, filler caps
    for sx in (-1, 1):
        m.add('black', box((sx * 0.78, 1.925, -2.05), (1.2, 0.03, 1.3)))
        for k in range(7):
            m.add('dark_steel', box((sx * 0.78, 1.945, -2.6 + k * 0.18), (1.16, 0.02, 0.05)))
    m.add(camo, cyl((0, 1.91, -1.65), (0, 1.97, -1.65), 0.36, seg=20))
    m.add(camo, cyl((0, 1.91, -2.85), (0, 1.99, -2.85), 0.12, seg=12))
    # rear plate, exhausts with armoured covers, towing hooks
    for sx in (-1, 1):
        m.add('steel', cyl((sx * 0.55, 1.35, -3.06), (sx * 0.55, 2.3, -3.06), 0.08, seg=12))
        m.add(camo, rbox((sx * 0.55, 1.85, -3.1), (0.32, 0.95, 0.26), 0.03, 1))
        m.add('dark_steel', box((sx * 1.25, 1.0, -3.08), (0.22, 0.26, 0.14)))
        m.add('dark_steel', box((sx * 1.25, 1.0, 3.07), (0.22, 0.26, 0.14)))
    # fenders over the tracks with front and rear mudguards
    for sx in (-1, 1):
        m.add(camo, box((sx * 1.45, 1.27, 0.1), (0.76, 0.04, 6.2)))
        m.add(camo, tube((sx * 1.45, 1.25, 3.2), (sx * 1.45, 0.95, 3.5), 0.76, 0.04, up_hint=(0, 0, 1)))
        m.add(camo, tube((sx * 1.45, 1.25, -3.0), (sx * 1.45, 0.95, -3.3), 0.76, 0.04, up_hint=(0, 0, -1)))
        # side pannier detail: bolted plate edge and the tow cable
        m.add('dark_steel', tube((sx * 1.79, 1.3, 2.6), (sx * 1.79, 1.3, -2.6), 0.03, 0.03, r=0.01))
        m.add('dark_steel', tube((sx * 1.79, 1.3, 2.6), (sx * 1.79, 1.6, 2.9), 0.03, 0.03))
    # spare track links on the glacis and the tools
    track_links(m, (0.0, 1.08, 2.98), 4, 0, size=(0.14, 0.73, 0.06), gap=0.03)
    # right side: shovel and axe on the fender; left side: a log bundle and the jack block
    m.add('wood', cyl((1.5, 1.33, 1.6), (1.5, 1.33, 0.2), 0.02, seg=8))                 # shovel handle
    m.add('dark_steel', box((1.5, 1.33, 1.78), (0.18, 0.02, 0.36)))                   # shovel blade
    m.add('wood', cyl((1.4, 1.33, -0.4), (1.4, 1.33, -1.4), 0.02, seg=8))               # axe handle
    m.add('dark_steel', box((1.4, 1.36, -0.3), (0.1, 0.06, 0.2)))                      # axe head
    m.add('dark_steel', box((1.5, 1.4, -2.2), (0.4, 0.22, 0.5)))                       # jack block / stowage box
    for k, (y, x) in enumerate(((1.42, -1.56), (1.42, -1.3), (1.66, -1.43))):
        m.add('wood', cyl((x, y, -1.5), (x, y, 1.3), 0.13, seg=12))                    # log bundle (3 logs)
    for z in (-1.0, 0.8):
        m.add('dark_steel', tube((-1.6, 1.3, z), (-1.25, 1.84, z), 0.05, 0.02))        # log straps
    # Bosch headlamp (left, as on the real tank) and a second lamp on the right fender for the beams
    for sx in (-1, 1):
        m.add('dark_steel', cyl((sx * 1.3, 1.95, 2.62), (sx * 1.3, 1.95, 2.8), 0.13, 0.12, seg=14))
        m.add('headlight', cyl((sx * 1.3, 1.95, 2.8), (sx * 1.3, 1.95, 2.83), 0.11, seg=14))
    # hull side marking decals (small) and the pannier bolt line are in the texture; antenna base is on the turret
    return m

def turret_outline(scale=1.0):
    pts = []
    for a in np.linspace(90, -90, 17):
        pts.append((math.cos(math.radians(a)) * 0.98 * scale, 0.3 + math.sin(math.radians(a)) * 0.98 * scale))
    # rear: taper to the stowage bin
    pts += [(0.95 * scale, -0.2), (0.82 * scale, -1.05), (0.5 * scale, -1.18), (-0.5 * scale, -1.18), (-0.82 * scale, -1.05), (-0.95 * scale, -0.2)]
    return pts

def build_turret():
    m = Mesh('Turret')
    camo = 'camo'
    bottom = [(x, 0.0, z) for x, z in turret_outline(1.0)]
    mid = [(x, 0.42, z) for x, z in turret_outline(1.0)]
    top = [(x, 0.8, z) for x, z in turret_outline(0.93)]
    m.add(camo, loft([bottom, mid, top]))
    m.add(camo, fan([(x, 0.8, z) for x, z in turret_outline(0.93)], (0, 1, 0)))
    m.add(camo, fan([(x, 0.0, z) for x, z in turret_outline(1.0)][::-1], (0, -1, 0)))
    # mantlet, cupola with vision slits and hatch, loader hatch, stowage bin with straps
    m.add(camo, rbox((0, 0.4, 1.08), (1.3, 0.58, 0.34), 0.05, 1))
    m.add(camo, cyl((-0.55, 0.78, -0.35), (-0.55, 1.1, -0.35), 0.36, seg=20))
    m.add(camo, cyl((-0.55, 1.1, -0.35), (-0.55, 1.14, -0.35), 0.32, seg=20))
    for i in range(7):
        a = i * 2 * math.pi / 7
        m.add('black', box((-0.55 + math.cos(a) * 0.36, 0.98, -0.35 + math.sin(a) * 0.36), (0.1, 0.07, 0.1)))
    m.add(camo, rbox((0.5, 0.82, -0.3), (0.5, 0.04, 0.55), 0.01, 1))
    m.add('dark_steel', box((0.5, 0.85, -0.52), (0.14, 0.04, 0.06)))
    m.add(camo, rbox((0, 0.36, -1.4), (1.5, 0.5, 0.42), 0.04, 1))
    for x in (-0.45, 0.45):
        m.add('dark_steel', box((x, 0.36, -1.4), (0.05, 0.54, 0.46)))
    # spare track links on both turret sides
    for sx in (-1, 1):
        track_links(m, (sx * 1.0, 0.4, 0.05), 4, 2, size=(0.06, 0.7, 0.14), gap=0.03)
    # "T-12" marking decals (slightly off the slanted side)
    for sx in (-1, 1):
        x = sx * 0.995
        zc, yc, w, h = -0.65, 0.42, 1.0, 0.5
        p = [(x, yc - h / 2, zc - w / 2 * sx), (x, yc - h / 2, zc + w / 2 * sx), (x, yc + h / 2, zc + w / 2 * sx), (x, yc + h / 2, zc - w / 2 * sx)]
        V, N, U, T = quad(p, (sx, 0, 0))
        U = np.array([[0, 0], [1, 0], [1, 1], [0, 1]], float)
        m.add('marking', (V, N, U, T))
    # antenna base and rod (the flag node sits at the rod top)
    m.add('dark_steel', cyl((-0.75, 0.78, -0.95), (-0.75, 0.95, -0.95), 0.045, seg=10))
    m.add('dark_steel', cyl((-0.75, 0.95, -0.95), (-0.75, 2.55, -0.95), 0.014, 0.008, seg=6))
    m.add('black', box((0.0, 0.1, 1.0), (0.4, 0.2, 0.2)))   # mantlet lower lip shadow block
    return m

def build_barrel():
    m = Mesh('Barrel')
    m.add('steel', cyl((0, 0, 0.0), (0, 0, 0.95), 0.125, 0.11, seg=16))             # recoil jacket
    m.add('steel', cyl((0, 0, 0.95), (0, 0, 4.5), 0.095, 0.07, seg=16))             # tube
    m.add('steel', cyl((0, 0, 4.5), (0, 0, 4.95), 0.11, seg=16))                    # muzzle brake body
    for z in (4.62, 4.8):
        m.add('steel', cyl((0, 0, z), (0, 0, z + 0.08), 0.16, seg=16))              # baffles
    m.add('black', cyl((0, 0, 4.95), (0, 0, 4.97), 0.06, seg=12))                   # bore
    # coaxial MG beside the gun
    m.add('dark_steel', cyl((0.28, 0.05, 0.0), (0.28, 0.05, 0.9), 0.025, seg=8))
    return m

def build_flag():
    """Cloth quad in the Flag node space: pole at the origin, free end along -Z (rearward), hanging down."""
    m = Mesh('Flag')
    nx, ny = 12, 7
    L, H = 0.9, 0.6
    for side in (1, -1):
        V, N, U, T = [], [], [], []
        for j in range(ny + 1):
            for i in range(nx + 1):
                V.append((0.0, -H + H * j / ny, -L * i / nx)); U.append((i / nx, j / ny)); N.append((side, 0, 0))
        V = np.array(V); U = np.array(U); N = np.array(N, float)
        for j in range(ny):
            for i in range(nx):
                a = j * (nx + 1) + i; b = a + 1; c = a + nx + 1; d = c + 1
                T.append([a, b, d]); T.append([a, d, c])
        T = cf._orient(V, np.array(T), np.tile([side, 0, 0], (len(T), 1)).astype(float))
        m.add('flag', (V, N, U, T))
    return m

def build_tracks(side):
    """One track belt around the running gear of a side (x offset applied by the node)."""
    path = []
    path += [(z, 0.045) for z in np.linspace(-1.9, 2.0, 9)]                 # ground run
    path += arc(2.5, 0.62, 0.52, -78, 78, 10)                                # around the sprocket
    path += [(z, y) for z, y in zip(np.linspace(2.2, -2.3, 9), np.linspace(1.12, 0.92, 9))]   # top run (under the fender)
    path += arc(-2.55, 0.5, 0.43, 100, 262, 10)                              # around the idler
    return belt(path, 0.725, 0.09)

def build_lod1():
    m = Mesh('LOD1')
    m.add('camo', box((0, 0.86, 0.0), (2.2, 0.78, 6.0)))
    m.add('camo', box((0, 1.58, -0.1), (3.56, 0.66, 5.9)))
    for sx in (-1, 1):
        m.add('track', box((sx * 1.45, 0.55, 0.0), (0.74, 1.1, 5.4)))
    m.add('camo', box((0, 2.35, -0.3), (1.95, 0.8, 2.2)))
    m.add('steel', cyl((0, 2.33, 0.8), (0, 2.33, 5.9), 0.1, 0.07, seg=6))
    m.add('camo', cyl((-0.55, 2.75, -0.5), (-0.55, 3.05, -0.5), 0.36, seg=8))
    return m

class TankGlb(Glb):
    """Same writer, JPEG for the big colour maps (normals and the alpha decal stay PNG)."""
    def texture(self, key, rgb, jpeg):
        return Glb.texture(self, key, rgb, key in ('camo', 'track', 'steel', 'rubber', 'wood', 'flag'))

def export(out_dir):
    tex = textures(); g = TankGlb()
    mdefs = materials(); matindex = {}
    body = build_hull(); turret = build_turret(); barrel = build_barrel(); flag = build_flag()
    wheel = road_wheel(); spr = sprocket(); idl = idler(); lod = build_lod1()
    trackL = build_tracks(-1); trackR = build_tracks(1)
    used = set()
    for mm in (body, turret, barrel, flag, wheel, spr, idl, lod): used |= set(mm.parts)
    used |= {'track'}
    for k in mdefs:
        if k in used: matindex[k] = g.material(mdefs[k], tex)
    # flag material double sided (both faces exist anyway); marking keeps alphaMode BLEND
    mi = {}
    mi['body'] = g.mesh('tiger_Body', body.flat(), matindex)
    mi['turret'] = g.mesh('tiger_Turret', turret.flat(), matindex)
    mi['barrel'] = g.mesh('tiger_Barrel', barrel.flat(), matindex)
    mi['flag'] = g.mesh('tiger_Flag', flag.flat(), matindex)
    mi['wheel'] = g.mesh('tiger_RoadWheel', wheel.flat(), matindex)
    mi['spr'] = g.mesh('tiger_Sprocket', spr.flat(), matindex)
    mi['idl'] = g.mesh('tiger_Idler', idl.flat(), matindex)
    mi['trackL'] = g.mesh('tiger_TrackL', [('track',) + tuple(trackL)], matindex)
    mi['trackR'] = g.mesh('tiger_TrackR', [('track',) + tuple(trackR)], matindex)
    mi['lod'] = g.mesh('tiger_LOD1', lod.flat(), matindex)

    nodes = [g.node('Body', mi['body'])]
    # suspension anchors (no mesh): the car physics reads these names
    nodes += [g.node('Wheel_FL', None, (-1.4, 0.4, 2.4)), g.node('Wheel_FR', None, (1.4, 0.4, 2.4)),
              g.node('Wheel_RL', None, (-1.4, 0.4, -2.4)), g.node('Wheel_RR', None, (1.4, 0.4, -2.4))]
    for sx, tag, mesh in ((-1, 'L', 'trackL'), (1, 'R', 'trackR')):
        nodes.append(g.node('Track_' + tag, mi[mesh], (sx * 1.45, 0, 0)))
        for k in range(8):
            z = -1.75 + 0.5 * k
            x = sx * (1.45 + (0.17 if k % 2 == 0 else -0.17))
            nodes.append(g.node('Road_%s%d' % (tag, k + 1), mi['wheel'], (x, 0.4, z)))
        nodes.append(g.node('Sprocket_' + tag, mi['spr'], (sx * 1.45, 0.62, 2.5)))
        nodes.append(g.node('Idler_' + tag, mi['idl'], (sx * 1.45, 0.5, -2.55)))
    n_flag = g.node('Flag', mi['flag'], (-0.75, 2.55, -0.95))
    n_barrel = g.node('Barrel', mi['barrel'], (0, 0.4, 1.1))
    n_turret = g.node('Turret', mi['turret'], (0, 1.97, -0.15), [n_barrel, n_flag])
    nodes.append(n_turret)
    nodes.append(g.node('Body_LOD1', mi['lod']))
    root = g.node('tiger_Root', None, None, nodes)
    path = os.path.join(out_dir, 'tiger_t12.glb')
    g.write(path, root)
    tris = body.tris() + turret.tris() + barrel.tris() + flag.tris() + 16 * wheel.tris() + 2 * spr.tris() + 2 * idl.tris() + len(trackL[3]) * 2
    print('tiger_t12  %d tris (body %d, turret %d, barrel %d, wheel %d x16, track %d x2, lod1 %d)  %.2f MB' % (
        tris, body.tris(), turret.tris(), barrel.tris(), wheel.tris(), len(trackL[3]), lod.tris(), os.path.getsize(path) / 1e6))

if __name__ == '__main__':
    out = sys.argv[1] if len(sys.argv) > 1 else os.path.join(os.path.dirname(os.path.abspath(__file__)), '..', 'unity-game', 'Assets', 'StreamingAssets', 'Models', 'vehicles')
    os.makedirs(out, exist_ok=True)
    export(out)
