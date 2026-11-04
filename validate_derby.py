#!/usr/bin/env python3
"""Unity-independent static checks for Derby Arena 2.0.0. Not a substitute for a Unity compile or device test."""
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

def png_size(p):
    b = p.read_bytes()
    return struct.unpack('>II', b[16:24]) if b[:8] == b'\x89PNG\r\n\x1a\n' else None

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
files = sorted(SCRIPTS.rglob('*.cs')) + sorted((ROOT / 'Assets/DerbyArena/Editor').rglob('*.cs')) + sorted((ROOT / 'Assets/DerbyArena/Ads').rglob('*.cs'))
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
            'ObjMesh', 'HudController', 'MenuController', 'TouchControls', 'UiKit', 'ChaseCamera', 'SynthAudio', 'AudioHub',
            'CheckpointGate', 'ZoneRing', 'AcidPool', 'Crowd', 'PostFx', 'Brazier', 'GlbOptions', 'Loc']:
    check(cls in names, 'Type exists: ' + cls)

boot = text(SCRIPTS / 'Core/DerbyBootstrap.cs')
check(boot.find('EnsureCamera()') < boot.find('ArenaLoader.Build'), 'Camera is created before arena')
meta = text(SCRIPTS / 'Core/DerbyBootstrap.cs.meta')
scene = text(ROOT / 'Assets/DerbyArena/Scenes/BlackoutSpillway.unity')
g = re.search(r'guid: (\w+)', meta)
check(g is not None and g.group(1) in scene, 'Scene references DerbyBootstrap script GUID')
camp = text(SCRIPTS / 'Core/Campaign.cs')
check('public const int Total = 50;' in camp, '50-level campaign')
for k in ['Reactor', 'Demolition', 'Boss', 'Checkpoint', 'Survival', 'LastStand']:
    check(k in camp, 'Objective kind: ' + k)
diag = text(SCRIPTS / 'Core/DerbyStartupDiagnostics.cs')
check('Version = "2.0.0"' in diag and 'VersionCode = 200' in diag, 'Runtime version 2.0.0 (200)')
loc = text(SCRIPTS / 'Core/Loc.cs')
check('public static string T(string turkish, string english)' in loc and 'SystemLanguage.Turkish' in loc, 'Turkish / English localisation (device language preselects)')
menu = text(SCRIPTS / 'UI/MenuController.cs')
check('BuildLanguage()' in menu and '"language"' in menu and 'LanguageChosen' in menu, 'Language picker on first launch + settings row')
check('BuildUpcomingSeason()' in menu and 'PvP' in menu and 'OpenStorePage' in menu, 'Roadmap info box: next season, planned cars, online PvP goal, rate button')
for f in ['UI/HudController.cs', 'Mission/MatchDirector.cs', 'UI/MenuController.cs', 'Core/Campaign.cs']:
    check(text(SCRIPTS / f).count('Loc.T(') >= 10, 'Localised strings in ' + f)

editor = text(ROOT / 'Assets/DerbyArena/Editor/UrpAndPlayerSetup.cs')
for mat in ['RuntimeLit.mat', 'RuntimeLitNormal.mat', 'RuntimeLitEmissive.mat', 'RuntimeLitGlass.mat', 'RuntimeFx.mat', 'RuntimeFxAdd.mat', 'RuntimeSky.mat']:
    check(mat in editor, 'Editor generates ' + mat)
check('stripEngineCode = false' in editor, 'Engine code stripping disabled (CapsuleCollider fix)')
check('KeepFogVariants();' in editor, 'Fog shader variants kept')
check('"com.lenstedreal.derbyarena"' in editor, 'Package id preserved')
check((ROOT / 'Assets/link.xml').exists(), 'link.xml present')
check('Version = "2.0.0"' in editor and 'VersionCode = 200' in editor, 'Player version 2.0.0 / code 200')
icons_hook = text(ROOT / 'Assets/DerbyArena/Editor/DerbyAndroidIcons.cs')
check('IPostGenerateGradleAndroidProject' in icons_hook and '#if UNITY_EDITOR && UNITY_ANDROID' in icons_hook, 'Android launcher icons written into the Gradle project (guarded for iOS builds)')
check('SaveAndReimport' not in editor, 'No asset re-import during the build (silently skipped the icons in 1.3.0)')
for d, k in [('mdpi', 1), ('hdpi', 1.5), ('xhdpi', 2), ('xxhdpi', 3), ('xxxhdpi', 4)]:
    for n, base in [('app_icon', 48), ('app_icon_round', 48), ('app_icon_foreground', 108), ('app_icon_background', 108)]:
        ip = ROOT / 'AndroidIcons/res' / ('mipmap-' + d) / (n + '.png')
        check(ip.exists() and png_size(ip) == (int(base * k), int(base * k)), f'Launcher icon {d}/{n}.png')
for n in ['app_icon.xml', 'app_icon_round.xml']:
    x = ROOT / 'AndroidIcons/res/mipmap-anydpi-v26' / n
    check(x.exists() and 'adaptive-icon' in text(x), 'Adaptive icon ' + n)
post = text(ROOT / 'Assets/DerbyArena/Shaders/DerbyPost.shader')
check('LinearToGammaSpace' in post and 'GammaToLinearSpace' in post, 'Colour grading in gamma space (no black crush)')
atm = text(SCRIPTS / 'World/Atmosphere.cs')
check('ReflectionProbe' in atm and 'ApplyBrightness' in atm, 'Sky reflection probe + brightness setting')
check('ForcePixel' not in text(SCRIPTS / 'Vehicles/VehicleVisual.cs') and 'ForcePixel' not in text(SCRIPTS / 'CameraRig/ChaseCamera.cs'), 'No forced per-pixel lights')
for mat in ['RuntimeVertexLit.mat', 'RuntimePost.mat']:
    check(mat in editor, 'Editor generates ' + mat)
check('ApplyIcons();' in editor, 'Editor applies the Derby Arena app icons')
for sh, name in [('DerbyPost.shader', '"Hidden/DerbyArena/Post"'), ('DerbyVertexLit.shader', '"DerbyArena/VertexLit"')]:
    sp = ROOT / 'Assets/DerbyArena/Shaders' / sh
    st = text(sp) if sp.exists() else ''
    check(name in st and st.count('{') == st.count('}'), 'Shader present and balanced: ' + sh)
check(text(ROOT / 'Assets/DerbyArena/Shaders/DerbyPost.shader').count('Pass') >= 5, 'Post shader has 5 passes')
for ic, size in [('icon_legacy.png', 1024), ('icon_adaptive_bg.png', 432), ('icon_adaptive_fg.png', 432), ('icon_round.png', 512)]:
    ip = ROOT / 'Assets/DerbyArena/Icons' / ic
    check(ip.exists() and png_size(ip) == (size, size), f'Icon {ic} {size}x{size}')
check((ROOT / 'Assets/Resources/Derby/keyart.jpg').exists(), 'Key art for menu / loading screen')

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
            'props/loot_ammo.glb', 'props/loot_armor.glb', 'arena/crowd.glb']:
    p = MODELS / rel
    j = glb(p) if p.exists() else None
    ok = j is not None
    if ok:
        modes = {pr.get('mode', 4) for m in j.get('meshes', []) for pr in m['primitives']}
        comps = {a['componentType'] for a in j['accessors']}
        ok = modes == {4} and comps <= {5126, 5123, 5125, 5121}
        if rel == 'arena/crowd.glb':
            ok = ok and sum(1 for n in j['nodes'] if n.get('name', '').startswith('CrowdGroup')) >= 12
        if rel.startswith('vehicles/'):
            ok = ok and {'Wheel_FL', 'Wheel_FR', 'Wheel_RL', 'Wheel_RR', 'Turret', 'Barrel'} <= {n.get('name') for n in j['nodes']}
    check(ok, 'GLB usable by MiniGlb: ' + rel)
for part in ['channel_floor', 'asphalt', 'wall_west', 'wall_east', 'wall_north', 'wall_south', 'barriers', 'bridge', 'pipes', 'catwalk']:
    check((MODELS / 'spillway' / (part + '.obj')).exists(), 'Spillway OBJ: ' + part)
for s in ['asphalt', 'concrete', 'steel']:
    for k in ['albedo', 'normal']:
        check((MODELS / 'spillway' / f'{s}_{k}.png').exists(), f'Spillway texture: {s}_{k}')

# 2.0: ads (AdMob test units), legal pages, battle kit, arena dressing, adaptive performance
ADS = ROOT / 'Assets/DerbyArena/Ads'
try:
    man = json.loads(text(ROOT / 'Packages/manifest.json'))
    regs = man.get('scopedRegistries', [])
    check(man['dependencies'].get('com.google.ads.mobile') == '11.6.0', 'Google Mobile Ads Unity plugin 11.6.0 in manifest')
    check(any('package.openupm.com' in r.get('url', '') and 'com.google' in r.get('scopes', []) for r in regs), 'OpenUPM scoped registry for com.google')
    check(man['dependencies'].get('com.unity.external-dependency-manager') == '2.1.0', 'External Dependency Manager 2.1.0')
except Exception as e:
    check(False, 'manifest ads entries', repr(e))
asm = json.loads(text(ADS / 'DerbyArena.Ads.asmdef'))
check(any(v.get('name') == 'com.google.ads.mobile' and v.get('define') == 'DERBY_ADS' for v in asm.get('versionDefines', [])), 'DERBY_ADS defined only when the plugin is installed')
check({'GoogleMobileAds.Api','GoogleMobileAds.Ump.Api'} <= set(asm.get('references', [])), 'Ads assembly references the plugin assemblies')
cfg = text(ADS / 'AdsConfig.cs')
check('public const bool TestMode = true;' in cfg, 'Ads in TEST mode (release: set false)')
check('ca-app-pub-3940256099942544/1033173712' in cfg and 'ca-app-pub-3940256099942544/5224354917' in cfg, 'Google test interstitial + rewarded units')
mgr = strip(text(ADS / 'AdsManager.cs'))
for api in ['ConsentInformation.Update', 'LoadAndShowConsentFormIfRequired', 'CanRequestAds', 'MobileAds.Initialize', 'InterstitialAd.Load', 'RewardedAd.Load', 'ShowPrivacyOptionsForm']:
    check(api in mgr, 'AdsManager uses ' + api)
raw = text(ADS / 'AdsManager.cs')
check(raw.find('#if DERBY_ADS') < raw.find('using GoogleMobileAds.Api;'), 'Plugin namespaces only behind DERBY_ADS')
outside = [f.name for f in sorted(SCRIPTS.rglob('*.cs')) if 'GoogleMobileAds' in text(f)]
check(not outside, 'Game scripts never reference the ad SDK directly', str(outside))
gma = text(ROOT / 'Assets/GoogleMobileAds/Resources/GoogleMobileAdsSettings.asset')
check('adMobAndroidAppId: ca-app-pub-' in gma and 'a187246822bbb47529482707f3e0eff8' in gma, 'AdMob settings asset with Android app ID')
md = text(SCRIPTS / 'Mission/MatchDirector.cs')
check('DeferDeath = OfferSecondChance' in md and 'ReviveUsed' in md, 'Rewarded second chance (once per match)')
hud = text(SCRIPTS / 'UI/HudController.cs')
check('ShowInterstitial' in hud and 'MatchFinished' in hud and 'DoubleReward' in hud and 'MaybeAskForRating' in hud, 'Result screen: capped interstitial, rewarded double scrap, rate prompt')
mn = text(SCRIPTS / 'UI/MenuController.cs')
check('RewardedScrapLeft' in mn and 'BuildLegal()' in mn and 'ShowPrivacyOptions' in mn and 'escapeKey' in mn, 'Menu: garage rewarded scrap, legal pages, ad privacy options, Android back')
lg = text(SCRIPTS / 'Core/Legal.cs')
check('AdMob' in lg and 'PrivacyUrl' in lg, 'In-game privacy summary mentions AdMob + web link')
check('VehicleKit.Build' in text(SCRIPTS / 'Vehicles/VehicleVisual.cs'), 'Battle kit on every vehicle')
check('ArenaDressing.Colosseum' in text(SCRIPTS / 'World/Arena.cs') and 'ArenaDressing.Spillway' in text(SCRIPTS / 'World/Arena.cs'), 'Stadium dressing in both arenas')
check('AddComponent<AdaptivePerformance>' in text(SCRIPTS / 'Core/DerbyBootstrap.cs'), 'Adaptive performance in matches')
check('SetPropertyBlock(charBlock)' in text(SCRIPTS / 'Vehicles/VehicleHealth.cs'), 'Wreck look without material instances')
try:
    from PIL import Image as _I
except ImportError:
    _I = None
if _I is None:
    print('[SKIP] Adaptive icon safe-zone check (Pillow not installed)')
else:
  try:
    fg = _I.open(ROOT / 'AndroidIcons/res/mipmap-xxxhdpi/app_icon_foreground.png').convert('RGBA')
    a = fg.split()[3]; w, h = fg.size; c = w / 2
    corner_ok = all(a.getpixel((int(c + dx * w * 0.33), int(c + dy * h * 0.33))) == 0 for dx in (-1, 1) for dy in (-1, 1))
    check(corner_ok, 'Adaptive icon foreground stays inside the safe zone')
  except Exception as e:
    check(False, 'adaptive icon check', repr(e))

fails = [r for r in res if r[0] == 'FAIL']
print('DERBY ARENA 2.0.0 STATIC VALIDATION (Unity-independent)')
for st, label, detail in res:
    if st == 'FAIL' or '-v' in sys.argv: print(f'[{st}] {label}' + (f' — {detail}' if detail else ''))
print(f'\nSummary: {len(res) - len(fails)} PASS, {len(fails)} FAIL')
print('Not covered: Unity package resolution, C# compilation, Play Mode, APK/IPA build, device behaviour.')
sys.exit(1 if fails else 0)
