#!/usr/bin/env python3
"""
DERBY ARENA 2.1 vehicle forge: builds the five vehicle GLBs procedurally (numpy + Pillow only).

Design space: metres, +X right, +Y up, +Z FRONT. Export flips Z (glTF front = -Z) and reverses the
winding, so the files load through MiniGlb exactly like the 1.x models: same node names (Body, Hood,
Wheel_FL/FR/RL/RR, Turret > Barrel), same wheel positions and radii (physics unchanged), "paint"
material recoloured per vehicle by the game, emissive lights, blended glass.

Run:  python3 scripts/carforge.py [out_dir]   (default: unity-game/Assets/StreamingAssets/Models/vehicles)
"""
import io, json, math, os, struct, sys
import numpy as np
from PIL import Image, ImageDraw, ImageFilter

UV = 0.5   # texture repeats every 2 m

# ======================================================================= geometry primitives
def _normalize(v):
    n = np.linalg.norm(v, axis=-1, keepdims=True)
    return v / np.where(n < 1e-12, 1.0, n)

def _orient(V, T, outward):
    """Flip triangles whose geometric normal disagrees with an outward hint (per triangle)."""
    T = np.asarray(T, dtype=np.int64).copy()
    a, b, c = V[T[:, 0]], V[T[:, 1]], V[T[:, 2]]
    g = np.cross(b - a, c - a)
    o = outward if outward.ndim == 2 else np.broadcast_to(outward, g.shape)
    flip = np.einsum('ij,ij->i', g, o) < 0
    T[flip, 1], T[flip, 2] = T[flip, 2], T[flip, 1]
    return T

def _smooth_normals(V, T):
    N = np.zeros_like(V)
    a, b, c = V[T[:, 0]], V[T[:, 1]], V[T[:, 2]]
    fn = np.cross(b - a, c - a)
    for k in range(3):
        np.add.at(N, T[:, k], fn)
    return _normalize(N)

def _planar_uv(V, axis):
    """Box-mapped UVs from the two axes orthogonal to `axis` (0..2)."""
    ax = [i for i in range(3) if i != axis]
    return V[:, ax] * UV

def box(c, s, uv=UV):
    return rbox(c, s, 0.0, 0)

def rbox(c, s, r=0.0, k=2):
    """Rounded box (Minkowski box + sphere) with smooth normals; k=0 gives a plain box with flat faces."""
    c = np.asarray(c, float); s = np.asarray(s, float); h = s / 2.0
    r = min(r, float(h.min()) * 0.98) if k > 0 else 0.0
    V, N, U, T = [], [], [], []
    base = 0
    for axis in range(3):
        for sign in (-1.0, 1.0):
            a1, a2 = [i for i in range(3) if i != axis]
            def samples(hh):
                if r <= 0 or k <= 0: return np.array([-hh, hh])
                i = hh - r
                left = [-hh] + [-(i + r * math.tan(math.radians(45.0 * (k - j) / k))) for j in range(1, k)] + [-i]
                right = [i] + [i + r * math.tan(math.radians(45.0 * j / k)) for j in range(1, k)] + [hh]
                return np.array(left + right)
            u = samples(h[a1]); v = samples(h[a2])
            uu, vv = np.meshgrid(u, v, indexing='ij')
            P = np.zeros(uu.shape + (3,))
            P[..., a1] = uu; P[..., a2] = vv; P[..., axis] = sign * h[axis]
            if r > 0:
                inner = h - r
                Q = np.clip(P, -inner, inner)
                Q[..., axis] = sign * inner[axis]
                n = _normalize(P - Q)
                S = Q + r * n
            else:
                S = P
                n = np.zeros_like(P); n[..., axis] = sign
            nu, nv = uu.shape
            Vf = S.reshape(-1, 3); Nf = n.reshape(-1, 3)
            idx = np.arange(nu * nv).reshape(nu, nv)
            q = np.stack([idx[:-1, :-1], idx[1:, :-1], idx[1:, 1:], idx[:-1, 1:]], -1).reshape(-1, 4)
            Tf = np.concatenate([q[:, [0, 1, 2]], q[:, [0, 2, 3]]])
            Tf = _orient(Vf, Tf, Nf[Tf[:, 0]])
            V.append(Vf + c); N.append(Nf); U.append(_planar_uv(Vf + c, axis)); T.append(Tf + base)
            base += len(Vf)
    return np.concatenate(V), np.concatenate(N), np.concatenate(U), np.concatenate(T)

def _basis(d, up_hint=(0, 1, 0)):
    f = _normalize(np.asarray(d, float))
    up = np.asarray(up_hint, float)
    if abs(np.dot(f, _normalize(up))) > 0.99: up = np.array([1.0, 0, 0])
    r = _normalize(np.cross(up, f)); u = np.cross(f, r)
    return r, u, f

def cyl(p0, p1, r0, r1=None, seg=16, caps=True):
    """Cylinder / cone from p0 to p1, smooth sides, flat caps."""
    p0 = np.asarray(p0, float); p1 = np.asarray(p1, float)
    if r1 is None: r1 = r0
    r, u, f = _basis(p1 - p0)
    ang = np.linspace(0, 2 * math.pi, seg, endpoint=False)
    ring = np.outer(np.cos(ang), r) + np.outer(np.sin(ang), u)
    V = np.concatenate([p0 + ring * r0, p1 + ring * r1])
    L = np.linalg.norm(p1 - p0)
    slope = (r0 - r1) / max(L, 1e-6)
    N = _normalize(np.concatenate([ring + f * slope, ring + f * slope]))
    i = np.arange(seg); j = (i + 1) % seg
    T = np.concatenate([np.stack([i, j, j + seg], 1), np.stack([i, j + seg, i + seg], 1)])
    T = _orient(V, T, N[T[:, 0]])
    U = np.stack([np.tile(ang / (2 * math.pi) * 2.0, 2), np.concatenate([np.zeros(seg), np.full(seg, L * UV)])], 1)
    if caps:
        for pc, rc, nrm in ((p0, r0, -f), (p1, r1, f)):
            if rc <= 0: continue
            b = len(V)
            cv = np.concatenate([[pc], pc + ring * rc])
            V = np.concatenate([V, cv]); N = np.concatenate([N, np.tile(nrm, (seg + 1, 1))])
            U = np.concatenate([U, np.concatenate([[[0.5, 0.5]], 0.5 + 0.5 * np.stack([np.cos(ang), np.sin(ang)], 1)]) * 0.3])
            ct = np.stack([np.zeros(seg, int), 1 + i, 1 + j], 1) + b
            T = np.concatenate([T, _orient(V, ct, np.tile(nrm, (seg, 1)))])
    return V, N, U, T

def tube(p0, p1, w, h, up_hint=(0, 1, 0), r=0.0, k=1):
    """Oriented (rounded) box from p0 to p1 with cross-section w x h."""
    p0 = np.asarray(p0, float); p1 = np.asarray(p1, float)
    L = np.linalg.norm(p1 - p0)
    V, N, U, T = rbox((0, 0, 0), (w, h, L), r, k)
    rt, up, f = _basis(p1 - p0, up_hint)
    R = np.stack([rt, up, f], 1)
    V = V @ R.T + (p0 + p1) / 2; N = N @ R.T
    return V, N, U, T

def quad(p, outward, uv_axis=None):
    """Flat quad from 4 corner points (any order), outward hint for the winding."""
    P = np.asarray(p, float)
    n = _normalize(np.cross(P[1] - P[0], P[2] - P[0]))
    if np.dot(n, outward) < 0: n = -n
    ax = int(np.argmax(np.abs(n))) if uv_axis is None else uv_axis
    T = _orient(P, np.array([[0, 1, 2], [0, 2, 3]]), np.tile(n, (2, 1)))
    return P, np.tile(n, (4, 1)), _planar_uv(P, ax), T

def lathe(profile, seg=24, inside=False):
    """Revolve (radius, t) profile points around the X axis (t along the axis). Points marked hard are split.
    Convex shells (tyres) are traversed from -t to +t along the outside; inside=True for bowl shapes (rim dish)."""
    pts = []
    for p in profile:
        r_, t_, hard = (p + (False,))[:3]
        pts.append((r_, t_))
        if hard: pts.append((r_, t_))
    ang = np.linspace(0, 2 * math.pi, seg, endpoint=False)
    rings = []
    for (r_, t_) in pts:
        ring = np.stack([np.full(seg, t_), r_ * np.cos(ang), r_ * np.sin(ang)], 1)
        rings.append(ring)
    V = np.concatenate(rings)
    i = np.arange(seg); j = (i + 1) % seg
    T = []
    for s in range(len(pts) - 1):
        a = s * seg; b = (s + 1) * seg
        T.append(np.stack([a + i, a + j, b + j], 1)); T.append(np.stack([a + i, b + j, b + i], 1))
    T = np.concatenate(T)
    # outward hint per segment from the profile tangent: (-dr, dt) for convex shells, (dr, -dt) for bowls
    tt0 = np.array([t for _, t in pts]); rr0 = np.array([r for r, _ in pts])
    hint = np.zeros((len(T), 3))
    seg_of = (T[:, 0] // seg)
    for s_ in range(len(pts) - 1):
        dr = rr0[s_ + 1] - rr0[s_]; dt = tt0[s_ + 1] - tt0[s_]
        ht, hr = (dr, -dt) if inside else (-dr, dt)
        sel = seg_of == s_
        if not sel.any(): continue
        rad = V[T[sel, 0]].copy(); rad[:, 0] = 0; rad = _normalize(rad + 1e-9)
        hint[sel] = rad * hr + np.array([ht, 0, 0])
    T = _orient(V, T, hint)
    # drop degenerate (collapsed) triangles
    a, b, c = V[T[:, 0]], V[T[:, 1]], V[T[:, 2]]
    T = T[np.linalg.norm(np.cross(b - a, c - a), axis=1) > 1e-10]
    N = _smooth_normals(V, T)
    tt = np.array([t for _, t in pts]); rr = np.array([r for r, _ in pts])
    # UV: u around, v = profile arc length / total
    arc = np.concatenate([[0], np.cumsum(np.hypot(np.diff(rr), np.diff(tt)))])
    arc = arc / max(arc[-1], 1e-6)
    U = np.stack([np.tile(ang / (2 * math.pi), len(pts)), np.repeat(arc, seg)], 1)
    return V, N, U, T

def loft(sections, outward_centre=None, close=True):
    """Skin between successive rings (same vertex count). Smooth normals (rings carry duplicates at creases)."""
    S = np.asarray(sections, float)            # (ns, K, 3)
    ns, K, _ = S.shape
    V = S.reshape(-1, 3)
    i = np.arange(K); j = (i + 1) % K if close else np.arange(1, K)
    if not close: i = i[:-1]
    T = []
    for s in range(ns - 1):
        a = s * K; b = (s + 1) * K
        T.append(np.stack([a + i, a + j, b + j], 1)); T.append(np.stack([a + i, b + j, b + i], 1))
    T = np.concatenate(T)
    a, b, c = V[T[:, 0]], V[T[:, 1]], V[T[:, 2]]
    T = T[np.linalg.norm(np.cross(b - a, c - a), axis=1) > 1e-10]
    # outward hint from the ring polygon itself (2.1.1): the in-plane normal of the ring edge each triangle sits
    # on, sided by the ring's Newell normal. The old centroid hint failed where a ring is non-convex (the wheel-well
    # notch of the hull ring): there the centroid lies outside the notch and the arch ceilings came out inward.
    Pn = np.roll(S, -1, axis=1)
    newell = np.cross(S, Pn).sum(1)                                   # (ns, 3), ring orientation x 2*area
    newell = newell / np.maximum(np.linalg.norm(newell, axis=1, keepdims=True), 1e-12)
    edge = Pn - S                                                     # edge k: point k -> k+1
    en = np.cross(edge, newell[:, None, :])                           # outward in-plane edge normal (CCW ring)
    ring_of = T[:, 0] // K
    k_of = T[:, 0] % K                                                # every quad triangle starts at ring s, point i
    r0 = np.minimum(ring_of, ns - 1); r1 = np.minimum(ring_of + 1, ns - 1)
    hint = en[r0, k_of] + en[r1, k_of]
    centres = S.mean(1)
    fallback = V[T[:, 0]] - np.repeat(centres, K, 0)[T[:, 0]]
    weak = np.linalg.norm(hint, axis=1) < 1e-9
    hint[weak] = fallback[weak]
    T = _orient(V, T, hint)
    N = _smooth_normals(V, T)
    # UV: u = perimeter position, v = ring index scaled by z
    per = np.concatenate([np.zeros((ns, 1)), np.cumsum(np.linalg.norm(np.diff(S, axis=1), axis=2), 1)], 1)
    U = np.stack([per.reshape(-1) * UV, np.repeat(S[:, 0, 2], K) * UV], 1)
    return V, N, U, T

def fan(ring, outward):
    """Flat polygon cap from a ring of points."""
    P = np.asarray(ring, float); K = len(P)
    c = P.mean(0)
    V = np.concatenate([[c], P])
    i = np.arange(K); j = (i + 1) % K
    T = np.stack([np.zeros(K, int), 1 + i, 1 + j], 1)
    n = np.asarray(outward, float)
    T = _orient(V, T, np.tile(n, (K, 1)))
    N = np.tile(_normalize(n), (K + 1, 1))
    return V, N, _planar_uv(V, int(np.argmax(np.abs(n)))), T

# ======================================================================= accumulation
class Mesh:
    def __init__(self, name):
        self.name = name; self.parts = {}
    def add(self, mat, prim):
        V, N, U, T = prim
        if mat in ('seam', 'interior'): mat = 'black'
        p = self.parts.setdefault(mat, [[], [], [], [], 0])
        p[0].append(V); p[1].append(N); p[2].append(U); p[3].append(np.asarray(T) + p[4]); p[4] += len(V)
    def mirror_x(self, mat, prim):
        """Add a part and its X mirror (winding re-oriented by the stored normals)."""
        V, N, U, T = prim
        self.add(mat, prim)
        Vm = V * np.array([-1, 1, 1]); Nm = N * np.array([-1, 1, 1])
        Tm = T[:, [0, 2, 1]]
        self.add(mat, (Vm, Nm, U, Tm))
    def tris(self):
        return sum(len(t) for p in self.parts.values() for t in p[3])
    def flat(self):
        out = []
        for mat, p in self.parts.items():
            out.append((mat, np.concatenate(p[0]), np.concatenate(p[1]), np.concatenate(p[2]), np.concatenate(p[3])))
        return out

# ======================================================================= textures
def _noise(size, cells, rng, octaves=3):
    img = np.zeros((size, size))
    amp = 1.0
    for o in range(octaves):
        n = rng.random((cells * 2 ** o + 1, cells * 2 ** o + 1))
        im = Image.fromarray((n * 255).astype(np.uint8)).resize((size, size), Image.BICUBIC)
        img += (np.asarray(im).astype(float) / 255.0 - 0.5) * amp
        amp *= 0.5
    return img

def tex_paint(size=1024, seed=7):
    """Grey paint with mottling, scratches and chips; the game recolours it by luminance (mean ~0.56)."""
    rng = np.random.default_rng(seed)
    lum = 0.56 + 0.05 * _noise(size, 4, rng) + rng.normal(0, 0.012, (size, size))
    height = np.zeros((size, size))
    layer = Image.new('L', (size, size), 0); d = ImageDraw.Draw(layer)
    for _ in range(70):   # scratches
        x, y = rng.integers(0, size, 2); L = rng.integers(30, 220); a = rng.uniform(0, math.pi)
        d.line([(x, y), (x + L * math.cos(a), y + L * math.sin(a))], fill=int(rng.integers(90, 200)), width=int(rng.integers(1, 3)))
    scr = np.asarray(layer).astype(float) / 255.0
    lum += scr * 0.22; height -= scr * 0.5
    layer = Image.new('L', (size, size), 0); d = ImageDraw.Draw(layer)
    for _ in range(110):  # chips
        x, y = rng.integers(0, size, 2); r = rng.integers(2, 7)
        d.ellipse([x - r, y - r, x + r, y + r], fill=255)
    chip = np.asarray(layer).astype(float) / 255.0
    rim = np.asarray(layer.filter(ImageFilter.MaxFilter(3))).astype(float) / 255.0 - chip
    lum = lum * (1 - chip * 0.55) + rim * 0.12
    height -= chip * 1.0
    lum = np.clip(lum, 0, 1)
    rgb = np.stack([lum, lum * 0.995, lum * 0.99], -1)
    return (rgb * 255).astype(np.uint8), height

def tex_normal(height, strength=2.0, size=512):
    h = np.asarray(Image.fromarray((np.clip(height, -1, 1) * 127 + 128).astype(np.uint8)).resize((size, size), Image.BILINEAR)).astype(float) / 127.0 - 1.0
    gy, gx = np.gradient(h)
    n = np.stack([-gx * strength, -gy * strength, np.ones_like(h)], -1)
    n = _normalize(n)
    return ((n * 0.5 + 0.5) * 255).astype(np.uint8)

def tex_rubber(size=512, seed=3):
    """Tyre: u around (x), v along the profile (y): sidewall - tread - sidewall."""
    rng = np.random.default_rng(seed)
    img = np.full((size, size), 0.10) + rng.normal(0, 0.012, (size, size))
    y = np.arange(size)[:, None] / size; x = np.arange(size)[None, :] / size
    y = np.broadcast_to(y, (size, size)); x = np.broadcast_to(x, (size, size))
    tread = (y > 0.34) & (y < 0.66)
    blocks = ((np.floor(x * 24) + np.floor((y - 0.34) / 0.32 * 3)) % 2 == 0)
    groove = (np.abs(((x * 24) % 1.0) - 0.5) < 0.08) | (np.abs(((y - 0.34) / 0.32 * 3 % 1.0) - 0.5) < 0.1)
    img = np.where(tread, np.where(blocks, 0.17, 0.13), img)
    img = np.where(tread & groove, 0.05, img)
    ring = (np.abs(y - 0.2) < 0.012) | (np.abs(y - 0.8) < 0.012)
    img = img + ring * 0.05
    lettering = ((np.abs(y - 0.25) < 0.02) | (np.abs(y - 0.75) < 0.02)) & (((x * 40) % 1.0) < 0.5)
    img = img + lettering * 0.035
    img = np.clip(img, 0, 1)
    return (np.stack([img, img, img * 1.02], -1) * 255).astype(np.uint8)

def tex_steel(size=512, seed=5):
    rng = np.random.default_rng(seed)
    img = 0.30 + 0.06 * _noise(size, 6, rng) + rng.normal(0, 0.02, (size, size))
    streak = np.cumsum(rng.normal(0, 0.004, (size, size)), 1); streak -= streak.mean(1, keepdims=True)
    img += streak
    layer = Image.new('L', (size, size), 0); d = ImageDraw.Draw(layer)
    for _ in range(40):
        x, y = rng.integers(0, size, 2); L = rng.integers(20, 120); a = rng.uniform(0, math.pi)
        d.line([(x, y), (x + L * math.cos(a), y + L * math.sin(a))], fill=160, width=1)
    img += np.asarray(layer).astype(float) / 255.0 * 0.3
    img = np.clip(img, 0, 1)
    return (np.stack([img, img, img * 1.05], -1) * 255).astype(np.uint8)

def tex_rust(size=512, seed=9):
    rng = np.random.default_rng(seed)
    n1 = _noise(size, 5, rng, 4); n2 = _noise(size, 12, rng, 3)
    rust = np.clip(0.5 + n1 * 1.6, 0, 1)
    base = np.stack([0.30, 0.31, 0.32]) + n2[..., None] * 0.1
    rc = np.stack([0.42, 0.20, 0.09]) + n2[..., None] * np.array([0.15, 0.08, 0.03])
    img = base * (1 - rust[..., None]) + rc * rust[..., None]
    return (np.clip(img, 0, 1) * 255).astype(np.uint8), n1 * 0.6

TEX = {}
def textures():
    if TEX: return TEX
    paint, h = tex_paint()
    TEX['paint'] = paint; TEX['paint_n'] = tex_normal(h, 2.2)
    TEX['rubber'] = tex_rubber()
    TEX['steel'] = tex_steel()
    rust, hr = tex_rust()
    TEX['rust'] = rust; TEX['rust_n'] = tex_normal(hr, 3.0)
    return TEX

# ======================================================================= vehicle assembly
def hull_ring(z, p):
    """Closed outline of the lower body at station z (design space). p: dict of station params.
    Points are duplicated at hard creases so the loft keeps sharp wheel-arch edges."""
    x_side, y_floor, y_arch, x_in = p['x_side'], p['y_floor'], p['y_arch'], p['x_in']
    x_belt, y_belt, y_top, r = p['x_belt'], p['y_belt'], p['y_top'], p['r_belt']
    right = []   # (x, y, hard)
    right.append((0.0, y_floor, False))
    right.append((x_in, y_floor, True))
    right.append((x_in, y_arch, True))
    right.append((x_side, y_arch, True))
    # side skin with slight tumblehome up to the belt radius
    for t in (0.35, 0.7):
        right.append((x_side + (x_belt - x_side) * t, y_arch + (y_belt - r - y_arch) * t, False))
    cx, cy = x_belt - r, y_belt - r
    for a in (0.0, 30.0, 60.0, 90.0):
        right.append((cx + r * math.cos(math.radians(a)), cy + r * math.sin(math.radians(a)), False))
    # deck crown to the centre
    x0 = x_belt - r
    for t in (0.25, 0.5, 0.75, 1.0):
        x = x0 * (1 - t)
        right.append((x, y_belt + (y_top - y_belt) * (1 - (x / x0) ** 2), False))
    ring = []
    def emit(x, y, hard):
        ring.append((x, y, z))
        if hard: ring.append((x, y, z))
    for (x, y, hd) in right: emit(x, y, hd)
    for (x, y, hd) in reversed(right[1:-1]): emit(-x, y, hd)
    return np.array(ring)

def arch_profile(z, zw, ar, ah, y_floor):
    d = abs(z - zw)
    if d >= ar: return y_floor
    return y_floor + ah * math.sqrt(1 - (d / ar) ** 2)

def flare(z, zw, ar, amount):
    d = abs(z - zw) / (ar * 1.35)
    if d >= 1: return 0.0
    return amount * (0.5 + 0.5 * math.cos(math.pi * d))

def build_hull(m, spec):
    """Lofted lower body with wheel arches and flares, front/rear caps. Returns the profile sampler."""
    zr, zf = spec['z_rear'], spec['z_front']
    zw_r, zw_f = -spec['wheel_z'], spec['wheel_z']
    ar = spec['tire_r'] + 0.16; ah = spec['arch_top'] - spec['y_floor']
    # stations: ends, arch samples (7 each), and a few in between
    st = {zr, zf, zr + 0.18, zf - 0.22}
    for zw in (zw_r, zw_f):
        for a in np.linspace(0, math.pi, 9):
            st.add(zw + ar * math.cos(a))
    for z in np.linspace(zr + 0.5, zf - 0.5, 9): st.add(float(z))
    stations = sorted(z for z in st if zr <= z <= zf)
    rings = []
    for z in stations:
        t = (z - zr) / (zf - zr)
        p = dict(spec['station'](z, t))
        p['x_side'] = p['x_side'] + flare(z, zw_r, ar, spec['flare']) + flare(z, zw_f, ar, spec['flare'])
        p['x_belt'] = p['x_belt'] + 0.55 * (flare(z, zw_r, ar, spec['flare']) + flare(z, zw_f, ar, spec['flare']))
        p['y_arch'] = max(arch_profile(z, zw_r, ar, ah, p['y_floor']), arch_profile(z, zw_f, ar, ah, p['y_floor']))
        p['y_arch'] = min(p['y_arch'], p['y_belt'] - p['r_belt'] - 0.06)
        p['x_in'] = spec['x_in']
        rings.append(hull_ring(z, p))
    m.add('paint', loft(rings))
    m.add('paint', fan(rings[0], (0, 0, -1)))
    m.add('paint', fan(rings[-1], (0, 0, 1)))
    def top_at(z):
        z = min(max(z, zr), zf); t = (z - zr) / (zf - zr)
        return spec['station'](z, t)['y_top']
    def side_at(z):
        z = min(max(z, zr), zf); t = (z - zr) / (zf - zr)
        p = spec['station'](z, t)
        return p['x_side'] + flare(z, zw_r, ar, spec['flare']) + flare(z, zw_f, ar, spec['flare']), p['y_belt']
    return top_at, side_at

def build_cabin(m, spec, top_at):
    c = spec['cabin']
    yb = c['y_belt']; yr = c['y_roof']; xb = c['x_belt']; xr = c['x_roof']
    z_wsb, z_wst, z_rwt, z_rwb = c['z_ws_base'], c['z_ws_top'], c['z_rw_top'], c['z_rw_base']
    # glass: windshield, rear window, side glass (two panes split by the B pillar)
    m.add('glass', quad([(-xb, yb, z_wsb), (xb, yb, z_wsb), (xr, yr, z_wst), (-xr, yr, z_wst)], (0, 0.5, 1)))
    m.add('glass', quad([(-xb, yb, z_rwb), (xb, yb, z_rwb), (xr, yr, z_rwt), (-xr, yr, z_rwt)], (0, 0.5, -1)))
    zb = c['z_bpillar']
    for s in (-1, 1):
        m.add('glass', quad([(s * xb, yb, z_wsb), (s * xb, yb, zb), (s * xr, yr, zb), (s * xr, yr, z_wst)], (s, 0.3, 0)))
        m.add('glass', quad([(s * xb, yb, zb), (s * xb, yb, z_rwb), (s * xr, yr, z_rwt), (s * xr, yr, zb)], (s, 0.3, 0)))
    # roof panel
    m.add('paint', rbox((0, yr + 0.02, (z_wst + z_rwt) / 2), (2 * xr + 0.08, 0.08, z_wst - z_rwt + 0.1), 0.035, 2))
    # pillars and sills
    pw = c.get('pillar', 0.09)
    for s in (-1, 1):
        m.add('dark_steel', tube((s * xb, yb - 0.02, z_wsb), (s * xr, yr, z_wst), pw, pw, r=0.02))            # A
        m.add('dark_steel', tube((s * xb, yb - 0.02, zb), (s * xr, yr, zb), pw * 0.9, pw * 0.9, r=0.02))     # B
        m.add('dark_steel', tube((s * xb, yb - 0.02, z_rwb), (s * xr, yr, z_rwt), pw, pw, r=0.02))           # C
        m.add('dark_steel', tube((s * xb, yb + 0.01, z_wsb), (s * xb, yb + 0.01, z_rwb), 0.05, 0.05))        # belt rail
    m.add('dark_steel', tube((-xr, yr, z_wst), (xr, yr, z_wst), 0.06, 0.06))   # windshield header
    m.add('dark_steel', tube((-xr, yr, z_rwt), (xr, yr, z_rwt), 0.06, 0.06))   # rear header
    m.add('dark_steel', tube((-xb, yb + 0.01, z_wsb), (xb, yb + 0.01, z_wsb), 0.06, 0.05))  # cowl
    m.add('dark_steel', tube((-xb, yb + 0.01, z_rwb), (xb, yb + 0.01, z_rwb), 0.06, 0.05))  # rear deck lip
    # interior block (dash + seats) so the glass does not show painted deck
    m.add('interior', rbox((0, yb - 0.1, (z_wsb + z_rwb) / 2 - 0.1), (2 * xb - 0.2, 0.55, z_wsb - z_rwb - 0.3), 0.05, 1))
    m.add('interior', box((0, yb + 0.02, z_wsb - 0.22), (2 * xb - 0.15, 0.08, 0.4)))   # dashboard
    if c.get('cage'):
        r = 0.028
        for s in (-1, 1):
            m.add('dark_steel', cyl((s * (xr - 0.06), yb - 0.3, zb), (s * (xr - 0.06), yr - 0.06, zb), r, seg=8))
            m.add('dark_steel', cyl((s * (xr - 0.06), yr - 0.06, zb), (s * (xr - 0.1), yb + 0.05, z_wsb - 0.1), r, seg=8))
            m.add('dark_steel', cyl((s * (xr - 0.06), yr - 0.06, zb), (s * (xr - 0.08), yr - 0.08, z_rwt), r, seg=8))
        m.add('dark_steel', cyl((-(xr - 0.06), yr - 0.06, zb), ((xr - 0.06), yr - 0.06, zb), r, seg=8))

def build_details(m, spec, top_at, side_at):
    d = spec['details']; zf = spec['z_front']; zr = spec['z_rear']
    xs_f, yb_f = side_at(zf - 0.2); xs_r, yb_r = side_at(zr + 0.2)
    yt_f = top_at(zf); yt_r = top_at(zr)
    # ---- front: bumper, air dam, grille with slats, headlights, fog lights
    bw = d.get('bumper_w', xs_f * 2 + 0.06); by = d.get('bumper_y', spec['y_floor'] + 0.3)
    m.add(d.get('bumper_mat', 'chrome'), rbox((0, by, zf + 0.07), (bw, 0.2, 0.18), 0.05, 2))
    m.add('black', box((0, spec['y_floor'] + 0.12, zf + 0.01), (bw * 0.9, 0.2, 0.08)))
    gw, gh = d.get('grille_w', 1.2), d.get('grille_h', yt_f - by - 0.22)
    gy = by + 0.16 + gh / 2
    m.add('black', box((0, gy, zf - 0.03), (gw, gh, 0.08)))
    slats = d.get('slats', 5)
    for i in range(slats):
        yy = gy - gh / 2 + gh * (i + 0.5) / slats
        m.add('chrome', box((0, yy, zf + 0.015), (gw * 0.94, gh / slats * 0.45, 0.03)))
    hx = d.get('head_x', (xs_f * 0.74, xs_f * 0.5)); hy = d.get('head_y', gy); hs = d.get('head_s', 0.2)
    for s in (-1, 1):
        for x in hx:
            m.add('black', box((s * x, hy, zf - 0.02), (hs + 0.06, hs + 0.06, 0.06)))
            m.add('headlight', rbox((s * x, hy, zf + 0.012), (hs, hs, 0.05), 0.02, 1))
        if d.get('fog', True):
            m.add('headlight', cyl((s * bw * 0.36, by, zf + 0.12), (s * bw * 0.36, by, zf + 0.17), 0.06, seg=10))
    # ---- rear: bumper, tail light bar, exhaust, plate
    m.add(d.get('bumper_mat', 'chrome'), rbox((0, by, zr - 0.07), (xs_r * 2 + 0.04, 0.18, 0.16), 0.05, 2))
    tw = d.get('tail_w', xs_r * 2 - 0.3)
    m.add('black', box((0, yt_r - 0.17, zr - 0.02), (tw + 0.08, 0.18, 0.06)))
    for s in (-1, 1):
        m.add('taillight', rbox((s * (tw / 2 - tw * 0.22), yt_r - 0.17, zr - 0.062), (tw * 0.42, 0.12, 0.04), 0.015, 1))
    for s in d.get('exhaust', (-1, 1)):
        x = s * d.get('exhaust_x', 0.55)
        m.add('chrome', cyl((x, spec['y_floor'] + 0.1, zr + 0.4), (x, spec['y_floor'] + 0.08, zr - 0.12), 0.055, seg=12))
        m.add('black', cyl((x, spec['y_floor'] + 0.08, zr - 0.1), (x, spec['y_floor'] + 0.08, zr - 0.125), 0.04, seg=12))
    # ---- hood: scoop or power bulge, seams
    if d.get('scoop'):
        zc = d['scoop']; yt = top_at(zc)
        m.add('black', rbox((0, yt + 0.07, zc), (0.5, 0.16, 0.7), 0.04, 2))
        m.add('black', box((0, yt + 0.11, zc + 0.35), (0.38, 0.06, 0.03)))
    for s in (-1, 1):
        # door seams and handles
        for z in d.get('door_seams', ()):
            xs, yb = side_at(z)
            m.add('seam', box((s * (xs - 0.005), (yb + spec['y_floor'] + 0.1) / 2, z), (0.03, yb - spec['y_floor'] - 0.3, 0.018)))
        for z in d.get('handles', ()):
            xs, yb = side_at(z)
            m.add('chrome', rbox((s * (xs + 0.01), yb - 0.17, z), (0.03, 0.035, 0.17), 0.01, 1))
        # mirror
        if d.get('mirror'):
            z, y = d['mirror']; xs, yb = side_at(z)
            # short stalks: the mirrors stay inside the fender flares so the physics hull (fitted to the bounds) keeps its 1.x width
            m.add('black', tube((s * (xs - 0.02), y, z), (s * (xs + 0.09), y + 0.02, z), 0.05, 0.05))
            m.add('black', rbox((s * (xs + 0.1), y + 0.02, z), (0.07, 0.12, 0.18), 0.03, 1))
        # side skirt
        if d.get('skirt'):
            z0, z1 = d['skirt']; xs, yb = side_at((z0 + z1) / 2)
            m.add('black', rbox((s * (xs + 0.01), spec['y_floor'] + 0.08, (z0 + z1) / 2), (0.1, 0.14, z1 - z0), 0.03, 1))
    # hood seam lines
    hz = d.get('hood_seam')
    if hz:
        z0, z1, hw = hz
        for s in (-1, 1):
            m.add('seam', box((s * hw, top_at((z0 + z1) / 2) + 0.004, (z0 + z1) / 2), (0.018, 0.012, z1 - z0)))
        m.add('seam', box((0, top_at(z0) + 0.004, z0), (hw * 2, 0.012, 0.018)))
    for extra in d.get('extras', ()):
        extra(m, spec, top_at, side_at)

def build_underbody(m, spec):
    yf = spec['y_floor']; zw = spec['wheel_z']; tx = spec['track']
    m.add('black', box((0, yf - 0.03, 0), (spec['x_in'] * 2 - 0.1, 0.08, spec['z_front'] - spec['z_rear'] - 0.9)))
    for z in (-zw, zw):
        m.add('dark_steel', cyl((-tx + 0.1, spec['tire_r'], z), (tx - 0.1, spec['tire_r'], z), 0.06, seg=10))
    m.add('dark_steel', cyl((0, spec['tire_r'] - 0.02, -zw + 0.2), (0, spec['tire_r'] - 0.02, zw - 0.2), 0.05, seg=10))
    m.add('dark_steel', rbox((0, spec['tire_r'] - 0.02, -zw), (0.5, 0.36, 0.4), 0.08, 1))   # differential

def build_wheel(spec):
    """Tyre + rim, axis along X, centred at the origin. Shared by the four wheel nodes."""
    r = spec['tire_r']; w = spec['tire_w']; rim = r * 0.62
    m = Mesh('Wheel')
    prof = [(rim, -w / 2 + 0.02), (r * 0.9, -w / 2 + 0.02), (r * 0.98, -w / 2 + 0.06, True), (r, -w * 0.34), (r, w * 0.34),
            (r * 0.98, w / 2 - 0.06, True), (r * 0.9, w / 2 - 0.02), (rim, w / 2 - 0.02)]
    m.add('VehicleRubber', lathe(prof, seg=28))
    # rim dish: outer lip, dish, hub, 6 spokes (both sides use the same mesh)
    lip = [(rim + 0.01, -w / 2 + 0.03), (rim - 0.03, -w / 2 - 0.005), (rim * 0.55, -w / 2 + 0.05), (rim * 0.55, w / 2 - 0.05), (rim - 0.03, w / 2 + 0.005), (rim + 0.01, w / 2 - 0.03)]
    m.add('chrome', lathe(lip, seg=24, inside=True))
    m.add('dark_steel', lathe([(rim * 0.56, -w / 2 + 0.04), (rim * 0.56, w / 2 - 0.04)], seg=16))
    for s in (-1, 1):
        m.add('dark_steel', cyl((s * (w / 2 - 0.0), 0, 0), (s * (w / 2 + 0.03), 0, 0), rim * 0.28, seg=12))
        for i in range(6):
            a = i * math.pi / 3
            p0 = (s * (w / 2 - 0.02), 0, 0); p1 = (s * (w / 2 - 0.0), rim * 0.95 * math.cos(a), rim * 0.95 * math.sin(a))
            m.add('chrome', tube(p0, p1, 0.07, 0.05, up_hint=(1, 0, 0)))
    return m

def build_turret(spec):
    t = spec['turret']; k = spec.get('turret_scale', 1.0)
    m = Mesh('Turret'); b = Mesh('Barrel')
    m.add('dark_steel', cyl((0, 0, 0), (0, 0.12 * k, 0), 0.4 * k, seg=20))
    m.add('dark_steel', rbox((0, 0.33 * k, -0.05 * k), (0.62 * k, 0.34 * k, 0.74 * k), 0.05, 2))
    m.add('paint', rbox((0.33 * k, 0.3 * k, 0.05 * k), (0.08, 0.26 * k, 0.4 * k), 0.02, 1))
    m.add('black', box((0, 0.52 * k, -0.2 * k), (0.3 * k, 0.06, 0.24 * k)))
    if t == 'rockets':
        b.add('dark_steel', rbox((0, 0, 0.55), (0.44, 0.3, 1.1), 0.03, 1))
        for x in (-0.11, 0.11):
            for y in (-0.07, 0.07):
                b.add('black', cyl((x, y, 1.08), (x, y, 1.12), 0.06, seg=10))
    elif t == 'twin':
        for x in (-0.11, 0.11):
            b.add('dark_steel', cyl((x, 0, 0), (x, 0, 1.45), 0.065, seg=12))
            b.add('black', cyl((x, 0, 1.3), (x, 0, 1.5), 0.085, seg=12))
        b.add('dark_steel', box((0, 0, 0.35), (0.4, 0.2, 0.5)))
    else:
        b.add('dark_steel', cyl((0, 0, 0), (0, 0, 1.3), 0.06, 0.055, seg=12))
        b.add('black', cyl((0, 0, 1.15), (0, 0, 1.36), 0.08, seg=12))
        b.add('dark_steel', box((0, 0, 0.3), (0.26, 0.18, 0.5)))
    return m, b

# ======================================================================= vehicle specs
def station_muscle(z, t):
    yf = 0.30 + 0.07 * max(0, (z - 1.9) / 0.9) ** 2 + 0.05 * max(0, (-z - 2.2) / 0.6) ** 2
    hood = 1.14 - 0.12 * max(0, (z - 1.0) / 1.75) ** 1.4         # hood sloping down to the nose
    trunk = 1.08 - 0.08 * max(0, (-z - 1.4) / 1.4) ** 1.2
    y_top = min(hood, trunk)
    y_belt = min(y_top - 0.02, 1.06)
    taper = 1.0 - 0.09 * max(0, (z - 1.9) / 0.85) ** 2 - 0.05 * max(0, (-z - 2.3) / 0.5) ** 2
    return dict(x_side=1.30 * taper, x_belt=1.26 * taper, y_floor=yf, y_belt=y_belt, y_top=y_top, r_belt=0.11)

def station_drifter(z, t):
    yf = 0.26 + 0.07 * max(0, (z - 1.6) / 0.75) ** 2 + 0.04 * max(0, (-z - 1.8) / 0.5) ** 2
    hood = 0.98 - 0.16 * max(0, (z - 0.6) / 1.75) ** 1.3
    trunk = 0.96 - 0.1 * max(0, (-z - 1.2) / 1.1) ** 1.3
    y_top = min(hood, trunk)
    taper = 1.0 - 0.14 * max(0, (z - 1.5) / 0.85) ** 2 - 0.06 * max(0, (-z - 1.9) / 0.4) ** 2
    return dict(x_side=1.14 * taper, x_belt=1.10 * taper, y_floor=yf, y_belt=min(y_top - 0.02, 0.9), y_top=y_top, r_belt=0.08)

def station_rammer(z, t):
    yf = 0.30 + 0.05 * max(0, (z - 1.7) / 0.65) ** 2
    hood = 1.22 - 0.06 * max(0, (z - 0.9) / 1.45) ** 1.2
    y_top = hood if z > -1.9 else 1.22 - 0.05 * ((-z - 1.9) / 0.4)
    taper = 1.0 - 0.05 * max(0, (z - 1.8) / 0.55) ** 2
    return dict(x_side=1.18 * taper, x_belt=1.16 * taper, y_floor=yf, y_belt=min(y_top - 0.02, 1.14), y_top=y_top, r_belt=0.05)

def station_hunter(z, t):
    yf = 0.27 + 0.06 * max(0, (z - 1.7) / 0.65) ** 2 + 0.03 * max(0, (-z - 1.9) / 0.45) ** 2
    hood = 1.04 - 0.12 * max(0, (z - 0.8) / 1.55) ** 1.4
    trunk = 1.02 - 0.05 * max(0, (-z - 1.0) / 1.35) ** 1.5
    y_top = min(hood, trunk)
    taper = 1.0 - 0.1 * max(0, (z - 1.6) / 0.75) ** 2 - 0.04 * max(0, (-z - 2.0) / 0.35) ** 2
    return dict(x_side=1.15 * taper, x_belt=1.11 * taper, y_floor=yf, y_belt=min(y_top - 0.02, 0.96), y_top=y_top, r_belt=0.09)

def station_rig(z, t):
    yf = 0.42 + 0.06 * max(0, (z - 2.0) / 0.8) ** 2
    hood = 1.52 - 0.04 * max(0, (z - 0.8) / 2.0)
    y_top = hood if z > -0.6 else 1.30                           # bed behind the cab
    taper = 1.0 - 0.04 * max(0, (z - 2.2) / 0.6) ** 2
    return dict(x_side=1.36 * taper, x_belt=1.33 * taper, y_floor=yf, y_belt=min(y_top - 0.02, 1.44), y_top=y_top, r_belt=0.07)

def extra_rig(m, spec, top_at, side_at):
    zf = spec['z_front']
    # bull bar, stacks, bed armour, fuel tank, roof horns
    for y in (0.62, 0.95, 1.28):
        m.add('dark_steel', cyl((-1.3, y, zf + 0.28), (1.3, y, zf + 0.28), 0.045, seg=10))
    for s in (-1, 1):
        m.add('dark_steel', cyl((s * 1.05, 0.5, zf + 0.28), (s * 1.05, 1.4, zf + 0.28), 0.045, seg=10))
        m.add('dark_steel', cyl((s * 1.05, 0.9, zf + 0.0), (s * 1.05, 0.9, zf + 0.28), 0.04, seg=8))
        m.add('chrome', cyl((s * 1.18, 1.3, -0.95), (s * 1.18, 2.55, -0.95), 0.09, seg=12))
        m.add('black', cyl((s * 1.18, 2.55, -0.95), (s * 1.18, 2.6, -0.95), 0.1, seg=12))
        m.add('armor', rbox((s * 1.3, 1.52, -1.75), (0.08, 0.5, 1.9), 0.02, 1))
        m.add('chrome', cyl((s * 1.3, 0.7, -0.15), (s * 1.3, 0.7, -1.0), 0.22, seg=14))     # tanks (inside the hull width)
        m.add('dark_steel', box((s * 1.3, 0.94, -0.57), (0.46, 0.06, 0.9)))                # tank step
    m.add('armor', rbox((0, 1.33, -1.75), (2.4, 0.08, 1.9), 0.02, 1))              # bed floor
    m.add('armor', rbox((0, 1.52, -2.72), (2.7, 0.5, 0.1), 0.02, 1))               # tailgate
    m.add('dark_steel', rbox((0, 1.45, -0.7), (2.5, 0.3, 0.14), 0.03, 1))          # headache rack base
    for i in range(5):
        m.add('dark_steel', cyl((-1.1 + i * 0.55, 1.45, -0.7), (-1.1 + i * 0.55, 2.35, -0.7), 0.035, seg=8))
    m.add('dark_steel', cyl((-1.15, 2.35, -0.7), (1.15, 2.35, -0.7), 0.04, seg=8))

def extra_rammer(m, spec, top_at, side_at):
    zf = spec['z_front']
    # plough: angled armour plate across the nose
    m.add('armor', tube((0, 0.16, zf + 0.34), (0, 0.92, zf + 0.06), 2.6, 0.08, up_hint=(0, 0, 1), r=0.02))
    for s in (-1, 1):
        m.add('dark_steel', cyl((s * 0.9, 0.5, zf - 0.3), (s * 0.9, 0.55, zf + 0.2), 0.04, seg=8))
    m.add('armor', rbox((0, top_at(1.45) + 0.03, 1.45), (1.3, 0.06, 0.9), 0.02, 1))     # hood plate
    for s in (-1, 1):
        for i in range(3):
            m.add('dark_steel', box((s * (0.3 + i * 0.3), top_at(1.45) + 0.07, 1.45), (0.06, 0.03, 0.06)))

def extra_hunter(m, spec, top_at, side_at):
    zr = spec['z_rear']
    # rocket pod on the trunk + antenna
    m.add('dark_steel', rbox((0, top_at(zr + 0.9) + 0.2, zr + 0.95), (0.9, 0.34, 0.95), 0.04, 1))
    for x in (-0.3, -0.1, 0.1, 0.3):
        for y in (-0.08, 0.08):
            m.add('black', cyl((x, top_at(zr + 0.9) + 0.2 + y, zr + 1.42), (x, top_at(zr + 0.9) + 0.2 + y, zr + 1.46), 0.055, seg=10))
    m.add('dark_steel', cyl((-0.9, top_at(zr + 0.9), zr + 0.7), (-0.95, top_at(zr + 0.9) + 1.1, zr + 0.6), 0.015, seg=6))

def extra_drifter(m, spec, top_at, side_at):
    zr = spec['z_rear']
    # low rear diffuser fins
    for x in (-0.5, -0.17, 0.17, 0.5):
        m.add('black', box((x, spec['y_floor'] + 0.1, zr + 0.25), (0.03, 0.16, 0.5)))

def extra_muscle(m, spec, top_at, side_at):
    # chrome trim strip along the belt between the arches
    for s in (-1, 1):
        xs, yb = side_at(0.0)
        m.add('chrome', tube((s * (xs - 0.005), yb - 0.06, -1.05), (s * (xs - 0.005), yb - 0.06, 1.05), 0.03, 0.03))

SPECS = {
    'player_muscle': dict(prefix='breakline_interceptor', paint=(0.80, 0.38, 0.12), turret='gun',
        wheel_z=1.769, track=1.1956, wheel_y=0.5246, tire_r=0.52, tire_w=0.40,
        z_rear=-2.80, z_front=2.72, y_floor=0.30, x_in=0.9, arch_top=1.07, flare=0.14, station=station_muscle,
        cabin=dict(y_belt=1.10, y_roof=1.66, x_belt=1.20, x_roof=0.96, z_ws_base=0.95, z_ws_top=0.22, z_rw_top=-0.72, z_rw_base=-1.5, z_bpillar=-0.35, cage=True),
        details=dict(scoop=2.1, door_seams=(-0.75, 0.98), handles=(-0.5,), mirror=(0.8, 1.16), skirt=(-1.0, 1.0), hood_seam=(1.0, 2.55, 0.98),
                     head_x=(0.95, 0.68), head_s=0.2, slats=5, grille_w=1.0, exhaust_x=0.55, extras=(extra_muscle,)),
        turret_pos=(0, 1.66, -0.35), barrel_pos=(0, 0.3, 0.4), hood=None),
    'enemy_raider_a': dict(prefix='raiderA', paint=(0.7, 0.12, 0.14), turret='gun',
        wheel_z=1.45, track=0.98, wheel_y=0.43, tire_r=0.43, tire_w=0.30,
        z_rear=-2.28, z_front=2.26, y_floor=0.26, x_in=0.74, arch_top=0.89, flare=0.1, station=station_drifter,
        cabin=dict(y_belt=0.92, y_roof=1.38, x_belt=1.04, x_roof=0.78, z_ws_base=0.75, z_ws_top=-0.05, z_rw_top=-0.75, z_rw_base=-1.35, z_bpillar=-0.42, pillar=0.07),
        details=dict(door_seams=(-0.7, 0.8), handles=(-0.4,), mirror=(0.6, 0.98), skirt=(-0.85, 0.85), hood_seam=(0.8, 2.1, 0.86),
                     head_x=(0.8,), head_s=0.16, slats=3, grille_w=0.9, grille_h=0.16, exhaust=(-1, 1), exhaust_x=0.22, fog=False, extras=(extra_drifter,)),
        turret_pos=(0, 1.38, -0.45), barrel_pos=(0, 0.26, 0.3), turret_scale=0.85),
    'enemy_raider_b': dict(prefix='raiderB', paint=(0.38, 0.44, 0.52), turret='gun',
        wheel_z=1.45, track=0.98, wheel_y=0.43, tire_r=0.43, tire_w=0.32,
        z_rear=-2.30, z_front=2.26, y_floor=0.30, x_in=0.74, arch_top=0.93, flare=0.16, station=station_rammer,
        cabin=dict(y_belt=1.14, y_roof=1.62, x_belt=1.08, x_roof=0.9, z_ws_base=0.8, z_ws_top=0.3, z_rw_top=-1.5, z_rw_base=-1.75, z_bpillar=-0.5, pillar=0.1, cage=True),
        details=dict(door_seams=(-0.6, 0.85), handles=(-0.3,), mirror=(0.65, 1.2), hood_seam=(0.85, 2.1, 0.9), bumper_mat='dark_steel',
                     head_x=(0.82,), head_s=0.22, slats=6, grille_w=1.3, exhaust=(1,), exhaust_x=0.6, fog=False, extras=(extra_rammer,)),
        turret_pos=(0, 1.62, -0.6), barrel_pos=(0, 0.26, 0.3), turret_scale=0.85),
    'enemy_raider_c': dict(prefix='raiderC', paint=(0.32, 0.46, 0.24), turret='rockets',
        wheel_z=1.45, track=0.98, wheel_y=0.43, tire_r=0.43, tire_w=0.30,
        z_rear=-2.34, z_front=2.26, y_floor=0.27, x_in=0.74, arch_top=0.9, flare=0.11, station=station_hunter,
        cabin=dict(y_belt=0.98, y_roof=1.44, x_belt=1.05, x_roof=0.82, z_ws_base=0.8, z_ws_top=0.15, z_rw_top=-0.55, z_rw_base=-1.1, z_bpillar=-0.25),
        details=dict(door_seams=(-0.55, 0.85), handles=(-0.3,), mirror=(0.65, 1.04), hood_seam=(0.85, 2.1, 0.86),
                     head_x=(0.82, 0.6), head_s=0.15, slats=4, grille_w=1.0, exhaust=(-1,), exhaust_x=0.55, fog=False, extras=(extra_hunter,)),
        turret_pos=(0, 1.44, -0.3), barrel_pos=(0, 0.26, 0.3), turret_scale=0.85),
    'enemy_boss_warrig': dict(prefix='boss', paint=(0.34, 0.06, 0.05), turret='twin',
        wheel_z=1.769, track=1.1956, wheel_y=0.5246, tire_r=0.52, tire_w=0.42,
        z_rear=-2.80, z_front=2.80, y_floor=0.42, x_in=0.9, arch_top=1.09, flare=0.1, station=station_rig,
        cabin=dict(y_belt=1.48, y_roof=2.12, x_belt=1.28, x_roof=1.15, z_ws_base=0.75, z_ws_top=0.45, z_rw_top=-0.55, z_rw_base=-0.6, z_bpillar=-0.05, pillar=0.11, cage=False),
        details=dict(door_seams=(-0.45,), handles=(-0.2,), mirror=(0.7, 1.6), hood_seam=(0.85, 2.6, 1.1), bumper_mat='dark_steel', bumper_y=0.62,
                     head_x=(1.05, 0.8), head_s=0.24, slats=7, grille_w=1.6, grille_h=0.7, exhaust=(), fog=True, extras=(extra_rig,)),
        turret_pos=(0, 2.12, -0.05), barrel_pos=(0, 0.3, 0.4)),
}

def build_vehicle(key):
    spec = SPECS[key]
    body = Mesh('Body')
    top_at, side_at = build_hull(body, spec)
    build_cabin(body, spec, top_at)
    build_details(body, spec, top_at, side_at)
    build_underbody(body, spec)
    wheel = build_wheel(spec)
    turret, barrel = build_turret(spec)
    return spec, body, wheel, turret, barrel

# ======================================================================= glTF export
def materials(prefix):
    p = prefix
    return {
        'paint':     dict(name=p + '_paint', pbr=dict(baseColorFactor=[1, 1, 1, 1], metallicFactor=0.35, roughnessFactor=0.42, tex='paint', normal='paint_n')),
        'dark_steel': dict(name=p + '_dark_steel', pbr=dict(baseColorFactor=[0.34, 0.35, 0.37, 1], metallicFactor=0.6, roughnessFactor=0.55, tex='steel')),
        'chrome':    dict(name=p + '_chrome', pbr=dict(baseColorFactor=[0.78, 0.77, 0.74, 1], metallicFactor=1.0, roughnessFactor=0.18)),
        'black':     dict(name=p + '_black', pbr=dict(baseColorFactor=[0.05, 0.05, 0.055, 1], metallicFactor=0.1, roughnessFactor=0.8)),
        'seam':      dict(name=p + '_seam', pbr=dict(baseColorFactor=[0.02, 0.02, 0.025, 1], metallicFactor=0.0, roughnessFactor=0.9)),
        'interior':  dict(name=p + '_interior', pbr=dict(baseColorFactor=[0.08, 0.075, 0.07, 1], metallicFactor=0.0, roughnessFactor=0.95)),
        'armor':     dict(name=p + '_armor', pbr=dict(baseColorFactor=[0.9, 0.88, 0.85, 1], metallicFactor=0.3, roughnessFactor=0.75, tex='rust', normal='rust_n')),
        'VehicleRubber': dict(name='VehicleRubber', pbr=dict(baseColorFactor=[1, 1, 1, 1], metallicFactor=0.0, roughnessFactor=0.9, tex='rubber')),
        'glass':     dict(name=p + '_glass', pbr=dict(baseColorFactor=[0.1, 0.14, 0.15, 0.3], metallicFactor=0.0, roughnessFactor=0.1), alphaMode='BLEND'),
        'headlight': dict(name=p + '_headlight', pbr=dict(baseColorFactor=[0.9, 0.9, 0.85, 1], metallicFactor=0.0, roughnessFactor=0.3), emissive=[1.0, 0.92, 0.75], strength=2.0),
        'taillight': dict(name=p + '_taillight', pbr=dict(baseColorFactor=[0.6, 0.05, 0.03, 1], metallicFactor=0.0, roughnessFactor=0.3), emissive=[1.0, 0.08, 0.04], strength=2.0),
    }

class Glb:
    def __init__(self):
        self.bin = bytearray(); self.views = []; self.accessors = []; self.images = []; self.textures = []
        self.materials = []; self.meshes = []; self.nodes = []; self.texindex = {}
    def view(self, data, target=None):
        while len(self.bin) % 4: self.bin.append(0)
        off = len(self.bin); self.bin += data
        v = dict(buffer=0, byteOffset=off, byteLength=len(data))
        if target: v['target'] = target
        self.views.append(v); return len(self.views) - 1
    def acc(self, arr, ctype, atype, target, minmax=False):
        arr = np.ascontiguousarray(arr)
        a = dict(bufferView=self.view(arr.tobytes(), target), componentType=ctype, count=len(arr), type=atype)
        if minmax:
            a['min'] = [float(x) for x in arr.min(0)]; a['max'] = [float(x) for x in arr.max(0)]
        self.accessors.append(a); return len(self.accessors) - 1
    def texture(self, key, rgb, jpeg):
        if key in self.texindex: return self.texindex[key]
        buf = io.BytesIO()
        if jpeg: Image.fromarray(rgb).save(buf, 'JPEG', quality=88)
        else: Image.fromarray(rgb).save(buf, 'PNG', optimize=True)
        self.images.append(dict(bufferView=self.view(buf.getvalue()), mimeType='image/jpeg' if jpeg else 'image/png', name=key))
        self.textures.append(dict(source=len(self.images) - 1))
        self.texindex[key] = len(self.textures) - 1
        return self.texindex[key]
    def material(self, mdef, tex):
        pbr = dict(mdef['pbr'])
        if 'tex' in pbr:
            key = pbr.pop('tex'); pbr['baseColorTexture'] = dict(index=self.texture(key, tex[key], key in ('paint', 'steel', 'rust')))
        normal = pbr.pop('normal', None)
        m = dict(name=mdef['name'], pbrMetallicRoughness=pbr)
        if normal: m['normalTexture'] = dict(index=self.texture(normal, tex[normal], False))
        if mdef.get('alphaMode'): m['alphaMode'] = mdef['alphaMode']; m['doubleSided'] = False
        if mdef.get('emissive'):
            m['emissiveFactor'] = mdef['emissive']
            m['extensions'] = dict(KHR_materials_emissive_strength=dict(emissiveStrength=mdef.get('strength', 1.0)))
        self.materials.append(m); return len(self.materials) - 1
    def mesh(self, name, parts, matindex):
        prims = []
        for mat, V, N, U, T in parts:
            # design space -> glTF: flip Z, reverse winding to keep faces outward
            Vg = (V * np.array([1, 1, -1])).astype(np.float32); Ng = (N * np.array([1, 1, -1])).astype(np.float32)
            Ug = U.astype(np.float32); Ug[:, 1] = 1.0 - Ug[:, 1]
            Tg = T[:, [0, 2, 1]]
            big = len(Vg) > 65000
            idx = self.acc(Tg.reshape(-1).astype(np.uint32 if big else np.uint16), 5125 if big else 5123, 'SCALAR', 34963)
            prims.append(dict(attributes=dict(POSITION=self.acc(Vg, 5126, 'VEC3', 34962, True), NORMAL=self.acc(Ng, 5126, 'VEC3', 34962),
                                              TEXCOORD_0=self.acc(Ug, 5126, 'VEC2', 34962)), indices=idx, material=matindex[mat], mode=4))
        self.meshes.append(dict(name=name, primitives=prims)); return len(self.meshes) - 1
    def node(self, name, mesh=None, t=None, children=None):
        n = dict(name=name)
        if mesh is not None: n['mesh'] = mesh
        if t is not None: n['translation'] = [float(t[0]), float(t[1]), float(-t[2])]   # design -> glTF
        if children: n['children'] = children
        self.nodes.append(n); return len(self.nodes) - 1
    def write(self, path, root):
        while len(self.bin) % 4: self.bin.append(0)
        j = dict(asset=dict(version='2.0', generator='DerbyArena-CarForge-2.1'), scene=0, scenes=[dict(nodes=[root])],
                 nodes=self.nodes, meshes=self.meshes, materials=self.materials, textures=self.textures, images=self.images,
                 accessors=self.accessors, bufferViews=self.views, buffers=[dict(byteLength=len(self.bin))],
                 extensionsUsed=['KHR_materials_emissive_strength'])
        js = json.dumps(j, separators=(',', ':')).encode()
        while len(js) % 4: js += b' '
        out = struct.pack('<III', 0x46546C67, 2, 12 + 8 + len(js) + 8 + len(self.bin))
        out += struct.pack('<II', len(js), 0x4E4F534A) + js + struct.pack('<II', len(self.bin), 0x004E4942) + bytes(self.bin)
        open(path, 'wb').write(out)

def export(key, out_dir):
    spec, body, wheel, turret, barrel = build_vehicle(key)
    tex = textures(); g = Glb()
    mdefs = materials(spec['prefix']); matindex = {}
    used = set(body.parts) | set(wheel.parts) | set(turret.parts) | set(barrel.parts)
    for k in ('paint', 'dark_steel', 'chrome', 'VehicleRubber', 'glass', 'headlight', 'taillight', 'black', 'seam', 'interior', 'armor'):
        if k in used: matindex[k] = g.material(mdefs[k], tex)
    p = spec['prefix']
    m_body = g.mesh(p + '_Body', body.flat(), matindex)
    m_wheel = g.mesh(p + '_Wheel', wheel.flat(), matindex)
    m_turret = g.mesh(p + '_Turret', turret.flat(), matindex)
    m_barrel = g.mesh(p + '_Barrel', barrel.flat(), matindex)
    n_body = g.node(p + '_Body', m_body)
    wz, tx, wy = spec['wheel_z'], spec['track'], spec['wheel_y']
    n_w = [g.node('Wheel_FL', m_wheel, (-tx, wy, wz)), g.node('Wheel_FR', m_wheel, (tx, wy, wz)),
           g.node('Wheel_RL', m_wheel, (-tx, wy, -wz)), g.node('Wheel_RR', m_wheel, (tx, wy, -wz))]
    n_barrel = g.node('Barrel', m_barrel, spec['barrel_pos'])
    n_turret = g.node('Turret', m_turret, spec['turret_pos'], [n_barrel])
    root = g.node(p + '_Root', None, None, [n_body] + n_w + [n_turret])
    path = os.path.join(out_dir, key + '.glb')
    g.write(path, root)
    tris = body.tris() + 4 * wheel.tris() + turret.tris() + barrel.tris()
    print('%-20s %6d tris (body %d, wheel %d x4)  %.2f MB' % (key, tris, body.tris(), wheel.tris(), os.path.getsize(path) / 1e6))

if __name__ == '__main__':
    out = sys.argv[1] if len(sys.argv) > 1 else os.path.join(os.path.dirname(__file__), '..', 'unity-game', 'Assets', 'StreamingAssets', 'Models', 'vehicles')
    os.makedirs(out, exist_ok=True)
    keys = sys.argv[2:] or list(SPECS)
    for k in keys: export(k, out)
