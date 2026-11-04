# Android başlatıcı ikonları

Build sırasında `DerbyAndroidIcons` (Editor) bu klasörü Unity'nin ürettiği Gradle projesine (`launcher/src/main/res`) kopyalar.

Kaynak: Derby Arena anahtar görseli (`Assets/DerbyArena/Icons/icon_legacy.png`'nin üretildiği 1024'lük sanat).

Uyarlanabilir ikon (Android 8+): katman 108 dp, başlatıcının gösterdiği alan ortadaki 72 dp, **güvenli bölge 66 dp çaplı daire**.
Görsel bu dairenin içine ölçeklenir (yazı ve araç her maske şeklinde tam görünür), kenarları arka plan katmanına (aynı görselin
bulanık, koyulaştırılmış hâli) yumuşakça karışır. 2.0 öncesinde görsel 72 dp alanı tam kaplıyordu; yuvarlak ve squircle
maskeler "DERBY" yazısının köşelerini kesiyordu.

Eski tip ikon (Android 7 ve altı): 48 dp, yuvarlatılmış köşe. Yuvarlak ikon: 48 dp daire.
Üretim betiği: `scripts/make_icons.py` (Pillow).
