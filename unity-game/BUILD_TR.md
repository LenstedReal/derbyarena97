# DERBY ARENA — Build (v1.1.0)

- Unity: **6000.3.27f1**, Android **ARM64 / IL2CPP**, Graphics API: GLES3 → Vulkan
- Render pipeline: **Built-in** (URP ve glTFast kaldırıldı; bkz. CHANGES_TR.md)
- Paket kimliği: `com.lenstedreal.derbyarena` — versionName `1.1.0`, versionCode `110`
- Paketler: Input System 1.11.2, uGUI 2.0.0 + çekirdek modüller (harici registry yok)

## Cloud Build / yerel build
1. Projeyi `unity-game/` klasöründen aç. `DerbyBuildSettingsEnforcer` build öncesi ayarları ve
   `Assets/Resources/Derby/Materials/*.mat` şablonlarını üretir.
2. Konsolda şunu ara: `DERBY BUILD SETTINGS APPLIED: BUILT-IN PIPELINE + RESOURCE MATERIALS ...`
3. Sahne: `Assets/DerbyArena/Scenes/BlackoutSpillway.unity` (tek sahne, her şey `DerbyBootstrap` ile kurulur).
4. Varlıklar `StreamingAssets/Models` altında (OBJ, PNG, GLB); çalışma anında yüklenir.

## Statik kontrol
`python validate_derby.py` (Unity gerektirmez; derleme veya oynanış kanıtı DEĞİLdir).
