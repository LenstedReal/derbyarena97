# DERBY ARENA 4.0 / 4.0.1 — Teknik Rapor

Sürüm: 4.0.1 (versionCode 401) · Unity 6000.3.27f1 · Built-in RP · paket `com.lenstedreal.derbyarena` (değişmedi)
Kapsam: 4.0 brief'inin tüm maddeleri (denetim, sinematik açılış, yıldız ilerlemesi, ekonomi/araç koleksiyonu, Tiger I
T-12, Çöl Harabeleri, ses, silah/animasyon, ekranlar, performans, kayıt taşıma, teslim belgeleri) + 4.0.1'de eklenen
**PUBG Mobile mantığında kişiselleştirilebilir kontroller**.

> Dürüstlük notu: Bu ortamda Unity Editor ve Android cihaz yok. "Doğrulandı" denen her şey Roslyn derlemesi, statik
> doğrulama (`validate_derby.py`) ve yazılım render'ı ile yapıldı. Oynanış, kare hızı ve bellek ölçümleri **kullanıcının
> cihaz testinden** gelecek; §8 neyin ölçülüp neyin ölçülmediğini ayırır.

## 1. Değişiklik raporu (4.0.0 → 4.0.1)

| Alan | Dosyalar | Özet |
|---|---|---|
| Kontrol modeli | `Core/ControlLayout.cs` (yeni), `Core/SaveData.cs` | 7 taşınabilir kontrol (direksiyon, gaz, fren, ateş, roket, nitro, drift), normalize konum + ölçek, 3 hazır düzen + özel; `SaveData` v6 (`ControlPos`, `ControlPreset`, `ControlScale`, `ControlAlpha`, `SteerAssist`), `MigrateControls` |
| Dokunmatik giriş | `UI/TouchControls.cs` | 4 direksiyon tipi: simit, oklar, **kaydırma (kayan joystick)**, **eğim** (`Accelerometer.current`, `Input.acceleration` yedeği); direksiyonu tutan parmak düğmelerin üstünden geçse de direksiyonu bırakmaz; `Suspended` (editör açıkken) |
| HUD | `UI/ControlWidgets.cs` (yeni), `UI/HudController.cs` | Kontroller düzenden kurulur (`ControlWidgets.Build/Layout`), `ApplyLayout()` ile yeniden; kayan pad parmağı izler; duraklatma menüsünde direksiyon tipi + **Kontrol Düzeni** |
| Düzen editörü | `UI/ControlLayoutEditor.cs` (yeni) | Tam ekran, sürükle-bırak, seçili düğme − / +, tüm set boyut/saydamlık, hazır düzen çipleri, Sıfırla / İptal (anlık görüntüye dön) / Kaydet; menü ve duraklatma üstünde (unscaled time) |
| Ayarlar | `UI/MenuController.cs` | Yeni **Kontroller** sayfası (10 satır + Düzeni Özelleştir + ipucu kartı + Varsayılana Dön); Ayarlar'da tek satır |
| Sürüş yardımı | `Vehicles/ArcadeCar.cs` | Oyuncu + `SteerAssist`: kaymada tutuş ×1.35'e kadar, karşı direksiyon (`slide × TurnRate × 0.45`), dönüş tepkisi 7 → 5.5 |
| Sinematik açılış | `UI/IntroSplash.cs`, `Core/DerbyBootstrap.cs` (`Boot`), `CameraRig/ChaseCamera.cs` (`Dolly`) | Zoom + ışık süpürmesi + korlar + başlık vuruşu; ilerleme = `Boot.Progress`; min 3.2 sn, maks 12 sn; `UiButton.Blocked` kilidi; bitişte kamera dolly |
| Açılış takılması | `Vehicles/MiniGlb.cs`, `World/GlbLibrary.cs`, `Core/DerbyBootstrap.cs` | Kademeli GLB içe aktarma; coroutine menü kurulumu; `Shader.WarmupAllShaders()`; ortam sesi/reverb sona; `PerfLog` |
| Ses | `Audio/SynthAudio.cs`, `Vehicles/EngineAudio.cs` | 3 motor katmanı + palet; öncelik/menzil; `AudioReverbZone`; `Wind`, `Ambience` |
| Silah/animasyon | `Combat/Fx.cs`, `Combat/WeaponRig.cs`, `Vehicles/VehicleVisual.cs`, `Vehicles/VehicleHealth.cs` | Namlu geri tepmesi (`Kick`), namlu alevi çeşitliliği, `Debris`, roket fırlatma dumanı + kamera |
| LOD | `scripts/carforge.py`, `Vehicles/VehicleKit.cs`, `Vehicles/VehicleVisual.cs` | `Body_LOD1` (≈930 üçgen) beş arabada; ölçüm/sığdırma dışı |
| Build | `Editor/UrpAndPlayerSetup.cs`, `.github/workflows/unity-android.yml`, `BUILD_TR.md` | ASTC; APK işi yalnızca elle; tek `main` dalı; 4.0.1 / 401 |
| Belgeler | `ASSET_INVENTORY_TR.md`, `KNOWN_ISSUES_TR.md`, bu rapor, `CHANGES_TR.md` | — |

4.0.0'da gelenler (100 bölüm, yıldız kapıları, garaj dükkânı, araç koleksiyonu, Tiger I T-12, Çöl Harabeleri, günlük
görevler, yeni arayüz, reklam yerleşimi, politika metinleri) `CHANGES_TR.md`'de tablo hâlinde.

## 2. Kontroller — tasarım kararları
- **Neden kaydırma varsayılan?** Simit döndürme jesti küçük ekranda hassas ve yorucu; oklar kademesiz değil. Kaydırma
  (PUBG Mobile'ın sol joystick mantığı) parmağın koyulduğu yerde başlar, mesafe oranında direksiyon verir, parmak
  kalkınca 6/sn hızla merkezlenir. Tam kilit mesafesi `ekran yüksekliği × 0.13 / hassasiyet` (1080p'de ~140 px);
  ölü bölge 0.6 %. Parmak tam kilidi geçerse başlangıç noktası parmağı izler (geri dönüş anında).
- **Eğim**: telefonu direksiyon gibi çevirmek (ekran normali etrafında rulo) yerçekimini ekranın yatay eksenine
  taşır; telefonu kendine doğru eğmek (pitch) bu ekseni etkilemez, kalibrasyon gerekmez. Input System sensör
  değerlerini varsayılan olarak ekran yönüne göre telafi eder (`compensateForScreenOrientation`) → doğrudan `x`;
  telafi kapalıysa veya eski `Input.acceleration` yolundaysa cihaz `y` ekseni (`LandscapeRight`'ta işaret ters).
  Tam kilit ≈ 20° (sin = 0.34 / hassasiyet), ölü bölge %6, 10/sn yumuşatma.
- **Otomatik gaz açık**: yeni oyuncu yalnızca direksiyon + fren + ateşle ilgilenir; fren basılıyken gaz kesilir, duran
  araç geri gider. Gaz pedalı HUD'da gizlenir, editörde soluk görünür (yer ayırmak için).
- **Sürüş yardımı** yalnızca oyuncuya: yapay zekâ aynı `ArcadeCar`'ı kullanır ama `Role == Player` koşulu var.
- **Düzen verisi**: güvenli alan içinde normalize (0..1) merkez + ölçek (0.6..1.6); boyutlar referans 1920×1080'de
  piksel; `CanvasScaler` yükseklik eşlemeli olduğundan her oranda aynı görünür. Editör, düğmeyi üst (96 px) ve alt
  (112 px) çubukların arasında ve kenarların içinde tutar.

## 3. Açılış takılması — analiz ve çözüm
Koddan belirlenen senkron maliyetler (4.0.0 ve öncesi):
1. **Arena kurulumu** tek karede: Kolezyum GLB (8.6 MB, 26 doku) `LoadImage` ile çözülüyordu → tek karede ~45 MB
   RGBA32 doku oluşturma + mesh yükleme (ölçülen değil, hesaplanan; bkz. §4).
2. **Shader derleme** ilk kullanımda: Standard varyantları + sky + post shader'ları ilk çizimde derleniyor, ilk
   karede 100 ms'lik takılmalar olarak görülür.
3. **Araç GLB'si + kit**: menüde sergilenen araç (4 doku) ve savaş kiti ölçümü (vertex taraması) aynı karede.
4. **UI kurulumu**: 200+ `GameObject` aynı karede (Poppins dinamik font atlas ilk kez oluşturulur).

4.0.1 çözümü:
- `MiniGlb` dört aşamaya bölündü; `GlbLibrary.Load` her doku ve her mesh için bir kare bekler (`yield return null`).
  Kolezyum 26 doku + 37 mesh → ~63 kareye yayılır (60 fps'de ~1 sn), tek 400-800 ms takılma yerine.
- `DerbyBootstrap.BootMenu` coroutine: arena → `Shader.WarmupAllShaders()` (bir kez, süre `DerbyStartupDiagnostics`
  ile loglanır) → araç → UI → ortam sesi; her adım `Boot.Set(progress, status)` ile sinematik açılışa raporlanır,
  açılış ekranı bu sırada girişi kilitler, yani takılmalar görsel olarak gizlenir **ve** kısalır.
- Maçta: arena ve araçlar zaten `GlbLibrary` şablon önbelleğinde (menüde yüklendi) → maç geçişi daha hafif.
- Ölçüm: `PerfLog` ilk 60 sn'yi 10 sn'lik pencerelerde loglar (`adb logcat -s Unity | grep DerbyPerf`): ortalama, p95,
  en kötü kare, 33 ms üstü takılma sayısı, GC farkı, toplam bellek. Beklenti (hesap): menü kurulumunda en kötü kare
  < 120 ms (shader ısıtma), maçın ilk penceresinde takılma ≤ 3, sonrasında 0-1.

## 4. Bütçeler (hesapla; cihazda doğrulanacak)
**Üçgen** (tipik derbi, 8 araç): arabalar 8-10k × 8 = ~75k yakın / LOD1 ile uzaktakiler ~1k; Kolezyum 27.7k;
kalabalık 81k (vertex-lit, tek malzeme); kit ve efektler ~10k → **~200k üçgen/kare** üst sınır (Adreno 6xx/Mali-G7x
için rahat; düşük kalite kalabalığı azaltır).
**Çizim çağrısı**: araç başına 5-8 (instancing ile aynı model paylaşılır), arena 37 + statik batching, kit birleşik
(4-6/araç), UI 1-3 canvas → hedef < 150.
**Doku belleği** (RGBA32 + mip, çalışma zamanı çözme): Kolezyum 44.6 MB, Tiger 11.7 MB, araba GLB'si 9.8-12.6 MB
(şablon başına bir kez), loot 4.2 MB; bir maçta en kötü durum (6 araç GLB'si + Kolezyum) ≈ 115 MB. Resources
dokuları ASTC (4.0.1): Harabeler PBR seti 4×(albedo+normal) ≈ 10 MB (RGBA32 olsaydı ~43 MB), bulutlar < 1 MB, UI
sprite'ları sıkıştırmasız ~1 MB. **Toplam GPU doku hedefi < 160 MB**, 3 GB RAM'li cihazda güvenli.
İyileştirme adayı (yapılmadı, bilinçli): GLB dokularını ASTC'ye derleme zamanında çevirmek — MiniGlb'nin
`LoadImage` yolu yerine `Resources`/AssetBundle gerektirir; 4.0.1 kapsamı dışında bırakıldı, bellek bütçesi
mevcut hâliyle sınırın içinde.
**Ses**: 8 araç × (oyuncu 3-4, rakip 2) ≈ 18 döngü + tek seferlikler; Unity 32 gerçek ses; öncelikler (oyuncu 16,
yüksek sesler 32, rakip motor 160, sessizler 200) patlama/top seslerinin kesilmemesini sağlar.

## 5. Kayıt taşıma (SaveData)
- v4 → v5: 10 bölümlük sezon → 20 (yıldızlar/başarısızlıklar yeniden eşlenir, `MigrateLegacyLevels`).
- v5 → v6 (4.0.1): kontrol alanları eklenir; kolay varsayılanlar **bir kez** uygulanır (`ControlMode = Swipe`,
  `AutoGas = true`, `SteerAssist = true`, Klasik düzen); diğer ayarlar ve ilerleme dokunulmaz. `Sanitize` tüm aralıkları
  sınırlar; `ControlPos` bozuksa hazır düzen üretilir.

## 6. FMOD değerlendirmesi
Brief FMOD'un değerlendirilmesini istedi. Sonuç: **bu sürümde Unity ses sistemi + sentez katmanları** (karar
gerekçesi):
1. FMOD Unity eklentisi (fmod.com, kayıt gerektirir) bu ortamda indirilemiyor; Build Automation'a "bilinmeyen" ikili
   eklemek derlemeyi riske atar (kullanıcı hatasız derleme istedi).
2. Lisans: gelir eşiğinin altında ücretsiz ama atıf ve FMOD Studio proje dosyaları gerekir; depoda hiçbir ses dosyası yok,
   her şey sentez.
3. İhtiyaç listesi (katmanlı motor, mesafe zayıflaması, reverb, öncelik) Unity `AudioSource` + `AudioReverbZone` +
   kod tarafı çapraz geçişle karşılandı (§1 Ses).

Gelecekte geçiş için olay adı tablosu (kod → FMOD event):
| Kod | FMOD event | Parametreler |
|---|---|---|
| `EngineAudio` (idle/engine/load) | `event:/Vehicle/Engine` | `rpm` 0-1, `throttle` 0-1, `tank` 0/1 |
| `tracks` | `event:/Vehicle/Tracks` | `speed` |
| `SynthAudio.Cannon/Reload` | `event:/Weapon/Cannon`, `event:/Weapon/Reload` | — |
| `Shot` | `event:/Weapon/MG` | `distance` (otomatik) |
| `RocketLaunch`, `Explosion` | `event:/Weapon/Rocket`, `event:/World/Explosion` | `size` |
| `Hit`, `Crash` | `event:/Vehicle/Hit`, `event:/Vehicle/Crash` | `impact` |
| `Ambience`, `Wind` | `event:/Ambience/Arena`, `event:/Ambience/Wind` | `arena` |
| `Cheer`, `Alarm`, UI tonları | `event:/Crowd/Cheer`, `event:/UI/*` | — |
| `AudioHub.Reverb` | snapshot `snapshot:/Arena|Ruins|Docks` | — |

## 7. Silah / animasyon kontrol listesi
| Madde | Durum |
|---|---|
| Top: mermi, namlu alevi, geri tepme, kovan, gövde itmesi, dolum çubuğu + sesi | 4.0.0 |
| Makineli: namlu geri tepmesi (yay), her atışta farklı alev | 4.0.1 |
| Araç vuruşu: kıvılcım + moloz; çarpma: kıvılcım + moloz + kamera | 4.0.1 |
| Roket: fırlatma dumanı + kamera tepkisi; patlama: ateş, duman, halka, kıvılcım, flaş, post-fx | 4.0.1 / mevcut |
| Tank: palet kaydırma, tekerlek dönüşü, namlu geri tepmesi, bayrak dalgası, sergide rölanti animasyonu | 4.0.0 |
| Araç: gövde yalpa/pitch, patinaj izleri, nitro alevi, hasar flaşı, enkaz görünümü | mevcut |

## 8. Doğrulama — yapılan / yapılmayan
**Yapılan (bu ortamda):**
- Roslyn semantik derleme, 5 yapılandırma: `UNITY_ANDROID,DERBY_ADS,UNITY_EDITOR`, `UNITY_IOS,DERBY_ADS,UNITY_EDITOR`
  (Editor betikleri dahil), `UNITY_ANDROID,DERBY_ADS`, `UNITY_IOS,DERBY_ADS`, `UNITY_ANDROID` → **0 hata**.
- `python3 -I validate_derby.py`: **344 kontrol, 0 FAIL** (sürüm/kod, paket kimliği, GLB düğüm sözleşmeleri ve LOD1,
  kayıt sürümü, kontrol sistemi dosyaları, CI/BUILD belgeleri).
- Araç GLB'leri ve LOD1 yazılım render'ı (`tools/glbview.py`, `GLBVIEW_LOD=1`): silüet, tekerlek ve taret konumları.
- Üretilen varlıkların boyut ve sayıları (`ASSET_INVENTORY_TR.md`).

**Yapılmayan (yapılamaz):** Unity Editor derlemesi/Play Mode, APK/AAB üretimi, Android cihazda kare hızı, bellek,
dokunmatik/ivmeölçer davranışı, ses dengesi, reklam SDK davranışı. Bunlar için `KNOWN_ISSUES_TR.md` §A listesi ve
`PerfLog` çıktısı kullanılacak; sonuçlar gelince düzeltme turu yapılır.

## 9. Teslim
- Kod: `main` dalı (tek dal). Build Automation hedefi `main`'e çevrilir (`BUILD_TR.md`), ardından isteğe bağlı olarak
  `unity/blackout-run` silinir.
- Sürüm 4.0.1 / 401; paket kimliği, logo, kayıt anahtarı (`derby.save.v3`) değişmedi.
- Gizli bilgi yok: reklam kimlikleri test, keystore/şifre yok.

## 10. 4.0.2 — Cihaz videosundan kök neden analizi
| Belirti | Kök neden | Düzeltme (dosya) |
|---|---|---|
| Açılış görseli yarı saydam donuyor, sonra menüyle üst üste | Açılışın ilk 0,5 sn'si, ağır ve kare kilitleyen işlerle (arena kurulumu, `Shader.WarmupAllShaders`) aynı anda koşuyordu; animasyon `unscaledDeltaTime` (en fazla 0,1 sn/kare) ile ilerlediği için uzun karelerde yerinde sayıyordu. Çıkış, menünün üstünde yapılan bir çapraz geçişti | Önce tam resim (`ReadyForWork`), sonra ağır işler; gerçek saat; çıkış siyah üzerinden (`UI/IntroSplash.cs`, `Core/DerbyBootstrap.cs`) |
| Yazılar birbirine giriyor | 4.0 Poppins'e geçti (Arial'den geniş harfler, yüksek satır), etiketler `Wrap + Overflow` ile kutularından taşıyordu; HUD'da mesafe ve mesaj satırları aynı yükseklikteydi | Tüm etiketlerde best-fit + truncate (`UI/UiKit.cs`), HUD üst düzeni (`UI/HudController.cs`) |
| Bölüm sekmeleri detay panelinin altında | Sekme ve kart genişlikleri 16:9 için sabit pikseldi | Detay panelinin solundaki alana oranlı yerleşim (`UI/MenuController.cs`) |
| Garaj önizlemesi oyundan farklı, parçalar arabadan uzakta | `VehicleKit.Measure` vitrin kökünün tüm mesh'lerini ölçüyordu; 3,8 m'lik parlayan halka arabanın parçası sayıldı | Ölçüm yalnızca "Visual" altındaki model (`Vehicles/VehicleKit.cs`) |
| Jant renkleri aynı | Yansıma probu olmayan sahnede metallic 0,95 albedoyu neredeyse tamamen siler | Metallic 0,15–0,35 (`Vehicles/VehicleVisual.cs`) |
| Ödüllü reklam hep "yükleniyor" | Google'ın örnek uygulama kimliğiyle UMP `Update` "publisher misconfiguration" döner, `CanRequestAds()` false kalır; kod bu durumda SDK'yı başlatmıyordu. Düğmeler de bir kez kurulup hiç güncellenmiyordu | Test modunda SDK her durumda başlar; `AdButtonLive` (`Ads/AdsManager.cs`, `UI/AdButtonLive.cs`) |
| Kanal arenası yüklemesi "%22"de ~11 sn | Her maç başında tüm shader varyantları yeniden ısıtılıyordu; kanal arenasının OBJ ve dokuları her ziyarette yeniden çözülüyordu | Isıtma oturumda bir kez; arena varlıkları oturum önbelleğinde (`Mission/MatchDirector.cs`, `World/Arena.cs`) |
| Engebede zıplama / takılma | İnce ışınlı tekerlek dikiş ve basamaklarda sıkışmayı tek fizik adımında 0'dan tama çıkarıyordu; amortisör zemin geometrisinin değişim hızına baktığı için her tümsek bir kuvvet sıçraması üretiyordu ve kuvvet sıfırın altına inemediği için bu sıçramalar toplanıp arabayı havaya atıyordu (doğrultma etkisi) | Küre tekerlek; basamakta sıkışma hız sınırı (rampa ve iniş serbest); amortisör gövdenin kendi hareketine bakar (skyhook) + rampadaki sürekli yükselme 0,2 sn süzgeçle eklenir; tümsek kaldırma sınırı aracın dikey ekseninde; yan-kesit simülasyonuyla 4.0.1'e karşı ölçüldü (`Vehicles/ArcadeCar.cs`, `TEST_REPORT_TR.md`) |
| Kamera duvarda / arabanın içinde | Engel görünce kamera pivot'a 1,6 m'ye kadar çekiliyordu (araba tavanı); geçiş sırasında duvarın içinden geçebiliyordu | Yükselen kısa bom (yükselme de çarpışma kontrollü), kamera hiçbir zaman çarpma noktasının gerisine konmaz, yavaş açılma, kare sonu duvar kontrolü (`CameraRig/ChaseCamera.cs`) |
| Maçta parçalar gövdeden ayrılıyor | Gövde virajda/frende 5°'ye kadar yatıyor, savaş kiti ise araba köküne bağlı olduğu için düz kalıyordu | Kit "Visual" tutucusunun altına, dinlenme konumuna göre bağlanır (`Vehicles/VehicleKit.cs` `FollowBody`) |
