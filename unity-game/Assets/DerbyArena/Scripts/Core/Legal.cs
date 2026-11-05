using UnityEngine;

namespace DerbyArena.Core
{
    /// <summary>
    /// Legal texts and links. The full privacy policy, terms and licence list are published on the
    /// project's GitHub Pages (docs/ folder of the repository); the in-game pages show a short,
    /// offline-readable version and open the web page for the full text.
    /// </summary>
    public static class Legal
    {
        public const string BaseUrl = "https://lenstedreal.github.io/derbyarena97";
        public const string PrivacyUrl = BaseUrl + "/privacy.html";
        public const string TermsUrl = BaseUrl + "/terms.html";
        public const string LicensesUrl = BaseUrl + "/licenses.html";
        /// <summary>
        /// 4.0.2: no e-mail address yet (the one in 4.0.x belonged to the old game and was removed). Until the new
        /// address is published, contact goes through the developer contact on the Google Play store page.
        /// </summary>
        public static string Contact { get { return Loc.T("Google Play mağaza sayfasındaki geliştirici iletişim bilgisi (e-posta yakında)", "the developer contact on the Google Play store page (e-mail coming soon)"); } }
        public const string Developer = "LenstedReal";
        public const string Updated = "2026-11-05";

        public static void Open(string url)
        {
            try { Application.OpenURL(url); }
            catch (System.Exception ex) { Debug.LogWarning("DERBY: could not open " + url + ": " + ex.Message); }
        }

        /// <summary>Short privacy summary shown inside the game (the web page is the full text).</summary>
        public static string PrivacySummary()
        {
            return Loc.T(
                "DERBY ARENA (LenstedReal) hesap açtırmaz, ad, e-posta veya telefon istemez; geliştiriciye hiçbir kişisel veri gönderilmez. İlerlemen ve ayarların yalnızca telefonunda saklanır ve Android yedeğine (Google hesabı) alınmaz; uygulamayı kaldırdığında hepsi silinir. " +
                "Eğim direksiyonu seçersen ivmeölçer yalnızca o anda okunur; bu veri cihazdan çıkmaz ve kaydedilmez.\n\n" +
                "REKLAMLAR: Oyun Google AdMob reklamları gösterir (bölüm sonlarında geçiş reklamı; ikinci şans, hurda ikramiyesi ve garajda isteğe bağlı ödüllü reklamlar). " +
                "Google reklam kimliğini, IP adresini ve cihaz bilgilerini reklam sunmak, ölçmek ve dolandırıcılığı önlemek için işleyebilir. " +
                "Avrupa Ekonomik Alanı, Birleşik Krallık ve İsviçre'de ilk açılışta rıza formu gösterilir; kararını Ayarlar → Reklam Gizlilik Seçenekleri'nden değiştirebilirsin. " +
                "Telefonunun ayarlarından reklam kimliğini sıfırlayabilirsin.\n\n" +
                "İNTERNET yalnızca reklamlar için kullanılır; internet olmadan oyun reklamsız çalışır. Oyun motoru (Unity) çökme ve donanım istatistiği (cihaz modeli, işletim sistemi, ekran) gönderebilir; kişisel kimlik içermez.\n\n" +
                "ÇOCUKLAR: Oyun 13 yaşından küçüklere yönelik değildir; reklamlar en fazla \"T\" (genç) düzeyindedir.\n\n" +
                "GELECEK: İlerlemeyi korumak için isteğe bağlı Google ile giriş eklenirse, işlenecek veriler önceden bu politikada açıklanacaktır.\n\n" +
                "HAKLARIN: KVKK ve GDPR kapsamındaki bilgi alma, düzeltme, silme ve itiraz hakların için: " + Contact + "\n\n" +
                "Tam metin: " + PrivacyUrl,
                "DERBY ARENA (LenstedReal) has no accounts and never asks for a name, e-mail or phone number; no personal data is sent to the developer. Your progress and settings are stored only on your phone and are not included in Android backups (Google account); uninstalling the app deletes all of them. " +
                "If you choose tilt steering, the accelerometer is read only while you play; that data never leaves the device and is not stored.\n\n" +
                "ADS: The game shows Google AdMob ads (an interstitial between some matches; optional rewarded ads for the second chance, the scrap bonus and in the garage). " +
                "Google may process your advertising ID, IP address and device information to serve and measure ads and to prevent fraud. " +
                "In the EEA, the UK and Switzerland a consent form is shown on first launch; you can change your choice in Settings → Ad Privacy Options. " +
                "You can reset your advertising ID in your phone's settings.\n\n" +
                "INTERNET is used only for ads; without a connection the game runs without ads. The game engine (Unity) may send crash and hardware statistics (device model, OS, screen) that do not identify you.\n\n" +
                "CHILDREN: The game is not directed at children under 13; ads are limited to the \"T\" (teen) rating.\n\n" +
                "FUTURE: If an optional Google sign-in is added to protect progress, the data it uses will be described in this policy first.\n\n" +
                "YOUR RIGHTS: for access, correction, deletion and objection requests (GDPR / KVKK): " + Contact + "\n\n" +
                "Full text: " + PrivacyUrl);
        }

        /// <summary>Credits and licences (offline copy of the web page).</summary>
        public static string LicensesSummary()
        {
            return Loc.T(
                "OYUN © LenstedReal. Araç, arena, seyirci ve nesne modelleri ile dokular LenstedReal için üretilmiş özel varlıklardır; ses efektleri oyun içinde sentezlenir, dışarıdan müzik kullanılmaz. " +
                "Kaplan T-12, Tiger I tankından esinlenen ve oyun için üretilmiş bir modeldir; gerçek bir marka veya üreticiyle bağlantısı yoktur.\n\n" +
                "YAZI TİPİ: Poppins — Indian Type Foundry, SIL Open Font License 1.1 (lisans metni oyun dosyalarında).\n\n" +
                "KULLANILAN YAZILIMLAR\n" +
                "• Unity Engine — Unity Technologies (Unity Yazılım Ek Koşulları)\n" +
                "• Unity Input System, uGUI, External Dependency Manager — Unity Companion License\n" +
                "• Google Mobile Ads SDK ve Unity eklentisi, User Messaging Platform — Google LLC, Apache License 2.0 / Google API Terms\n" +
                "• AndroidX kütüphaneleri — Apache License 2.0\n\n" +
                "MARKALAR: Google Play, Android ve AdMob, Google LLC'nin markalarıdır. Bu oyun Google tarafından üretilmemiş veya onaylanmamıştır. Araç ve arena adları ile tasarımları hayalidir; gerçek markalarla bağlantısı yoktur.\n\n" +
                "Tam metin: " + LicensesUrl,
                "GAME © LenstedReal. Vehicle, arena, crowd and prop models and textures are custom assets made for LenstedReal; sound effects are synthesised in-game, no third-party music is used. " +
                "The Tiger T-12 is a game model inspired by the Tiger I tank, with no connection to any brand or manufacturer.\n\n" +
                "FONT: Poppins — Indian Type Foundry, SIL Open Font License 1.1 (licence text ships with the game).\n\n" +
                "SOFTWARE USED\n" +
                "• Unity Engine — Unity Technologies (Unity Software Additional Terms)\n" +
                "• Unity Input System, uGUI, External Dependency Manager — Unity Companion License\n" +
                "• Google Mobile Ads SDK and Unity plugin, User Messaging Platform — Google LLC, Apache License 2.0 / Google API Terms\n" +
                "• AndroidX libraries — Apache License 2.0\n\n" +
                "TRADEMARKS: Google Play, Android and AdMob are trademarks of Google LLC. This game is not made or endorsed by Google. Vehicle and arena names and designs are fictional and unrelated to real brands.\n\n" +
                "Full text: " + LicensesUrl);
        }

        public static string TermsSummary()
        {
            return Loc.T(
                "Oyunu indirip oynayarak bu koşulları kabul edersin. Oyunu kişisel ve ticari olmayan eğlence için kullanabilirsin.\n\n" +
                "Hile, oyun kodunu veya dosyalarını değiştirme, tersine mühendislik ve reklam sistemini kötüye kullanma (sahte tıklama, otomatik izleme) yasaktır.\n\n" +
                "Hurda, yıldız, araçlar, parçalar ve ödüllü reklamla verilen ikinci şans yalnızca oyun içi değerlerdir; gerçek para karşılığı yoktur, satılamaz ve devredilemez.\n\n" +
                "Oyun \"olduğu gibi\" sunulur; özellikler, denge ve içerik güncellemelerle değişebilir. Reklamlar Google'ın kendi koşullarına tabidir.\n\nTam metin: " + TermsUrl,
                "By downloading and playing the game you accept these terms. You may use it for personal, non-commercial entertainment.\n\n" +
                "Cheating, modifying the game's code or files, reverse engineering and abusing the ad system (fake clicks, automated viewing) are not allowed.\n\n" +
                "Scrap, stars, vehicles, parts and second chances granted for rewarded ads are in-game values only; they have no real-money worth and cannot be sold or transferred.\n\n" +
                "The game is provided \"as is\"; features, balance and content may change with updates. Ads are subject to Google's own terms.\n\nFull text: " + TermsUrl);
        }
    }
}
