# DERBY ARENA — TEST VE ONARIM RAPORU

Güncelleme: 2026-10-31  
Unity hedefi: 6000.3.27f1 / Android ARM64 / IL2CPP

## Bu pakette yapılan kaynak düzeltmeleri

1. URP varlığı artık `ScriptableObject.CreateInstance<UniversalRenderPipelineAsset>()` ile boş oluşturulmuyor; URP'nin `UniversalRenderPipelineAsset.Create(renderer)` fabrikası kullanılıyor.
2. Derleme öncesi `IPreprocessBuildWithReport` callback'i URP, kalite seviyesi override'ları, sahne listesi ve Android kimliğini yeniden uygular.
3. Daha önce oluşturulmuş fakat renderer listesi boş olan hatalı URP asset'i silinip yeniden oluşturulur.
4. GLTFast 6.20.0'ın root namespace/API geçişi nedeniyle 6.19.0'a sabitlendi. 6.20.0 sürüm notları root namespace'in `GLTFast.*` yerine `Unity.Cloud.Gltfast.*` olarak değiştiğini açıkça bildiriyor.
5. GLB indirme, başlık doğrulama, import, instantiate ve iptal/hata yollarına fallback eklendi; import kaynakları temizleniyor.
6. World shader seçiminde URP Lit/URP Unlit fallback'i eklendi; `Hidden/InternalErrorShader` kaldırıldı.
7. Başlangıç hata ekranı yalnızca Editor/Development Build'de gösteriliyor; release'de tüm ekranı kapatan debug paneli çizilmiyor.
8. Android sürümü 1.0.8 / versionCode 108 olarak güncellendi.

## Bu oturumda çalışan statik kontroller

- ZIP bütünlüğü: başarılı.
- Unity-independent varlık ve kaynak denetimi: 42 PASS, 4 WARN, 0 FAIL (onarım sonrası son kontrol).
- OBJ: 10 dosya; vertex/face indeksleri geçerli.
- PNG: 9 dosya; PNG imzaları ve boyut başlıkları okunabilir.
- GLB: 5 dosya; başlık ve JSON yapısı doğrulandı.
- Ana sahne ve `DerbyBootstrap` GUID eşleşmesi: kaynakta mevcut.

## Bilinen eksikler / dürüst sınırlar

- Bu ortamda Unity Editor, Android SDK ve C# derleyicisi yok. Dolayısıyla C# compile, URP asset serialization, Play Mode, GameCI/Cloud Build, APK imza ve cihaz testi çalıştırılmadı.
- `ProjectSettings.asset`, `GraphicsSettings.asset`, `QualitySettings.asset` arşivde yok. Onarım kodu bu ayarları Unity Editor içinde oluşturup atamayı hedefliyor; gerçek Unity açılışında doğrulanmalı.
- glTFast dokümantasyonuna göre runtime'da yüklenen GLB'lerin shader variant'ları build'e dahil edilmelidir. Bu arşivde Editor'de yakalanmış `ShaderVariantCollection` yok; bu, araç modellerinin APK'de pembe/eksik materyalle görünmesi açısından açık bir yayın kapısıdır.
- Spillway OBJ/PNG varlıkları mevcut ama `SpillwayBuilder` şu an bunları kullanmıyor; arena sahnesi primitive geometrilerle kuruluyor. Bu, görsel kaliteyi sınırlayan içerik işidir ve bu kaynak onarımında tamamlanmış sayılmaz.
- Unity Build Automation e-postasındaki `TraceVirtualOffset` HLSL kayıtları precision-conversion uyarılarıdır; tek başına derlemeyi durdurduklarını göstermez.
- Aynı e-postadaki 10 `AssemblyUpdater` hatası `UnityCloudBuild.Newtonsoft.Json.Serialization` ve `UnityEditor.CloudBuild.BuildLogger` referanslarını gösteriyor; paylaşılan kesitte DerbyArena C# kaynak satırı görünmüyor. Bu nedenle bunların Unity Cloud Build iç araçlarından kaynaklandığı kesinleştirilemez. Tam build logu ve ilk hata/stack trace gereklidir.
- Android kurulumundaki “Paket geçersiz” uyarısı için APK binary'si arşivde bulunmuyor; dolayısıyla APK yapısı/imzası bu oturumda incelenemedi.

## Sonraki zorunlu kabul kapıları

1. Unity 6000.3.27f1 ile Package Manager çözümlemesi ve C# compile: 0 error.
2. Setup menüsü ve build callback logu: URP + tüm quality seviyeleri doğrulanmış.
3. Editor Play Mode: sahne, kamera, araç GLB'leri/fallback, HUD, AI ve görev akışı test edilmiş.
4. Android build: APK imzası ve package/version metadata doğrulanmış.
5. Fiziksel cihaz: kurulum, açılış, 10 dakikalık oyun, dokunmatik, ses, performans, arka plana geçiş ve yeniden başlatma test edilmiş.

**Bu kapılar geçilmeden “kusursuz”, “yayına hazır” veya “APK çalışıyor” denmemelidir.**
