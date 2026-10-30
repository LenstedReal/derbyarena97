# DERBY ARENA — BLACKOUT RUN

**Unity:** 6000.3.27f1  
**Render Pipeline:** Universal Render Pipeline 17.3.0  
**glTFast:** 6.19.0  
**Android kimliği:** `com.lenstedreal.derbyarena`  
**Hedef sürüm:** `1.0.8` / `versionCode 108` / ARM64 / IL2CPP

> Bu paket kaynak kod ve statik doğrulama içerir. Unity Editor/Android SDK bu çalışma ortamında bulunmadığı için C# derlemesi, Play Mode, APK üretimi ve gerçek cihaz testi burada yapılamadı. APK'nın hazır olduğu iddia edilmez.

## Projeyi açma

1. Unity Hub ile **6000.3.27f1** sürümünü kur.
2. Unity Hub > Open ile `unity-game` klasörünü aç.
3. Package Manager bağımlılıklarının tamamlanmasını bekle.
4. `Derby > Repair Project And Android Settings` menüsünü çalıştır. Otomatik kurulum da açılışta denenir.
5. Console'da `DERBY BUILD SETTINGS APPLIED: URP / DERBY ARENA / 1.0.8 / 108 / ARM64` kaydını doğrula.
6. `Assets/DerbyArena/Scenes/BlackoutSpillway.unity` sahnesini açıp Play'e bas.

## Kontroller

- WASD: sürüş ve yön
- Shift: el freni
- Space: nitro
- F: ateş
- R: roket
- Esc: duraklat
- Enter: maç bitince yeniden başlat
- Mobil: sol alt direksiyon alanı; sağ tarafta gaz, fren, nitro, el freni ve ateş alanları

## Android derlemesi

1. File > Build Profiles (veya Build Settings) > Android.
2. Build Settings menüsü URP, kalite seviyeleri ve Android kimliğini derleme öncesi tekrar uygular.
3. Package: `com.lenstedreal.derbyarena`
4. Version: `1.0.8`, versionCode `108`
5. Scripting backend: IL2CPP; Architecture: ARM64; minimum SDK: API 25.
6. Build al; APK'yı Android'e yüklemeden önce dosya boyutunu ve imzasını doğrula.

## Batch doğrulama (Unity Editor gerekir)

```bash
Unity -batchmode -nographics -projectPath unity-game -executeMethod DerbyArena.EditorTools.BatchValidate.Run -quit
```

## Varlıklar ve render

- Araç GLB dosyaları `Assets/StreamingAssets/Models/vehicles/` içindedir.
- GLB isteği, import veya sahneye yerleştirme başarısız olursa oyun artık araç gövdesi fallback'i oluşturur.
- `StreamingAssets/Models/spillway/` altındaki 10 OBJ ve 9 PNG mevcut; mevcut `SpillwayBuilder` bunları sahneye yüklemiyor, arena geometrisini kodla primitive objelerden oluşturuyor. Bu nedenle bu varlıklar şimdilik arşivde korunuyor; sahneye entegre edilmiş sayılmaz.
- Runtime shader seçiminde URP Lit/URP Unlit tercih edilir; Unity'nin pembe `Hidden/InternalErrorShader` shader'ı fallback olarak kullanılmaz.
- glTFast'ın runtime shader variant'ları ayrıca build'e dahil edilmelidir. Bu pakette Unity Editor'de GLB'ler yüklenerek üretilmiş bir `ShaderVariantCollection` yok; Android build öncesi beş GLB'nin tamamını Editor'de yükleyip Graphics > Shader Preloading koleksiyonunu kaydetmek ve Preloaded Shaders listesine eklemek gerekir.

## CI

Kökteki `.github/workflows/unity-android.yml` GameCI akışıdır. GitHub repository secrets:
- `UNITY_LICENSE`
- `UNITY_EMAIL`
- `UNITY_PASSWORD`

CI veya Unity Build Automation başarılı sonuç vermeden APK'nın çalıştığı kabul edilmemelidir.
