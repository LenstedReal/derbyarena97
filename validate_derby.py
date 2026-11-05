#!/usr/bin/env python3
"""Unity-independent static checks for Derby Arena 4.0.2. Not a substitute for a Unity compile or device test."""
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
check('Version = "4.0.2"' in diag and 'VersionCode = 402' in diag, 'Runtime version 4.0.2 (402)')
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
check('Version = "4.0.2"' in editor and 'VersionCode = 402' in editor, 'Player version 4.0.2 / code 402')
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
for name in ['ChapterNamesTr', 'ChapterNamesEn', 'ChapterFeaturesTr', 'ChapterFeaturesEn', 'BossNamesTr', 'BossNamesEn', 'BossHealthByChapter']:
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
check('MigrateLegacyLevels' in sdm and 'public int Version = 7;' in sdm and 'd.Version < 5' in sdm and 'd.Version = 5;' in sdm, 'Save v5 migration from 10-level seasons (kept under v7)')
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

# 4.0.1: cinematic intro synced to the real boot, staged GLB import, layered audio, controls
boot = text(SCRIPTS / 'Core/DerbyBootstrap.cs')
check('public static bool Active' in intro and 'Boot.Progress' in intro and 'Boot.Status' in intro and 'MaxTotal' in intro and 'UiButton.Blocked = true' in intro, '4.0.1 intro: progress-synced cinematic, input locked, safety cap')
check('public static class Boot' in boot and 'IEnumerator BootMenu(' in boot and 'Shader.WarmupAllShaders()' in boot and 'chase.Dolly(IntroSplash.Leaving ? 3f : 60f)' in boot and 'cam.Dolly(3f)' in intro and 'AddComponent<PerfLog>()' in boot and 'AudioHub.Reverb(' in boot, '4.0.1 boot: coroutine menu build, shader warm-up, camera dolly, perf log, reverb')
mg = text(SCRIPTS / 'Vehicles/MiniGlb.cs')
check('public static Import Prepare(' in mg and 'PreloadTextures(Import' in mg and 'PreloadMeshes(Import' in mg and 'public static void Finish(' in mg and 'MiniGlb.Prepare(' in text(SCRIPTS / 'World/GlbLibrary.cs'), '4.0.1 GLB import staged over frames (no startup stall)')
check('public sealed class PerfLog' in text(SCRIPTS / 'Core/PerfLog.cs') and 'DerbyPerf' in text(SCRIPTS / 'Core/PerfLog.cs'), '4.0.1 PerfLog writes frame-time windows to logcat')
ea = text(SCRIPTS / 'Vehicles/EngineAudio.cs')
check('SynthAudio.EngineIdle()' in ea and 'SynthAudio.EngineLoad()' in ea and 'SynthAudio.Tracks()' in ea and 'priority = player ? 16 : 160' in ea, '4.0.1 layered engine audio (idle / engine / load, tank tracks, voice priorities)')
sa = text(SCRIPTS / 'Audio/SynthAudio.cs')
check('public static void Reverb(DerbyArena.Core.ArenaKind kind)' in sa and 'AudioReverbZone' in sa and 's.priority = loud' in sa, '4.0.1 reverb zones per arena, one-shot priorities')
fx2 = text(SCRIPTS / 'Combat/Fx.cs')
check('public static void Debris(' in fx2 and 'Random.Range(0.8f, 1.3f)' in fx2 and 'public void Kick(float amount)' in text(SCRIPTS / 'Vehicles/VehicleVisual.cs') and 'Visual.Kick(0.6f)' in text(SCRIPTS / 'Combat/WeaponRig.cs'), '4.0.1 weapon polish: muzzle variety, debris, gun kick')
for rel in ['vehicles/player_muscle.glb', 'vehicles/enemy_raider_a.glb', 'vehicles/enemy_raider_b.glb', 'vehicles/enemy_raider_c.glb', 'vehicles/enemy_boss_warrig.glb']:
    jj = glb(MODELS / rel)
    lod = [n for n in jj['nodes'] if n.get('name') == 'Body_LOD1']
    ltris = sum(jj['accessors'][pr['indices']]['count'] // 3 for m in jj['meshes'] if m['name'].endswith('_Body_LOD1') for pr in m['primitives'])
    check(len(lod) == 1 and 500 <= ltris <= 1500, '4.0.1 car LOD1 node present and light: ' + rel, str(ltris))
check('n.EndsWith("_LOD1")' in text(SCRIPTS / 'Vehicles/VehicleKit.cs') and 'EndsWith("_LOD1")) continue' in text(SCRIPTS / 'Vehicles/VehicleVisual.cs'), '4.0.1 LOD1 excluded from kit measurement and hull fit')
check('MobileTextureSubtarget.ASTC' in text(ROOT / 'Assets/DerbyArena/Editor/UrpAndPlayerSetup.cs'), '4.0.1 Android textures ASTC')
cl = text(SCRIPTS / 'Core/ControlLayout.cs')
check('public enum ControlId' in cl and 'public enum SteerMode { Wheel = 0, Arrows = 1, Swipe = 2, Tilt = 3 }' in cl and 'public const int Count = 7;' in cl and 'Classic = 0, Driver = 1, LeftHand = 2, Custom = 3' in cl, '4.0.1 control layout model: 7 controls, 4 steer modes, 3 presets + custom')
sd = text(SCRIPTS / 'Core/SaveData.cs')
check('public int Version = 7;' in sd and 'static void MigrateControls(' in sd and 'public float[] ControlPos' in sd and 'public bool SteerAssist = true;' in sd and 'ControlLayout.Sanitize(d);' in sd, '4.0.1 save v6: control layout fields, migration, sanitize')
tc = text(SCRIPTS / 'UI/TouchControls.cs')
check('case SteerMode.Swipe:' in tc and 'case SteerMode.Tilt:' in tc and 'Accelerometer.current' in tc and 'compensateForScreenOrientation' in tc and 'public static bool Suspended;' in tc and 'ScreenOrientation.LandscapeRight' in tc, '4.0.1 touch: swipe joystick, tilt steering, editor suspend')
cw = text(SCRIPTS / 'UI/ControlWidgets.cs'); ce = text(SCRIPTS / 'UI/ControlLayoutEditor.cs')
check('public static ControlWidget Build(' in cw and 'public static void Layout(' in cw and 'public static void Open(bool tank, Action onClose)' in ce and 'ControlLayout.SetPos(' in ce and 'ControlLayout.SetScale(' in ce and 'CancelAndClose' in ce, '4.0.1 shared control widgets + drag/resize layout editor')
hud2 = text(SCRIPTS / 'UI/HudController.cs'); menu2 = text(SCRIPTS / 'UI/MenuController.cs')
check('ControlWidgets.Build(' in hud2 and 'ControlLayoutEditor.Open(' in hud2 and 'public void ApplyLayout()' in hud2 and 'UpdateSwipePad()' in hud2, '4.0.1 HUD builds controls from the layout, editor reachable from pause')
check('case "controls": BuildControlsPage(); break;' in menu2 and 'ControlLayoutEditor.Open(' in menu2 and 'ControlLayout.ResetAll();' in menu2 and 'SteerAssist' in menu2, '4.0.1 Settings → Controls page (mode, sensitivity, auto gas, aid, size, opacity, presets, editor)')
check('Save.Data.SteerAssist' in text(SCRIPTS / 'Vehicles/ArcadeCar.cs') and 'slide * Spec.TurnRate * 0.45f' in text(SCRIPTS / 'Vehicles/ArcadeCar.cs'), '4.0.1 driving aid in the car physics (player only)')
wf = text(ROOT.parent / '.github/workflows/unity-android.yml')
check("if: ${{ github.event_name == 'workflow_dispatch' }}" in wf and "- main" in wf, '4.0.1 CI: APK job manual only, static validation on main')
bt = text(ROOT / 'BUILD_TR.md')
check('yalnızca `main` dalında' in bt and 'Branch `main`' in bt and 'Dal: `main`' in bt and 'Branch: `main`, klasör `/docs`' in bt, '4.0.2 BUILD_TR: single main branch for Build Automation, GitHub Pages from main /docs')
# 4.0.2: video fixes (intro, overlaps, handling, camera, garage, ads, fresh start, legal, names)
ac = text(SCRIPTS / 'Vehicles/ArcadeCar.cs')
check('bool Probe(' in ac and 'Physics.SphereCast(' in ac and '(ClimbRate + rampRise + Mathf.Max(0f, approach)) * dt' in ac and 'float compVel = Mathf.Clamp(approach + riseLp[i], -MaxCompVel, MaxLandVel);' in ac and 'riseLp[i] += (rise - riseLp[i]) * Mathf.Clamp01(dt / RiseFilter);' in ac and 'void OnCollisionStay(' in ac, '4.0.2 rough-ground handling: tyre sphere probe, step rate limit (ramps and landings followed at once), body-motion damper + filtered ramp rise, low-obstacle climb')
check('float away = Vector3.Dot(lv, up);' in ac and 'lv.y > MaxBumpRise' not in ac and 'k * maxLen * 2.5f' in ac and 'grounded >= 3 && !Locked && launchTimer <= 0f' in ac and 'kicked.Launched(0.5f)' in text(SCRIPTS / 'Combat/Projectiles.cs'), '4.0.2 bump cap along the car up axis (ramps keep their speed, wrecks and knock-ups exempt), landing force cap as 4.0.1')

cc = text(SCRIPTS / 'CameraRig/ChaseCamera.cs')
check('void KeepOutOfWalls(' in cc and 'followVy' in cc and 'boom = allowed < boom' in cc and 'public float OrbitScreenShift' in cc, '4.0.2 chase camera: filtered height, boom that pulls in fast, no frame inside walls, menu framing')
check('public static bool ReadyForWork' in intro and 'BlackOut' in intro and 'Mask' not in intro and 'IntroSplash.ReadyForWork' in boot, '4.0.2 intro: full picture before blocking work, leaves through black (no overlap with the menu)')
check('if (!DerbyArena.Core.Boot.ShadersWarm)' in text(SCRIPTS / 'Mission/MatchDirector.cs'), '4.0.2 shader warm-up once per session (not before every match)')
ar = text(SCRIPTS / 'World/Arena.cs')
check('static readonly Dictionary<string, Material> spillMats' in ar and 'spillMeshes[part] = mesh;' in ar, '4.0.2 channel arena assets cached for the session')
uk = text(SCRIPTS / 'UI/UiKit.cs')
check('t.resizeTextForBestFit = true;' in uk and 't.verticalOverflow = VerticalWrapMode.Truncate;' in uk, '4.0.2 every label fits its box (best fit + truncate): no overlapping text')
hud3 = text(SCRIPTS / 'UI/HudController.cs')
check('const float FeedTop' in hud3 and 'bool loadingUp' in hud3 and 'MaybeAskForRating(panel.transform)' in hud3 and 'AdButtonLive' in hud3, '4.0.2 HUD: feed below progress/boss bar, countdown hidden behind loading page, rating strip inside panel, live ad button')
mc3 = text(SCRIPTS / 'UI/MenuController.cs')
check('const float DetailWidth' in mc3 and 'area.offsetMax = new Vector2(-(DetailWidth + 24f), 0f);' in mc3 and 'OrbitScreenShift' in mc3 and '"Scrim"' in mc3 and 'BLACKOUT RUN' not in mc3, '4.0.2 menu: level tabs/cards fit left of the detail panel, scrims, car framed right of the panel, no Blackout Run')
check('Save.ResetProgressNow()' in mc3 and 'adsMgr.Describe()' in mc3, '4.0.2 settings: reset progress (two taps) and ad status in test mode')
kit3 = text(SCRIPTS / 'Vehicles/VehicleKit.cs')
check('var visual = car.Find("Visual");' in kit3 and 'armor >= 4' not in kit3 and '"RollBar"' not in kit3 and '"SpotLens"' in kit3, '4.0.2 kit: measured on the model only (garage = match), no auto spikes / roll bars, spot lamps')
check('bright ? 0.35f : 0.15f' in text(SCRIPTS / 'Vehicles/VehicleVisual.cs'), '4.0.2 rim colours readable (low metallic)')
check('static void FollowBody(Transform root, Transform car)' in kit3 and kit3.count('FollowBody(root, car);') == 2 and 'Matrix4x4.TRS(visual.localPosition, BodyTurn, visual.localScale) * visual.worldToLocalMatrix' in kit3, '4.0.2 kit leans with the body (child of the Visual holder) and is measured in the resting frame')
check('visual.gameObject.name = "VisualOld";' in text(SCRIPTS / 'Core/DerbyBootstrap.cs'), '4.0.2 garage rebuild: the old model is detached before a cached model arrives in the same frame')
check('Mathf.Pow(Mathf.Abs(touchSteer), 1.3f)' in text(SCRIPTS / 'UI/TouchControls.cs'), '4.0.2 touch steering response curve (finer small corrections, full lock kept)')
check('SetStaticBool(typeof(PlayerSettings.Android), "optimizedFramePacing", true);' in editor and 'SetStaticBool(typeof(PlayerSettings), "gcIncremental", true);' in editor, '4.0.2 smoothness: Android frame pacing and incremental GC (set by reflection, cannot break the build)')
am = text(ROOT / 'Assets/DerbyArena/Ads/AdsManager.cs')
check('if (canRequest || AdsConfig.TestMode) InitializeSdk();' in am and 'public void RequestRewarded()' in am and 'public string Describe()' in am, '4.0.2 ads: test builds start the SDK even without consent info, live requests, status line')
check('public sealed class AdButtonLive' in text(SCRIPTS / 'UI/AdButtonLive.cs'), '4.0.2 live rewarded-ad buttons')
sd3 = text(SCRIPTS / 'Core/SaveData.cs')
check('if (d.Version < 7) { ResetProgress(d); d.Version = 7; }' in sd3 and 'public static void ResetProgressNow()' in sd3 and 'if (Data.Version != before) Write();' in sd3, '4.0.2 save v7: one-time fresh start, written at once')
lg = text(SCRIPTS / 'Core/Legal.cs')
legal_all = lg + ''.join(text(ROOT.parent / 'docs' / f) for f in ['index.html', 'privacy.html', 'terms.html', 'licenses.html']) + text(ROOT / 'StoreListing/PRIVACY_POLICY.md')
check('istanbulrunner' not in legal_all and 'proton.me' not in legal_all and 'Google Play' in lg, '4.0.2 legal: the old game\'s e-mail removed everywhere (contact via the Play listing until the new address)')
check('Mathf.Clamp(boom - 0.25f, 0.6f, Mathf.Max(0.6f, len))' in cc and 'Mathf.Max(3.2f, boom - 0.25f)' not in cc, '4.0.2 chase camera never placed past the wall hit; the lift is collision-checked')
check('public static bool Leaving' in intro and 'void OnApplicationPause(bool paused)' in intro, '4.0.2 intro: no dolly race on a late exit, background time not counted')
check('interstitialDue = false;          // never a full-screen ad right after the rewarded one' in hud3 and 'public float HoldUntil;' in text(SCRIPTS / 'UI/AdButtonLive.cs') and '"ADS UNAVAILABLE RIGHT NOW"' in text(SCRIPTS / 'UI/AdButtonLive.cs'), '4.0.2 ads: no interstitial after a rewarded ad, readable failure captions')
check('public static string Upper(string s)' in text(SCRIPTS / 'Core/Loc.cs') and 'Loc.Upper(spec.DisplayName)' in text(SCRIPTS / 'Mission/MatchDirector.cs'), '4.0.2 Turkish capitals for rival names')
check('Application.lowMemory +=' in ar and 'if (albedo != null && mats[set] != null) spillMats[set] = mats[set];' in ar, '4.0.2 arena cache: only textured materials cached, dropped on low memory')
check('2026-11-05' in lg and '2026-11-05' in text(ROOT.parent / 'docs/privacy.html'), '4.0.2 legal texts dated')
gar3 = text(SCRIPTS / 'Core/Garage.cs'); vs3 = text(SCRIPTS / 'Vehicles/VehicleSpec.cs')
check('"KIZIL ŞİMŞEK"' in gar3 and '"KAPLAN T-12"' in gar3 and 'BREAKLINE' not in gar3 and 'Loc.T("Kara Mamut", "Black Mammoth")' in vs3 and 'Breakline' not in vs3, '4.0.2 new vehicle names (TR + EN)')
check('BossNamesTr' in camp and 'OMEGA WAR RIG' not in camp, '4.0.2 boss names localised, no retired vehicle names')
check('Version = "4.0.2"' in diag and 'VersionCode = 402' in diag, '4.0.2 runtime version')
for doc in ['ASSET_INVENTORY_TR.md', 'KNOWN_ISSUES_TR.md', 'TECHNICAL_REPORT_4.0_TR.md', 'CHANGES_TR.md']:
    check((ROOT / doc).exists() and len(text(ROOT / doc)) > 1500, '4.0.1 delivery document present: ' + doc)
check('## 4.0.1 (401)' in text(ROOT / 'CHANGES_TR.md') and '## 4.0.2 (402)' in text(ROOT / 'CHANGES_TR.md'), '4.0.1 and 4.0.2 change log entries')

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
print('DERBY ARENA 4.0.2 STATIC VALIDATION (Unity-independent)')
for st, label, detail in res:
    if st == 'FAIL' or '-v' in sys.argv: print(f'[{st}] {label}' + (f' — {detail}' if detail else ''))
print(f'\nSummary: {len(res) - len(fails)} PASS, {len(fails)} FAIL')
print('Not covered: Unity package resolution, C# compilation, Play Mode, APK/IPA build, device behaviour.')
sys.exit(1 if fails else 0)
