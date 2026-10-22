# Derby Arena 1.0.5 — Oyun tasarımı

## Oyun döngüsü

Üç dakikalık tek oyunculu hayatta kalma derbisi. Oyuncu, kapalı bir kolizeumda dört AI rakibe karşı ağır araç ve taret kullanır. Rakiplerin sağlıkları sıfırlanırsa veya süre sonunda oyuncunun toplam dayanıklılığı en yüksekse oyuncu kazanır. Beraberlik mümkündür.

## Savaş ve sürüş

- Arcade fizik sürüşü, yumuşatılmış mobil direksiyon, otomatik gaz, fren/geri, nitro ve el freniyle drift.
- Taret otomatik nişan yardımına sahiptir; oyuncu isterse ayarlardan kapatıp ekranın sağ tarafında sürükleyerek manuel nişan yapabilir.
- Çarpışma hasarı bağıl hız üzerinden hesaplanır ve kısa tekrar-hasar aralığı vardır.
- Mermiler havuzdan çıkar; yüksek hızda hedef atlamasını azaltmak için her fizik adımında ışın testi yapılır.
- Tamir, zırh, nitro, silah ve mühimmat ganimetleri zamanla yeniden belirir.

## Arena ve görsel yön

İkinci kaynak arşivindeki özgün kolizeum GLB'si, tribün kalabalığı, ayrı araç modelleri, ganimetler ve VFX modelleri Godot sahnesine entegre edilmiştir. Fizik sınırı modelin dekoratif ağlarından ayrı tutulur; 3D mobil renderer ve düşük/orta/yüksek performans ayarları kullanılır.

## Platform ve kimlik

- Ad: **Derby Arena**
- Sürüm: **1.0.5** (Android versionCode 105)
- Paket: `com.lenstedreal.derbyarena`
- Godot 4.7.2 Stable, GDScript, Android arm64-v8a
- Ağ bağlantısı gerektirmez; tek oyunculu offline oyun.

## Girişler

**Mobil:** otomatik gaz, sol direksiyon alanı, ateş/otomatik nişan, fren-geri, nitro, drift.

**PC test:** WASD, F ateş, Space nitro, Shift drift, Esc duraklat.
