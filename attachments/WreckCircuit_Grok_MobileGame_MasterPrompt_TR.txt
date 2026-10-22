# GROK MASTER PROMPT — WRECK CIRCUIT (Android-first 3D car-combat game)

> Kullanım: Grok'a Unity AI videosunu referans olarak ve bu dosyanın tamamını ver. Videoyu *oynanış türü ve fikir referansı* olarak kullan; videodaki marka, proje dosyası veya varlıkların sende olduğunu varsayma.

## 0. ROLÜN VE TESLİMAT STANDARDIN

Sen kıdemli bir Android oyun teknik direktörü, Godot geliştiricisi, oyun tasarımcısı ve mobil optimizasyon uzmanısın. Amacın, Unity AI paneline bağlı olmayan, gerçekten Android telefonda test edilebilecek, 3D bir arena araç savaşı oyunu hazırlamak. Sadece fikir, mockup, görsel veya tek dosyalık demo istemiyorum. Çalıştırılabilir ve yeniden açılabilir bir Godot projesi, eksiksiz kaynak dosyaları, kurulum/build talimatları ve otomatik Android paket oluşturma iş akışı üret.

Kullanıcı çoğunlukla Android telefon ve Termux kullanıyor; masaüstü Unity Editor veya Unity AI paneline güvenilemez. Gerekli olduğunda GitHub Actions gibi bulut derleme iş akışı kullan. Unity AI, Unity Editor, C#, ücretli varlıklar, çevrim içi servisler veya PC üzerinde elle yapılması şart olan işler zorunlu bağımlılık olmasın.

## 1. OYUN KİMLİĞİ VE KAPSAM

- Çalışma adı: **Wreck Circuit**. Bu isim değiştirilebilir ve kesin ticari isim olduğu iddia edilmemeli.
- Tür: üçüncü şahıs kamerayla oynanan, 3D, tek oyunculu, arcade araç savaş arenası.
- Platform: Android telefon; yatay ekran (landscape).
- Çekirdek fikir: oyuncu zırhlı bir araç sürer, rakip araçlarla kapalı bir arenada çarpışır, araca monte silahı kullanır, hasar alır/verir ve turu kazanmak için rakipleri etkisiz hâle getirir.
- Referans videoda görülen arena oluşturma, araç, araca takılan silah, nişan/ateş ve seyirci atmosferi fikirlerini genel seviyede al. Videodan birebir model, logo, telifli varlık, marka veya kod kopyalama. Özgün araç tasarımı, arena dili, UI ve isim oluştur.
- Korku oyunu/GDD/başka bir projeyle karıştırma. Hikâye gerektirmeyen, ayrı bir araba-savaş oyunu projesi olarak ele al.
- İlk sürüm çevrim dışı ve tek oyunculu olacak. Multiplayer, hesap sistemi, reklam, ödeme, battle pass ve backend ekleme; bunlar ilk sürümün kapsamı dışında.

## 2. SABİT TEKNOLOJİ KARARI

- Motor: **Godot 4.7.2 Stable**. 4.8 beta/dev veya deneysel sürüm kullanma.
- Dil: **GDScript 2.0, statik tipler mümkün olan yerlerde kullanılsın**. C#, GDExtension ve üçüncü taraf eklenti kullanma.
- Render: önce Android hedefli Godot **Mobile renderer**. Eski/uyumsuz cihazlar için Compatibility renderer'a geçişin nasıl test edileceğini dokümante et; ilk teslimatta iki renderer arasında rastgele konfigürasyon değiştirme.
- Android paketleri: önce debug APK; release için imzalı APK/AAB iş akışını ve gerekli güvenli secrets listesini dokümante et. Keystore/şifreleri kaynak koduna gömme.
- Proje Android öncelikli, offline, klavye/fare gerektirmeyen dokunmatik kontrolle çalışmalı.
- Godot'un gerçek API ve düğüm adlarını kullan. Eski Godot 3 API'leri (`Spatial`, `KinematicBody`, vb.) yazma. Var olmayan API, dosya, paket veya doğrulanmamış build sonucu uydurma.

## 3. ÖNCE ANALİZ, SONRA ÜRETİM — KAPILAR

İşi aşağıdaki kapılarla yürüt. Bir aşama bitmeden sonrakine geçme:

### Kapı A — Plan ve risk analizi
Önce kod yazmadan şunları döndür:
1. Teknik mimari ve sahne ağacı.
2. Oynanış döngüsü, kontroller ve tur kazanma/kaybetme kuralları.
3. Her kaynak dosyanın amacı ve diğer dosyalarla bağlantısı.
4. Android performans bütçesi ve düşük cihaz fallback'leri.
5. Android APK/AAB üretim yolu: telefondaki Godot Android Editor ile üretim ve/veya GitHub Actions ile bulut üretim seçeneklerini karşılaştır. Telefon kullanıcısı için güvenilir varsayılanı seç.
6. Bilinen motor/araç fiziği riskleri ve nasıl test edileceği.
7. Doğrulama listesi. Belirsiz veya test edilmemiş bir şeyi “çalışıyor” diye işaretleme.

Planın sonunda varsayılanlarla devam et; kritik olmayan konular için tekrar tekrar soru sorma. Kullanıcı planı onayladıktan sonra Kapı B'ye geç.

### Kapı B — Oynanabilir dikey kesit (MVP)
Önce şu küçük ama gerçekten oynanabilir sahneyi tamamla:
- Başlangıç menüsü: Play, Settings, Exit/Quit (Android'de güvenli geri davranışı), ses aç/kapat.
- Tek bir kompakt arena.
- Bir oyuncu aracı ve **bir** AI rakip araç.
- Çalışan arcade sürüş ve çarpışma.
- Bir adet araca monte silah; mühimmat yerine ısınma veya cooldown sistemi; ateş animasyonu/ışık/iz ve çarpışma hasarı.
- Can sistemi, hasar geri bildirimi, rakip yok olunca Victory; oyuncu aracı yok olunca Defeat; Restart ve Main Menu.
- Süreli maç (varsayılan 180 saniye), süre dolunca kalan can/hasar üzerinden sonuç ya da açıkça belgelenmiş basit berabere kuralı.
- HUD: oyuncu canı, rakip canı, maç süresi, boost/cooldown, pause.
- Ekran dokunmatik kontrolü ve ayarlanabilir hassasiyet.
- Oyun duraklatıldığında fizik, AI, silah, sayaç ve ses davranışı doğru şekilde durmalı.

MVP tamamlanmadan çok sayıda araç, 20 rakip, online multiplayer, açık dünya, hikâye modu veya mağaza ekleme.

### Kapı C — İçerik ve polish
MVP doğrulandıktan sonra şu özellikleri sırayla ekle:
1. İkinci silah tipi veya alternatif ateş modu.
2. Boost ve el freni/drift hissi.
3. 2–3 arena varyasyonu (aynı mantık; düşük maliyetli geometri ve malzeme değişimleri).
4. Düşük poli robot seyirci siluetleri/seyirci sesi; önce görsel ve performans maliyetini değerlendir.
5. VFX/SFX, araç hasar geri bildirimi, kamera sarsıntısı için ayar ve erişilebilirlik.
6. Yerel kayıt: ses, grafik ve kontrol ayarları; sürüm güvenli JSON/ConfigFile yapısı.

## 4. OYUN TASARIMI VE KONTROLLER

### Kamera
- Üçüncü şahıs takip kamerası; araçtan bağımsız şekilde ani titreşmemeli.
- Kamera duvarların içine girmesin; basit ray/mesafe kontrolü kullan.
- Oyuncunun görüşünü kapatmayan, sürat ve çarpışmayı hissettiren yumuşak takip.

### Dokunmatik HUD (yatay ekran)
- Sol alt: direksiyon joystick'i veya büyük sol/sağ direksiyon tuşları.
- Alt orta/sol: gaz ve fren/geri tuşları.
- Sağ alt: ateş, boost ve el freni.
- Ekranın sağ yarısında parmak sürükleme ile taret/nişan kontrolü; opsiyonel hedefe yumuşak otomatik nişan desteği.
- Ayarlar: kontrol yerleşimi seçeneği, hassasiyet, kamera hassasiyeti, titreşim aç/kapat, otomatik nişan aç/kapat.
- HUD güvenli alanı, çentik/delik ve 16:9 ile 20:9 ekranları hesaba katmalı.
- Butonlar aynı anda basıldığında (ör. sürüş + ateş + boost) doğru çalışmalı. `InputEventScreenTouch`/`InputEventScreenDrag` veya Godot'un doğru kontrol düğümlerini kullan; aynı input'un birden çok kez işlenmesini engelle.

### Sürüş ve AI
- İlk prototipte aşırı karmaşık simüle lastik modeli yapma. Tahmin edilebilir, mobilde ayarlanabilir arcade sürüş uygula.
- Gaz/fren, direksiyon, hız limiti, sürtünme, drift/slip, geri vites, çarpışma tepkisi ayrı parametreler olsun.
- Kullanılacak gövde/physics yaklaşımını gerekçelendir. Godot `VehicleBody3D` seçersen sınırlamalarını ve mobildeki test yöntemini açıkla; custom arcade kontrol seçersen fizik adımını (`_physics_process`) ve kuvvetleri doğru uygula. İki kontrol sistemini aynı anda kullanma.
- AI durum makinesi: `APPROACH`, `ATTACK`, `EVADE/RECOVER`, `STUNNED` veya aynı netlikte durumlar. Duvara takılıp sonsuza kadar kalmasın; stuck detection ve geri toparlanma ekle.
- AI aynı anda bütün araçlara kusursuz isabet etmesin; tepki süresi, nişan hatası, bekleme aralığı ve zorluk parametreleri olsun.
- Oyuncunun ateş ettiği araç dışında alakasız hedeflere hasar uygulama. Çarpışma katmanları ve collision mask'leri belirgin olsun.

### Silah/hasar
- İlk silah: kısa/orta menzilli mermi veya fiziksel projectile. Hasar, ateş hızı, menzil, hız, cooldown ve görsel etki ayarları tek bir config/Resource üzerinden değiştirilebilsin.
- Sürekli yeni node oluşturarak bellek sızıntısı yaratma; proje büyürse object pooling kullan.
- Hasar tek bir merkezi API'den (`apply_damage` benzeri) işlensin; aynı çarpışmadan tekrar tekrar damage uygulanmasını önle.
- Araç ölüm/yok oluş akışı tek kez tetiklensin ve maç yöneticisine haber versin.
- Patlama ekranı kaplamasın; VFX yoğunluğu grafik ayarına göre azaltılabilsin.

## 5. GÖRSEL YÖN VE VARLIKLAR

- Stil: özgün, endüstriyel sci-fi demolition derby; koyu çelik, turuncu/amber tehlike ışıkları ve sınırlı cyan vurguları. Her şeyi neonla doldurma; oyuncu aracı ve rakip silahları okunaklı olmalı.
- Başlangıçta Blender veya Unity'de hazırlanmış varlıkların zaten bulunduğunu varsayma. Basit ama özenli low-poly 3D gövdeleri Godot MeshInstance3D/primitive mesh ve malzemelerle üret; sonradan değiştirilebilecek şekilde sahne ve materyalleri ayır.
- Araç görsellerinde gövde, tampon, teker, roll-cage ve silah yuvasını ayrı node'lar yap; ilk sürüş için gerçek araç modeline ihtiyaç olmadan okunaklı silüet çıkar.
- Arena: çevre duvarları, giriş kapısı, ışık kuleleri, az sayıda yıkılabilir olmayan engel ve zemin çizgileri. İlk MVP'de yüksek maliyetli yıkım simülasyonu yok.
- Dışarıdan indirilen görsel/ses/font kullanacaksan lisansını açıkça kaydet, kaynak URL ve lisans bilgisini `ASSET_LICENSES.md` içinde belirt. Ücretli/telifli veya Unity tanıtım videosundan alınmış varlık ekleme.
- Varlıklar eksikse oyunu boş bırakma; built-in primitive placeholder ve materyallerle oyun açılır olsun.

## 6. MOBİL PERFORMANS HEDEFLERİ

- Varsayılan hedef: yaygın Android telefonlarda kararlı 30 FPS alt sınırı; orta/üst seviye cihazlarda 60 FPS hedefi. Sonuçların gerçek cihaz ölçümü olmadan garanti edilmediğini yaz.
- Render ölçeği/çözünürlük ayarı, gölge kalitesi, efekt yoğunluğu ve seyirci ayrıntı seviyesi için Low/Medium/High seçenekleri.
- Gerçek zamanlı gölge üreten ışık sayısını sınırlı tut; mümkün olan yerlerde baked/fake lighting kullan.
- Çok sayıda fizik objesi, yüksek poligonlu araç, pahalı ekran uzayı efektleri ve her karede bellek tahsisinden kaçın.
- Partikül sayısını sınırlı tut. Oyuncunun uzağındaki efektleri kıs veya kapat.
- Physics FPS, collision layer/mask ve frame time için varsayılanlar koy ve nedenini belge.
- Uygulama arka plana gidince oyunu durdur; ses ve kayıt davranışını yönet.

## 7. KAYNAK DOSYA MİMARİSİ

Aşağıdaki gibi tutarlı bir yapı üret; gerçekten gerekli olmayan dosyaları sebepsiz ekleme, fakat sahneyi tek büyük script'e sıkıştırma:

- `project.godot`
- `README_TR.md` — telefonda kurulum, açma, test, export.
- `GAME_DESIGN_TR.md` — temel döngü ve kurallar.
- `TEST_PLAN_TR.md` — elle/otomatik testler.
- `ASSET_LICENSES.md` — kullanılan varlıklar ve lisanslar.
- `scenes/main_menu.tscn`
- `scenes/arena/arena_mvp.tscn`
- `scenes/vehicles/player_vehicle.tscn`
- `scenes/vehicles/enemy_vehicle.tscn`
- `scenes/weapons/basic_turret.tscn`
- `scenes/ui/hud.tscn`, `scenes/ui/pause_menu.tscn`, `scenes/ui/result_panel.tscn`
- `scripts/autoload/game_state.gd` veya ihtiyaca göre tek bir `MatchManager` autoload.
- `scripts/vehicles/arcade_vehicle_controller.gd`
- `scripts/vehicles/vehicle_health.gd`
- `scripts/ai/enemy_vehicle_ai.gd`
- `scripts/weapons/projectile.gd`, `scripts/weapons/turret.gd`
- `scripts/match/match_manager.gd`
- `scripts/ui/touch_controls.gd`, `scripts/ui/hud_controller.gd`, `scripts/ui/pause_menu.gd`
- `assets/` altında yalnızca gerçekten kullanılan yerel varlıklar.
- `.github/workflows/android-build.yml` — mümkünse debug APK artifact'i, release signing secrets varsa imzalı AAB; secrets yoksa release imzasını atlayıp debug paketini üret ve bunu açıkça yaz.

Dosya adları farklı olabilir; fakat her sorumluluk net ayrılmalı. Gereksiz autoload tekelleri veya global state kullanma.

## 8. ANDROID EXPORT / BULUT BUILD

- Proje doğrudan Godot 4.7.2 Stable ile açılmalı.
- Android Editor ile açma ve Android'e export adımlarını `README_TR.md` içinde sıralı, telefonda uygulanabilir şekilde yaz.
- Telefonda APK üretilemezse kullanıcıyı çıkmaza sokma: GitHub Actions üzerinden bulut build için eksiksiz workflow, sürüm pinleme, export preset ve artifact indirme adımları hazırla.
- CI workflow Godot editör sürümü ve export template sürümünü aynı tutmalı; Android SDK/JDK gereksinimlerini doğrula. Başarılıymış gibi yazmadan önce workflow mantığını kontrol et.
- Debug APK'yi test amacıyla üret. Play Store için release AAB ve signing gerekir; keystore ve parolaları GitHub Actions Secrets olarak al, kod/repo içine yazma.
- Paket kimliği konfigüre edilebilir olsun: varsayılan örnek `com.lenstedreal.wreckcircuit`, kullanıcı değiştirebilir. İmzalama bilgisi, mağaza metadata'sı veya reklam SDK'sı uydurma.
- `export_presets.cfg` içinde gerçek olmayan secret veya cihazdan cihaza değişen mutlak dosya yolu kullanma.

## 9. KALİTE VE DOĞRULAMA KURALLARI

Her kod üretiminde:
1. Godot 4.7.2 API'sini doğrula; var olmayan property, signal veya method kullanma.
2. Tüm referans verilen sahne/node yolları gerçek olmalı; sahne dosyasını script'ten önce veya aynı teslim içinde üret.
3. Her `.tscn` dosyası `project.godot` ile uyumlu; `load/preload`, resource UID ve input action isimleri var olmalı.
4. Döngüler ve physics mantığı `_physics_process` içinde; görsel animasyonlar gerektiğinde `_process` içinde.
5. `delta` kullan; kare hızından bağımsız hareket uygula.
6. Null node referansları, tekrar tetiklenen signals, pause/resume, restart ve scene change akışlarını kontrol et.
7. Her aşama sonunda gerçek komutları, beklenen çıktıyı ve başarısız olursa nasıl teşhis edileceğini listele.
8. Kodu `...`, TODO placeholder veya “geri kalanını kendin tamamla” şeklinde yarım bırakma. Bir dosyayı üretemiyorsan açıkça belirt ve daha küçük, çalışan aşamaya böl.
9. Build/test çalıştırmadıysan “test edildi” deme. Çalıştırılan komut ve gerçek sonucu ayrı yaz.
10. Güvenlik/kararlılık için rastgele `npm`, pip veya Godot plugin bağımlılığı ekleme. Bu proje web sitesi değil; Next.js/HTML/React oyunun ana motoru olmasın.

## 10. TEST KONTROL LİSTESİ

En az şu senaryoları hazırla ve mümkün olanları otomatikleştir:
- Proje açılıyor, başlangıç menüsü geliyor.
- Play'e basınca arena yükleniyor.
- Oyuncu aracı gaz/fren/sağ-sol girdi alıyor; oyun duraklatılınca hareket duruyor.
- Ekranda aynı anda direksiyon, gaz ve ateş input'u çalışıyor.
- Silah cooldown'u doğru, mermi çarpışınca yalnızca doğru hedef hasar alıyor.
- Oyuncu/AI canı sıfırlanınca sonuç ekranı yalnızca bir defa açılıyor.
- Restart eski AI/projectile ve timer state'ini bırakmıyor.
- Pause/resume sonrası fizik ve timer doğru devam ediyor.
- Uygulama arka plan/ön plana geçişinde ses ve oyun durumu doğru.
- 16:9 ve 20:9 ekran oranlarında HUD butonları ekran dışında kalmıyor.
- Low kalite ayarında partikül/gölge sayısı azaltılıyor.
- Android debug APK kuruluyor ve açılıyor; derleme sonucu gerçekten gözlemlenmediyse bu test “bekliyor” olarak işaretleniyor.

## 11. GROK'UN ÇIKTI FORMATI

Kapı A ilk yanıtında yalnızca planı ve teknik kararları üret. Plan sonuna şu tabloyu koy: `Risk | Etki | Önlem | Test yöntemi`.

Kullanıcı planı onayladıktan sonra Kapı B kaynak projesini üret. Her dosyayı eksiksiz olarak `dosya yolu` ve tam içerik şeklinde sun; imkan varsa tüm projeyi tek ZIP olarak ver. ZIP yanında mutlaka:
- dosya ağacı,
- kurulum/çalıştırma komutları,
- Android APK/AAB üretim yönergesi,
- GitHub'a push yönergesi,
- tamamlanan ve doğrulanmayan testler,
- bilinen kısıtlamalar.

Çıktı kalitesi önceliği:
1. Gerçekten açılan ve oynanabilen MVP,
2. Android input ve kararlı performans,
3. temiz dosya mimarisi ve otomatik build,
4. görsel polish.

Gerçekçi ol. Bir mesajda devasa ama tutarsız proje dökme. Kapılarla ilerle, kendi çıktını dosya referansları ve sahne yolları üzerinden denetle, varsayımını açıkça belirt. İlk yanıtında **kod üretme; önce Kapı A planını sun**.
