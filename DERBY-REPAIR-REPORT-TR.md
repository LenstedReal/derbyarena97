# DERBY ARENA — KAYNAK ONARIM RAPORU (EKRAN / STARTUP HOTFIX)

## Tam repo bütünlüğü
Bu revizyon, kullanıcının yüklediği `DERBY-ARENA-FULL-SOURCE.zip` arşivinden üretildi. Önceki sabit arşivin aksine, bu paket `.github/`, `.grok/`, `native-android/`, `wreck-circuit/`, web/PWA, scripts, migrations, screenshots ve diğer kaynak klasörlerini çıkarmadan korur. Yalnızca şu Unity dosyaları ve raporlar değiştirildi:

- `unity-game/Assets/DerbyArena/Editor/UrpAndPlayerSetup.cs`
- `unity-game/Assets/DerbyArena/Scripts/Core/DerbyBootstrap.cs`
- `unity-game/Assets/DerbyArena/Scripts/Core/DerbyStartupDiagnostics.cs`
- `unity-game/Assets/DerbyArena/Scripts/World/SpillwayBuilder.cs`
- `unity-game/Assets/DerbyArena/Scripts/Vehicles/GltfVehicleVisual.cs`
- `unity-game/Packages/manifest.json`
- `unity-game/BUILD_TR.md` ve `unity-game/TEST_REPORT_TR.md`
- `validate_derby.py`

## Kök nedene dönük değişiklikler
1. Runtime pipeline bootstrap: URP Resources asset'i `BeforeSceneLoad` aşamasında yüklenir ve aktif pipeline atanır.
2. Shader inclusion: URP Lit, emissive Lit, Unlit `.mat` asset'leri Resources altında oluşturulur; dinamik `Shader.Find` tek referans yolu olmaktan çıkar.
3. Camera-first startup: ana kamera dünya/araç/HUD oluşturulmadan önce açılır.
4. Fault isolation: arena, mission, player, controls, HUD, AI ve audio aşamaları birbirinden ayrılır.
5. Emergency world/player: temel arena/oyuncu oluşturma başarısız olursa kamera boş sahnede kalmaz.
6. Runtime materials: kaynak shader/material yokluğu artık arena kurulumunu tek başına exception ile durdurmaz.
7. GLTFast 6.19.0 pin: mevcut `using GLTFast;` kaynak kodunun 6.20.0 breaking root namespace değişikliğine geçmeden derlenebilmesini sağlar.

## Dürüst doğrulama sınırı
Unity Editor/Android SDK burada çalışmadığı için bu paket compile veya cihaz testinden geçmedi. Kod yolu üzerinde statik kontroller yapılabilir; gerçek Android render ve install kabul testi Unity Cloud Build + Poco X6 Pro gerektirir.
