# DERBY ARENA — TEST VE ONARIM RAPORU

Güncelleme: 2026-10-31  
Unity hedefi: 6000.3.27f1 / URP 17.3.0 / Android ARM64 / IL2CPP

## Bu revizyonda ele alınan ekranın gelmemesi sorunu

Kaynakta iki somut risk vardı:

- `SpillwayBuilder` ve araç fallback gövdesi runtime'da `Shader.Find` ile shader bulmaya çalışıyordu. Unity, yalnızca `Shader.Find` ile referans verilen shader'ı Player build içeriğine otomatik dahil etmeyebilir; shader build'den çıkarılırsa `Shader.Find` null dönebilir veya pembe hata shader'ı görülebilir.
- `DerbyBootstrap.Awake()` ana kamerayı arena kurma, görev işaretleri ve oyuncu araç kurulumundan sonra oluşturuyordu. Bu adımlardan biri exception verirse kamera hiç oluşmayabiliyor ve ekran boş kalabiliyordu.

Bu revizyonda:

1. URP pipeline asset `Assets/Resources/Derby/BlackoutURP.asset` konumuna alınacak şekilde Editor kurulumuna taşındı.
2. Runtime başlangıcında, sahne yüklenmeden önce, Resources'tan URP yüklenip `GraphicsSettings.defaultRenderPipeline` ve `QualitySettings.renderPipeline` atanıyor.
3. URP Lit / Emissive Lit / Unlit materyalleri gerçek `.mat` asset'leri olarak `Assets/Resources/Derby/Materials/` içine üretiliyor. Böylece shader yalnızca dinamik string ile bulunmuyor; Material asset üzerinden Player build'e referanslanıyor.
4. Ana kamera `SpillwayBuilder.Build` öncesinde kuruluyor ve solid-color temizleme rengi, target display, culling mask ve klip aralığı açıkça ayarlanıyor.
5. Dünya, görev işaretleri, araç, dokunmatik kontrol, HUD, AI ve ses aşamaları ayrı try/catch sınırlarına alındı. Bir alt sistem çökerse tüm `Awake()` yarıda kalmıyor.
6. Dünya kurulumu başarısızsa acil arena; ana oyuncu aracı oluşturulamazsa görünür acil araç üretiliyor.
7. Materyal kaynakları eksikse arena geometrisi yalnızca bu nedenle exception ile durmuyor.
8. GLTFast yükleyicisindeki başarısız import fallback gövdesinin null materyal ataması güvenli hâle getirildi.
9. GLTFast 6.19.0'a sabitlendi; 6.20.0 root namespace değişikliğini mevcut kodla karıştırmıyor.
10. Android paket/sürüm hedefi 1.0.9 / versionCode 109 olarak arttırıldı.

## Bu pakette çalışan statik kontroller

- ZIP bütünlüğü: kontrol edilecek.
- Tam repo dosya ağacı: orijinal kullanıcı ZIP'indeki yollar korunacak; yalnızca Unity onarım dosyaları ve raporlar değiştirilecek.
- OBJ/PNG/GLB: geometri ve dosya imzaları doğrulandı.
- Unity açılış zinciri, kaynak metinleri üzerinden kontrol ediliyor.

## Bu oturumda yapılamayan testler

Bu ortamda Unity Editor, Android SDK ve gerçek cihaz bulunmadığı için C# derleme, URP asset serializasyonu, Play Mode, Cloud Build, APK imzası ve Poco X6 Pro cihaz testi çalıştırılamaz. Bu nedenle ekran sorununun cihazda tamamen giderildiği henüz doğrulanmış değildir.

## Zorunlu kabul adımları

1. Unity 6000.3.27f1 ile Package Manager çözümlemesi ve C# compile: 0 error.
2. Editor Console'da `DERBY BUILD SETTINGS APPLIED: URP + RESOURCE SHADERS ...` satırı.
3. Play Mode'da en az kamera, zemin, oyuncu gövdesi ve HUD görünür.
4. Cloud Build sonrası APK metadata: `com.lenstedreal.derbyarena`, 1.0.9 / 109.
5. Poco X6 Pro'da kurulum, açılış, oyun sahnesi, GLB/fallback, dokunmatik, ses ve yeniden başlatma testi.

**Bu paket kaynak kod onarımıdır; doğrulanmış çalışan APK değildir.**
