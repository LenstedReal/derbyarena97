# DERBY ARENA 4.0.2 — Bilinen Sorunlar ve Cihazda Doğrulanacaklar

Bu sürüm Unity Editor'de **oynatılmadı** ve **cihazda test edilmedi** (4.0.1 cihaz videosundaki hatalar düzeltildi, düzeltmeler cihazda henüz görülmedi) (bu ortamda Unity yok). Kod beş yapılandırmada
Roslyn ile derlendi (0 hata) ve tüm statik kontrollerden geçti (`validate_derby.py`, 0 FAIL); geometri yazılım render'ıyla bakıldı. Aşağıdaki liste,
ilk cihaz testinde özellikle bakılması gereken yerleri ve bilinen sınırları açıkça yazar.

## A. Cihazda doğrulanması gereken davranışlar (4.0.1'de yeni)
| # | Konu | Ne beklenir | Sorun olursa |
|---|---|---|---|
| A1 | Kaydırmalı direksiyon | Sol yarıya dokunup kaydırınca araç döner; parmak kalkınca düzelir; pad parmağın altına kayar | Hassasiyet (Ayarlar → Kontroller) veya direksiyon tipini değiştir; `TouchControls.cs` `range = Screen.height * 0.13f / sens` |
| A2 | Eğim direksiyonu | Telefonu direksiyon gibi yatırınca döner; LandscapeRight'ta ters değil | Ters dönüyorsa `ReadTilt`: Input System ekran yönüne göre telafi ediyorsa `a.x`, etmiyorsa `sign * a.y` kullanılır; ivmeölçer yoksa 0 döner (araç düz gider) |
| A3 | Düzen editörü | Düğmeler sürüklenir, − / + boyutlandırır, Kaydet sonrası HUD yeni yerleri kullanır; İptal eski düzene döner; duraklatmadan açılınca oyun devam etmez | `ControlLayoutEditor.cs`; editör açıkken `TouchControls.Suspended = true` |
| A4 | Otomatik gaz varsayılanı | Eski kayıtta da ilk açılışta otomatik gaz açık, kaydırma modu seçili (bir kez, `MigrateControls`) | Ayarlar → Kontroller'den kapatılır; tekrar zorlanmaz |
| A5 | Sinematik açılış | Siyahtan görsel yükselir, çubuk gerçek yüklemeyi gösterir, "HAZIR" olunca dokunarak geçilir; en geç 12 sn'de menü | Çubuk hiç ilerlemezse `Boot.Set` çağrıları (`DerbyBootstrap.BootMenu`) logcat'te `DERBY` satırlarıyla izlenir |
| A6 | Açılış takılması | Menü kurulumu sırasında kare düşüşü eskisinden kısa olmalı; `DerbyPerf` satırları maçın ilk 60 sn'sini ölçer | Hâlâ takılıyorsa logcat'teki `startup` zamanlamaları (`DerbyStartupDiagnostics`) ile hangi adımın uzun sürdüğü görülür |
| A7 | Ses katmanları | Rölantide düşük uğultu, gazda yük sesi; tankta palet takırtısı; patlama/top sesleri motor seslerinin altında kalmaz | Çok yüksek/düşükse `EngineAudio.Volume` (oyuncu 0.42, rakip 0.22) |
| A8 | Araç LOD | Uzaktaki araçlar sadeleşir, yakında tam model; geçişte "yok olma" olmamalı | `VehicleVisual.SetupLod` eşikleri 0.12 / 0.015 |
| A9 | Namlu geri tepmesi | Makineli atışında namlu kısa geri gidip gelir; GLB'de `Barrel` düğümü olmayan yedek modelde etki yoktur | — |
| A10 | ASTC | Build Automation logunda doku sıkıştırma ASTC; GLES3 cihazda görüntü bozulmaz | Bozulursa `ApplyBuildSettings` içindeki ASTC satırı kaldırılır (ETC2'ye döner) |

## A2. 4.0.2'de cihazda bakılacaklar (video düzeltmeleri)
| # | Konu | Ne beklenir |
|---|---|---|
| B1 | Açılış | Siyah → tam parlak DERBY ARENA görseli + durum satırı; yarı saydam donuk kare yok; çıkışta siyaha geçip menü açılır, ikisi üst üste binmez |
| B2 | İlk açılış | İlerleme bir kez sıfırlanır (Bölüm 1, 0 hurda, sadece Kızıl Şimşek); ayarlar ve kontrol düzeni korunur |
| B3 | Yazılar | Hiçbir ekranda yazı yazının üstüne binmez; uzun yazılar kendi kutusunda küçülür; Ayarlar/Kontroller satırları tablette (16:10, 4:3) sağ sütunun altına girmez |
| B4 | HUD | İlerleme ve mesafe aynı satırda; mesajlar onların altında; geri sayım yükleme ekranı kapanınca görünür |
| B5 | Bölümler | 6 sekme ve 20 kart detay panelinin solunda; arkadaki araba kartların arasından görünmez |
| B6 | Sürüş | Kaldırım/boru ayağı gibi alçak engellerde zıplama ve takılma belirgin azalır; çok yavaşken de dönülür; düz giderken küçük parmak hareketleri arabayı sağa sola savurmaz (direksiyon tepki eğrisi); atlama rampaları uçurur (çok yüksek hızda rampa girişinde ön tampon bir an rampaya değebilir; fark edilirse `ArcadeCar.RiseSlope` 0.16 → 0.2) |
| B7 | Kamera | Duvar dibinde, geri geri duvara yanaşınca ve boruların/köprünün altında kamera duvarın, arabanın içine veya arenanın dışına geçmez, ileri-geri pompalamaz; drift'te arabanın etrafında dönmez |
| B8 | Garaj | Parçalar arabaya oturur (garajda ve maçta aynı) ve virajda gövdeyle birlikte yatar; jant renkleri belirgin; "Spot Farlar" tavanın önünde; parça değiştirince yeni parça hemen görünür |
| B9 | Reklam | Ayarlar'daki "Reklam: TEST · …" satırı birkaç saniye içinde "rewarded ready" gösterir; garajdaki düğme mavi ve basılabilir olur; ödüllü reklam Google test reklamı olarak açılır; ödüllü reklamdan sonra "SONRAKİ"ye basınca ikinci bir tam ekran reklam gelmez. Satırda hata yazıyorsa (ör. "init: …", "rewarded: …") o metnin ekran görüntüsü yeterli |
| B10 | Yükleme | Aynı arenaya ikinci girişte yükleme ekranı kısa; "%22" gibi uzun donma kalmaz |

Bir madde beklendiği gibi değilse kısa bir video veya ekran görüntüsü yeterli.

## B. Bilinen sınırlar
1. **FMOD yok.** Değerlendirme yapıldı (bkz. `TECHNICAL_REPORT_4.0_TR.md` §6): eklenti çevrim dışı kurulamıyor ve
   lisans/plugin boyutu bu projeye uygun görülmedi; Unity sesi + sentez katmanlarıyla aynı hedefler sağlandı.
   Gelecekte FMOD'a geçiş için olay adı tablosu raporda.
2. **Reklam kimlikleri TEST.** `AdsConfig.TestMode = true` ve Google test birimleri (resmî test kimlikleri, doğru). Test modunda
   SDK, rıza servisi cevap vermese de başlar; Play'e göndermeden önce gerçek kimlikler girilmeli ve `TestMode = false`
   yapılmalı (bkz. `docs/REKLAM_PLANI_TR.md`).
3. **Sezon 6 yayında değil** (bölüm listesinde "YAKINDA · YOL HARİTASI").
4. **Eğim modu masaüstünde çalışmaz** (ivmeölçer yok); klavye/gamepad her modda çalışır.
5. **Düzen editörü düğmeleri ekranın kenarına taşımaz**: güvenli alan ve üst/alt çubuk içinde kalır (bilerek).
6. **Kayan joystick yalnızca sol/sağ yarıda** (direksiyonun bulunduğu tarafta) başlar; diğer taraftaki serbest
   dokunuşlar yok sayılır (yanlışlıkla direksiyon kırmayı önlemek için).
7. **Performans ölçümü cihaza bağlı.** `PerfLog` sayıları bu ortamda üretilemedi; teknik rapordaki bütçeler hesapla,
   cihaz sonuçları kullanıcı testinden gelecek.
8. **Unity Build Automation "119 errors"** AssemblyUpdater çıktısıdır; derleme sonucuna bakılır (önceki sürümde de
   böyleydi).

## C. 4.0.0'dan taşınan, henüz cihazda görülmemiş maddeler
- Tiger I T-12 görselleri (kamuflaj, T-12 işareti, bayrak dalgası, palet kaydırma) yazılım render'ında doğru;
  Unity'de malzeme/ışık farkı olabilir.
- Çöl Harabeleri: PBR döşeme UV'leri metre bazlı; dokularda dikiş görülürse `MeshFactory.BoxTiled` `Tile` değeri.
- Garaj önizlemesi: parça değişiminde `Showcase.Rebuild()` modeli yeniden kurar; eski cihazda kısa takılma olabilir.

## D. Açık iletişim adresi
Eski oyunun e-postası kaldırıldı. Yeni adres eklenene kadar gizlilik politikası iletişimi Google Play mağaza sayfasındaki
geliştirici iletişim bilgisine yönlendiriyor. Play Console kaydı için geçerli bir geliştirici e-postası gerekecek; adres
belli olunca `Core/Legal.cs` (`Contact`) ve `docs/*.html` güncellenecek. Google ile giriş / bulut kayıt henüz yok.
