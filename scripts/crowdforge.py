#!/usr/bin/env python3
"""
DERBY ARENA 2.1 crowd forge: robot spectators for the Colosseum tribunes (arena/crowd.glb).

Same contract as the 1.x file: 24 nodes CrowdGroup_00..23 (one per 15 degree slice, Crowd.cs bounces them in a
travelling wave), each mesh = one vertex-coloured primitive (bodies, flags) + one emissive primitive (visors,
chest lights). Rows follow the tribune steps measured from the 1.x crowd: feet at y 4.85 / 6.65 / 8.45 / 10.25 /
12.05 on radii 31.6 / 33.5 / 35.3 / 37.1 / 38.9 (two staggered sub-rows each). Near rows get full robots
(head, visor, torso, two arms, legs, antenna), far rows a cheaper build. Poses: idle, cheering, one arm up,
seated, flag bearer. Positions are Unity space; the export mirrors X (MiniGlb mirrors it back).
"""
import json, math, os, struct, sys
import numpy as np
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import carforge as cf

PALETTE = [(0.95, 0.5, 0.12), (0.85, 0.15, 0.15), (0.2, 0.45, 0.9), (0.25, 0.7, 0.35), (0.9, 0.8, 0.15),
           (0.6, 0.3, 0.85), (0.9, 0.9, 0.9), (0.45, 0.48, 0.52), (0.1, 0.75, 0.8), (0.9, 0.35, 0.6)]
EYES = [(0.3, 1.0, 1.0), (1.0, 0.6, 0.2), (0.4, 1.0, 0.4), (1.0, 0.3, 0.3)]
ROWS = [(31.6, 4.85), (33.5, 6.65), (35.3, 8.45), (37.1, 10.25), (38.9, 12.05)]

class Group:
    def __init__(self):
        self.V = []; self.N = []; self.C = []; self.T = []; self.n = 0      # coloured
        self.EV = []; self.EN = []; self.ET = []; self.en = 0              # emissive
    def add(self, prim, color, R, pos):
        V, N, U, T = prim
        V = V @ R.T + pos; N = N @ R.T
        self.V.append(V); self.N.append(N); self.C.append(np.tile(np.array(color + (1.0,)), (len(V), 1))); self.T.append(T + self.n); self.n += len(V)
    def add_e(self, prim, R, pos):
        V, N, U, T = prim
        V = V @ R.T + pos; N = N @ R.T
        self.EV.append(V); self.EN.append(N); self.ET.append(T + self.en); self.en += len(V)
    def tris(self): return sum(len(t) for t in self.T) + sum(len(t) for t in self.ET)

def box8(c, s):
    """Eight-vertex box with corner normals: a third of the vertices of a flat-shaded box (the robots are 30 m away)."""
    c = np.asarray(c, float); h = np.asarray(s, float) / 2
    corners = np.array([[sx, sy, sz] for sx in (-1, 1) for sy in (-1, 1) for sz in (-1, 1)], float)
    V = corners * h + c
    N = corners / np.linalg.norm(corners, axis=1, keepdims=True)
    # faces as (a,b,c,d) in CCW order seen from outside (indices: sx*4 + sy*2 + sz)
    F = [(0, 1, 3, 2), (4, 6, 7, 5), (0, 4, 5, 1), (2, 3, 7, 6), (0, 2, 6, 4), (1, 5, 7, 3)]
    T = np.array([[a, b, c_] for (a, b, c_, d) in F for (a, b, c_) in ((a, b, c_), (a, c_, d))])
    T = cf._orient(V, T, N[T[:, 0]])
    return V, N, np.zeros((8, 2)), T

def quad(c, w, h, nz=1.0):
    """Vertical quad facing +Z (visor, light, antenna)."""
    c = np.asarray(c, float)
    V = np.array([[-w / 2, -h / 2, 0], [w / 2, -h / 2, 0], [w / 2, h / 2, 0], [-w / 2, h / 2, 0]]) + c
    N = np.tile(np.array([0, 0, nz]), (4, 1))
    T = np.array([[0, 1, 2], [0, 2, 3]])
    return V, N, np.zeros((4, 2)), T

def rot_y(a):
    c, s = math.cos(a), math.sin(a)
    return np.array([[c, 0, s], [0, 1, 0], [-s, 0, c]])

def robot(g, rng, pos, yaw, near):
    """One robot standing at pos (feet), facing yaw. near: full detail."""
    R = rot_y(yaw)
    body = PALETTE[rng.integers(len(PALETTE))]
    dark = tuple(c * 0.45 for c in body)
    metal = (0.3, 0.32, 0.36)
    eye = EYES[rng.integers(len(EYES))]
    pose = rng.choice(['idle', 'cheer', 'onearm', 'seated', 'flag'], p=[0.3, 0.3, 0.2, 0.12, 0.08]) if near else rng.choice(['idle', 'cheer', 'seated'], p=[0.45, 0.4, 0.15])
    scale = rng.uniform(0.9, 1.1)
    legs_h = 0.62 * scale if pose != 'seated' else 0.3 * scale
    torso_h = 0.55 * scale; head = 0.32 * scale; w = 0.46 * scale
    y0 = 0.0
    # legs (one block), torso, head
    g.add(box8((0, y0 + legs_h / 2, 0), (w * 0.7, legs_h, 0.28 * scale)), dark, R, pos)
    ty = y0 + legs_h + torso_h / 2
    g.add(box8((0, ty, 0), (w, torso_h, 0.32 * scale)), body, R, pos)
    hy = y0 + legs_h + torso_h + head / 2 + 0.04
    g.add(box8((0, hy, 0), (head, head, head * 0.9)), metal if rng.random() < 0.5 else body, R, pos)
    # visor + chest light (emissive quads)
    g.add_e(quad((0, hy + 0.02, head * 0.45 + 0.01), head * 0.7, head * 0.22), R, pos)
    if near:
        g.add_e(quad((0, ty + 0.1, 0.16 * scale + 0.01), 0.08, 0.08), R, pos)
        g.add(quad((head * 0.3, hy + head * 0.5 + 0.08, 0), 0.03, 0.16), metal, R, pos)   # antenna
    # arms: near rows get two arms; far rows only the cheering pair as one raised block
    if near:
        for side in (-1, 1):
            up = pose == 'cheer' or (pose == 'onearm' and side == 1) or (pose == 'flag' and side == 1)
            if up:
                g.add(box8((side * (w / 2 + 0.07), ty + torso_h * 0.55, 0), (0.12 * scale, 0.5 * scale, 0.12 * scale)), dark, R, pos)
            else:
                g.add(box8((side * (w / 2 + 0.07), ty - 0.05, 0.02), (0.12 * scale, 0.42 * scale, 0.12 * scale)), dark, R, pos)
    elif pose == 'cheer':
        for side in (-1, 1):
            g.add(box8((side * (w / 2 + 0.07), ty + torso_h * 0.55, 0), (0.12 * scale, 0.5 * scale, 0.12 * scale)), dark, R, pos)
    if pose == 'flag':
        fy = ty + torso_h * 0.8
        g.add(box8((w / 2 + 0.07, fy + 0.5, 0), (0.03, 1.4, 0.03)), metal, R, pos)
        g.add(quad((w / 2 + 0.07 + 0.3, fy + 0.95, 0), 0.6, 0.4), PALETTE[rng.integers(len(PALETTE))], R, pos)
    return eye

def build(groups=24, per_row=7, seed=5):
    rng = np.random.default_rng(seed)
    out = []
    for gi in range(groups):
        g = Group(); eyes = []
        a0 = gi * 2 * math.pi / groups; da = 2 * math.pi / groups
        for ri, (r, y) in enumerate(ROWS):
            near = ri < 1
            for sub in range(2):
                rr = r + sub * 0.55 + rng.uniform(-0.05, 0.05)
                count = per_row + (1 if sub == 0 else 0)
                for k in range(count):
                    if rng.random() < 0.12: continue       # empty seats
                    a = a0 + da * (k + 0.5 + sub * 0.5 + rng.uniform(-0.15, 0.15)) / (count + 0.5)
                    pos = np.array([math.sin(a) * rr, y + sub * 0.02, math.cos(a) * rr])
                    yaw = math.atan2(-pos[0], -pos[2]) + rng.uniform(-0.25, 0.25)   # face the centre
                    eyes.append(robot(g, rng, pos, yaw, near))
        out.append((g, eyes))
    return out

def export(path, groups):
    glb = cf.Glb()
    # materials: vertex-coloured body (white base so COLOR_0 shows), emissive visor
    mi_body = glb.material(dict(name='CrowdBody', pbr=dict(baseColorFactor=[1, 1, 1, 1], metallicFactor=0.25, roughnessFactor=0.55)), {})
    mi_eye = glb.material(dict(name='CrowdVisor', pbr=dict(baseColorFactor=[0.9, 0.95, 1, 1], metallicFactor=0.0, roughnessFactor=0.4), emissive=[0.4, 0.9, 1.0], strength=3.0), {})
    node_ids = []
    total = 0
    for gi, (g, eyes) in enumerate(groups):
        V = np.concatenate(g.V); N = np.concatenate(g.N); C = np.concatenate(g.C); T = np.concatenate(g.T)
        EV = np.concatenate(g.EV); EN = np.concatenate(g.EN); ET = np.concatenate(g.ET)
        prims = []
        for (Vx, Nx, Tx, Cx, mi) in ((V, N, T, C, mi_body), (EV, EN, ET, None, mi_eye)):
            # the crowd has no 180-degree visual root: mirror X here so MiniGlb's own X mirror lands it exactly
            Vg = (Vx * np.array([-1, 1, 1])).astype(np.float32); Ng = (Nx * np.array([-1, 1, 1])).astype(np.float32)
            Tg = Tx[:, [0, 2, 1]]
            big = len(Vg) > 65000
            attrs = dict(POSITION=glb.acc(Vg, 5126, 'VEC3', 34962, True), NORMAL=glb.acc(Ng, 5126, 'VEC3', 34962))
            if Cx is not None:
                ci = glb.acc((np.clip(Cx, 0, 1) * 255 + 0.5).astype(np.uint8), 5121, 'VEC4', 34962)
                glb.accessors[ci]['normalized'] = True
                attrs['COLOR_0'] = ci
            idx = glb.acc(Tg.reshape(-1).astype(np.uint32 if big else np.uint16), 5125 if big else 5123, 'SCALAR', 34963)
            prims.append(dict(attributes=attrs, indices=idx, material=mi, mode=4))
        glb.meshes.append(dict(name='CrowdGroup_%02d' % gi, primitives=prims))
        node_ids.append(glb.node('CrowdGroup_%02d' % gi, len(glb.meshes) - 1, (0, 0, 0)))
        total += g.tris()
    root = glb.node('CrowdRoot', None, None, node_ids)
    glb.write(path, root)
    print('crowd: %d groups, %d tris, %.2f MB' % (len(groups), total, os.path.getsize(path) / 1e6))

if __name__ == '__main__':
    out = sys.argv[1] if len(sys.argv) > 1 else os.path.join(os.path.dirname(os.path.abspath(__file__)), '..', 'unity-game', 'Assets', 'StreamingAssets', 'Models', 'arena', 'crowd.glb')
    export(out, build())
