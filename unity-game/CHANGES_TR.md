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
