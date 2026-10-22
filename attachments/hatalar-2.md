# DERBY-ARENA — HATALAR RAPORU

Repo: `github.com/LenstedReal/Derby-arena` (public · `main` · 4 commit · 0 yıldız · 0 fork)
Tarih: 22 Ekim 2026

> İstediğiniz gibi sert yazdım. Ama sert olmak uydurmak demek değil: her madde bir kanıt etiketi taşıyor.

---

## 0. Önce dürüst olalım: neyi okuyabildim, neyi okuyamadım

| İş | Sonuç |
|---|---|
| `git clone` | **Başarısız.** Sandbox’ta ağ kapalı: `Could not resolve host: github.com` |
| Okuyabildiklerim | Kök sayfa, `README.md`, `design_guidelines.json`, `test_result.md`, `.gitignore`, `.gitconfig` |
| Açamadıklarım | `backend/`, `frontend/`, `derby-arena/`, `memory/`, `tests/`, `test_reports/`, `.emergent/` (GitHub `tree` sayfaları robots.txt ile kapalı), commit mesajları, canlı demo `derby-arena.vercel.app` |

**Sonuç: tek satır kaynak kod okumadım.** Bu yüzden “şu fonksiyon şu yüzden bozuk” demeyeceğim; deseydim uydurmuş olurdum. Her madde bir etiket taşır:

- **[KANITLI]** okuduğum dosyada birebir var
- **[ÇIKARIM]** okuduklarımdan çıkan mantıklı sonuç, kodda doğrulanmadı
- **[KONTROL]** kaynak kodda bakılması gereken şey (bölüm 4)

---

## 1. Neden demo gibi hissettiriyor?

**Kısa cevap: oyunun “görünüşü” tarif edilmiş, “hissi” hiç tarif edilmemiş.**

`design_guidelines.json` 170 satır boyunca renk, font, ekran düzeni ve buton yerini anlatıyor. Ses, fizik, çarpışma, hasar, düşman yapay zekâsı, ekran sarsıntısı, parçacık, haptik, ekonomi dengesi ve öğretici için tek satır yok. AI ajanına menünün nasıl görüneceğini söylediniz; sürüşün nasıl hissettireceğini söylemediniz. Çıkan şey: menüleri olan ama oyunu olmayan bir iskelet.

### Demo hissi kontrol tablosu (spec’e göre)

| Oyunu “oyun” yapan şey | Spec’te var mı? |
|---|---|
| Ses / müzik (motor, çarpışma, nitro, silah, UI) | **Yok.** `platform_libraries` içinde ses kütüphanesi yok |
| Sürüş ve çarpışma hissi (fizik, hasar modeli) | **Yok** |
| Vuruş geri bildirimi (ekran sarsıntısı, hit-stop) | **Yok** |
| Parçacık / efekt (kıvılcım, duman, patlama) | **Yok** |
| Haptik (titreşim) | **Yok** (`expo-haptics` listede yok) |
| Düşman davranışı | **Yok.** Yalnızca “Remaining Enemies” sayacı |
| Ekonomi dengesi | **Yok.** Scrap para birimi var; kazanç/maliyet eğrisi yok |
| Öğretici / ilk açılış | **Yok** |
| İlerleme kaydı (kalıcılık) | **Yok** |
| Hata ve boş durumlar | **Çoğu “N/A”.** Yalnızca Garage’da “Error fetching stats” ve HUD’da “Connection/pause overlay” var |
| Masaüstü kontrol (klavye / gamepad) | **Yok.** Sadece dokunmatik joystick + 5 buton |
| Kalite katmanı içeriği (LOW…ULTRA ne değiştirir) | **Yok.** Sadece dört isim |
| İçerik çeşitliliği (kaç arena, kaç düşman tipi) | **Belirsiz.** “Grid of arenas/difficulties” yazıyor, sayı yok |

Bu tablonun her satırı demo hissine doğrudan katkı yapar. [KANITLI: spec’te yok] → [ÇIKARIM: kodda da ya yok ya da tesadüfen var]

### Yedi ana neden

1. **Spec %100 UI.** `instructions_to_main_agent` içindeki 8 maddenin tamamı yerleşim, renk, font ya da kontrol yeri. Oyun hissi maddesi sıfır. [KANITLI]

2. **Kapsam geniş, derinlik yok.** 6 ekran, 4 yükseltme kategorisi (Engine, Armor, Turret, Magnet), 5 aksiyon (Throttle, Brake, Nitro, Handbrake, Fire), silah ve mühimmat, minimap, bölge sayacı, kill sayacı, zorluk seçimi, 4 kalite seviyesi, kamera modu, direksiyon hassasiyeti. Repoda 4 commit var; bu, kapsamın kademeli olgunlaştırılmadığına işaret ediyor. [KANITLI: kapsam ve commit sayısı · ÇIKARIM: ince kalma]

3. **Kimlik krizi.** Repo adı “Derby-arena” (yıkım yarışı), açıklama “Race Battle win” (yarış + savaş), spec ise post-apokaliptik araç savaşı + daralan bölge (battle royale). Yarış mı, derby mi, nişancı mı, battle royale mı? Tek cümlelik bir oyun tanımı yok. [KANITLI]

4. **Ürün katmanı boş.** README tek satır. Test kaydı boş. Kimse oyunun ne olduğunu, nasıl oynandığını, nasıl çalıştırıldığını bilemez. Demo, tanımı olmayan şeydir. [KANITLI]

5. **Yer tutucu varlıklar.** Menü arka planı, endüstriyel doku ve arena önizlemesi Unsplash stok fotoğraf bağlantıları. “Arena önizleme” gerçek bir arena değil, stok bir paslı araba fotoğrafı. [KANITLI: spec · KONTROL: kodda kullanılıyor mu]

6. **Platform uyuşmazlığı.** Spec: mobil, yatay, dokunmatik (Expo, landscape kilidi, sol/sağ sanal joystick). Yayın: Vercel, yani web. Masaüstünden açan biri klavyeyle oynayamıyorsa oyun “tıklayınca bir şey olmayan” bir sayfa olur. [KANITLI: spec ve yayın adresi · ÇIKARIM: uyuşmazlık]

7. **Sahipsiz proje izleri.** İki farklı üretici iskeleti iç içe: `google-gemini/aistudio-repository-template`’ten oluşturulmuş, üstüne Emergent ajanının dosyaları binmiş (`.emergent`, `memory`, `test_reports`, `tests`, `backend`, `frontend`, `test_result.md`, `design_guidelines.json`); ayrıca repo adıyla aynı isimli bir `derby-arena/` klasörü var. Gerçek oyun hangisi? [KANITLI: varlıkları · ÇIKARIM: karışıklık]

---

## 2. Hata listesi

Önem: **KRİTİK** (oyunu oyun olmaktan çıkarır) · **YÜKSEK** · **ORTA** · **DÜŞÜK**

### KRİTİK

**H-01 · README yok**
- Kanıt [KANITLI]: `README.md` 1 satır, 29 bayt: `# Here are your Instructions`. Şablon artığı.
- Etki: Oyunun ne olduğu, kontrolleri, nasıl çalıştırılacağı, hangi ortam değişkenlerinin gerektiği belli değil. İki hafta sonra siz bile hatırlamazsınız.
- Çözüm: Tek cümlelik pitch, ekran görüntüsü/GIF, kontroller (dokunmatik + klavye), kurulum komutları, `.env` tablosu, Vercel kök dizin ayarı, bilinen eksikler.

**H-02 · Test kaydı boş**
- Kanıt [KANITLI]: `test_result.md` yalnızca Emergent test protokolü şablonu (103 satır). “Testing Data” başlığının altında tek bir görev, tek bir `working: true/false`, tek bir test planı yok.
- Etki: “Çalışıyor” iddiasının kaydı yok; bir şey bozulduğunda fark eden olmaz. (`tests/` ve `test_reports/` klasörleri var ama açamadım; içlerinde ne olduğunu bilmiyorum.)
- Çözüm: Her özellik için görev girdisi; en az 5 smoke test: menü → garaj → arena seçimi → maç → sonuç ekranı.

**H-03 · Çekirdek oyun döngüsü tanımsız**
- Kanıt [KANITLI]: “Race Battle win” ↔ “DERBY ARENA” ↔ spec’te bölge sayacı + kill + sıralama (placement).
- Etki: Hangi sistemin neden var olduğu belli değil; her şey yarım kalır.
- Çözüm: Tek cümle yazın. Örnek: “Daralan arenada son hayatta kalan araç kazanır; hasar çarpışmadan ve silahtan gelir; ödül Scrap; Scrap yükseltmeye gider.” Sonra bu cümleye hizmet etmeyen her şeyi kesin.

**H-04 · Game-feel hiç tanımlanmamış**
- Kanıt [KANITLI]: bölüm 1’deki tablo.
- Etki: Menüler cilalı, sahne cansız. “Demo” hissinin ana sebebi bu.
- Çözüm: İkinci bir doküman yazın (`game_feel.md`): ses listesi (motor RPM döngüsü, çarpışma, nitro, silah, UI tık), kamera sarsıntısı ve hit-stop değerleri, parçacık listesi, haptik tablosu, hasar modeli, düşman durumları (kovala / yan geç / kaç), Scrap kazanç ve yükseltme maliyet tablosu.

### YÜKSEK

**H-05 · Web yayını ile mobil-yatay spec çelişiyor**
- Kanıt [KANITLI]: spec yalnızca dokunmatik kontrol tanımlıyor; yayın adresi `derby-arena.vercel.app`.
- Etki [ÇIKARIM]: masaüstünde kontrol yok; dikey telefonda HUD bozuk.
- Çözüm: WASD/oklar + Space (el freni) + Shift (nitro) + fare/Ctrl (ateş) + Gamepad API; dikey ekranda “cihazı yatay çevirin” katmanı.

**H-06 · İki iskelet iç içe + `derby-arena/` klasörü**
- Kanıt [KANITLI]: kök listede `.emergent`, `backend`, `frontend`, `derby-arena`, `memory`, `test_reports`, `tests`; “Generated from google-gemini/aistudio-repository-template”.
- Etki [ÇIKARIM]: Hangi klasörün canlıda olduğu belirsiz. Vercel “Root Directory” yanlışsa yanlış proje yayınlanır; çift bağımlılık, çift sürüm.
- Çözüm: Tek kaynak klasörü bırakın, diğerlerini silin, Vercel kök dizinini ve build komutunu README’ye yazın.

**H-07 · Spec kendi içinde çelişiyor**
Hepsi `design_guidelines.json` içinde [KANITLI]:
- “Do NOT use blur/glassmorphism” (hem `instructions_to_main_agent` hem `glassmorphism.enabled: "false"`) ↔ Result Screen: “modal over **blurred** 3D scene” ↔ `platform_libraries.blur: "expo-blur"`. Ajan hangisine uyacak?
- “Never use blue, indigo, or purple” ↔ `info: "#607D8B"` (mavi-gri).
- `"enabled": "false"` ve `"shadow_tier": "0"` metin olarak yazılmış (boolean/sayı değil). Bunu okuyan kod `"false"` metnini doğru sayabilir. [ÇIKARIM]
- `personality` alanı `7 Dark-First…` diye başlıyor; şablondan kalma “7”.
- Kalite seviyeleri (LOW, MEDIUM, HIGH, ULTRA) var ama hangisinin neyi değiştirdiği yok; ayarlar ekranı dekoratif kalabilir. [ÇIKARIM]
- Çözüm: Tek karar (blur var mı yok mu), renk kuralını `info` ile uyumlu hale getirin, tipleri düzeltin, kalite tablosunu yazın (gölge, çözünürlük ölçeği, parçacık sayısı, düşman sayısı).

**H-08 · Harici stok görsel bağımlılığı**
- Kanıt [KANITLI]: spec’te 3 Unsplash URL’si (`main_menu_bg_fallback`, `industrial_texture`, `arena_preview`).
- Etki [ÇIKARIM]: Ağ hatasında boş menü; “demo” hissinin en görünür kaynağı; harici bağlantı bir gün ölebilir. Kodda kullanılıp kullanılmadığı [KONTROL].
- Çözüm: Görselleri `assets/` altına indirin, WebP’e çevirin, `expo-image` ile önbellekleyin. Arena önizlemelerini oyunun kendi render’ından üretin.

**H-09 · Ortam değişkenleri ve test kimlikleri repo dışı, belgelenmemiş**
- Kanıt [KANITLI]: `.gitignore` içinde `.env`, `.env.*`, `*.env`, `memory/test_credentials.md`; kökte `.env.example` yok; README boş.
- Etki [ÇIKARIM]: Temiz clone çalışmaz ya da yarım çalışır; testler kimlik dosyası olmadan koşamaz.
- Çözüm: `.env.example` ekleyin (değerler boş), README’de tablo.

**H-10 · Backend’in rolü ve canlıdaki durumu belirsiz**
- Kanıt [KANITLI]: `backend/` var; spec “Error fetching stats” ve “Connection/pause overlay” durumlarını tanımlıyor, yani oyun ağa/sunucuya bağımlı olacak şekilde tasarlanmış.
- Etki [ÇIKARIM]: Emergent şablonlarında backend genelde ayrı bir Python sunucusudur; Vercel’de ayrıca yapılandırılmadıkça çalışmaz. Canlı demoda skor, Scrap ve garaj istekleri ya hiç gitmiyordur ya da hata veriyordur.
- Kontrol [KONTROL]: frontend’in istek attığı adres, CORS, `vercel.json`.

**H-11 · İlerleme kalıcılığı tanımsız**
- Kanıt [KANITLI]: Garage Scrap ile çalışıyor; spec’te nerede saklandığı yok.
- Etki [ÇIKARIM]: Sayfa yenilenince para ve yükseltmeler sıfırlanıyorsa oyun demo gibidir; ilerlemenin anlamı kalmaz.
- Çözüm: Önce yerel kayıt (AsyncStorage / localStorage), sonra isteğe bağlı sunucu.

### ORTA

**H-12 · CI yok**
- Kanıt [KANITLI]: kökte `.github/` klasörü yok (kök liste alfabetik; olsaydı `.emergent` ile `backend` arasında görünürdü).
- Etki: Hiçbir push derleme, tip denetimi, lint ya da test görmüyor; Vercel’e bozuk build gidebilir.
- Çözüm: GitHub Actions: `npm ci` → `tsc --noEmit` → `eslint` → `jest` → web export.

**H-13 · Kökte çalıştırılabilir proje yok**
- Kanıt [KANITLI]: kökte `package.json`, `.env.example`, `LICENSE`, `.github/` yok.
- Etki: Klonlayan biri nerede `npm install` çalıştıracağını bilmez (üç aday: `frontend/`, `derby-arena/`, `backend/`).
- Çözüm: README’de net adımlar; mümkünse kökten tek komut (`npm run dev`).

**H-14 · `.gitignore` başka projelerden kalma**
- Kanıt [KANITLI]: Next.js (`/.next/`, `next-env.d.ts`), Chainlit (`chainlit.md`, `.chainlit`), `agenthub/agents/youtube/db`, `.ac` bu projeyle ilgisiz görünüyor. Expo’ya özgü satırlar (`.expo/`, `web-build/`, `ios/`, `android/`, `*.jks`, `*.p8`, `*.p12`, `*.mobileprovision`) yok.
- Etki [ÇIKARIM]: Spec Expo diyor; alt klasörlerde ayrı `.gitignore` yoksa Expo’nun yerel dosyaları repoya sızabilir. [KONTROL]
- Çözüm: Projeye uygun bir `.gitignore`; alt klasör `.gitignore`’larını kontrol edin.

**H-15 · `.gitconfig` repoda**
- Kanıt [KANITLI]: `[user] email = github@emergent.sh · name = emergent-agent-e1`.
- Etki: Yerel git kimliği dosyası commit’lenmiş; amacı belirsiz ve kafa karıştırıcı.
- Çözüm: Silin. Commit yazarlıklarını kontrol edin: `git log --format='%an <%ae>' | sort | uniq -c`.

**H-16 · Commit hijyeni**
- Kanıt [KANITLI]: 4 commit. Mesajları okuyamadım [KONTROL].
- Etki [ÇIKARIM]: Bu kapsamdaki bir proje için 4 commit, büyük toplu commit’ler demek; hata çıkınca geri dönmek ve `git bisect` zor.
- Çözüm: Küçük, tek konulu commit’ler; özellik başına dal.

### DÜŞÜK

**H-17 · Lisans yok.** [KANITLI] Repo public ama `LICENSE` yok: hukuken kimse kullanamaz, katkı veremez. Kullanılan font ve görsellerin lisans notları da yok.

**H-18 · Repo vitrini boş.** [KANITLI] About: “Race Battle win” (anlamsız tek cümle), 0 yıldız, 0 fork, 0 izleyici, sürüm yok. Topic etiketi görünmüyor.

**H-19 · Marka çakışması.** [ÇIKARIM] “Derby Arena”, İngiltere’deki gerçek bir velodrom/etkinlik mekânının adı; aramada ilk sonuçlar onu gösteriyor. Mağaza ve arama görünürlüğü için ayırt edici bir isim düşünün.

---

## 3. İlk yedi düzeltme (bu sırayla)

1. Tek cümlelik oyun tanımı ve çekirdek döngü kararı (H-03)
2. Tek kaynak klasörü; diğerlerini sil; Vercel kök dizin ayarı (H-06)
3. README + `.env.example` + çalıştırma adımları (H-01, H-09, H-13)
4. Masaüstü kontrolleri + yatay çevirme uyarısı (H-05)
5. Game-feel paketi: 6 ses + ekran sarsıntısı + kıvılcım/duman + haptik (H-04)
6. Stok görselleri yerel varlıklarla değiştir (H-08)
7. CI + 5 smoke test; `test_result.md`’yi gerçekten doldur (H-02, H-12)

---

## 4. Kaynak kodda bakılması gerekenler (açamadığım klasörler için)

**frontend/ (veya derby-arena/)**
- `package.json`: Expo SDK sürümü; `three`, `expo-gl` ve Three’yi Expo’ya bağlayan katman (`expo-three` ya da `@react-three/fiber/native`) ile peer uyarıları. npm meta verisine göre `expo-three` 8.0.0 Expo 51 dönemine ait ve `three ^0.166.0` istiyor; daha yeni Three/Expo sürümleriyle çakışma çıkabilir.
- `app.json`: `orientation: "landscape"`, web bundler ayarı
- Oyun döngüsü: sabit zaman adımı var mı, yoksa hız kare hızına mı bağlı?
- Fizik: gerçek motor (Rapier/Cannon) mı, elle yazılmış daire/AABB çarpışma mı? Çarpışmada momentum ve hasar var mı?
- Düşman: gerçekten karar veriyor mu, yoksa sabit ya da rastgele mi hareket ediyor?
- Girdi: çoklu dokunma, joystick ölü bölgesi, klavye/gamepad
- Ses: hiç var mı; tarayıcı autoplay kilidi nasıl aşılıyor?
- Bellek: her karede `new Vector3()`, geometri/materyal `dispose()` eksikliği, sahne değişiminde temizlik
- Durum: ekranlar arası state, kalıcılık
- Hata yönetimi: ErrorBoundary, WebGL context lost, yükleme/boş/hata durumları
- Güvenli alan: sol/sağ inset gerçekten uygulanmış mı?
- Font ve görseller: paketli mi uzaktan mı? (Barlow Condensed, Rajdhani)

**backend/**
- Hangi endpoint’ler var, frontend gerçekten çağırıyor mu?
- CORS ayarı (`*` mi?), kimlik doğrulama
- Skor/Scrap istemciye güvenerek mi kaydediliyor? (hile riski)
- DB bağlantısı env’den mi; varsayılan değerler
- Bağımlılıklar sabitlenmiş mi?

**tests/ ve test_reports/**
- Testler gerçekten koşuyor mu? Son rapor: kaç geçti, kaç kaldı? API mi UI mi kapsıyor?

**memory/ ve .emergent/**
- PRD/plan dosyası var mı; yapılacaklar listesi gerçek durumla uyuşuyor mu?
- Sır taraması: `git grep -nEi "(api[_-]?key|secret|token|password)"`

**Vercel**
- Root Directory, Build/Output komutları, `vercel.json` (SPA yönlendirmesi), ortam değişkenleri, son build log’u

---

## 5. Bu raporun sınırları ve derinleştirme yolu

Kod seviyesinde analiz için üç yol var:

1. **Ağı açın.** Ayarlardan sandbox ağ erişimini açarsanız `git clone` ile tüm repoyu satır satır okur, bu raporu dosya:satır kanıtlarıyla güncellerim.
2. **ZIP yükleyin.** GitHub’da Code → Download ZIP, sonra buraya yükleyin.
3. **Dosya yapıştırın.** En az: `frontend/package.json`, `app.json`, ana oyun/sahne dosyası, `backend/server.py`, `vercel.json`.

Canlı demoyu ben açamadım. Telefondan 2 dakika deneyip şunları yazarsanız ilgili maddeleri kesinleştiririm: kaç saniyede açıldı · ses var mı · direksiyon nasıl hissettiriyor · çarpışmada ne oluyor · düşman ne yapıyor · Scrap kazanınca yükseltme alınabiliyor mu · sayfayı yenileyince kayıt duruyor mu.
