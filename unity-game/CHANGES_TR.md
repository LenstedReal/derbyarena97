# DERBY ARENA — Değişiklikler

## 4.0.2 (402) — Cihaz videosundaki hataların düzeltmesi, yeni araç adları, sıfır ilerleme

Kullanıcının cihazda çektiği 3,5 dakikalık oyun videosu kare kare incelendi; görülen her hata koda kadar izlendi.

| # | Sorun (videoda) | Düzeltme |
|---|---|---|
| 1 | **Açılış animasyonu donuk / menüyle üst üste** | Ağır açılış işleri (arena, shader, araç) kareleri kilitliyordu; animasyon yarı saydam karede donuyordu, sonra kaybolurken menünün üstüne biniyordu. Artık açılış önce tam resmi gösteriyor, ağır işler ancak ondan sonra başlıyor; çıkış siyaha geçerek oluyor (resim → siyah → menü), ikisi asla üst üste görünmüyor. Zamanlama gerçek saate bağlı, ışık süpürmesi (gri dikdörtgen gibi görünüyordu) ve tekrarlanan "DERBY ARENA" yazısı kaldırıldı |
| 2 | **Yazılar birbirine giriyor** | Her yazı artık kutusuna sığacak kadar küçülüyor ve dışına taşmıyor (tüm arayüz). HUD üst düzeni: süre → görev şeridi → ilerleme ve mesafe aynı satırda yan yana → patron çubuğu → mesaj satırları (patron çubuğu varsa aşağı kayar); geri sayım mesajların altında, arabanın üstünde; yükleme ekranı varken geri sayım ve mesajlar gizli |
| 3 | **Bölümler sayfası** | 6. sekme ("YAKINDA · YOL HARİTASI") sağdaki detay panelinin altında kalıyordu: sekmeler ve kartlar artık detay panelinin solundaki alana oranla yerleşiyor, her ekran oranında sığıyor. Arkadaki 3B araba kartların arasından görünmesin diye sayfaya koyu perde |
| 4 | **Ana menü** | Araba sol panelin altına giriyordu: kamera arabayı panelin sağındaki boş alanda gösteriyor; panelin desenli dokusu (telefonda titreşen çizgiler) düz koyu panelle değişti; görev ödül yazıları panel kenarına taşmıyor |
| 5 | **Kontrol / sürüş** | Engebeli zeminde araba zıplıyor, takılıyor ve savruluyordu. Süspansiyon yeniden kuruldu ve 4.0.1 ile yan yana bir yan-kesit fizik simülasyonunda karşılaştırıldı: tekerlek ince ışın yerine küçük bir küre (dikiş, kaldırım, boru ayağı üzerinden yuvarlanır); basamakta yay en fazla lastik tırmanma hızında sıkışır, rampa (~20°'ye kadar) ve iniş anında izlenir; amortisör zeminin değil gövdenin hareketine bakar, rampadaki sürekli yükselmeyi ise 1,5 m'lik yol boyunca süzerek ekler (zamana göre süzgeç yüksek hızda rampayı kaçırıyordu; ikinci bağımsız incelemenin bulgusu). Sonuç (simülasyon): pürüzlü zeminde gövde sarsıntısı 2–3 kat az ve araba neredeyse hiç havaya kalkmıyor (4.0.1'de 20 m/s'de 0,6 sn havadaydı), 30 cm kaldırımda sıçrama ~2,5 kat az, 9°'lik atlama rampaları nitro hızına kadar eskisi gibi (biraz daha iyi) uçuruyor, 11 m/s'lik inişte gövde yere vurmuyor. Tümsek sınırı aracın kendi dikey ekseninde (rampa hızı kesilmez; ölmüş araç ve roket savurması muaf). Alçak engele burnundan çarpınca hafif kaldırma yardımı; havada yatay dengeleme güçlendi; araba çok yavaşken de dönebiliyor (duvardan kurtulmak kolay); yere bastırma kuvveti azaltıldı; parmak/eğim direksiyonunda hafif tepki eğrisi (küçük hareket = ince düzeltme, düz giderken araba titremez; tam kaydırma yine tam dönüş). Akıcılık: Android kare düzenleme (frame pacing) ve artımlı çöp toplayıcı açık |
| 6 | **Kamera** | Duvarın ve arabanın içine giriyor, drift'te arabanın etrafında dönüyordu. Yükseklik tümseklerden arındırılıyor, kamera arabanın arkasında daha sıkı duruyor (kaymada yön takibi kesiliyor), engel görünce hemen yaklaşıp yavaşça açılıyor ve kısaldığında arabanın üstüne yükseliyor; bu yükselme de çarpışma kontrolünden geçiyor (köprü, boru, konteyner altı). Kamera duvarın gerisine hiç konmuyor (ince sınır duvarlarının dışına çıkma ve kalın duvarda ileri-geri pompalama yok); hiçbir kare duvarın içinde bitmiyor |
| 7 | **Ekran efekti** | Nitro / darbe renk kayması kenarlarda kırmızı-mavi bulanıklık yapıyordu: %60 hafifletildi |
| 8 | **Yükleme takılması** | Kanal arenasının yüklemesi "%22"de ~11 sn donuyordu. Shader ısıtma her maçtan önce değil oturumda bir kez; kanal arenasının dokuları ve modelleri oturum boyunca saklanıyor (ikinci ziyaret anında) |
| 9 | **Garaj: önizleme ≠ oyun, alakasız parçalar** | Garajdaki araç ölçülürken altındaki parlayan halka da arabanın parçası sayılıyordu: tampon demirleri, dikenler, borular arabadan metrelerce uzakta çıkıyordu. Ölçüm artık yalnızca araç modelinden ve gövdenin dinlenme konumunda; garaj ve maç birebir aynı. Maçta gövde virajda/frende 5°'ye kadar yatıyor ama parçalar düz kalıyordu (spoyler, farlar gövdeden ayrılıp içine giriyordu): parçalar artık gövdeye bağlı, onunla birlikte yatıyor. Garajda parça değiştirince eski model aynı karede hâlâ sahnedeydi ve parçalar ona takılabiliyordu (yeni araçta görünmüyordu): eski model önce ayrılıyor. Zırh yükseltmesinin otomatik eklediği diken çatalı ve tavandaki taret ile çakışan çapraz borular kaldırıldı; "çatı rafı" (tareti kesiyordu) yerine **spot farlar** |
| 10 | **Jant rengi değişmiyor** | Sahnede yansıma olmadığı için çok metalik jant her renkte aynı koyu griydi: metaliklik düşürüldü, renkler görünür; neon jant hafif parlıyor |
| 11 | **Reklam "yükleniyor"da kalıyor** | Test uygulama kimliğiyle Google rıza servisi "yapılandırma yok" diyordu, oyun da bu yüzden reklam SDK'sını hiç başlatmıyordu. Test modunda SDK her durumda başlıyor; garajdaki ve sonuç ekranındaki ödüllü reklam düğmeleri canlı (reklam gelince kendiliğinden aktif olur); Ayarlar'da test modunda reklam durumu satırı. Test kimlikleri doğru (Google'ın resmî test birimleri) |
| 12 | **Silip yükleyince ilerleme geri geliyordu** | Kök neden: Android'in otomatik yedeği. Kayıt (PlayerPrefs) Google hesabına yedekleniyor, oyun silinip tekrar kurulunca geri yükleniyordu. Android yedeği kapatıldı (`Editor/DerbyAndroidNoBackup.cs`, derlemede `android:allowBackup="false"`): oyunu silmek artık ilerlemeyi de siler; ilerlemeyi korumak ileride isteğe bağlı Google girişiyle gelecek. İstenen sıfır başlangıç: 4.0.2'nin ilk açılışında ilerleme bir kez sıfırlanır (bölümler, yıldızlar, hurda, yükseltmeler, araçlar, parçalar, görevler); ayarlar korunur. Ayarlar'da sıfırlama düğmesi yok |
| 13 | **Araç ve menü adları** | Kızıl Şimşek (Red Bolt), Gölge Tilki (Shadow Fox), Demir Boğa (Iron Bull), Akrep (Scorpion), Kara Mamut (Black Mammoth), Kaplan T-12 (Tiger T-12); menüde "BLACKOUT RUN" yerine "ÇARPIŞ · YIK · KAZAN"; kanal arenası "Paslı Kanal"; patron adları Türkçe/İngilizce |
| 14 | **Politikalar** | Eski oyunun e-posta adresi her yerden kaldırıldı (oyun içi, web sayfaları, mağaza metni). Yeni adres gelene kadar iletişim Google Play mağaza sayfasındaki geliştirici bilgisi üzerinden. Gizlilik, koşullar ve lisanslar baştan yazıldı (veri silme yolu, reklam içerik sınırı, ileride Google girişi notu) |
| 15 | Sonuç ekranı | Puan isteme kutusu panelin altına taşıyordu: panelin içine alındı; "sonraki bölüm ★ gerek" yazısı ödüllü reklam düğmesiyle çakışmıyor; ödüllü reklamdan hemen sonra tam ekran reklam çıkmıyor; "reklam tamamlanmadı" yazısı okunacak kadar ekranda kalıyor |
| 16 | Küçük hatalar (bağımsız inceleme) | Rakip adları büyük harfte Türkçe doğru ("DEMİR BOĞA", "GÖLGE TİLKİ"); ilerleme satırı mesafe yokken ortalanıyor; açılış uygulama arka plandayken geçen süreyi saymıyor ve geç çıkışta menü kamerası bir dakikalık yavaş yaklaşmaya takılmıyor; reklam servisi hiç başlayamazsa düğme "başlıyor…" yerine "şu an kullanılamıyor" diyor; kanal arenası önbelleği telefon belleği azalınca bırakılıyor, indirilemeyen doku önbelleğe yazılmıyor; Ayarlar/Kontroller satırları tablet ekranında sağ sütunun altına girmiyor |

Test: C# Roslyn ile 5 yapılandırmada 0 hata; `validate_derby.py` 0 FAIL; süspansiyon yan-kesit simülasyonu (4.0.1 ile
karşılaştırmalı); iki bağımsız kod incelemesi (bulgular ve düzeltmeler `TEST_REPORT_TR.md`). Video bulgularının hepsi koda kadar
izlendi; **cihazda tekrar test edilmedi** (bu ortamda Unity ve telefon yok).

## 4.0.1 (401) — Kontroller (PUBG Mobile mantığı), sinematik açılış, açılış takılması, ses katmanları, tek `main` dalı

Paket kimliği, logo ve kayıtlar aynı. 4.0.0'ın üzerine; eksik bırakılan tüm 4.0 maddeleri + kontrol isteği.

| # | Yeni | Ayrıntı |
|---|---|---|
| 1 | **Kontroller baştan: kolay ve kişiselleştirilebilir** | Yeni varsayılan **kaydırmalı direksiyon** (ekranın sol yarısına dokun, sağa-sola kaydır; mesafe = açı, kayan joystick görseli), **otomatik gaz açık** (fren durur / geri gider), **sürüş yardımı** (kaymada ek tutuş, hafif karşı direksiyon, yumuşatılmış dönüş; kapatılabilir). Dört direksiyon tipi: kaydırma / simit / oklar / **eğim** (ivmeölçer, kalibrasyon gerekmez). Ayarlar → **Kontroller** sayfası: direksiyon tipi, hassasiyet, otomatik gaz, sürüş yardımı, otomatik nişan, düğme boyutu (%70-140), düğme saydamlığı (%40-100), düzen (Klasik / Sürücü / Sol El), titreşim, **Varsayılana Dön** |
| 2 | **Düzen editörü (PUBG Mobile mantığı)** | Ayarlar → Kontroller → **Düzeni Özelleştir** ve duraklatma menüsünde **Kontrol Düzeni**: her düğme (direksiyon, gaz, fren, ateş, roket, nitro, drift) parmakla sürüklenir, seçili düğme − / + ile boyutlandırılır, tüm set için boyut ve saydamlık, üç hazır düzen, Sıfırla / İptal / Kaydet. Düzen kayıt dosyasında tutulur (`SaveData` v6; eski kayıtlar kayıpsız taşınır, kolay kontrol varsayılanları bir kez uygulanır) |
| 3 | **Sinematik açılış** | Karanlıktan yükselen görsel, yavaş yaklaşma, ışık süpürmesi, kor taneleri, "DERBY ARENA / BLACKOUT RUN" vuruşu; **ilerleme çubuğu gerçek yüklemeye bağlı** (arena → shader ısıtma → araç → arayüz), durum satırı; en az ~3 sn, yükleme bitmeden geçmez (12 sn emniyet), bitince dokunarak atlanır; bitişte menü kamerası orbit'e süzülür |
| 4 | **Açılış takılması** | GLB içe aktarma artık kademeli (`Prepare → PreloadTextures → PreloadMeshes → Finish`, kare başına bir doku/mesh); menü kurulumu coroutine; `Shader.WarmupAllShaders()` açılışta bir kez; ortam/reverb sonradan. Takılma nedenleri ve ölçüm yöntemi teknik raporda |
| 5 | **Ses katmanları** | Motor: rölanti / motor / yük döngüleri devir tahminiyle çapraz geçişli; tank: daha ağır perde + hıza bağlı palet takırtısı; oyuncu tüm katmanlar (öncelik 16), rakipler iki katman (öncelik 160, 3B); tek seferlik seslerde ses yüksekliğine göre öncelik ve menzil; arena başına reverb bölgesi; Harabeler'de rüzgâr, diğerlerinde arena uğultusu |
| 6 | **Silah / animasyon cilası** | Makineli namlusu her atışta geri tepip yaylanır (tankta mevcut top geri tepmesi), namlu alevi her seferinde farklı (boyut, yan kıvılcım, ara sıra duman), araç vuruşlarında koyu moloz parçaları (mermi ve çarpma), roket fırlatmada duman + hafif kamera tepkisi |
| 7 | **Araç LOD** | Beş arabada `Body_LOD1` (≈930 üçgen; gövde silueti, kabin, lastikler, taret): 12 % ekran yüksekliğinin altında devreye girer; kit ölçümü ve gövde sığdırma LOD1'i yok sayar |
| 8 | **Android doku sıkıştırma** | ASTC (`UrpAndPlayerSetup.ApplyBuildSettings`), bellek bütçesi teknik raporda |
| 9 | **PerfLog** | Her maçın ilk 60 sn'si: 10 sn'lik pencerelerde ortalama / p95 / en kötü kare süresi, 33 ms üstü takılma sayısı, GC farkı, toplam bellek → logcat `DerbyPerf` |
| 10 | **Tek dal, yeşil CI** | Tüm kod `main`'de; GitHub Actions'ta APK işi yalnızca elle (lisans secret'ı yok diye push'lar kırmızı olmuyor); Build Automation dal geçişi `BUILD_TR.md`'de |
| 11 | Belgeler | `ASSET_INVENTORY_TR.md`, `KNOWN_ISSUES_TR.md`, `TECHNICAL_REPORT_4.0_TR.md` (değişiklik raporu, takılma analizi, bellek/çizim bütçeleri, FMOD değerlendirmesi, doğrulanan / doğrulanmayan) |

Test: C# Roslyn ile 5 yapılandırmada 0 hata; `validate_derby.py` 344 kontrol 0 FAIL; LOD1 ve araçlar yazılım render'ıyla
kontrol edildi. **Unity'de oynatma ve cihaz testi burada yapılamadı** — ilk cihaz testi kullanıcıda; `KNOWN_ISSUES_TR.md`
hangi davranışların cihazda doğrulanması gerektiğini listeler.

## 4.0.0 (400) — Büyük güncelleme: 100 bölüm, garaj dükkânı, Tiger I T-12, Çöl Harabeleri, yeni arayüz

| # | Yeni | Ayrıntı |
|---|---|---|
| 1 | **100 bölüm, yıldızla açılan ilerleme** | 5 sezon × 20 bölüm (10. bölüm Büyük Derbi, 20. patron). Her sezonun ilk bölümü o ana kadarki yıldızların %50'sini, 6/11/16. bölümler %45'ini ister; kart ve detay panelinde "★ N gerek" yazar, sonuç ekranı da söyler. Eski kayıtlar (10 bölümlük sezonlar) kayıpsız yeni düzene taşınır (`SaveData` v5) |
| 2 | **Garaj dükkânı** | Boya (12), jant (6), neon (8, biri nabızlı), şerit (7), tavan (far çubuğu / raf / siren), kanat (alçak / yüksek / ördek kuyruğu), tampon (bull bar / çelik pulluk / dikenli), egzoz (yan / baca / alevli). Hurdayla alınır, bazıları yıldız ister; araçta canlı önizleme, ikinci dokunuşta satın alma. Yükseltmeler aynı sayfada |
| 3 | **Araç koleksiyonu** | Interceptor (ücretsiz), Razor Drifter, Bastion Rammer, Siege Hunter, War Rig ve son araç **Tiger I "T-12"**: fiyat + yıldız + patron şartları; stat çubukları; seçilen araç maçta ve ana menüde |
| 4 | **Tiger I T-12 (`scripts/tankforge.py`)** | Tarihî oranlarda Panzerkampfwagen VI Tiger I: üç tonlu püskürtme kamuflaj, "T-12" ay-yıldız işareti, yedek palet baklaları, aletler ve kütük demeti, çift egzoz, 8,8 cm KwK 36 (çift kademeli namlu freni), iç içe tekerlekler, dönen paletler, antende dalgalanan Türk bayrağı, LOD1. Top: her dolumda bir mermi, geri tepme, kovan, "TOP HAZIR / DOLDURULUYOR" çubuğu; ROKET tuşu HE mermi atar |
| 5 | **Çöl Harabeleri arenası** | Yıkık çöl kasabası: kum zemin, toprak çevre yolu ve sokaklar, siper olarak dört yıkık ev, kırık çevre duvarı, 14 harabe blok, minare kalıntısı, ölü palmiyeler, kum tepeleri, moloz, kirişler, toz pusu, enkaz. Sezon 2'nin derbileri ve Sezon 5'in tek numaralı derbileri burada |
| 6 | **Günlük görevler** | Her gün 3 görev (imha / kazan / nitro / roket / çarpma / yıldız), hurda ödülü; ana menüde özet, ayrı sayfada ödül alma; gece yarısı yenilenir, çevrim dışı çalışır |
| 7 | **Arayüz baştan** | Poppins yazı tipi (OFL), 9 dilimli panel/düğme/kart grafikleri, 45 ikon (`scripts/uiforge.py`), yeni ana menü (OYNA kartı, görev paneli, çipler), 10×2 bölüm kartları (ikon + yıldız + kilit), sezon yıldız çubuğu, garaj sekmeleri, ayarlar ikonları, HUD ikonları, sonuç ekranında yıldız animasyonu, geri sayım artık mesajların üstüne binmiyor |
| 8 | **Reklam yerleşimi** | Geçiş reklamı yalnızca sonuç ekranından çıkarken (3. bölümden sonra, 3 maçta 1, ≥120 sn ara); aynı maçta ödüllü "ikinci şans" izlendiyse geçiş reklamı yok. Ödüllüler: ikinci şans, hurda ×2, garajda +150 (günde 3). Hâlâ TEST kimlikleri |
| 9 | Ses | Top atışı, dolum, palet takırtısı, motor rölanti/yük katmanları, rüzgâr döngüleri (sentez) |
| 10 | Politika metinleri | Reklam gizlilik yolu düzeltildi, Poppins lisansı ve Tiger I notu eklendi (oyun içi + web) |

Test: C# Roslyn ile 5 yapılandırmada 0 hata (Android/iOS, reklamlı/reklamsız, Editor betikleri dahil); statik doğrulama 312 kontrol; T-12 ve arena geometrisi yazılım render'ıyla kontrol edildi. **Unity'de oynatma ve cihaz testi burada yapılamadı.** Yapılmayanlar: sinematik açılış (mevcut açılış ekranı duruyor), FMOD (Unity sesi korundu), silah/animasyon cilasının geri kalanı.


## 2.1.0 (210) — araç modelleri, grafik motoru, hata düzeltmeleri

Paket kimliği, logo ve kayıtlar aynı. 2.0.0'ın üzerine model, grafik motoru ve hata düzeltme güncellemesi.

| # | Yeni | Ayrıntı |
|---|---|---|
| 1 | **Araç modelleri sıfırdan (`scripts/carforge.py`)** | 1.x'te oyuncu ile patron aynı kutu modeldi, üç rakip de birebir aynıydı (sadece boya farklıydı). Şimdi beş ayrı araç: Breakline Interceptor (uzun kaputlu muscle, hava alığı, çift egzoz, kafes), Razor Drifter (alçak kupe, difüzör), Bastion Rammer (kutu gövdeli station, kar küreği, kaput zırhı), Siege Hunter (sedan, bagajda roket podu, anten), War Rig (kamyon: bacalar, yakıt tankları, zırhlı kasa, boğa demiri, çift namlu). Gerçek tekerlek kemerleri, çamurluk şişkinlikleri, tumblehome'lu kabin ve direkler, ızgara, farlar, stop paneli, aynalar, kapı derzleri, iç mekân, şasi/aks. Boya dokusu (çizik, çentik) + normal haritası, lastik sırtı dokusu, pas dokusu. Araç başına 6-8k üçgen (1.x: 1.4k). Tekerlek konumları ve yarıçapları birebir aynı: fizik değişmedi |
| 2 | **Model yükleyici hatası (1.x'ten beri)** | glTF X ekseninde aynalanıyor ama üçgen sarımı çevrilmiyordu: Unity bütün GLB modelleri ters yüz çiziyordu (yakın yüzler atılıyor, uzak iç yüzler görünüyordu; araçların "düz ve karanlık" durmasının nedeni buydu). Artık dosyadaki normallere göre üçgen üçgen düzeltiliyor; normal haritaları için tanjantlar hesaplanıyor; aynı model+malzeme çiftleri GPU instancing ile tek çizim çağrısında |
| 3 | Savaş kiti modele uyuyor | Kit parçaları artık gövdeyi ölçerek yerleşiyor (tavan, kaput, bagaj yüksekliği, kapı genişliği): spoyler bagajda, kompresör kaputta, ışık barı tavanın ön kenarında, şeritler cam üstünden geçmiyor |
| 4 | **Arena dokuları sıfırdan (`scripts/texforge.py`)** | Kolezyum modelinin içindeki 21 görsel değiştirildi: kum zemin (lastik izleri, çakıl, nemli lekeler), Roma taşı blok duvar, bazalt, çatlak beton, pas akıntılı perçinli çelik, uyarı şeritli bariyer, lastik sırtı; hepsi döşenebilir ve normal haritalı. Kanal arenasının asfalt/beton/çelik dokuları da yenilendi (1.x: düz gri, nokta desenli kum) |
| 5 | **Gökyüzü (`Shaders/DerbySky.shader`)** | Temaya özel gradyan, güneş veya ay diski ve parlaması, güneşe göre aydınlanan hareketli kümülüs bulut katmanı, ufuk pusu, gece yıldızları. Altı ışık havası: altın gün, turuncu pus, zehirli, neon gece, kızıl, liman akşamı. Yansıma probu da bu gökyüzünü kullanır; shader yoksa 1.x prosedürel gökyüzüne düşer |
| 6 | **Tribün robotları (`scripts/crowdforge.py`)** | 1.x'te ayrık kare yüzeylerden oluşan seyirciler yerine üç boyutlu robotlar: 10 takım rengi, parlayan vizör ve göğüs ışığı, anten, pozlar (duran, tezahürat, tek kol, oturan, bayrak sallayan), boş koltuklar; ön sıra tam detay, arka sıralar sade. Aynı 24 grup ve dalga animasyonu, dosya 5,2 MB → 2,2 MB |
| 7 | Işık hüzmeleri ve enkazlar | Kolezyum projektörleri ve kanal lambalarından zemine düşen hafif ışık konileri (gece temalarında daha güçlü). Kolezyumdaki eski kutu enkazlar yerine 2.1 araç modellerinin yanmış enkazları siper olarak duruyor |
| 8 | **Post 2.0** | Yüksek/Ultra'da ACES filmik ton eşleme (yarım güç, koyu zemin okunur kalır), film greni, yakın patlamada beyaz flaş, nitroda renk ayrışması, çarpışma/roket darbesinde kısa sarsıntı. Kırmızı hasar kenarı HUD'da kaldı |
| 9 | **ULTRA kalite** | 1440p, 4x MSAA, çok yüksek gölge, 75 m gölge mesafesi, 6 ışık. Otomatik seçim en fazla Yüksek verir; Ultra elle seçilir, FPS düşerse uyarlanabilir performans devreye girer |
| 10 | Ayar: **EKRAN EFEKTLERİ** | Flaş, sarsıntı, nitro efekti ve greni tek tuşla kapatır |
| 11 | **Draw call azaltma** | Araç savaş kiti malzeme başına tek mesh. Arena dekoru static batching ile birleştiriliyor |
| 12 | **Açılış ekranı** | Siyahtan DERBY ARENA ana görseline geçiş (uygulama logosuyla aynı görsel), turuncu yükleme çizgisi; menü arkada hazırlanır. Yalnızca ilk açılışta, dokununca geçilir |
| 13 | **Sezon 6 hazır ama kapalı** | Konteyner Limanı arenası (`World/DocksArena.cs`), 6. sezon planı, patron ve ışık teması kodda hazır; `Campaign.ReleasedSeasons = 5` olduğu için bölümler ekranı eskisi gibi 5 sezon + "YAKINDA · YOL HARİTASI" sekmesi gösterir. Yayınlamak için sabiti 6 yap |
| 14 | **Hata düzeltmeleri (bağımsız kod incelemesiyle bulundu)** | • Kutu biçimli tüm parçalar (savaş kiti plakaları, siperler, sandıklar, neon şeritler, konteynerler) 1.x'ten beri ters yüz üretiliyordu: dışarıdan bakınca arka yüzleri görünüyor, yanlış aydınlanıyordu (`MeshFactory.Box`). • Kanal arenasının OBJ modelleri X aynalanırken üçgen yönü çevrilmiyordu; tek yüzlü kanal zemini ve asfalt yalnızca alttan görünür durumdaydı (`ObjMesh`). • Açılış ekranını geçmek için yapılan dokunuş arkadaki menü düğmesine de basıyordu (ilk açılışta dil seçimi körlemesine yapılabiliyordu); artık açılış boyunca düğmeler kilitli. • Araç gövdelerinde tekerlek davlumbazı tavanları içe bakıyordu (modeller yeniden üretildi). • Kit yerleşimi anten/egzoz/roket podu yüzünden kemer düzlemini kapı üstüne alıyordu; artık tavan yüksekliğinden ölçülüyor. • Far hüzmeleri modelin kendi far lambalarına hizalanıyor (patron farları 0,3 m yukarıdaydı). • Bulut dokusunda ufak dikiş çizgisi. • Oyuncu "ikinci şans" ekranındayken son rakip/patron kendi çarpışma hasarıyla ölürse maç kazanılamıyordu (süre bitince haksız yenilgi). Artık canlanınca zafer geliyor, ayrıca her karede güvenlik kontrolü var. • Uyarlanabilir performans yavaş telefonda ikinci maçtan sonra gölgeleri geri açıyordu (2.0 hatası). • 8 rakipli Son Kalan bölümlerinde iki araç aynı noktada doğup patlayabiliyordu: dolu noktada yakında boş yer aranıyor. • Reklam açılamazsa ses yanlış geri yükleniyordu; geç gelen ödül sinyali kayboluyordu; reklam SDK'sı hiç cevap vermezse ikinci şans ekranı 999 sn bekliyordu (artık 90 sn). • Post shader: 1440p'de gren kümelenmesi ve çok parlak ışıkta siyah piksel riski giderildi |
| 15 | CI düzeltmesi | GitHub'daki statik doğrulama Pillow olmadan da çalışıyor (2.0.0'da kırmızıydı, Unity derlemesiyle ilgisi yoktu) |

Test: tüm C# kodu Roslyn derleyicisiyle Unity/AdMob API taslaklarına karşı tip denetiminden geçti (reklamlı ve reklamsız, Android ve iOS); reklam kodu Google'ın 11.6.0 kaynağıyla derlendi; statik doğrulama 283 kontrol. Araç modelleri, kolezyum + tribün ve gökyüzü yazılım render'ıyla (numpy) incelendi, kit yerleşimi aynı algoritmayla simüle edildi. Unity'de oynatma ve cihaz testi burada yapılamadı.

---

# DERBY ARENA — v2.0.0 (versionCode 200)

Paket kimliği ve uygulama logosu aynı. Kayıtlar korunur (kayıt sürümü 4'e otomatik yükselir).

| # | Yeni | Ayrıntı |
|---|---|---|
| 1 | **Savaş kiti (araç modelleri)** | Her araca tavan ışık barı (yanan lambalar), yarış şeritleri, ön koruma demiri. Rolüne göre: koçbaşı dikenleri (koçlar, boss, ağır zırh), kapı plakaları, roll-bar, spoyler, motor üstü kompresör, yan egzozlar. Paylaşılan malzeme, gölgesiz → performansa yük yok |
| 2 | **Stadyum dekoru (arazi)** | Kolezyum: kırmızı-beyaz bariyer başlıkları, 4 projektör kulesi, lastik yığınları (çarpılabilir), varil grupları, kurukafa bayrakları. Bent arenası: yollara/borulara/kulelere çakışmayan lastik ve varil öbekleri |
| 3 | Grafik | Bloom artık Orta kalitede de açık; vinyet hafifletildi. Patlayan araçlar malzeme kopyalamadan kararıyor (bellek sızıntısı giderildi) |
| 4 | **Uyarlanabilir performans** | Maçta FPS 2 ölçümde üst üste hedefin %80'inin altına düşerse önce gölgeler, sonra çözünürlük (%85 → %72) iner. Ayarlar'da DİNAMİK ÇÖZÜNÜRLÜK açılıp kapatılabilir |
| 5 | **Reklamlar (TEST kimlikleri)** | Google AdMob 11.6.0 + UMP rıza formu. Ödüllü: ikinci şans, hurdayı 2 katına çıkar, garajda +150 hurda (günde 3). Geçiş: ilk 2 bölüm yok, 3 maçta 1, en az 120 sn ara. Eklenti yoksa oyun reklamsız derlenir. Plan: `docs/REKLAM_PLANI_TR.md` |
| 6 | Yasal sayfalar | Ayarlar'da Gizlilik, Kullanım Koşulları, Lisanslar (çevrimdışı özet + web'de tam metin), Reklam gizlilik seçenekleri (AEA/UK/CH), Puan ver. Web sayfaları `docs/` klasöründe (GitHub Pages) |
| 7 | Puan isteği | 3. galibiyetten sonra bir kez sorar; "Sonra" derse 8 galibiyet sonra tekrar; "Hayır" derse bir daha sormaz |
| 8 | Android geri tuşu | Menü alt sayfaları → ana sayfa; yasal sayfa → ayarlar; sonuç ekranı → menü |
| 9 | Gizlilik | Unity analitiği kapatıldı (`submitAnalytics=false`); targetSdk otomatik (en yüksek kurulu) |

Yapılmadı: Sezon 6 ve yeni arena (2.1.0'da hazırlandı, henüz kapalı).

---

# DERBY ARENA — v1.5.0 (versionCode 150)

| # | Yeni | Ayrıntı |
|---|---|---|
| 1 | **İngilizce dil desteği** | Tüm menü, HUD, görev, ipucu, sonuç ve mesaj metinleri Türkçe + İngilizce. İlk açılışta **DİL SEÇ / CHOOSE LANGUAGE** ekranı (telefon dili önceden seçili). Ana menüde DİL düğmesi ve Ayarlar'da DİL satırı |
| 2 | **Yol haritası bilgi kutusu** | Ana menüde "YENİ SEZON · YOL HARİTASI" rozeti ve Bölümler'de "YAKINDA · YOL HARİTASI" sekmesi. Sezon 6 (yakında), yeni araçlar ve kaplamalar (planlandı). Çevrim içi PvP ve Battle Royale hedefi (oyun ilgi görür ve küresel puanı yükselirse). Yıldız ilerlemesi ve PUAN VER düğmesi (Google Play sayfasını açar) |
| 3 | Oyun bitmiş gibi davranmıyor | Son bölüm bitince "TÜM BÖLÜMLER TAMAM! Yeni sezon bir sonraki güncellemede". Kampanya yeni sezon eklenecek şekilde düzenlendi; kayıtlar korunur |
| 4 | **Kare hızı seçeneği** | 30 / 60 / 90 / 120 FPS (yüksek tazelemeli telefonlar için); fizik 60 Hz'de sabit, enterpolasyonlu |
| 5 | Kayıptan sonra ipucu | Sonuç ekranı, bir sonraki denemede rakiplerin zayıflayacağını söyler |
| 6 | Google Play hazırlığı | `StoreListing/`: TR + EN mağaza metinleri, iki dilli gizlilik politikası, 512 ikon, 1024×500 görsel. AAB adımları `BUILD_TR.md` içinde |

---

# DERBY ARENA — v1.4.0 (versionCode 140)

Paket kimliği aynı: `com.lenstedreal.derbyarena`. 1.3.0'daki kötüleşmeler geri alındı ve kök nedenlerinden düzeltildi.
Referans: Unity AI Open Beta tanıtım videosu (kum zemin her zaman parlak ve okunur, seyrek taş bloklar, turuncu pus, far huzmeleri).

| # | Sorun (1.3.0) | Kök neden | 1.4.0 düzeltmesi |
|---|---|---|---|
| 1 | Uygulama ikonu varsayılan yeşil Android | İkon, build sırasında dokuyu yeniden içe aktarmaya çalışıyordu; Unity buna izin vermez, kod sessizce atlıyordu | İkonlar artık Unity'nin ürettiği Gradle projesine doğrudan yazılıyor (`AndroidIcons/res`, 5 yoğunluk: uyarlanabilir + yuvarlak + eski tip). Görünüm, telefondaki eski DERBY ARENA ikonuyla aynı (yuvarlak, başlık + araç tam görünür) |
| 2 | Oyun çok karanlık, zemin ve engeller aynı siyah | Kontrast lineer uzayda 0,5 merkezli uygulanıyordu: sRGB ~0,25'ten koyu her şey **tam siyaha** kırpılıyordu | Renk düzeltme gamma uzayında; ton eşleme yalnız parlak uçlarda; vinyet hafif |
| 3 | Temalar karanlık | Düşük ışık ve ortam, koyu sis | 5 tema videoya göre yeniden ayarlandı: Altın Gün, Turuncu Pus, Toksik, Neon Gece (mor pus), Kızıl |
| 4 | Araçlar kara siluet | Sahnede yansıma yok, metal boya siyah yansıtıyordu | Gökyüzü yansıma probu (bir kez çizilir, ucuz) |
| 5 | Parlaklık ayarı yok | — | **PARLAKLIK** %80–140: Ayarlar ve Duraklat menüsünde, anında uygulanır, tüm kalite seviyelerinde çalışır |
| 6 | Yükleme ekranında görsel kesik, sayaç sırasında üstte kalıyor | Görsel ekranı kaplayacak şekilde büyütülmüştü; yavaş solma | Görsel solda bütün olarak (kesilmeden), bilgiler sağda, gerçek ilerleme çubuğu; sayaç başlamadan tamamen kayboluyor |
| 7 | "BÖLÜM 2 · BÖLÜM 1" tekrarı | Bölüm ve sezon aynı adla yazılıyordu | Bölüm = seviye, Sezon = 10'lu grup ("BÖLÜM 2 · SEZON 1 · ...") |
| 8 | Kolezyum tamamen boş | 1.3.0'da tüm engeller kaldırılmıştı | Seyrek engel: videodaki gibi 3 alçak taş blok duvarı + 2 küçük şeritli rampa (kutu çarpıştırıcı, görünür renkler) |
| 9 | Spillway'de görünmez engel | 1.3.0 boru dibine görünmez blok koymuştu | Kaldırıldı |
| 10 | Bölüm 10 / hayatta kal geçilemiyor | Hasar eğrisi dik, patron güçlü | Rakip hasarı %25–35 düşük, isabet daha düşük, aynı anda en fazla 2 ateş eden (sezon 4'ten itibaren 3). Patron 460 → 380 can, silah 3,4 → 2,6, roket 20 → 16, süre 300 sn. Hayatta Kal 60–100 sn, en fazla 2–3 rakip, yeniden doğma 5 sn, hasar ×0,75 |
| 11 | Aynı bölümde takılınca çıkış yok | — | Otomatik yardım: her kayıpta rakip hasarı −%15, canı −%10, +15 sn (en fazla 3 kademe; kazanınca sıfırlanır) |
| 12 | Araç "etkili" görünmüyor | Çarpışma sadece can düşürüyordu | Bindirince rakip savrulur ve döner; far huzmeleri (gece güçlü, gündüz hafif) |
| 13 | FPS | 1440p ekranlarda gereksiz piksel; zorunlu piksel ışıkları | Çözünürlük sınırı (Yüksek 1080p, Orta 900p, Düşük 720p); zorunlu piksel ışıkları kaldırıldı; mangal ışığı 4 → 2; elit modeller önceden yükleniyor; shader'lar yükleme ekranında ısıtılıyor |

## Google Play
`StoreListing/`: 512×512 uygulama ikonu ve 1024×500 tanıtım görseli. AAB adımları `BUILD_TR.md` içinde.

---

# DERBY ARENA / BLACKOUT RUN — v1.3.0 (versionCode 130)

Paket kimliği aynı: `com.lenstedreal.derbyarena`. Unity 6000.3.27f1, Built-in RP. 1.2.0 üzerine eklendi; yeniden yazım yok.

## Videolardan tespit edilip düzeltilenler
| # | Görülen | Düzeltme |
|---|---|---|
| 1 | Yıkım arenasında (Kolezyum) seyirci yok | Kotlin `crowd.glb`: 24 robot seyirci grubu, köşe renkli shader, dalga animasyonu, imha ve sert çarpmada tezahürat sesi |
| 2 | Arena içi engebeli, araç takılıyor | Kolezyum içindeki moloz, beton, bariyer, lastik ve direk yığını ("Props") ile mesh çarpıştırıcısı kaldırıldı. Zemin düz. 6 hurda araçtan 4'ü duvar dibine (çaprazlara) siper olarak taşındı, 2'si gizlendi |
| 3 | Spillway borularının içinden geçiliyor | 6 eğimli boruya yönlü kutu çarpıştırıcı ve rampa olmasın diye alt blok eklendi |
| 4 | Bölüm 10 patronu çok zor | Can 744 → 460 (bölüm 1), eskortsuz. Roket aralığı 3,5 → 5,5 sn. Şok dalgası %50 yerine %40 canın altında, uyarısı 1,2 → 1,5 sn, hasarı 26 → 18 × zorluk |
| 5 | Can ~10 sn'de 218 → 21 düşüyor | Saldırı jetonu: aynı anda en fazla 2 düşman ateş eder (bölüm 3'te 3, bölüm 5'te 4). Ateş ritmi 0,9–1,5 sn açık / 1,3–2,4 sn kapalı. Düşman silah hasarı ve çarpma hasarı yeniden dengelendi |
| 6 | Çarpışmalar çok can götürüyor | Taban `(hız−6)×2,4`, 5–34 aralığı (eski 3,2 ve 6–48). Oyuncu hedefse ×0,75, oyuncu çarparsa ×1,35. Kendine hasar %20–25 |
| 7 | Araçlar kara siluet | Araç tipine göre boya rengi, gövde yansıma ışığı, emisyon artışı. Yalnız araçları aydınlatan dolgu ışığı (katman 9) |
| 8 | Kırmızı hasar vinyeti ekranı kapatıyor | En fazla 0,45 (eski 0,75). Düşük can nabzı 0,12–0,2 |
| 9 | Siyah duman görüşü kapatıyor | Daha açık, küçük ve kısa ömürlü duman |
| 10 | Hep aynı iki görev (yıkım / hücre) | 3 yeni görev: **Kontrol Noktası** (bölüm 4), **Hayatta Kal** (13), **Son Kalan** (daralan bölge, 15) |
| 11 | Bölüm ilerledikçe yenilik yok | Her bölümün kendi özelliği var: asit havuzları (3), gece ve farlar (4), elit mor rakipler (4–5) |
| 12 | Uygulama ikonu Unity varsayılanı | Derby Arena görseli: Android uyarlanabilir (ön plan maske güvenli alanında, taşmaz), yuvarlak, eski tip ve iOS |
| 13 | Yükleme ekranı boş | Anahtar görsel, görev türü rozeti, ipucu |
| 14 | Son düşmanı bulmak zor | Ekran dışı hedef oku. Son 2 rakipte en yakın düşmanı gösterir |
| 15 | Düşman canı görünmüyor | Düşman üstünde can çubukları (8'lik havuz), elitler mor |
| 16 | Vuruş hissi zayıf | Araca isabet flaşı (MaterialPropertyBlock, materyal kopyası yok) ve patlama şok halkası |
| 17 | Görsel kalite | Bloom, ACES ton eşleme, tema renk düzeltmesi, vinyet (OnRenderImage; Orta ve Yüksek kalitede) |
| 18 | Gölge yok, araç havada gibi | Araç altında yumuşak blob gölge, drift ve el freninde lastik izi |
| 19 | Hız hissi | Nitro egzoz alevi, gövde yalpası (±5°) ve yunuslama (±3°), havada süzülen korlar |
| 20 | Kolezyum cansız | Bariyer üstünde 8 titreşen mangal, 4'ünde ışık var |
| 21 | Spillway düz ve gri | Duvar boyunca neon şeritler |
| 22 | Maç başı ani | Geri sayımda kamera önden arkaya süzülür |
| 23 | Düşmanlar üst üste yığılıyor | Ayrışma kuvveti, bölge farkındalığı |
| 24 | HUD her karede metin yeniliyor | Metin yalnız değişince atanıyor (Canvas yeniden kurulmuyor). Bileşen önbellekleri (motor sesi, nişangâh) |
| 25 | Aynı mesaj üst üste | 1,5 sn içinde tekrar eden mesaj bastırılıyor |
| 26 | Kalite seçimi yok gibi | İlk açılışta cihaz belleği, shader seviyesi ve HDR desteğine göre otomatik kalite. Kullanıcı değiştirirse o kalır |
| 27 | Hızlı bitiş yıldızı her görevde anlamsız | 2. yıldız görev türüne göre: Hayatta Kal'da imha hedefi, diğerlerinde par süresi |
| 28 | Bölüm kartları hep "REAKTÖR/YIKIM" | 6 tür renkli etiket, bölüm özellik satırı, detayda 2. yıldız koşulu |
| 29 | Süspansiyon sekmesi | Sönüm 0,6 → 0,75, gövde çarpıştırıcısı yerden biraz yükseltildi |
| 30 | Roket hasarı sabit 42 | Araca özel (`RocketDamage`): oyuncu 48, avcı 18, patron 20 |

## Kampanya planı (50 bölüm)
`R` reaktör, `D` yıkım, `C` kontrol noktası, `S` hayatta kal, `L` son kalan, `B` patron.
1: `RDRCDRCDRB` · 2: `RDSCLRDSCB` · 3: `SLRCDLRSCB` · 4: `LRCSDLCRSB` · 5: `RLSCDLRSCB`.
Patron canı bölüme göre 460 / 620 / 780 / 940 / 1120.

## Yeni dosyalar
`World/Crowd.cs`, `World/PostFx.cs`, `Shaders/DerbyPost.shader`, `Shaders/DerbyVertexLit.shader`, `Icons/*.png`, `Resources/Derby/keyart.jpg`, `StreamingAssets/Models/arena/crowd.glb`.
Dosya silinmedi.

---

# DERBY ARENA / BLACKOUT RUN — v1.2.0 (versionCode 120)

Paket kimliği korundu: `com.lenstedreal.derbyarena` (Android + iOS). Unity 6000.3.27f1, Built-in render pipeline.

## Videodan tespit edilen hatalar ve kök nedenleri
| Görülen | Kök neden | Düzeltme |
|---|---|---|
| Ekranda `Can't add component because class 'CapsuleCollider' doesn't exist!` | Engine code stripping: kod `CreatePrimitive(Cylinder/Capsule)` ile CapsuleCollider istiyor ama sınıf derlemeden atılmış | Hiçbir yerde primitive yok (prosedürel mesh), `stripEngineCode=false`, `link.xml` |
| Araçlar simsiyah siluet, dünya karanlık | Düz ambient 0.12, gökyüzü yok, metalik %100 malzeme yansıma ortamı olmadan siyah | Trilight ortam ışığı, prosedürel gökyüzü, ana+dolgu ışık, metallik sınırlandı, 5 tema |
| Sis telefonda yok | Sis koddan açılıyor, otomatik fog stripping varyantları siliyor | `KeepFogVariants()` |
| Kaba sürüş, sekme, takla | WheelCollider + yüksek sürtünme, el freni sonrası tutuş geri gelmiyordu | Raycast süspansiyonlu arcade araç, roll/pitch sönümü, otomatik doğrulma |
| Kocaman düz renkli kutu butonlar, demo hissi | Kodla basit Image kutuları | Yuvarlak, yarı saydam, basılma geri bildirimli kontroller; **direksiyon simidi**; radar; vinyet |
| Teşhis yazıları sürekli ekranda | Panel her açılışta 25 sn | Sadece hata olursa, 10 sn, ekranın altında |
| Tek görev, menü yok | — | Ana menü, 50 bölüm, garaj, ayarlar, sonuç/yıldız ekranı |

## Yeni sistemler
* **Sürüş** `ArcadeCar`: 4 ışın süspansiyon, yanal tutuş, hedef dönüş hızı, drift (el freni bırakılınca tutuş geri gelir), nitro, hız sınırı, takla kurtarma, arena dışına düşünce güvenli noktaya dönüş.
* **Hasar** `VehicleHealth`: tek merkez, kalkan, doğma koruması, çarpma hasarı (hızlı olan az hasar alır, Rammer çarpanı), tek seferlik ölüm, yanık enkaz + duman/ateş, enkaz 9 sn sonra batar.
* **Silah** `WeaponRig`: ısınan makineli (mermi derdi yok), otomatik nişan konisi + yapışkan kilit, güdümlü roket, roket patlaması araç başına **bir kez** hasar.
* **Yapay zekâ** `EnemyDriver`: Flanker yan çizer, Rammer önünü keser, Hunter mesafe tutar, Patron 3 faz + şok dalgası (uyarı halkası). Bıyık ışınlarıyla engelden kaçış, takılınca geri vites, görüş hattı kontrolü, ateş/ara ritmi, bölüme göre isabet.
* **Görevler** `MatchDirector`: Reaktör (3-5 hücre + tahliye), Yıkım (hepsini yok et), Patron. Geri sayım, süre, yıldız (kazan / par süresi / %50 can), ödül, kilit açma, kayıt.
* **Arenalar** `ArenaLoader`: Blackout Spillway (OBJ + PBR doku + reaktör kuleleri + ışık direkleri), Kolezyum (Kotlin GLB: tribün, sütun, hurda araçlar, mesh çarpışma).
* **Varlık** `GlbLibrary` + `MiniGlb`: GLB bir kez içe aktarılır, şablondan klonlanır (5 düşman = 1 içe aktarma), normal map desteği.
* **Efekt** `Fx`: havuzlu parçacık (kıvılcım, ateş, duman, toz, kırıntı), iz çizgileri, patlama ışığı; maç sırasında nesne üretimi yok.
* **Ses**: motor (hıza göre vites hissi), atış, metal çarpma, patlama, roket, ganimet, geri sayım, ortam; hepsi sentez (lisans derdi yok).
* **Arayüz**: direksiyon simidi veya ok tuşları, otomatik gaz seçeneği, hassasiyet, kalite (düşük/orta/yüksek), titreşim; güvenli alan (çentik).
* **iOS**: aynı kimlik, iOS 13+, build number 120 (imza Unity Build Automation'da Apple hesabınla).

## Kaldırılanlar
`WheelDrive`, `MissionDirector`, `MissionRules`, `SpillwayBuilder`, `SpillwayAssets`, `GltfVehicleVisual` (yerlerine yukarıdakiler).
