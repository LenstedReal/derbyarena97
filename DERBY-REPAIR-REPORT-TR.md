# DERBY ARENA — ONARIM TESLİM RAPORU

## Kapsam
Bu teslim, yüklenen tam repo arşivini korur; odaklanan derleme/runtime hedefi `unity-game/` altındaki Unity 6000.3.27f1 Android projesidir. `native-android/`, `wreck-circuit/` ve web uygulaması silinmemiş veya başka projeyle değiştirilmemiştir.

## Düzeltilenler
- URP asset üretimi URP 17.3.0'ın `UniversalRenderPipelineAsset.Create(renderer)` factory metoduna geçirildi.
- URP, tüm Quality seviyelerine build öncesi atanıyor; eski bozuk renderer-list asset'i tespit edilip yeniden oluşturuluyor.
- Android PlayerSettings build callback ile yeniden uygulanıyor: `com.lenstedreal.derbyarena`, 1.0.8 / 108, IL2CPP, ARM64, landscape.
- glTFast 6.19.0'a sabitlendi. 6.20.0 release notes root namespace'i `GLTFast.*`'tan `Unity.Cloud.Gltfast.*`'a taşıdığını belirtiyor; projedeki eski namespace kullanımına uyum için bu sürüm geçişi ertelendi.
- GLB yükleyicide başlık kontrolü, hata/iptal yakalama, fallback araç ve importer dispose akışı eklendi.
- Runtime shader seçimi URP Lit → URP Unlit → Unlit fallback sırasına alındı; hata shader'ı kullanılmıyor.
- Tam ekran startup hata overlay'i release build'de kapatıldı.

## Kontrol sonuçları
`validate_derby.py`: **42 PASS / 4 WARN / 0 FAIL**. ZIP bütünlüğü doğrulandı. 10 OBJ, 9 PNG ve 5 GLB yapısal olarak kontrol edildi.

## Kalan yayın kapıları
Bu ortamda Unity Editor, Android SDK ve C# compiler bulunmadığından Unity compile, Play Mode, Cloud Build ve gerçek cihaz testi yapılamadı. Dört Unity ProjectSettings dosyası ZIP'te yok; URP onarım kodu bunları Unity Editor/build sırasında üretip atamayı hedefliyor. Ayrıca spillway OBJ/PNG dosyaları şu an sahne oluşturucu tarafından kullanılmıyor; mevcut arena kodla primitive geometri kuruyor. Dolayısıyla görsel kalite ve gerçek APK kabul testi henüz tamamlanmış değil.

**Bu teslim düzeltilmiş kaynak paketidir; doğrulanmış çalışan APK değildir.** Önce Unity Editor/Cloud Build doğrulaması, ardından cihaz testi gerekir.
