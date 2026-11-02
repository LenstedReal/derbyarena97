# DERBY ARENA — Build (v1.3.0)

| | |
|---|---|
| Unity | 6000.3.27f1 |
| Pipeline | Built-in |
| Paketler | Input System 1.11.2, uGUI 2.0.0, çekirdek modüller (harici registry yok) |
| Android | `com.lenstedreal.derbyarena`, 1.3.0 / 130, ARM64, IL2CPP, min API 25, GLES3 → Vulkan |
| iOS | `com.lenstedreal.derbyarena`, 1.3.0 / build 130, iOS 13+, IL2CPP |

## Unity Build Automation
1. Dal: `unity/blackout-run`, proje yolu `unity-game`, Unity 6000.3.27f1.
2. Android hedefi mevcut ("Blackout Run Android"). Build öncesi `DerbyBuildSettingsEnforcer` tüm ayarları ve
   `Assets/Resources/Derby/Materials/*.mat` şablonlarını üretir. Logda şunu ara:
   `DERBY BUILD SETTINGS APPLIED: BUILT-IN PIPELINE + RESOURCE MATERIALS / DERBY ARENA / 1.3.0 / 130 / ARM64 + iOS`
3. iOS: Build Automation'da yeni hedef → iOS, aynı dal. Apple Developer hesabı (yıllık ücretli) + sertifika/provisioning
   profili gerekir; bunlar olmadan iOS derlemesi imzalanamaz.
4. Google Play için AAB: Android hedefinde "Build App Bundle" seç + keystore'u Build Automation'a yükle.

## Kontroller
Dokunmatik: sol alt direksiyon simidi (veya ayarlardan oklar), sağda GAZ / FREN / NİTRO / DRİFT / ATEŞ / ROKET.
Klavye: WASD/oklar, Space drift, Shift nitro, J/F ateş, K/R roket, Esc duraklat. Gamepad: sol çubuk, tetikler, A ateş, B roket.

## Statik kontrol
`python validate_derby.py` (Unity gerektirmez). Derleme, oynanış ve cihaz kanıtı **değildir**.
