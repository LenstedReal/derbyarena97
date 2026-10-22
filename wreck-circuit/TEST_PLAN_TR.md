# Test

Bu oturumda Godot 4.7.2.stable headless ile `tests/smoke.gd` koştu: menü açıldı, arena yüklendi, 80 fizik karesinde hız 7.91 m/s. Çıkış kodu 0. APK üretilmedi, telefonda denenmedi.

## Masaüstü, Godot 4.7.2

- [ ] Menü açılır, SAVAŞ arenaya gider
- [ ] Ayar kalite / hassasiyet / titreşim / nişan kaydı oyun kapanınca durur
- [ ] Ses anahtarı Master bus'ı kapatır, bip kesilir
- [ ] WASD aracı yürütür, duraklatma fiziği dondurur, DEVAM devam ettirir
- [ ] Ateş rakibe hasar yazar, aynı çarpışma çift hasar basmaz
- [ ] Ölünce veya 180 sn dolunca tek sonuç paneli
- [ ] `godot --headless --path . -s res://tests/smoke.gd` çıkış kodu 0

## Android

- [ ] 16:9, 19.5:9 veya 20:9 telefonda yatay tam ekran, butonlar çentiğin dışında
- [ ] İki parmak: gaz + direksiyon birlikte
- [ ] Geri tuşu duraklatır, ana menüde çıkar
- [ ] Uygulama arkadayken ses kesilir, dönünce kaldığı yerden sürer
- [ ] Düşük kalite gölgeyi kapatır
