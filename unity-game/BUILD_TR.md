# DERBY ARENA — BLACKOUT RUN

**Unity:** 6000.3.27f1  
**Render Pipeline:** Universal Render Pipeline 17.3.0  
**glTFast:** 6.19.0  
**Android kimliği:** `com.lenstedreal.derbyarena`  
**Hedef sürüm:** `1.0.9` / `versionCode 109` / ARM64 / IL2CPP

> Bu paket tam repo arşivini korur ve Unity Android açılış/render sorunlarına yönelik kaynak düzeltmeleri içerir. Bu ortamda Unity Editor/Android SDK olmadığı için APK üretildiği veya cihazda çalıştığı iddia edilmez.

## Önemli açılış ve ekran düzeltmeleri

1. `DerbyStartupDiagnostics` sahne yüklenmeden önce `Resources/Derby/BlackoutURP.asset` dosyasını yükleyip Graphics + mevcut Quality override ayarına atar. Böylece GLTFast başlamadan önce Built-in Pipeline ile açılma ihtimali kapatılmaya çalışılır.
2. URP pipeline ve Universal Renderer artık `Assets/Resources/Derby/` altında oluşturulur; build içinde Resources üzerinden bulunabilir.
3. URP Lit, emissive Lit ve URP Unlit materyalleri `Assets/Resources/Derby/Materials/` altında üretilir. Runtime `Shader.Find` tek bağımlılık olmaktan çıkar; shader/material referansları build içerik toplamasına girer.
4. Ana kamera arena, materyal, görev ve araç kurulumundan önce oluşturulur. Sonraki bir kurulum aşaması hata verse bile sahnede aktif kamera bulunur.
5. Arena oluşturma, görev işaretleri, araç, HUD, düşman ve ses hazırlığı ayrı hata sınırlarına alındı. Kritik aşama başarısız olursa basit acil arena ve görünür acil oyuncu aracı devreye girer.
6. Runtime materyal bulunamazsa `SpillwayBuilder` artık sadece bu nedenle exception fırlatmaz; primitive renderer'ın varsayılan materyali korunur.
7. glTFast 6.19.0 kullanılır. 6.20.0 sürümünde `GLTFast.*` kök ad alanı `Unity.Cloud.Gltfast.*` olarak değiştiği için mevcut kaynak kodu uyumsuz yükseltmeden korur.

## Projeyi açma

1. Unity Hub ile **6000.3.27f1** sürümünü kur.
2. Unity Hub > Open ile `unity-game` klasörünü aç.
3. Package Manager bağımlılıklarının tamamlanmasını bekle.
4. `Derby > Repair Project And Android Settings` menüsünü çalıştır.
5. Console'da `DERBY BUILD SETTINGS APPLIED: URP + RESOURCE SHADERS / DERBY ARENA / 1.0.9 / 109 / ARM64` kaydını doğrula.
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

- Package: `com.lenstedreal.derbyarena`
- Version: `1.0.9`, versionCode `109`
- Scripting backend: IL2CPP; Architecture: ARM64; minimum SDK: API 25
- Build öncesi callback URP, kalite override'ları, sahne listesi ve Android kimliğini yeniden uygular.
- APK'yı kurmadan önce package/version metadata ve imza doğrulanmalıdır.

## Statik doğrulama

Kökteki `validate_derby.py` Unity dışı kontrolleri çalıştırır: paket sürümü, sahne/boot dosyaları, açılış sırası, runtime pipeline bootstrap'i, Resources shader materyalleri, 10 OBJ, 9 PNG ve 5 GLB. Bu kontrol C# compile, Play Mode, APK build veya fiziksel cihaz testi yerine geçmez.

## Diğer proje alanları

Bu teslim, yüklediğin orijinal repo ağacını korur. `native-android/`, `wreck-circuit/`, web/PWA kaynakları, `.github/`, `.grok/`, migration ve screenshot klasörleri silinmemiştir. Unity Android hedefi yalnızca `unity-game/` altındadır.
