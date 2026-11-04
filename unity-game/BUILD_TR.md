# DERBY ARENA — Build (v4.0.1)

| | |
|---|---|
| Unity | 6000.3.27f1 |
| Pipeline | Built-in |
| Paketler | Input System 1.11.2, uGUI 2.0.0, Google Mobile Ads 11.6.0 (OpenUPM), çekirdek modüller |
| Android | `com.lenstedreal.derbyarena`, 4.0.1 / 401, ARM64, IL2CPP, min API 25, GLES3 → Vulkan, ASTC dokular |
| iOS | `com.lenstedreal.derbyarena`, 4.0.1 / build 401, iOS 13+, IL2CPP |

## Tek dal: `main`
4.0.1'den itibaren tüm kod **yalnızca `main` dalında**. `unity/blackout-run` dalı artık kullanılmıyor; Unity Build
Automation hedefleri `main`'den derler. Geçiş sırası (bir kez):

1. `main`'e push et (Termux komutları teslim notunda).
2. Unity Dashboard → **DevOps → Build Automation → Configurations** → "Blackout Run Android" → **Edit** →
   **Basic settings → Branch**: `main` → **Save Changes**. Varsa "Blackout Run AAB" hedefinde aynısını yap.
3. Bir derleme başlat; log'da `DERBY BUILD SETTINGS APPLIED: ... / 4.0.1 / 401 / ...` satırını gör.
4. Ancak bundan sonra eski dalı sil: `git push origin --delete unity/blackout-run` (isteğe bağlı; silmeden
   bırakılırsa zarar vermez, sadece eski kalır).

GitHub Actions (`.github/workflows/unity-android.yml`): her push'ta sadece **statik doğrulama** (`validate_derby.py`)
çalışır. APK işi Unity lisans secret'ları ister ve 4.0.1'den itibaren **yalnızca elle** (Actions → unity-android →
Run workflow) başlar; bu yüzden push'lar artık kırmızı X almaz. Gerçek derlemeler Build Automation'dan alınır.

## Unity Build Automation
1. Dal: `main`, proje yolu `unity-game`, Unity 6000.3.27f1.
2. Android hedefi mevcut ("Blackout Run Android"). Build öncesi `DerbyBuildSettingsEnforcer` tüm ayarları ve
   `Assets/Resources/Derby/Materials/*.mat` şablonlarını üretir. Logda şunu ara:
   `DERBY BUILD SETTINGS APPLIED: BUILT-IN PIPELINE + RESOURCE MATERIALS / DERBY ARENA / 4.0.1 / 401 / ARM64 + iOS`
3. iOS: Build Automation'da yeni hedef → iOS, aynı dal. Apple Developer hesabı (yıllık ücretli) + sertifika/provisioning
   profili gerekir; bunlar olmadan iOS derlemesi imzalanamaz.
4. Google Play için AAB: Android hedefinde "Build App Bundle" seç + keystore'u Build Automation'a yükle.
5. "119 errors" gibi AssemblyUpdater satırları derlemeyi durdurmaz; derleme sonucuna (APK/AAB üretildi mi) bak.

## Kontroller (4.0.1)
Dokunmatik, varsayılan: **kaydırmalı direksiyon** (ekranın sol yarısına dokun, sağa-sola kaydır), **otomatik gaz açık**,
sağda FREN / NİTRO / DRİFT / ATEŞ / ROKET. Ayarlar → Kontroller: direksiyon tipi (kaydırma / simit / oklar / eğim),
hassasiyet, otomatik gaz, sürüş yardımı, düğme boyutu ve saydamlığı, düzen (Klasik / Sürücü / Sol El) ve
**Düzeni Özelleştir** (PUBG Mobile mantığı: her düğmeyi sürükle, − / + ile boyutlandır, Kaydet). Aynı editör duraklatma
menüsünden de açılır.
Klavye: WASD/oklar, Space drift, Shift nitro, J/F ateş, K/R roket, Esc duraklat. Gamepad: sol çubuk, tetikler, A ateş, B roket.

## Statik kontrol
`python validate_derby.py` (Unity gerektirmez). Derleme, oynanış ve cihaz kanıtı **değildir**.


## Google Play için AAB (Android App Bundle) — Unity Build Automation

Kaynak: Unity Build Automation belgeleri ("Configure a build" ve "Sign an Android application").
Önerilen düzen: iki hedef. **APK** (telefonda deneme, debug imza yeterli) ve **AAB** (Google Play, kendi anahtarın).

### A) Yükleme anahtarı (bir kez, Termux)
```
pkg install -y openjdk-17
keytool -genkeypair -v -storetype JKS -keystore ~/derby-upload.keystore -alias derby -keyalg RSA -keysize 2048 -validity 10000
cp ~/derby-upload.keystore /sdcard/Download/
```
"Enter key password" sorusunda **Enter**'a bas: anahtar şifresi keystore şifresiyle aynı olur.
Dosyayı ve şifreyi güvenli yerde sakla, **GitHub'a koyma**.

### B) AAB hedefini oluştur (tarayıcıdan)
1. Unity Dashboard → **DevOps** → **Build Automation** → **Configurations**.
2. Mevcut "Blackout Run Android" hedefinde **⋮ → Duplicate** (yoksa **Add new configuration** → Android). Adını "Blackout Run AAB" yap.
3. **Basic settings**:
   - Branch `main`, Project subfolder `unity-game`, Unity sürümü 6000.3.27f1.
   - **Android SDK version**: listedeki en yüksek sürüm (API 36 varsa onu seç).
   - **Credentials** → **Bundle ID**: `com.lenstedreal.derbyarena`.
   - **Credentials set** → **Add new provisioning credentials (for release)**:
     **Name** `derby-release`, **Keystore file** `derby-upload.keystore`, **Keystore password**, **Key alias** `derby`, **Key password** (aynı şifre).
4. **Advanced Settings** → **Platform specific settings (Android)**:
   - **Build app bundles (.aab) instead of an APK** → AÇIK
   - **Build asset packs** → KAPALI
   - **Make split binary application builds** → KAPALI
5. **Save Changes** ve sonra **Build**. Bitince indirme listesinde `.aab` dosyası olur.
6. APK hedefi aynı kalsın. Orada **Auto-generated debug keystore (for development only)** kullanılabilir. AAB telefona doğrudan kurulmaz.

### C) Google Play Console
1. **Uygulama oluştur**: ad DERBY ARENA, varsayılan dil Türkçe, Oyun, Ücretsiz.
2. **Mağaza girişi**: `StoreListing/STORE_LISTING_TR_EN.md` metinleri, `play_icon_512.png`, `feature_graphic_1024x500.png`, en az 2 ekran görüntüsü.
3. **Uygulama içeriği**:
   - Gizlilik politikası URL'si: `StoreListing/PRIVACY_POLICY.md` metnini Google Sites gibi herkese açık bir sayfada yayınla, linki gir.
   - Veri güvenliği: oyun veri toplamıyor.
   - İçerik derecelendirmesi anketi: araçlar arası, kan içermeyen çizgi film tarzı şiddet.
   - Hedef kitle: 13 yaş ve üstü önerilir.
   - Reklam: var (AdMob; yayın öncesi gerçek kimlikler ve `TestMode=false`).
4. **Test → Kapalı test** → yeni sürüm → `.aab` yükle → **Play App Signing** açık kalsın.
5. **Yeni kişisel geliştirici hesabıysan**: üretime çıkmadan önce kapalı testte **en az 12 test kullanıcısı 14 gün kesintisiz** kayıtlı olmalı. Sonra Play Console'dan üretim erişimi istenir.
6. Hedef API: Play şu an **API 36 (Android 16)** istiyor. AAB'yi yükleyince Play Console gösterir. Uyarı çıkarsa B-3'teki **Android SDK version** alanını yükselt.
7. Her yeni yüklemede versionCode artmalı. Kod bunu sürümle birlikte yükseltiyor (4.0.1 → 401).


## 2.0.0 — Reklam eklentisi notları
- `Packages/manifest.json` içinde OpenUPM kayıt defteri (`com.google` kapsamı), `com.google.ads.mobile` 11.6.0 ve `com.unity.external-dependency-manager` 2.1.0 var. Unity Build Automation bunları ilk derlemede indirir.
- External Dependency Manager, Android derlemesinde Google kütüphanelerini Gradle'a kendisi ekler. "Resolve" uyarısı çıkarsa Unity'de Assets → External Dependency Manager → Android Resolver → Force Resolve.
- Paket indirilemezse oyun yine derlenir (`DERBY_ADS` tanımsız kalır), sadece reklam gelmez. Derleme günlüğünde `com.google.ads.mobile` hatası var mı bak.
- Yayın öncesi: `AdsConfig.TestMode=false`, gerçek birim ve uygulama kimlikleri (bkz. `docs/REKLAM_PLANI_TR.md`).
