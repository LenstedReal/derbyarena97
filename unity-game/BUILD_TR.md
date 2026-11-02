# DERBY ARENA — Build (v1.4.0)

| | |
|---|---|
| Unity | 6000.3.27f1 |
| Pipeline | Built-in |
| Paketler | Input System 1.11.2, uGUI 2.0.0, çekirdek modüller (harici registry yok) |
| Android | `com.lenstedreal.derbyarena`, 1.4.0 / 140, ARM64, IL2CPP, min API 25, GLES3 → Vulkan |
| iOS | `com.lenstedreal.derbyarena`, 1.4.0 / build 140, iOS 13+, IL2CPP |

## Unity Build Automation
1. Dal: `unity/blackout-run`, proje yolu `unity-game`, Unity 6000.3.27f1.
2. Android hedefi mevcut ("Blackout Run Android"). Build öncesi `DerbyBuildSettingsEnforcer` tüm ayarları ve
   `Assets/Resources/Derby/Materials/*.mat` şablonlarını üretir. Logda şunu ara:
   `DERBY BUILD SETTINGS APPLIED: BUILT-IN PIPELINE + RESOURCE MATERIALS / DERBY ARENA / 1.4.0 / 140 / ARM64 + iOS`
3. iOS: Build Automation'da yeni hedef → iOS, aynı dal. Apple Developer hesabı (yıllık ücretli) + sertifika/provisioning
   profili gerekir; bunlar olmadan iOS derlemesi imzalanamaz.
4. Google Play için AAB: Android hedefinde "Build App Bundle" seç + keystore'u Build Automation'a yükle.

## Kontroller
Dokunmatik: sol alt direksiyon simidi (veya ayarlardan oklar), sağda GAZ / FREN / NİTRO / DRİFT / ATEŞ / ROKET.
Klavye: WASD/oklar, Space drift, Shift nitro, J/F ateş, K/R roket, Esc duraklat. Gamepad: sol çubuk, tetikler, A ateş, B roket.

## Statik kontrol
`python validate_derby.py` (Unity gerektirmez). Derleme, oynanış ve cihaz kanıtı **değildir**.


## Google Play için AAB (Android App Bundle)
1. **Yükleme anahtarı (bir kez):** Termux'ta
   `pkg install openjdk-17` ve sonra
   `keytool -genkeypair -v -keystore derby-upload.keystore -alias derby -keyalg RSA -keysize 2048 -validity 10000`.
   Şifreleri ve dosyayı **güvenli bir yerde sakla, GitHub'a koyma**. Kaybedersen güncelleme yükleyemezsin.
2. **Unity Build Automation → Credentials → Android → Add**: keystore dosyasını, alias'ı (`derby`) ve şifreleri gir.
3. **Build Automation → Configurations → "Blackout Run Android" → Edit → Advanced Settings (Android)**:
   - "Build Android App Bundle (AAB)" seçeneğini aç
   - Credentials olarak az önce eklediğin keystore'u seç
   - Telefonda test için APK hedefini ayrı tut (AAB telefona doğrudan kurulmaz).
4. Build bitince `.aab` dosyasını indir.
5. **Play Console** → Uygulama oluştur → Test → Dahili test → Yeni sürüm → `.aab` yükle.
   "Play App Signing" açık kalsın (Google dağıtım anahtarını kendisi tutar, sen yükleme anahtarıyla imzalarsın).
6. Play'in istekleri:
   - **Hedef API 36 (Android 16) zorunlu.** Unity hedefi "en yüksek kurulu SDK" seçili; Play Console AAB'yi yükleyince
     hedef API'yi gösterir. 36'nın altındaysa haber ver, ayarı sabitleyelim.
   - Gizlilik politikası URL'si, içerik derecelendirme anketi, veri güvenliği formu (oyun kişisel veri toplamıyor).
   - `StoreListing/play_icon_512.png`, `StoreListing/feature_graphic_1024x500.png` ve en az 2 ekran görüntüsü.
