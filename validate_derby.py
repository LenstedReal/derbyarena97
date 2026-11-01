#!/usr/bin/env python3
"""Unity-independent static checks for Derby Arena 1.2.0. Not a substitute for a Unity compile or device test."""
from pathlib import Path
import json, re, struct, sys

ROOT = Path(__file__).resolve().parent / 'unity-game'
SCRIPTS = ROOT / 'Assets/DerbyArena/Scripts'
MODELS = ROOT / 'Assets/StreamingAssets/Models'
res = []

def check(ok, label, detail=''):
    res.append(('PASS' if ok else 'FAIL', label, detail))

def text(p):
    return p.read_text(encoding='utf-8', errors='ignore') if p.exists() else ''

def strip(src):
    out = []; i = 0; n = len(src)
    while i < n:
        c = src[i]; d = src[i:i + 2]
        if d == '//':
            j = src.find('\n', i); i = n if j < 0 else j; continue
        if d == '/*':
            j = src.find('*/', i + 2); i = n if j < 0 else j + 2; continue
        if c == '"':
            i += 1
            while i < n and src[i] != '"':
                if src[i] == '\\': i += 1
                i += 1
            i += 1; out.append('""'); continue
        if c == "'":
            i += 1
            while i < n and src[i] != "'":
                if src[i] == '\\': i += 1
                i += 1
            i += 1; out.append("'x'"); continue
        out.append(c); i += 1
    return ''.join(out)

check('6000.3.27f1' in text(ROOT / 'ProjectSettings/ProjectVersion.txt'), 'Unity version 6000.3.27f1')
try:
    deps = json.loads(text(ROOT / 'Packages/manifest.json')).get('dependencies', {})
    check(deps.get('com.unity.inputsystem') == '1.11.2', 'Input System 1.11.2', deps.get('com.unity.inputsystem', 'missing'))
    check(deps.get('com.unity.ugui') == '2.0.0', 'uGUI 2.0.0')
    for k in ['com.unity.modules.physics', 'com.unity.modules.particlesystem', 'com.unity.modules.audio',
              'com.unity.modules.unitywebrequest', 'com.unity.modules.imageconversion', 'com.unity.modules.jsonserialize']:
        check(k in deps, 'module ' + k)
    check('com.unity.render-pipelines.universal' not in deps and 'com.atteneder.gltfast' not in deps, 'No URP / glTFast package (Built-in + MiniGlb)')
except Exception as e:
    check(False, 'manifest.json', repr(e))

# C# structure
files = sorted(SCRIPTS.rglob('*.cs')) + sorted((ROOT / 'Assets/DerbyArena/Editor').rglob('*.cs'))
names = {}
for f in files:
    t = strip(text(f)); st = []; ok = True
    for ch in t:
        if ch in '([{': st.append(ch)
        elif ch in ')]}':
            if not st or st[-1] != {')': '(', ']': '[', '}': '{'}[ch]: ok = False; break
            st.pop()
    check(ok and not st, 'Balanced braces: ' + f.name)
    for m in re.finditer(r'\b(?:class|struct|enum)\s+(\w+)', t):
        names.setdefault(m.group(1), []).append(f.name)
dups = {k: v for k, v in names.items() if len(v) > 1}
check(not dups, 'No duplicate type names', str(dups))
allsrc = ''.join(strip(text(f)) for f in files)
for old in ['WheelDrive', 'MissionDirector', 'SpillwayBuilder', 'GltfVehicleVisual', 'ProjectilePool', 'UniversalRenderPipeline', 'GLTFast', 'CreatePrimitive']:
    check(re.search(r'\b' + old + r'\b', allsrc) is None, 'No reference to removed API/type: ' + old)
for cls in ['DerbyBootstrap', 'GameFlow', 'Campaign', 'SaveData', 'ArcadeCar', 'VehicleHealth', 'VehicleVisual', 'WeaponRig', 'Projectiles', 'Fx',
            'EnemyDriver', 'MatchDirector', 'PlayerDriver', 'Pickup', 'ReactorCell', 'ExtractionPad', 'ArenaLoader', 'GlbLibrary', 'MiniGlb',
            'ObjMesh', 'HudController', 'MenuController', 'TouchControls', 'UiKit', 'ChaseCamera', 'SynthAudio', 'AudioHub']:
    check(cls in names, 'Type exists: ' + cls)

boot = text(SCRIPTS / 'Core/DerbyBootstrap.cs')
check(boot.find('EnsureCamera()') < boot.find('ArenaLoader.Build'), 'Camera is created before arena')
meta = text(SCRIPTS / 'Core/DerbyBootstrap.cs.meta')
scene = text(ROOT / 'Assets/DerbyArena/Scenes/BlackoutSpillway.unity')
g = re.search(r'guid: (\w+)', meta)
check(g is not None and g.group(1) in scene, 'Scene references DerbyBootstrap script GUID')
camp = text(SCRIPTS / 'Core/Campaign.cs')
check('public const int Total = 50;' in camp, '50-level campaign')

editor = text(ROOT / 'Assets/DerbyArena/Editor/UrpAndPlayerSetup.cs')
for mat in ['RuntimeLit.mat', 'RuntimeLitNormal.mat', 'RuntimeLitEmissive.mat', 'RuntimeLitGlass.mat', 'RuntimeFx.mat', 'RuntimeFxAdd.mat', 'RuntimeSky.mat']:
    check(mat in editor, 'Editor generates ' + mat)
check('stripEngineCode = false' in editor, 'Engine code stripping disabled (CapsuleCollider fix)')
check('KeepFogVariants();' in editor, 'Fog shader variants kept')
check('"com.lenstedreal.derbyarena"' in editor, 'Package id preserved')
check((ROOT / 'Assets/link.xml').exists(), 'link.xml present')

# assets
def glb(p):
    b = p.read_bytes()
    if b[:4] != b'glTF' or struct.unpack('<I', b[8:12])[0] != len(b): return None
    off = 12; j = None
    while off + 8 <= len(b):
        cl, ct = struct.unpack('<II', b[off:off + 8])
        if ct == 0x4E4F534A: j = json.loads(b[off + 8:off + 8 + cl])
        off += 8 + cl
    return j
for rel in ['vehicles/player_muscle.glb', 'vehicles/enemy_raider_a.glb', 'vehicles/enemy_raider_b.glb', 'vehicles/enemy_raider_c.glb',
            'vehicles/enemy_boss_warrig.glb', 'arena/colosseum.glb', 'props/loot_health.glb', 'props/loot_nitro.glb',
            'props/loot_ammo.glb', 'props/loot_armor.glb']:
    p = MODELS / rel
    j = glb(p) if p.exists() else None
    ok = j is not None
    if ok:
        modes = {pr.get('mode', 4) for m in j.get('meshes', []) for pr in m['primitives']}
        comps = {a['componentType'] for a in j['accessors']}
        ok = modes == {4} and comps <= {5126, 5123, 5125, 5121}
        if rel.startswith('vehicles/'):
            ok = ok and {'Wheel_FL', 'Wheel_FR', 'Wheel_RL', 'Wheel_RR', 'Turret', 'Barrel'} <= {n.get('name') for n in j['nodes']}
    check(ok, 'GLB usable by MiniGlb: ' + rel)
for part in ['channel_floor', 'asphalt', 'wall_west', 'wall_east', 'wall_north', 'wall_south', 'barriers', 'bridge', 'pipes', 'catwalk']:
    check((MODELS / 'spillway' / (part + '.obj')).exists(), 'Spillway OBJ: ' + part)
for s in ['asphalt', 'concrete', 'steel']:
    for k in ['albedo', 'normal']:
        check((MODELS / 'spillway' / f'{s}_{k}.png').exists(), f'Spillway texture: {s}_{k}')

fails = [r for r in res if r[0] == 'FAIL']
print('DERBY ARENA 1.2.0 STATIC VALIDATION (Unity-independent)')
for st, label, detail in res:
    if st == 'FAIL' or '-v' in sys.argv: print(f'[{st}] {label}' + (f' — {detail}' if detail else ''))
print(f'\nSummary: {len(res) - len(fails)} PASS, {len(fails)} FAIL')
print('Not covered: Unity package resolution, C# compilation, Play Mode, APK/IPA build, device behaviour.')
sys.exit(1 if fails else 0)
