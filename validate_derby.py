#!/usr/bin/env python3
"""Unity-independent static checks for Derby Arena 4.0.0. Not a substitute for a Unity compile or device test."""
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
check('public const int ReleasedSeasons = 5;' in camp and 'Total = ReleasedSeasons * PerChapter' in camp, '100-level campaign (season 6 gated behind ReleasedSeasons)')
for k in ['Reactor', 'Demolition', 'Boss', 'Checkpoint', 'Survival', 'LastStand']:
    check(k in camp, 'Objective kind: ' + k)
diag = text(SCRIPTS / 'Core/DerbyStartupDiagnostics.cs')
check('Version = "4.0.0"' in diag and 'VersionCode = 400' in diag, 'Runtime version 4.0.0 (400)')
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
check('Version = "4.0.0"' in editor and 'VersionCode = 400' in editor, 'Player version 4.0.0 / code 400')
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
    check((MODELS / 'spillway' / f'{s}_albedo.jpg').exists() and (MODELS / 'spillway' / f'{s}_normal.png').exists(), f'Spillway texture: {s}')

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

# 2.1: season 6 (Container Port), post 2.0, ultra profile, draw-call merging
camp = text(SCRIPTS / 'Core/Campaign.cs')
m = re.search(r'public const int Total = (\d+);', camp)
total = int(m.group(1)) if m else 0
check(total == 0, 'Total is derived from ReleasedSeasons')
def arr(name):
    mm = re.search(name + r'\s*=\s*(?:new\s+\w+\[\]\s*)?\{(.*?)\};', camp, re.S)
    return [x for x in re.findall(r'"[^"]*"|[\d.]+f', mm.group(1))] if mm else []
for name in ['ChapterNamesTr', 'ChapterNamesEn', 'ChapterFeaturesTr', 'ChapterFeaturesEn', 'BossNames', 'BossHealthByChapter']:
    check(len(arr(name)) == 6, 'Campaign.%s has six entries (5 released + prepared season 6)' % name, str(len(arr(name))))
# 4.0: 20 levels per season written as two 10-char halves joined with +
plan_src = re.search(r'string\[\] Plan\s*=\s*\{(.*?)\};', camp, re.S)
plans = [''.join(re.findall(r'"([^"]*)"', line)) for line in plan_src.group(1).strip().split('\n') if '"' in line] if plan_src else []
check(len(plans) == 6, 'Campaign.Plan has six seasons', str(len(plans)))
check(all(len(p) == 20 and set(p) <= set('RDCSLB') and p.endswith('B') and p[9] == 'D' for p in plans), 'Every season plan: 20 known missions, big derby at 10, boss at 20')
check('public const int PerChapter = 20;' in camp and 'LegacyPerChapter = 10' in camp, '4.0: 20 levels per season, legacy constant kept for save migration')
check('ArenaKind { Spillway, Colosseum, Docks, Ruins }' in camp and 'c.Chapter == 6) c.Arena = ArenaKind.Docks' in camp, 'Season 6 plays in the Container Port; Ruins arena kind exists')
check('(n - 1) / (float)(Total - 1)' in camp and 'StarsRequired(int n)' in camp and 'IsOpen(int n)' in camp, 'Difficulty over 100 levels, star gates')
sdm = text(SCRIPTS / 'Core/SaveData.cs')
check('MigrateLegacyLevels' in sdm and 'public int Version = 5;' in sdm and 'd.Version < 5' in sdm, 'Save v5 migration from 10-level seasons')
gar = text(SCRIPTS / 'Core/Garage.cs')
check('public enum PartSlot' in gar and gar.count('P("paint_') >= 10 and 'TryBuy(string id)' in gar and 'class Fleet' in gar and 'Id = "t12"' in gar, 'Garage shop catalogue and vehicle collection with the T-12')
check('ForPlayer()' in text(SCRIPTS / 'Vehicles/VehicleSpec.cs') and 'VehicleSpec.ForPlayer()' in text(SCRIPTS / 'Mission/MatchDirector.cs'), 'The match spawns the selected garage vehicle')
check('KitStyle.ForPlayer()' in text(SCRIPTS / 'Vehicles/VehicleKit.cs') and 'DressRims' in text(SCRIPTS / 'Vehicles/VehicleVisual.cs'), 'Garage cosmetics reach the kit and the rims')
dm = text(SCRIPTS / 'Core/DailyMissions.cs')
check('MissionGoal.Kills' in dm and 'Claim(int index)' in dm and 'DailyMissions.Tick' in text(SCRIPTS / 'Mission/MatchDirector.cs'), 'Daily missions tracked and claimable')
uik = text(SCRIPTS / 'UI/UiKit.cs')
check('Derby/Fonts/Poppins-Medium' in uik and 'Derby/UI/' in uik, 'UI loads the bundled font and sprites')
for f in ['Fonts/Poppins-Bold.ttf', 'Fonts/Poppins-Medium.ttf', 'Fonts/OFL-Poppins.txt', 'UI/panel.png', 'UI/button_primary.png', 'UI/ic_star.png', 'UI/ic_lock.png', 'UI/ic_tank.png']:
    check((ROOT / 'Assets/Resources/Derby' / f).exists(), 'Resource present: ' + f)
check('OnPreprocessTexture' in text(ROOT / 'Assets/DerbyArena/Editor/DerbyAssetImport.cs'), 'UI texture import rules')
# 4.0: Tiger I T-12
tiger = ROOT / 'Assets/StreamingAssets/Models/vehicles/tiger_t12.glb'
check(tiger.exists() and 200000 < tiger.stat().st_size < 3000000, 'Tiger I T-12 model present (0.2-3 MB)')
try:
    tj = glb(tiger) if tiger.exists() else None
except Exception:
    tj = None
if tj:
    names = [n.get('name', '') for n in tj.get('nodes', [])]
    need = ['Body', 'Turret', 'Barrel', 'Track_L', 'Track_R', 'Road_L1', 'Road_R8', 'Sprocket_L', 'Idler_R', 'Flag', 'Wheel_FL', 'Wheel_RR', 'Body_LOD1']
    check(all(n in names for n in need), 'T-12 node contract (tracks, road wheels, sprocket/idler, turret/barrel, flag, LOD1)', str([n for n in need if n not in names]))
    mats = [m.get('name', '') for m in tj.get('materials', [])]
    check('tiger_camo' in mats and 'tiger_track' in mats and 'tiger_flag' in mats and 'tiger_marking' in mats and not any('paint' in m for m in mats), 'T-12 materials: fixed camouflage (no recolourable paint), track, flag, marking')
tv = text(SCRIPTS / 'Vehicles/TankVisual.cs')
check('_MainTex_ST' in tv and 'Recoil()' in tv and 'flagMesh' in tv, 'TankVisual: track scroll, recoil, flag cloth')
wr = text(SCRIPTS / 'Combat/WeaponRig.cs')
check('FireCannon' in wr and 'FireShell' in wr and 'SynthAudio.Cannon()' in wr, 'Cannon: shells, recoil, casing, sound')
vs = text(SCRIPTS / 'Vehicles/VehicleSpec.cs')
check('TigerT12()' in vs and 'Tank = true, Cannon = true' in vs and 'FixedPaint = true' in vs, 'T-12 spec: tank, cannon, fixed camouflage')
check('SetupLod' in text(SCRIPTS / 'Vehicles/VehicleVisual.cs') and 'LODGroup' in text(SCRIPTS / 'Vehicles/VehicleVisual.cs'), 'Model LOD groups from *_LOD1 nodes')
check('RuntimeLitDecal' in text(ROOT / 'Assets/DerbyArena/Editor/UrpAndPlayerSetup.cs') and 'Mats.Decal' in text(SCRIPTS / 'Vehicles/MiniGlb.cs'), 'Textured decals use the fade material')
mn2 = text(SCRIPTS / 'UI/MenuController.cs')
check('Campaign.Seasons + 1' in mn2 and 'BuildUpcomingSeason' in mn2, 'Level list keeps the roadmap tab after the released seasons')
arena = text(SCRIPTS / 'World/Arena.cs')
check('kind == ArenaKind.Docks) loader.BuildDocks()' in arena and 'Ready = true;' in arena, 'Arena loader builds the docks and marks it ready')
docks = text(SCRIPTS / 'World/DocksArena.cs')
check('info.Kind = ArenaKind.Docks' in docks and 'StaticBatchingUtility.Combine' in docks and 'ColliderBox("BoundN"' in docks, 'Docks: info, batching, tall bound walls')
atm = text(SCRIPTS / 'World/Atmosphere.cs')
check('Mathf.Clamp(index, 0, 5)' in atm and 'case 5:' in atm, 'Atmosphere theme for season 6')
sd = text(SCRIPTS / 'Core/SaveData.cs')
check('while (d.Unlocked < Campaign.Total && d.Stars[d.Unlocked] > 0) d.Unlocked++;' in sd, 'Players who cleared level 50 get level 51')
check('Mathf.Clamp(d.Quality, 0, 3)' in sd and 'ScreenEffects = true' in sd, 'Save: ultra quality and screen-effects switch')
boot = text(SCRIPTS / 'Core/DerbyBootstrap.cs')
check('q >= 3 ? 4 :' in boot and '(q == 2 ? 1080 : 1440)' in boot, 'Ultra profile: 4x MSAA, 1440p')
sh = text(ROOT / 'Assets/DerbyArena/Shaders/DerbyPost.shader')
check(all(u in sh for u in ['half _Aces;', 'half _Grain;', 'half _Flash;', 'half _Speed;', 'SampleSource(i.uv)']), 'Post shader: ACES, grain, flash, nitro split')
check(sh.count('Pass\n') + sh.count('Pass\r\n') >= 5 and 'sin(dot' not in sh, 'Post shader keeps 5 passes, sin-free grain hash')
pfx = text(SCRIPTS / 'World/PostFx.cs')
check('public static void Hit(' in pfx and 'public static void Blast(' in pfx and 'NitroActive' in pfx, 'PostFx: hit jolt, blast flash, nitro')
check('PostFx.Blast(' in text(SCRIPTS / 'Combat/Fx.cs') and 'PostFx.Hit(' in text(SCRIPTS / 'Vehicles/VehicleHealth.cs'), 'Feedback hooked to explosions and player hits')
kit = text(SCRIPTS / 'Vehicles/VehicleKit.cs')
check('Merge(root);' in kit and 'CombineMeshes' in kit and 'DestroyImmediate' in kit, 'Battle kit merged per material')
check('StaticBatchingUtility.Combine' in text(SCRIPTS / 'World/ArenaDressing.cs'), 'Arena dressing static batched')
mnu = text(SCRIPTS / 'UI/MenuController.cs')
check('% 4' in mnu and 'EKRAN EFEKTLERİ' in mnu and 'y -= 72f;' in mnu, 'Settings: 4 quality levels, screen effects row, 13 rows fit')

# 2.1 review fixes
md2 = text(SCRIPTS / 'Mission/MatchDirector.cs')
check('Phase != MatchPhase.Downed) return;' in md2 and 'winPending = true' in md2 and 'if (winPending) { winPending = false; Win(); }' in md2, 'A rival dying while the player is downed still wins the match')
check('if (Boss != null && Boss.Dead) { Win(); return; }' in md2, 'Win safety net in TickPlaying')
check('ReviveTimeLeft = 90f;' in md2 and '999f' not in md2, 'Second-chance ad wait is capped (no soft-lock)')
check('static bool Occupied(' in md2, 'Rivals never spawn inside another car')
check('"RampBack"' in text(SCRIPTS / 'World/DocksArena.cs'), 'Docks ramps are humps (no vertical step)')
check('AdaptivePerformance.Step >= 1' in text(SCRIPTS / 'Core/DerbyBootstrap.cs'), 'Adaptive shadows-off survives later matches')
check(sh.count('#pragma target 3.0') == 2 and 'floor(pix)' in sh and 'min((float3)c, 64.0)' in sh, 'Post shader: SM3 for SV_POSITION, full-precision grain, NaN-safe ACES')
check('if (showing) AudioListener.pause = audioWasPaused;' in text(ADS / 'AdsManager.cs'), 'Ads: audio restored only after a real show')

intro = text(SCRIPTS / 'UI/IntroSplash.cs')
check('Resources.Load<Texture2D>("Derby/keyart")' in intro and 'Shown = true' in intro and 'unscaledDeltaTime' in intro, 'Opening screen: key art, cold start only, unscaled time')
check('IntroSplash.Play()' in text(SCRIPTS / 'Core/DerbyBootstrap.cs') and (ROOT / 'Assets/Resources/Derby/keyart.jpg').exists(), 'Opening screen wired and its art present')

# 2.1 vehicle models (scripts/carforge.py): five distinct bodies, same node contract and wheel geometry as 1.x
WHEELS = {'vehicles/player_muscle.glb': (1.1956, 0.5246, 1.769), 'vehicles/enemy_boss_warrig.glb': (1.1956, 0.5246, 1.769),
          'vehicles/enemy_raider_a.glb': (0.98, 0.43, 1.45), 'vehicles/enemy_raider_b.glb': (0.98, 0.43, 1.45), 'vehicles/enemy_raider_c.glb': (0.98, 0.43, 1.45)}
bodies = {}
for rel, (tx, wy, wz) in WHEELS.items():
    j = glb(MODELS / rel)
    nodes = {n.get('name'): n for n in j['nodes']}
    def near(n, v): return all(abs(a - b) < 0.002 for a, b in zip(n.get('translation', [0, 0, 0]), v))
    check(near(nodes['Wheel_FL'], (-tx, wy, -wz)) and near(nodes['Wheel_FR'], (tx, wy, -wz)) and near(nodes['Wheel_RL'], (-tx, wy, wz)) and near(nodes['Wheel_RR'], (tx, wy, wz)),
          'Wheel positions unchanged (physics identical): ' + rel)
    mats = {m['name']: m for m in j['materials']}
    paint = [m for n, m in mats.items() if 'paint' in n.lower()]
    check(len(paint) == 1 and 'baseColorTexture' in paint[0]['pbrMetallicRoughness'] and 'normalTexture' in paint[0], 'Recolourable textured paint with normal map: ' + rel)
    check(any(m.get('alphaMode') == 'BLEND' for m in mats.values()) and sum(1 for m in mats.values() if m.get('emissiveFactor')) == 2, 'Glass + head/tail lights: ' + rel)
    tris = sum(j['accessors'][pr['indices']]['count'] // 3 for m in j['meshes'] for pr in m['primitives'] if 'indices' in pr)
    check(6000 <= tris <= 12000, 'Model detail in budget (6k-12k unique tris, 1.x had 1.4k): ' + rel, str(tris))
    body = [m for m in j['meshes'] if m['name'].endswith('_Body')][0]
    acc = j['accessors'][body['primitives'][0]['attributes']['POSITION']]
    bodies[rel] = (round(acc['min'][2], 2), round(acc['max'][2], 2), round(acc['max'][1], 2), tris)
check(len({v[:3] for v in bodies.values()}) == 5, 'Five distinct vehicle bodies (not one model with five paints)', str(bodies))
check('FixWinding(pos, nor, tri)' in text(SCRIPTS / 'Vehicles/MiniGlb.cs') and 'RecalculateTangents' in text(SCRIPTS / 'Vehicles/MiniGlb.cs') and 'enableInstancing = true' in text(SCRIPTS / 'Vehicles/MiniGlb.cs'), 'MiniGlb: winding fixed after the X mirror, tangents, instancing')
kit2 = text(SCRIPTS / 'Vehicles/VehicleKit.cs')
check('static Shape Measure(' in kit2 and 'Cut(a, b2, c, 0, 0.02f, seg)' in kit2 and 'roofZ1 - 0.08f' in kit2, 'Battle kit placed from the measured body shape')

# 2.1 environment: textures, sky, crowd, light shafts, wrecks
col = glb(MODELS / 'arena/colosseum.glb')
imgs = {i['name']: i for i in col['images']}
check(all(imgs[n].get('mimeType') == 'image/jpeg' for n in ['sand_base', 'stone_base', 'basalt_base', 'metal_base', 'concrete_base']), 'Colosseum albedo textures replaced (texforge JPEG)')
check('texforge' in col.get('asset', {}).get('generator', ''), 'Colosseum GLB carries the texforge generator tag')
check((ROOT / 'Assets/Resources/Derby/Textures/clouds.png').exists(), 'Cloud panorama in Resources')
sky = text(ROOT / 'Assets/DerbyArena/Shaders/DerbySky.shader')
check('Shader "DerbyArena/Sky"' in sky and '_CloudTex' in sky and '_SunDir' in sky and '_Stars' in sky and 'FallBack "Skybox/Procedural"' in sky, 'Sky shader: clouds, sun, stars, fallback')
check('RuntimeSky2' in text(ROOT / 'Assets/DerbyArena/Editor/UrpAndPlayerSetup.cs') and 'ThemedSky' in text(SCRIPTS / 'World/Mats.cs') and 'SkyOf(ref t' in text(SCRIPTS / 'World/Atmosphere.cs'), 'Themed sky wired with the procedural fallback')
check(text(SCRIPTS / 'World/Atmosphere.cs').count('SkyOf(ref t,') == 6, 'Six sky moods (one per theme)')
crowd = glb(MODELS / 'arena/crowd.glb')
ctris = sum(crowd['accessors'][pr['indices']]['count'] // 3 for m in crowd['meshes'] for pr in m['primitives'])
check(30000 <= ctris <= 100000 and len(crowd['meshes']) == 24, 'Crowd: 24 groups, %d tris' % ctris)
check(any('COLOR_0' in pr['attributes'] for m in crowd['meshes'] for pr in m['primitives']) and any(mm.get('emissiveFactor') for mm in crowd['materials']), 'Crowd: vertex-coloured bodies + emissive visors')
check('public static Mesh Shaft(' in text(SCRIPTS / 'World/MeshFactory.cs') and 'ShaftMaterial(' in text(SCRIPTS / 'World/ArenaDressing.cs') and 'ArenaDressing.Shaft(' in arena, 'Light shafts on floodlights and light posts')
check('void SpawnWreck(int index)' in arena and 'SpawnWreck(w)' in arena, 'Colosseum cover wrecks are the 2.1 vehicle models')

fails = [r for r in res if r[0] == 'FAIL']
print('DERBY ARENA 4.0.0 STATIC VALIDATION (Unity-independent)')
for st, label, detail in res:
    if st == 'FAIL' or '-v' in sys.argv: print(f'[{st}] {label}' + (f' — {detail}' if detail else ''))
print(f'\nSummary: {len(res) - len(fails)} PASS, {len(fails)} FAIL')
print('Not covered: Unity package resolution, C# compilation, Play Mode, APK/IPA build, device behaviour.')
sys.exit(1 if fails else 0)
