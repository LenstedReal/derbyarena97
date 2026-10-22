# Wreck Circuit

Tek oyunculu, çevrimdışı, yatay tam ekran araba savaş MVP'si. Godot 4.7.2 Stable, Mobile renderer, GDScript.

Kolezyum düzeni yalnızca esindir. Unity videosundan mesh, doku veya kod kopyalanmadı. Geometri bu projede bloklarla kuruldu.

## Telefonda dene

1. Bilgisayarda Godot 4.7.2 aç, bu klasörü içe aktar.
2. Export Templates 4.7.2 kur.
3. Project > Export > Android > Export With Debug. Çıktı `build/wreck-circuit-debug.apk`.
4. APK'yı telefona kur. Oyun sensör yatay ve tam ekran açılır. Çentik payı HUD'u içeri iter.
5. Alternatif: bu klasörü GitHub'a push et, Actions'tan `wreck-circuit-debug-apk` indir. Bu ortamda APK üretilmedi.

Paket adı: `com.lenstedreal.wreckcircuit`. Yalnızca arm64-v8a. Gradle kapalı. İmza debug keystore. Keystore repoda yok.

## Kontroller

Sol alt joystick direksiyon. GAZ, FREN, EL FREN, BOOST, ATEŞ. Sağ üst sürükleme nişan. DURAKLAT veya geri tuşu menüyü açar. Masaüstünde WASD, F ateş, Space boost, Shift el freni, Esc duraklat.

Kalite: Düşük 0.7 ölçek gölgesiz, Orta 0.85, Yüksek 1.0 ve 2x MSAA. Ayar `user://settings.cfg` içine yazılır.
