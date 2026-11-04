namespace DerbyArena.Ads
{
    /// <summary>
    /// Every ad identifier and placement rule of the game in one place.
    ///
    /// Release checklist (see docs/REKLAM_PLANI_TR.md):
    ///   1. Create the app and the ad units in AdMob.
    ///   2. Replace the three Android IDs below (and the iOS ones if iOS ships).
    ///   3. Put the Android app ID into Assets/GoogleMobileAds/Resources/GoogleMobileAdsSettings.asset
    ///      (or Assets > Google Mobile Ads > Settings in the Editor).
    ///   4. Set TestMode = false.
    /// Until then the Google-provided test units are used: they always fill and never pay, so a
    /// test build can click and watch ads without risking the AdMob account.
    /// </summary>
    public static class AdsConfig
    {
        /// <summary>true: Google test units + test request configuration. Must be false in the Play release.</summary>
        public const bool TestMode = true;

        // Google test units (public, documented by Google). Replace with your own for the release.
        public const string AndroidInterstitial = "ca-app-pub-3940256099942544/1033173712";
        public const string AndroidRewarded = "ca-app-pub-3940256099942544/5224354917";
        public const string IosInterstitial = "ca-app-pub-3940256099942544/4411468910";
        public const string IosRewarded = "ca-app-pub-3940256099942544/1712485313";

        /// <summary>Google test app ID (the real one lives in GoogleMobileAdsSettings.asset; this is only shown in diagnostics).</summary>
        public const string TestAndroidAppId = "ca-app-pub-3940256099942544~3347511713";

        // ------------------------------------------------------------------ placement rules
        /// <summary>No interstitial before this level has been completed (first impressions are ad free).</summary>
        public const int InterstitialFreeLevels = 2;
        /// <summary>At most one interstitial every N finished matches...</summary>
        public const int InterstitialEveryMatches = 3;
        /// <summary>...and never within this many seconds of the previous interstitial or rewarded ad.</summary>
        public const float InterstitialMinGapSeconds = 120f;
        /// <summary>Rewarded scrap ads per calendar day in the garage.</summary>
        public const int RewardedScrapPerDay = 3;
        /// <summary>Scrap given per rewarded ad in the garage.</summary>
        public const int RewardedScrapAmount = 150;
        /// <summary>Seconds the second-chance offer stays open after the player's car is wrecked.</summary>
        public const float ReviveWindowSeconds = 8f;
        /// <summary>Health fraction restored by a second chance.</summary>
        public const float ReviveHealthFraction = 0.6f;

        /// <summary>UMP debug geography for EEA testing needs the device's hashed ID from logcat ("Use new ConsentDebugSettings.Builder().addTestDeviceHashedId(...)").</summary>
        public static readonly string[] UmpTestDeviceHashedIds = { };
    }
}
