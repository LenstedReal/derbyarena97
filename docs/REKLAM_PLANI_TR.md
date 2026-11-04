# DERBY ARENA — Reklam planı (AdMob)

Şu an oyunda **Google'ın resmi test kimlikleri** var. Test reklamları "Test Ad" etiketiyle gelir; tıklamak hesabına zarar vermez.
Gerçek kimlikleri koymadan önce yayına çıkma.

## AdMob'da açman gereken 2 reklam birimi
| Birim | Tür | Nerede çıkıyor | Kural |
|---|---|---|---|
| `derby_interstitial` | **Geçiş (Interstitial)** | Maç sonu ekranından menüye / sonraki bölüme / tekrar oynaya geçerken | İlk 2 bölümde yok · en fazla 3 maçta 1 · iki tam ekran reklam arası en az 120 sn |
| `derby_rewarded` | **Ödüllü (Rewarded)** | 1) **İkinci şans**: araç patlayınca 8 sn içinde "REKLAM İZLE · DEVAM ET" → %60 can + kısa kalkan (maç başına 1) · 2) **Hurdayı 2 katına çıkar** (sonuç ekranı, maç başına 1) · 3) **Garaj: +150 hurda** (günde 3) | Yalnızca oyuncu isterse açılır |

Banner ve uygulama açılış reklamı **koymadık**: dar ekranda kontrolleri kapatıyor, puanı düşürür. Gelirin büyük kısmı ödüllüden gelir.

## Gerçek kimliklere geçiş (sırayla)
1. AdMob → Uygulamalar → Uygulama ekle → Android, Google Play'de yayında → `com.lenstedreal.derbyarena`. **Uygulama kimliği** (`ca-app-pub-XXXX~YYYY`) alırsın.
2. Uygulamada yukarıdaki 2 birimi oluştur, **birim kimliklerini** (`ca-app-pub-XXXX/ZZZZ`) kopyala.
3. `unity-game/Assets/DerbyArena/Ads/AdsConfig.cs`:
   - `AndroidInterstitial` ve `AndroidRewarded` → gerçek birim kimlikleri
   - `TestMode = true` → **`false`**
4. `unity-game/Assets/GoogleMobileAds/Resources/GoogleMobileAdsSettings.asset` → `adMobAndroidAppId:` satırına gerçek **uygulama** kimliği. (Unity'de Assets → Google Mobile Ads → Settings menüsünden de değişir.)
5. AdMob → Gizlilik ve mesajlaşma → **GDPR (Avrupa) mesajı** oluştur, uygulamaya bağla, yayınla. Bu yapılmazsa rıza formu çıkmaz ve AEA'da reklam gelmez. İsteğe bağlı: ABD eyalet mesajı.
6. **app-ads.txt**: AdMob'un verdiği satırı (`google.com, pub-XXXX, DIRECT, f08c47fec0942fa0`) Play Console'daki geliştirici web sitenin **kök adresine** koy (ör. `https://lenstedreal.github.io/app-ads.txt` → bunun için `lenstedreal.github.io` adlı ayrı bir repo gerekir) ve Play'de web sitesi olarak o adresi yaz.
7. Play Console → Uygulama içeriği → **Reklamlar: Evet, reklam içeriyor**; Veri güvenliği formu: Reklam kimliği / Cihaz kimlikleri, Uygulama etkileşimleri, Çökme günlükleri, Tanılama → "Reklam veya pazarlama", "Analiz", "Dolandırıcılığı önleme"; aktarım şifreli; kullanıcı silme isteyebilir.
8. Kendi telefonunda test ederken kendi reklamına **tıklama**. Logcat'te çıkan "test device" kimliğini `AdsConfig.UmpTestDeviceHashedIds` içine ekleyebilirsin.

## Beklenti (tahmin değil, yön)
Ödüllü reklam: izleme oranı yüksek oyunlarda günlük aktif oyuncu başına 1–3 izleme olur. Geçiş reklamını sıklaştırmak kısa vadede para, uzun vadede oyuncu kaybı demek; ayarları ilk haftanın verisine bakıp `AdsConfig` içinden değiştir.
