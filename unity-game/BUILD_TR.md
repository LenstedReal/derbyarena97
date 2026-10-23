# DERBY ARENA — Blackout Run

Unity **6000.3.27f1** (Unity 6.3 LTS). URP. Klasör: `unity-game/`.

Bu makinede Unity Editor, Android SDK ve Unity lisansı yok. Proje dosyaları yazıldı. Derleme, Play Mode ve APK **çalıştırılmadı**. APK hazır değil.

## Aç

1. Unity Hub ile 6000.3.27f1 kur.
2. Bu klasörü aç. İlk açılışta `Derby/Setup URP And Android` menüsü de otomatik gecikmeyle çalışır.
3. Sahne: `Assets/DerbyArena/Scenes/BlackoutSpillway.unity`
4. Play. Yatay oyun. WASD, Shift el freni, Space nitro, F ateş, R roket, Esc duraklat, Enter yeniden.
5. Android: File > Build Settings > Android > Build. Kimlik `com.lenstedreal.derbyarena`, sürüm `1.0.5`, ARM64, IL2CPP.

Batch kontrol (Editor şart):

```
Unity -batchmode -nographics -projectPath unity-game -executeMethod DerbyArena.EditorTools.BatchValidate.Run -quit
```

## Araç modelleri

`Assets/StreamingAssets/Models/vehicles/` içindeki GLB dosyaları `origin/main` üzerindeki `native-android` varlığından alındı. glTFast 6.20.0 (OpenUPM, `com.atteneder.gltfast`) ile yüklenir. Kolezyum modeli bilerek kullanılmadı. Yeni zemin BLACKOUT SPILLWAY kodla kurulur.

## CI

Kökteki `.github/workflows/unity-android.yml` GameCI kullanır. Koşulmazsa başarılı sayma. Gerekli Secrets: `UNITY_LICENSE`, `UNITY_EMAIL`, `UNITY_PASSWORD`.
