# DERBY ARENA 4.0.1 — Varlık Envanteri

Tüm varlıklar bu depodaki Python üreteçleriyle (`scripts/*.py`, numpy + Pillow) veya çalışma zamanında kodla
üretilir. Dışarıdan alınan tek şey Poppins yazı tipidir (SIL OFL 1.1, `Resources/Derby/Fonts/OFL-Poppins.txt`).
Hiçbir varlık üçüncü taraf bir modelden, fotoğraftan veya ticari paketten kopyalanmamıştır.

## 1. 3B modeller (`Assets/StreamingAssets/Models`, GLB, MiniGlb ile yüklenir)

| Dosya | Boyut | Üçgen | Düğümler / içerik | Üretici |
|---|---:|---:|---|---|
| `vehicles/player_muscle.glb` | 0.76 MB | 8.4k tekil (10.2k çizilen) | `*_Body`, `Wheel_FL..RR`, `Turret>Barrel`, **`Body_LOD1`** (944 üçgen) | `scripts/carforge.py` |
| `vehicles/enemy_raider_a.glb` | 0.73 MB | ~9.3k | aynı sözleşme + LOD1 (928) | carforge |
| `vehicles/enemy_raider_b.glb` | 0.91 MB | ~9.5k | aynı + LOD1 (928) | carforge |
| `vehicles/enemy_raider_c.glb` | 0.75 MB | ~9.7k | aynı + LOD1 (928) | carforge |
| `vehicles/enemy_boss_warrig.glb` | 0.94 MB | ~10.6k | aynı + LOD1 (944) | carforge |
| `vehicles/tiger_t12.glb` | 0.99 MB | 5.4k | `Body`, `Body_LOD1`, `Turret>Barrel+Flag`, `Track_L/R`, `Road_L1..8/R1..8`, `Sprocket_*`, `Idler_*`; kamuflaj JPEG 2048, normal/metalik haritalar | `scripts/tankforge.py` |
| `arena/colosseum.glb` | 8.57 MB | 27.7k | 37 mesh, 26 doku (JPEG albedo + PNG normal; `texforge`) | `scripts/texforge.py` + `glbtex.py` |
| `arena/crowd.glb` | 2.18 MB | 81.4k | 24 grup, vertex renkli, emissive vizörler | `scripts/crowdforge.py` |
| `props/loot_*.glb` (4) | 0.35 MB × 4 | küçük | toplanabilir kutular | kod |
| `spillway/*.obj + dokular` | ~5 MB | — | Spillway arenası (OBJ, `ObjMesh.cs`) | 1.x üreteci |

Toplam StreamingAssets: **22 MB** (sıkıştırılmamış; APK içinde ~20 MB).

Araç GLB'lerindeki dokular çalışma zamanında (JPEG/PNG → `Texture2D`) çözülür; 4.0.1'de bu çözme işlemi
`MiniGlb.Prepare / PreloadTextures / PreloadMeshes / Finish` adımlarıyla karelere yayılır.

## 2. Resources (`Assets/Resources/Derby`, 4.2 MB)

| Yol | İçerik |
|---|---|
| `keyart.jpg` (310 KB) | Açılış / yükleme görseli (logo değiştirilmedi) |
| `Textures/clouds.png` (180 KB) | Gökyüzü bulut panoraması (texforge, kenarsız sarmal) |
| `Textures/ruins_{sand,road,brick,plaster}.jpg + *_n.png` (3.2 MB) | Çöl Harabeleri PBR seti (`texforge`), dünya-metre UV ile döşenir |
| `Fonts/Poppins-Medium.ttf, Poppins-Bold.ttf` (315 KB) | UI yazı tipi (OFL) |
| `UI/*.png` (≈150 KB) | panel, panel_ring, button, button_primary, pill, card, card_ring, tab, carbon, glow, hazard + 45 `ic_*` ikon (`scripts/uiforge.py`) |
| `Materials/*.mat` | Derleme öncesi `UrpAndPlayerSetup` tarafından üretilen şablonlar: RuntimeLit, RuntimeLitNormal, RuntimeLitEmissive, RuntimeLitGlass, RuntimeLitDecal, RuntimeFx, RuntimeFxAdd, RuntimeSky, RuntimeSky2, RuntimeVertexLit, RuntimePost |

Android'de Resources dokuları **ASTC** ile sıkıştırılır (`EditorUserBuildSettings.androidBuildSubtarget = ASTC`,
4.0.1). UI sprite'ları ve ikonlar sıkıştırmasız/mip'siz kalır (`Editor/DerbyAssetImport.cs`).

## 3. Shader'lar (`Assets/DerbyArena/Shaders`)
- `DerbySky.shader` — bulutlar, güneş, yıldızlar; `Skybox/Procedural` yedeği.
- `DerbyPost.shader` — patlama parlaması, vuruş sarsıntısı, nitro ayrımı, film greni (SM3, NaN güvenli ACES).
- `DerbyVertexLit.shader` — kalabalık ve uzak sahne nesneleri.

## 4. Ses
Tüm sesler `Audio/SynthAudio.cs` ile açılışta sentezlenir (dosya yok): motor (idle / engine / load katmanları), palet
(`Tracks`), top (`Cannon`), dolum (`Reload`), atış, vuruş, çarpma, patlama, roket, pickup, UI tonları, kalabalık,
alarm, rüzgâr (`Wind`) ve arena ortam sesi (`Ambience`). Arena başına `AudioReverbZone` (Kolezyum: Arena,
Harabeler: Plain, Rıhtım: Hangar, diğerleri: Concerthall).

## 5. Kod (`Assets/DerbyArena`, 53 C# dosyası, ~15.6k satır)
| Klasör | Dosyalar |
|---|---|
| `Scripts/Core` | AdaptivePerformance, Campaign, **ControlLayout (yeni)**, DailyMissions, DerbyBootstrap, DerbyStartupDiagnostics, GameFlow, Garage, Legal, Loc, **PerfLog (yeni)**, SaveData |
| `Scripts/UI` | **ControlLayoutEditor (yeni)**, **ControlWidgets (yeni)**, HudController, IntroSplash, MenuController, TouchControls, UiKit |
| `Scripts/Vehicles` | ArcadeCar, EngineAudio, MiniGlb, MiniJson, TankVisual, VehicleHealth, VehicleKit, VehicleSpec, VehicleVisual |
| `Scripts/World` | Arena, ArenaDressing, Atmosphere, Crowd, DocksArena, GlbLibrary, Mats, MeshFactory, ObjMesh, PickupSpin, PostFx, RuinsArena |
| `Scripts/Combat` | Fx, Projectiles, WeaponRig |
| `Scripts/Mission` | MatchDirector, Pickups |
| `Scripts/AI` | EnemyDriver |
| `Scripts/CameraRig` | ChaseCamera |
| `Scripts/Audio` | SynthAudio |
| `Ads` | AdsConfig, AdsManager (DERBY_ADS tanımlıyken Google Mobile Ads) |
| `Editor` | UrpAndPlayerSetup (build ayarları + materyal şablonları), DerbyAssetImport |

## 6. Üreteçler (`scripts/`)
`carforge.py` (5 araç + LOD1), `tankforge.py` (Tiger I T-12), `texforge.py` (döşenebilir PBR), `glbtex.py`
(GLB doku değişimi), `crowdforge.py`, `uiforge.py` (UI sprite + ikon), `make_icons.py` (uygulama ikonu — değiştirilmedi).
Hepsi `python3 -I scripts/<ad>.py` ile tekrar üretilebilir; `validate_derby.py` çıktıları doğrular (344 statik kontrol).

4.0.1 temizliğinde oyunla ilgisi olmayan eski web (Vite/React), Godot (`wreck-circuit/`) ve Kotlin (`native-android/`) dosyaları depodan kaldırıldı; geçmişte (git history) durmaya devam eder.
