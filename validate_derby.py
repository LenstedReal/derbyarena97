#!/usr/bin/env python3
"""Unity-independent static integrity checks for Derby Arena. Does not claim runtime validation."""
from pathlib import Path
import json, struct, sys
ROOT = Path(__file__).resolve().parent / 'unity-game'
errors=[]; notes=[]; warnings=[]
def check(ok, label, detail=''):
    (notes if ok else errors).append(('PASS' if ok else 'FAIL', label, detail))
def warn(label, detail=''):
    warnings.append(('WARN', label, detail))
pv=ROOT/'ProjectSettings/ProjectVersion.txt'
check(pv.exists() and '6000.3.27f1' in pv.read_text(errors='ignore'), 'Unity version file', str(pv.relative_to(ROOT.parent)))
try:
    m=json.loads((ROOT/'Packages/manifest.json').read_text()); deps=m.get('dependencies',{})
    for key, expected, label in [
      ('com.unity.inputsystem','1.11.2','Input System dependency'),
      ('com.unity.render-pipelines.universal','17.3.0','URP dependency'),
      ('com.unity.ugui','2.0.0','uGUI dependency'),
      ('com.atteneder.gltfast','6.20.0','glTFast dependency')]:
        check(deps.get(key)==expected,label,deps.get(key,'missing'))
except Exception as e: check(False,'Package manifest JSON',repr(e))
objdir=ROOT/'Assets/StreamingAssets/Models/spillway'; objs=sorted(objdir.glob('*.obj'))
check(len(objs)==10,'Arena OBJ count',f'{len(objs)} found')
for p in objs:
    verts=0; faces=0; bad=[]; polygon_sizes={}
    try:
        with p.open(errors='strict') as f:
            for ln,line in enumerate(f,1):
                parts=line.strip().split()
                if not parts or parts[0].startswith('#'): continue
                if parts[0]=='v': verts+=1
                elif parts[0]=='f':
                    faces+=1; polygon_sizes[len(parts)-1]=polygon_sizes.get(len(parts)-1,0)+1
                    for tok in parts[1:]:
                        try: idx=int(tok.split('/')[0])
                        except ValueError: bad.append((ln,tok)); continue
                        if idx==0 or abs(idx)>verts: bad.append((ln,tok))
        check(verts>0 and faces>0 and not bad,f'OBJ valid: {p.name}',f'{verts} vertices, {faces} faces, polygons={polygon_sizes}, bad={bad[:2]}')
    except Exception as e: check(False,f'OBJ readable: {p.name}',repr(e))
pngs=sorted(objdir.glob('*.png')); check(len(pngs)==9,'Arena PNG count',f'{len(pngs)} found')
for p in pngs:
    try:
        b=p.read_bytes(); ok=b[:8]==b'\x89PNG\r\n\x1a\n' and len(b)>=24
        dims=str(struct.unpack('>II',b[16:24])) if ok else 'invalid PNG header'
        check(ok,f'PNG signature: {p.name}',dims)
    except Exception as e: check(False,f'PNG readable: {p.name}',repr(e))
vdir=ROOT/'Assets/StreamingAssets/Models/vehicles'; glbs=sorted(vdir.glob('*.glb'))
check(len(glbs)==5,'Vehicle GLB count',f'{len(glbs)} found')
for p in glbs:
    try:
        b=p.read_bytes(); ok=len(b)>=20 and b[:4]==b'glTF' and struct.unpack('<I',b[4:8])[0]==2 and struct.unpack('<I',b[8:12])[0]==len(b)
        check(ok,f'GLB header: {p.name}',f'{len(b)} bytes')
    except Exception as e: check(False,f'GLB readable: {p.name}',repr(e))
scene=ROOT/'Assets/DerbyArena/Scenes/BlackoutSpillway.unity'; boot=ROOT/'Assets/DerbyArena/Scripts/Core/DerbyBootstrap.cs'; spill=ROOT/'Assets/DerbyArena/Scripts/World/SpillwayBuilder.cs'
check(scene.exists(),'Main scene exists',str(scene.relative_to(ROOT)))
check(boot.exists() and 'class DerbyBootstrap' in boot.read_text(errors='ignore'),'Bootstrap script exists')
check(spill.exists() and 'class SpillwayBuilder' in spill.read_text(errors='ignore'),'Arena builder exists')
for name in ['ProjectSettings.asset','EditorSettings.asset','GraphicsSettings.asset','QualitySettings.asset']:
    p=ROOT/'ProjectSettings'/name
    if p.exists(): check(True,f'Unity setting file: {name}','present')
    else: warn(f'Unity setting file: {name}','missing from archive; Unity may generate defaults on first open, but this must be verified')
print('DERBY ARENA STATIC VALIDATION (Unity-independent)')
for status,label,detail in notes+warnings+errors: print(f'[{status}] {label}' + (f' — {detail}' if detail else ''))
print(f'\nSummary: {len(notes)} PASS, {len(warnings)} WARN, {len(errors)} FAIL')
print('Not covered: Unity package resolution, C# compilation, Play Mode, visuals, APK build, or device behavior.')
if not errors: print('STATIC CHECKS PASSED; this is not proof that the Unity project runs.')
sys.exit(1 if errors else 0)
