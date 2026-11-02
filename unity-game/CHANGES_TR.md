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
