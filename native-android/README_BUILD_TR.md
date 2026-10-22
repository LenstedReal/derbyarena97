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

## BLACKOUT RUN — yeniden tasarlanan prototip
- Eski Roma kolezyumu görseli, terk edilmiş endüstriyel taşkın tüneli / BLACKOUT SPILLWAY arena modeliyle değiştirildi.
- 3 reaktör hücresi fiziksel olarak arena GLB'sine yerleştirildi; oyuncu hücrelerin yanından geçince toplanır.
- 4 dakikalık görev penceresi: 3 hücreyi güvenceye al, sonra kuzeydeki tahliye pedine ulaş. Yalnızca tüm düşmanları yok etmek artık zafer sağlamaz.
- Filament GPU render, takip kamerası, araç sürüşü, düşman AI, çarpışma/hasar, silahlar, ganimet ve efektler korunup yeni görev döngüsüne bağlandı.
- HUD, sıradaki hedefi, hücre ilerlemesini, tahliye kilidini, hedef mesafesini ve kalan süreyi gösterir.
- Java/Kotlin bytecode hedefleri JVM 17'de eşitlendi (Kotlin compilerOptions açıkça JVM_17).

## Test durumu
Yeni arena ve araç GLB dosyaları GLB 2.0 başlık/JSON kontrollerinden geçti; asset pipeline doğrulaması 0 sorun raporladı. ExtractionRunSystem saf Kotlin harness testi geçti. Bu çalışma ortamında Gradle dağıtımı indirilemediği için **Android APK derlemesi ve gerçek cihaz testi tamamlanmış değildir**. APK'nın hazır olduğu ancak CI'da derleme başarılı olduktan sonra söylenebilir.
