#!/usr/bin/env python3
"""Unity-independent static integrity checks for Derby Arena. Does not claim runtime validation."""
from pathlib import Path
import json, struct, sys, re
ROOT = Path(__file__).resolve().parent / 'unity-game'
SCRIPTS = ROOT / 'Assets/DerbyArena/Scripts'
errors = []; notes = []; warnings = []

def check(ok, label, detail=''):
    (notes if ok else errors).append(('PASS' if ok else 'FAIL', label, detail))

def warn(label, detail=''):
    warnings.append(('WARN', label, detail))

def text(p):
    return p.read_text(errors='ignore') if p.exists() else ''

pv = ROOT / 'ProjectSettings/ProjectVersion.txt'
check(pv.exists() and '6000.3.27f1' in pv.read_text(errors='ignore'), 'Unity version file', str(pv.relative_to(ROOT.parent)))

# ---- packages: Built-in pipeline + own importers, so only core packages are needed
try:
    m = json.loads((ROOT / 'Packages/manifest.json').read_text()); deps = m.get('dependencies', {})
    for key, expected, label in [('com.unity.inputsystem', '1.11.2', 'Input System dependency'),
                                 ('com.unity.ugui', '2.0.0', 'uGUI dependency')]:
        check(deps.get(key) == expected, label, deps.get(key, 'missing'))
    for key in ['com.unity.render-pipelines.universal', 'com.atteneder.gltfast']:
        check(key not in deps, f'No external render/glTF package: {key}', 'removed on purpose (Built-in pipeline + MiniGlb)')
    check('scopedRegistries' not in m, 'No scoped registries (package resolution only needs the Unity registry)')
except Exception as e:
    check(False, 'Package manifest JSON', repr(e))

# ---- arena assets
objdir = ROOT / 'Assets/StreamingAssets/Models/spillway'; objs = sorted(objdir.glob('*.obj'))
check(len(objs) == 10, 'Arena OBJ count', f'{len(objs)} found')
for p in objs:
    verts = 0; faces = 0; bad = []; polygon_sizes = {}
    try:
        with p.open(errors='strict') as f:
            for ln, line in enumerate(f, 1):
                parts = line.strip().split()
                if not parts or parts[0].startswith('#'): continue
                if parts[0] == 'v': verts += 1
                elif parts[0] == 'f':
                    faces += 1; polygon_sizes[len(parts) - 1] = polygon_sizes.get(len(parts) - 1, 0) + 1
                    for tok in parts[1:]:
                        try: idx = int(tok.split('/')[0])
                        except ValueError: bad.append((ln, tok)); continue
                        if idx == 0 or abs(idx) > verts: bad.append((ln, tok))
        check(verts > 0 and faces > 0 and not bad, f'OBJ valid: {p.name}', f'{verts} vertices, {faces} faces, polygons={polygon_sizes}, bad={bad[:2]}')
    except Exception as e:
        check(False, f'OBJ readable: {p.name}', repr(e))
for name in ['channel_floor', 'asphalt', 'wall_west', 'wall_east', 'wall_north', 'wall_south', 'barriers', 'bridge', 'pipes', 'catwalk']:
    check((objdir / f'{name}.obj').exists(), f'SpillwayAssets part exists: {name}.obj')
pngs = sorted(objdir.glob('*.png')); check(len(pngs) == 9, 'Arena PNG count', f'{len(pngs)} found')
for p in pngs:
    try:
        b = p.read_bytes(); ok = b[:8] == b'\x89PNG\r\n\x1a\n' and len(b) >= 24
        dims = str(struct.unpack('>II', b[16:24])) if ok else 'invalid PNG header'
        check(ok, f'PNG signature: {p.name}', dims)
    except Exception as e:
        check(False, f'PNG readable: {p.name}', repr(e))

# ---- vehicle GLBs: verify the structure MiniGlb relies on
vdir = ROOT / 'Assets/StreamingAssets/Models/vehicles'; glbs = sorted(vdir.glob('*.glb'))
check(len(glbs) == 5, 'Vehicle GLB count', f'{len(glbs)} found')
for p in glbs:
    try:
        b = p.read_bytes(); ok = len(b) >= 20 and b[:4] == b'glTF' and struct.unpack('<I', b[4:8])[0] == 2 and struct.unpack('<I', b[8:12])[0] == len(b)
        check(ok, f'GLB header: {p.name}', f'{len(b)} bytes')
        off = 12; js = None; has_bin = False
        while off + 8 <= len(b):
            cl, ct = struct.unpack('<II', b[off:off + 8])
            if ct == 0x4E4F534A: js = json.loads(b[off + 8:off + 8 + cl])
            if ct == 0x004E4942: has_bin = True
            off += 8 + cl
        names = {n.get('name') for n in js['nodes']} if js else set()
        need = {'Wheel_FL', 'Wheel_FR', 'Wheel_RL', 'Wheel_RR', 'Turret', 'Barrel'}
        check(js is not None and has_bin and need <= names, f'GLB nodes MiniGlb/Bind needs: {p.name}', f'missing={sorted(need - names)}')
        comps = {a['componentType'] for a in js['accessors']}
        strides = [v.get('byteStride') for v in js['bufferViews'] if v.get('byteStride')]
        prim_modes = {pr.get('mode', 4) for me in js['meshes'] for pr in me['primitives']}
        check(comps <= {5126, 5123, 5125, 5121} and prim_modes == {4}, f'GLB uses float/index accessors and triangle lists only: {p.name}', f'components={sorted(comps)} modes={sorted(prim_modes)} strides={strides[:2]}')
    except Exception as e:
        check(False, f'GLB readable: {p.name}', repr(e))

# ---- scripts
scene = ROOT / 'Assets/DerbyArena/Scenes/BlackoutSpillway.unity'; boot = SCRIPTS / 'Core/DerbyBootstrap.cs'; spill = SCRIPTS / 'World/SpillwayBuilder.cs'
check(scene.exists(), 'Main scene exists', str(scene.relative_to(ROOT)))
boot_text = text(boot)
check('class DerbyBootstrap' in boot_text, 'Bootstrap script exists')
check('class SpillwayBuilder' in text(spill), 'Arena builder exists')
check('EnsurePrimaryCamera();' in boot_text and boot_text.find('EnsurePrimaryCamera();') < boot_text.find('SpillwayBuilder.Build(world);'), 'Primary camera is created before arena generation')
check('SpawnEmergencyVehicle' in boot_text and 'BuildEmergencyArena' in boot_text, 'Startup fallback scene and vehicle paths exist')
for rel, token, label in [
    ('Core/DerbyStartupDiagnostics.cs', 'OnGUI', 'On-screen diagnostics (also in release builds)'),
    ('Core/DerbyStartupDiagnostics.cs', 'BeforeSceneLoad', 'Diagnostics install before scene load'),
    ('World/ObjMesh.cs', 'class ObjMesh', 'OBJ parser exists'),
    ('World/SpillwayAssets.cs', 'class SpillwayAssets', 'Arena OBJ/texture loader exists'),
    ('World/PickupSpin.cs', 'class PickupSpin', 'Pickup spin component exists'),
    ('Vehicles/MiniGlb.cs', 'class MiniGlb', 'Built-in GLB importer exists'),
    ('Vehicles/MiniJson.cs', 'class MiniJson', 'JSON reader exists'),
    ('Mission/MissionDirector.cs', 'zoneBox.isTrigger = true', 'Extract zone is a real trigger (mission can be completed)'),
    ('UI/TouchControls.cs', 'RocketRect', 'Rocket button is wired into touch input'),
    ('UI/HudController.cs', 'StickBase', 'Analog joystick base/knob exists'),
    ('UI/HudController.cs', 'ApplySafeArea', 'HUD respects the safe area'),
]:
    check(token in text(SCRIPTS / rel), label, rel)

editor = text(ROOT / 'Assets/DerbyArena/Editor/UrpAndPlayerSetup.cs')
check('const string RuntimeFolder = "Assets/Resources/Derby"' in editor and all(n in editor for n in ['RuntimeLit.mat', 'RuntimeLitNormal.mat', 'RuntimeLitEmissive.mat', 'RuntimeLitGlass.mat', 'RuntimeFx.mat']), 'Runtime shader materials are generated as Resources')
check('GraphicsSettings.defaultRenderPipeline = null' in editor, 'Built-in render pipeline is enforced (no SRP asset)')
leftovers = [str(p.relative_to(ROOT)) for p in (ROOT / 'Assets/DerbyArena').rglob('*.cs') if re.search(r'UniversalRenderPipeline|using GLTFast|Unity\.Cloud\.Gltfast|ImportGltf', p.read_text(errors='ignore'))]
check(not leftovers, 'No URP / glTFast API references remain in C# sources', ', '.join(leftovers))
for name in ['ProjectSettings.asset', 'EditorSettings.asset', 'GraphicsSettings.asset', 'QualitySettings.asset']:
    p = ROOT / 'ProjectSettings' / name
    if p.exists(): check(True, f'Unity setting file: {name}', 'present')
    else: warn(f'Unity setting file: {name}', 'missing from archive; the editor setup script applies the needed values on first import/build')

print('DERBY ARENA STATIC VALIDATION (Unity-independent)')
for status, label, detail in notes + warnings + errors: print(f'[{status}] {label}' + (f' — {detail}' if detail else ''))
print(f'\nSummary: {len(notes)} PASS, {len(warnings)} WARN, {len(errors)} FAIL')
print('Not covered: Unity package resolution, C# compilation, Play Mode, visuals, APK build, or device behavior.')
if not errors: print('STATIC CHECKS PASSED; this is not proof that the Unity project runs.')
sys.exit(1 if errors else 0)
