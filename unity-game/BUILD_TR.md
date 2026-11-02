# DERBY ARENA — Build (v1.5.0)

| | |
|---|---|
| Unity | 6000.3.27f1 |
| Pipeline | Built-in |
| Paketler | Input System 1.11.2, uGUI 2.0.0, çekirdek modüller (harici registry yok) |
| Android | `com.lenstedreal.derbyarena`, 1.5.0 / 150, ARM64, IL2CPP, min API 25, GLES3 → Vulkan |
| iOS | `com.lenstedreal.derbyarena`, 1.5.0 / build 150, iOS 13+, IL2CPP |

## Unity Build Automation
1. Dal: `unity/blackout-run`, proje yolu `unity-game`, Unity 6000.3.27f1.
2. Android hedefi mevcut ("Blackout Run Android"). Build öncesi `DerbyBuildSettingsEnforcer` tüm ayarları ve
   `Assets/Resources/Derby/Materials/*.mat` şablonlarını üretir. Logda şunu ara:
   `DERBY BUILD SETTINGS APPLIED: BUILT-IN PIPELINE + RESOURCE MATERIALS / DERBY ARENA / 1.5.0 / 150 / ARM64 + iOS`
3. iOS: Build Automation'da yeni hedef → iOS, aynı dal. Apple Developer hesabı (yıllık ücretli) + sertifika/provisioning
   profili gerekir; bunlar olmadan iOS derlemesi imzalanamaz.
4. Google Play için AAB: Android hedefinde "Build App Bundle" seç + keystore'u Build Automation'a yükle.

## Kontroller
Dokunmatik: sol alt direksiyon simidi (veya ayarlardan oklar), sağda GAZ / FREN / NİTRO / DRİFT / ATEŞ / ROKET.
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
   - Branch `unity/blackout-run`, Project subfolder `unity-game`, Unity sürümü 6000.3.27f1.
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
   - Reklam: yok.
4. **Test → Kapalı test** → yeni sürüm → `.aab` yükle → **Play App Signing** açık kalsın.
5. **Yeni kişisel geliştirici hesabıysan**: üretime çıkmadan önce kapalı testte **en az 12 test kullanıcısı 14 gün kesintisiz** kayıtlı olmalı. Sonra Play Console'dan üretim erişimi istenir.
6. Hedef API: Play şu an **API 36 (Android 16)** istiyor. AAB'yi yükleyince Play Console gösterir. Uyarı çıkarsa B-3'teki **Android SDK version** alanını yükselt.
7. Her yeni yüklemede versionCode artmalı. Kod bunu sürümle birlikte yükseltiyor (1.5.0 → 150).
