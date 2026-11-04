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
        public const string Contact = "contact.istanbulrunner@proton.me";
        public const string Updated = "2026-11-04";

        public static void Open(string url)
        {
            try { Application.OpenURL(url); }
            catch (System.Exception ex) { Debug.LogWarning("DERBY: could not open " + url + ": " + ex.Message); }
        }

        /// <summary>Short privacy summary shown inside the game (the web page is the full text).</summary>
        public static string PrivacySummary()
        {
            return Loc.T(
                "DERBY ARENA hesap açtırmaz, ad, e-posta veya telefon istemez. İlerlemen ve ayarların yalnızca telefonunda saklanır.\n\n" +
                "REKLAMLAR: Oyun Google AdMob reklamları gösterir (bölüm sonlarında geçiş reklamı; ikinci şans, hurda ikramiyesi ve garajda isteğe bağlı ödüllü reklamlar). " +
                "Google reklam kimliğini, IP adresini ve cihaz bilgilerini reklam sunmak, ölçmek ve dolandırıcılığı önlemek için işleyebilir. " +
                "Avrupa Ekonomik Alanı, Birleşik Krallık ve İsviçre'de ilk açılışta rıza formu gösterilir; kararını Ayarlar → Reklam Gizlilik Seçenekleri'nden değiştirebilirsin. " +
                "Telefonunun ayarlarından reklam kimliğini sıfırlayabilirsin.\n\n" +
                "İNTERNET yalnızca reklamlar için kullanılır; internet olmadan oyun reklamsız çalışır. Oyun motoru (Unity) çökme ve donanım istatistiği (cihaz modeli, işletim sistemi, ekran) gönderebilir; kişisel kimlik içermez.\n\n" +
                "ÇOCUKLAR: Oyun 13 yaşından küçüklere yönelik değildir.\n\n" +
                "HAKLARIN: KVKK ve GDPR kapsamındaki bilgi alma, düzeltme, silme ve itiraz hakların için: " + Contact + "\n\n" +
                "Tam metin: " + PrivacyUrl,
                "DERBY ARENA has no accounts and never asks for a name, e-mail or phone number. Your progress and settings are stored only on your phone.\n\n" +
                "ADS: The game shows Google AdMob ads (an interstitial between some matches; optional rewarded ads for the second chance, the scrap bonus and in the garage). " +
                "Google may process your advertising ID, IP address and device information to serve and measure ads and to prevent fraud. " +
                "In the EEA, the UK and Switzerland a consent form is shown on first launch; you can change your choice in Settings → Ad Privacy Options. " +
                "You can reset your advertising ID in your phone's settings.\n\n" +
                "INTERNET is used only for ads; without a connection the game runs without ads. The game engine (Unity) may send crash and hardware statistics (device model, OS, screen) that do not identify you.\n\n" +
                "CHILDREN: The game is not directed at children under 13.\n\n" +
                "YOUR RIGHTS: for access, correction, deletion and objection requests (GDPR / KVKK): " + Contact + "\n\n" +
                "Full text: " + PrivacyUrl);
        }

        /// <summary>Credits and licences (offline copy of the web page).</summary>
        public static string LicensesSummary()
        {
            return Loc.T(
                "OYUN © LenstedReal. Araç, arena, seyirci ve nesne modelleri ile dokular LenstedReal için üretilmiş özel varlıklardır; ses efektleri oyun içinde sentezlenir, dışarıdan müzik kullanılmaz. " +
                "Tiger I (T-12) tarihî bir aracın oyun için üretilmiş modelidir; gerçek markalarla bağlantısı yoktur.\n\n" +
                "YAZI TİPİ: Poppins — Indian Type Foundry, SIL Open Font License 1.1 (lisans metni oyun dosyalarında).\n\n" +
                "KULLANILAN YAZILIMLAR\n" +
                "• Unity Engine — Unity Technologies (Unity Yazılım Ek Koşulları)\n" +
                "• Unity Input System, uGUI, External Dependency Manager — Unity Companion License\n" +
                "• Google Mobile Ads SDK ve Unity eklentisi, User Messaging Platform — Google LLC, Apache License 2.0 / Google API Terms\n" +
                "• AndroidX kütüphaneleri — Apache License 2.0\n\n" +
                "MARKALAR: Google Play, Android ve AdMob, Google LLC'nin markalarıdır. Bu oyun Google tarafından üretilmemiş veya onaylanmamıştır. Araç ve arena tasarımları hayalidir; gerçek markalarla bağlantısı yoktur.\n\n" +
                "Tam metin: " + LicensesUrl,
                "GAME © LenstedReal. Vehicle, arena, crowd and prop models and textures are custom assets made for LenstedReal; sound effects are synthesised in-game, no third-party music is used. " +
                "The Tiger I (T-12) is a game model of a historical vehicle with no connection to any brand.\n\n" +
                "FONT: Poppins — Indian Type Foundry, SIL Open Font License 1.1 (licence text ships with the game).\n\n" +
                "SOFTWARE USED\n" +
                "• Unity Engine — Unity Technologies (Unity Software Additional Terms)\n" +
                "• Unity Input System, uGUI, External Dependency Manager — Unity Companion License\n" +
                "• Google Mobile Ads SDK and Unity plugin, User Messaging Platform — Google LLC, Apache License 2.0 / Google API Terms\n" +
                "• AndroidX libraries — Apache License 2.0\n\n" +
                "TRADEMARKS: Google Play, Android and AdMob are trademarks of Google LLC. This game is not made or endorsed by Google. Vehicle and arena designs are fictional and unrelated to real brands.\n\n" +
                "Full text: " + LicensesUrl);
        }

        public static string TermsSummary()
        {
            return Loc.T(
                "Oyunu kişisel eğlence için kullanabilirsin. Hile, kodu değiştirme ve reklam sistemini kötüye kullanma yasaktır. " +
                "Ödüllü reklam karşılığı verilen hurda ve ikinci şans oyun içi değerlerdir, gerçek para karşılığı yoktur. " +
                "Oyun \"olduğu gibi\" sunulur; özellikler güncellemelerle değişebilir.\n\nTam metin: " + TermsUrl,
                "You may use the game for personal entertainment. Cheating, modifying the code and abusing the ad system are not allowed. " +
                "Scrap and second chances granted for rewarded ads are in-game values with no real-money worth. " +
                "The game is provided \"as is\"; features may change with updates.\n\nFull text: " + TermsUrl);
        }
    }
}
