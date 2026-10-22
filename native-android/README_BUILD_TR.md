# Derby Arena 1.0.5 — Native Android (Kotlin)

Bu proje Godot export zincirinden ayrılmış **native Android / Kotlin + Jetpack Compose** sürümüdür. 3B görüntüleme için Google Filament ve glTF/GLB varlıkları kullanılır.

## Uygulama kimliği
- Ad: Derby Arena
- Paket: `com.lenstedreal.derbyarena`
- Sürüm: `1.0.5` (`versionCode = 105`)
- Minimum Android: API 24
- Kaynak modeller: `app/src/main/assets/models/`

## Android Studio ile derleme
1. Bu klasörü Android Studio'da aç.
2. JDK 21 ve Android SDK Platform 37.0 kurulu olsun. Build Tools 36.0.0 yeterlidir.
3. Gradle senkronizasyonunu tamamla.
4. `:app:assembleDebug` göreviyle debug APK üret.
5. Çıktı: `app/build/outputs/apk/debug/app-debug.apk`.

## GitHub Actions
`.github/workflows/android-native.yml`, `setup-android` eylemini kullanmadan SDK Manager aracılığıyla gerekli bileşenleri kurmayı ve APK üretmeyi dener. GitHub'a push yaptıktan sonra **Actions → Derby Arena Native Android APK** sayfasında çalıştırma sonucuna ve `DerbyArena-1.0.5-native-debug-apk` artifact'ine bakabilirsin.

## İçerik
- Compose tabanlı oyun ekranı, menü/seviye seçimi, HUD, mobil kontroller ve sonuç ekranı
- Araç hareketi, düşman AI, çarpışma/hasar, silahlar, ganimet, VFX ve kamera sistemleri için Kotlin kaynakları
- Filament tabanlı GPU renderer ve yedek çizim yolu
- Arena, kalabalık, oyuncu/düşman araçları, ganimet ve efektler için GLB modelleri

## Test durumu
Kaynak ve kaynak varlık kontrolleri yapılmıştır; bu çalışma ortamında Android SDK/JDK olmadığı ve Gradle dağıtımı indirilemediği için **APK derlemesi ve gerçek cihaz testi tamamlanmış değildir**. APK'nın hazır olduğu ancak CI'da derleme başarılı olduktan sonra söylenebilir.
