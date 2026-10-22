# Derby Arena 1.0.5

**Derby Arena**, Android yatay ekran için geliştirilen tek oyunculu 3D araç savaş oyunudur. Godot 4.7.2 Stable, GDScript ve Mobile renderer kullanır.

## Bu sürümde

- Gerçek kolezyum ve tribün GLB modelleri; görseldeki enkaz kümeleri için ayrı statik çarpışma meshleri ve arenayı çevreleyen mobil dostu segmentli bariyer.
- Beş araçlık savaş alanı: oyuncu ve dört rakip AI; rakipler yakın dövüş, topçu ve çevresel manevra profillerine sahip.
- Kalıcı garaj seçimi: Muscle, Raider A/B/C ve War Rig görünümleri.
- Tamir, zırh, nitro, overdrive ve mühimmat güçlendiricileri; gerçek GLB pickup modelleri ve yeniden doğma süresi.
- Arcade sürüş, hızlanırken kamera FOV tepkisi, animasyonlu teker pivotları, boost egzoz alevleri, çarpışma hasarı, yönlü taret, GLB iz mermileri, namlu parlaması ve hızlı çarpışma raycast’i.
- Yükseltilmiş savaş HUD'u, otomatik gaz + sol direksiyon + otomatik nişanlı ateş akışı, duraklatma, sonuç ekranı ve ayarlar.
- Düşük ekran karmaşası: ayrı gaz düğmesi kaldırıldı; fren/geri tek düğmede, nitro dokunma-başlatmalı ve fare test kontrolleri de oyun fiziğine bağlandı.
- 16 atışlık mermi havuzu; yüksek hızlı mermilerde fizik adımı başına raycast kontrolüyle ıskalanan çarpışmalar azaltıldı.
- Düşük/orta/yüksek grafik seçenekleri ve 2,4 saniyelik başlangıç koruması.

## Kontroller — mobil kullanım basitleştirildi

**Mobil:** Araç otomatik gaz verir; sol alttaki geniş direksiyon alanında parmağını sağa/sola sürükleyerek yön ver. Sağ alttaki **ATEŞ / OTO NİŞAN** düğmesini basılı tut: taret en yakın canlı rakibe nişan alır ve ateş eder. **NİTRO** kısa bir hız patlaması başlatır; düğmeyi basılı tutmak yerine hazır olduğunda tekrar dokun. **FREN / GERİ** basılı tutulduğunda aracı yavaşlatır ve durduktan sonra geri sürer. **DRİFT** el frenini uygular. Direksiyon parmağı bırakıldığında ortaya döner.

Nişan yardımını Ayarlar'dan kapatırsan, ateş düğmesine ek olarak sağ orta alanda sürükleyerek taret açısını elle düzeltebilirsin. Duraklatma ve sonuç ekranları açıldığında sürüş/ateş girdileri otomatik temizlenir; uygulama arka plana geçtiğinde takılı kalan tuşlar bırakılır.

**PC test:** WASD sürüş, F ateş, Space nitro (basıp bırak), Shift el freni, Esc duraklatma. Ekrandaki düğmeler fareyle de kullanılabilir.

## Android paketi

- Uygulama adı: `Derby Arena`
- Sürüm: `1.0.5` (versionCode `105`)
- Paket adı: `com.lenstedreal.derbyarena`
- Hedef mimari: `arm64-v8a`
- Çıktı: `build/derby-arena-1.0.5.apk`

Godot 4.7.2 export templates ve Android SDK/JDK gerekir. GitHub Actions iş akışı kökteki `.github/workflows/android-build.yml` dosyasındadır. `workflow_dispatch` ile manuel başlatılır ve APK `derby-arena-1.0.5-debug-apk` artifact'i olarak yüklenir.

## Kaynak ve varlık notu

`assets/models/` içindeki arena, crowd, araç, pickup ve VFX GLB dosyaları kullanıcı tarafından sağlanan `Derby-arena` kaynak arşivinden alınmıştır. Bu dosyaların ayrı lisans bildirimi kaynak arşivinde bulunmadığından, kamuya açık mağaza dağıtımından önce varlık lisanslarının doğrulanması gerekir.
