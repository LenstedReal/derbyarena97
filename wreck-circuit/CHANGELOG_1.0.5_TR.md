# Derby Arena 1.0.5 — kaynak paketi güncelleme notları

## Kontroller ve sürüş

- Mobilde ayrı GAS düğmesi kaldırıldı; otomatik gaz sürüşe odaklanmayı kolaylaştırır.
- Sol direksiyon alanı, oto nişanlı ateş, FREN/GERİ, NİTRO ve DRİFT kontrolleri sadeleştirildi.
- Masaüstünde ekrandaki düğmelere fareyle basıldığında kontrol girdisi araç fiziğine aktarılır.
- Direksiyon girişleri yumuşatıldı; yüksek hızda aşırı dönmeyi azaltan hız kararlılığı eklendi.
- Nitronun düğme kenarıyla tetiklenmesi, basılı tutarak soğuma sonrası tekrar tekrar tetiklenmesini önler.
- Duraklatma/sonuç ekranı ve uygulama odağı değişiminde takılı kalan girdiler temizlenir.

## Araç/arena/çatışma

- Menüde vaat edilen dört rakip için dördüncü AI aracı arenaya eklendi; smoke test rakip sayısını doğrular.
- GLB araç gövdesinin yere oturuşuyla daha iyi eşleşmesi için varsayılan çarpışma kutusu daraltılıp aşağı hizalandı.

- Gerçek kolizeyum ve kalabalık GLB'leri sahneye entegre edildi; seyirci modeli arena ile aynı ölçeğe hizalandı.
- Araç modelindeki isim son ekli Wheel_* pivotları tanınır ve hızla döndürülür.
- Araç çarpışma gövdesi, model ölçeğine daha yakın hale getirildi.
- Ölen araç enkaz olarak kalır ama çarpışma hasarı dağıtmaya devam etmez.
- Atış havuzu 8'den 16'ya çıkarıldı; hızlı mermiler için kare başına raycast çarpışma kontrolü korunur.
- Menü/README mobil kontrolleri doğru anlatacak şekilde güncellendi.

## Paket kimliği ve CI

- Ad: Derby Arena
- Sürüm: 1.0.5 / versionCode 105
- Application ID: `com.lenstedreal.derbyarena`
- Android hedefi: arm64-v8a
- GitHub Actions iş akışı: `.github/workflows/android-build.yml`, manuel `workflow_dispatch` ve `main` push tetikleyicisi.

Bu notlar kaynak kodu değişikliklerini listeler; Godot derlemesi ve gerçek telefonda doğrulama ilk CI ve cihaz testinden sonra yapılmalıdır.
