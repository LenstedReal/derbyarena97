# Derby Arena — Kotlin Android migration (1.0.5)

Android oyunu Godot export zincirinden çıkarılıp `native-android/` içinde Kotlin + Jetpack Compose + Filament tabanlı native Android projesine taşınıyor. Mevcut web uygulaması kökte korunur; aynı isimli kök `app/` klasörüyle karışmaması için Android kaynakları `native-android/app/` altında tutulur.

- Paket adı: `com.lenstedreal.derbyarena`
- Sürüm: `1.0.5` (`versionCode=105`)
- Godot `android-build.yml` yerine artık root `.github/workflows/android-build.yml` native Gradle APK derlemesini yürütür.
- Debug APK çıktısı: `native-android/app/build/outputs/apk/debug/app-debug.apk`

## Termux'ta uygulama

```bash
cd ~/derbyarena97
unzip -o ~/storage/downloads/DerbyArena_Native_1.0.5_Integration.zip -d .
git diff --check
git status --short
```

Önizleme mantıklı görünüyorsa:

```bash
git add native-android .github/workflows/android-build.yml README_NATIVE_MIGRATION_TR.md
git commit -m "Migrate Derby Arena Android build to native Kotlin"
git push origin main
```

Ardından GitHub deposunda **Actions** sekmesine gir. Başarılı derleme, `DerbyArena-1.0.5-native-debug-apk` adlı artifact olarak APK'yı verir.

## Test dürüstlüğü
Bu arşiv yerel kaynak ve GLB varlık kontrollerinden geçirildi. Bu çalışma ortamında Android SDK/JDK bulunmadığından ve Gradle indirilemediğinden APK burada derlenemedi; başarılı APK yalnızca GitHub Actions derlemesi tamamlandığında doğrulanmış sayılır.
