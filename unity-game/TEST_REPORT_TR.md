# DERBY ARENA / BLACKOUT RUN — v1.1.0 (versionCode 110) düzeltme raporu

> **Dürüst durum:** Bu ortamda Unity Editor, Android SDK ve cihaz yok. Aşağıdaki değişiklikler kod
> incelemesi, ekran görüntüsü analizi ve Unity bağımsız statik kontrollerle yapıldı. **C# derlemesi,
> Play Mode, APK ve telefon testi henüz çalıştırılmadı.** İlk gerçek doğrulama Unity Cloud Build + telefon.

## 1. Teşhis (kanıtlı)

Gönderilen ekran görüntüsü pikselleri ölçüldü:

- Sol alttaki "beyaz kutu" koddaki sürüş alanı: `Color(1,1,1,0.12f)` yani **%12 saydam beyaz** olmalıydı,
  ekranda **tam opak beyaz (250,250,250)**. Diğer düğmeler de olması gerekenden opak.
- Üstteki zaman yazısında farklı rakamlar **üst üste basılmış**.

İkisi de aynı şeyin belirtisi: **hiçbir kamera çizim yapmıyordu**. Kamera çizmeyince ekran temizlenmez;
yarı saydam HUD her karede öncekinin üstüne çizilip opaklaşır, değişen zaman rakamları birikir.
Yani sorun HUD değil, 3D hattın hiç ayağa kalkmamasıydı.

Kök neden: proje URP'ye bağlıydı ama URP asset'leri / global ayarları yalnızca Editor kodu ile
çalışma anında üretiliyordu ve repoda `GraphicsSettings/QualitySettings` yoktu. Cloud Build'de bu zincir
doğrulanamıyor; bozulunca URP kamera çizmiyor (siyah ekran).

Ek olarak kodda şu **kanıtlanabilir** hatalar vardı:

1. **Görev tamamlanamıyordu:** `ExtractZone` tetikleyicisi (`OnTriggerStay`) kullanıyor ama silindir
   primitive'in çarpışma kutusu `isTrigger` değildi → `InsideExtract` hiç true olmuyor, "Kazandın" yok.
2. **Arena küplerden ibaretti;** repodaki 10 OBJ + 9 PNG kullanılmıyordu.
3. Mobilde **roket düğmesi yoktu** (roket yalnızca klavyede `R`).
4. Duraklat düğmesi hem `onClick` hem dokunma kodundan tetikleniyordu (çift tetik = pause hemen kalkar).
5. Kazanma/kaybetme sonrası mobilde **yeniden oynama yoktu** (yalnızca Enter tuşu veya duraklat menüsü).
6. Hasar dumanı parçacıklarına materyal atanmamıştı (pembe/görünmez).

## 2. Değişiklikler

| Alan | Değişiklik |
|---|---|
| Render | **Built-in pipeline.** URP ve glTFast paketleri kaldırıldı (manifest). Hiçbir üretilmiş SRP asset'ine bağımlılık yok. |
| Materyal | `Resources/Derby/Materials` içinde 5 şablon (Lit, Normal, Emissive, Glass, Fx); Editor kurulumu üretir, shader varyantları build'e girer. |
| Araçlar | `MiniGlb`: bağımlılıksız GLB yükleyici (dokular, cam, emisyon). Başarısızsa **görünür** yedek gövde + ekranda neden. |
| Arena | `SpillwayAssets` + `ObjMesh`: gerçek OBJ'ler ve albedo/normal dokular yüklenir. Zemin/duvar çarpışması kutu kabuğunda, köprü ve bariyerler mesh collider. Yüklenemezse eski küp arena görünür kalır. |
| Atmosfer | Reaktör kulesi (parlayan halkalar + ışık), sis, ana + dolgu ışığı, tehlike lambaları. Hücre ve güç-yükseltmelerine dönen/süzülen ışık sütunları. |
| HUD | Yeniden yazıldı: CAN/NİTRO çubukları, zaman, hücre sayacı, hedef metni, **gerçek analog joystick** (taban + hareket eden topuz), basılma geri bildirimi, güvenli alan, duraklatma menüsü, sonuç ekranı + "yeniden oyna". Türkçe karakterler düzeltildi. |
| Giriş | Yeni Input System **ve** eski Input arasında otomatik geçiş; düğmeler gerçek ekran dikdörtgenlerine göre test edilir. EventSystem bağımlılığı kalktı. Roket düğmesi + mermi sayacı eklendi. |
| Görev | Çıkış bölgesi gerçek tetikleyici kutu; yeşil ışık sütunu. |
| Tanılama | Release APK'de de görünen ekran paneli (ilk 25 sn): boru hattı, kamera sayısı, arena/araç yükleme durumu, son hatalar. |
| Ayarlar | Android grafik API: GLES3 öncelikli; Input: Both; sürüm 1.1.0 / 110. Paket kimliği **korundu:** `com.lenstedreal.derbyarena`. |

## 3. Telefonda ilk açılışta beklenen tanılama paneli

```
DERBY ARENA v1.1.0 | Built-in | kamera: 1 | OpenGLES3
Başlangıç: kamera=True oyuncu=True görev=True hud=True
Arena: 10/10 mesh, 1.x sn
Araç Breakline Interceptor: GLB tamam
```

- `Arena: ... eksik: ...` → hangi OBJ yüklenemedi (küp arena yedek olarak kalır).
- `Araç ...: GLB hata (...)` → neden yazılır; yedek gövde kullanılır.
- `HATA:` satırları → son 4 hata (ekran görüntüsü atarsan kök neden doğrudan görünür).
- Panelde `kamera: 0` yazarsa sorun render hattının dışında demektir.

## 4. Test adımları

1. Orijinal projenin yedeğini al.
2. `unity-game/` ve `validate_derby.py` dosyalarını repoya kopyala (üzerine yaz), commit + push.
3. Unity Cloud Build #7: derleme hatası çıkarsa **log'u olduğu gibi** gönder.
4. APK'yı kur: panel + oyun dünyası + araç görünmeli; joystick ile dön, GAZ ile ilerle, hücreleri topla
   (mavi ışık sütunları), 3/3 olunca yeşil bölgede 6 sn kal → "GÖREV TAMAM".

## 5. Doğrulanmayanlar / bilinen riskler

- **C# derlemesi doğrulanmadı** (derleyici yok). Parantez dengesi ve semboller çapraz kontrol edildi.
- Araç yönü/dokusu: GLB'de ön yüz −Z; mevcut 180° döndürme korundu ama gerçek cihazda görsel teyit gerekir.
- Araç boyaları fiziksel metal yerine düşük metalik (ortam yansıması yok); isteğe göre ince ayar.
- Arena düzeni OBJ'lerin kendi yerleşimini kullanır (X ekseni Unity için aynalandı); hücre/güç konumları eski.
- Düşman yapay zekâ ve fizik değerleri dokunulmadı; oynanış dengesi cihazda ayarlanmalı.
- Ses sentezlenmiş; efektler (parçacık) yalnızca sade.
