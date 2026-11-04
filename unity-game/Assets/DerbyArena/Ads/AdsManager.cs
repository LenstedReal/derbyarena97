using System;
using UnityEngine;
#if DERBY_ADS
using GoogleMobileAds.Api;
using GoogleMobileAds.Ump.Api;
#endif

namespace DerbyArena.Ads
{
    /// <summary>
    /// Google Mobile Ads (AdMob) for DERBY ARENA: consent (UMP), one interstitial and one rewarded
    /// unit, preloaded and reloaded after every show. Lives in its own assembly so the game code
    /// never references the plugin directly: when the package is missing the DERBY_ADS symbol is not
    /// defined and every call here degrades to "no ad available" (callbacks still fire).
    ///
    /// Placement rules (frequency caps, free levels, daily limits) are in AdsConfig; the game asks
    /// through MaybeShowInterstitial / ShowRewarded and never talks to the SDK itself.
    /// </summary>
    public sealed class AdsManager : MonoBehaviour
    {
        public static AdsManager Instance { get; private set; }

        /// <summary>SDK initialised and allowed to request ads (consent given or not required).</summary>
        public bool Initialized { get; private set; }
        /// <summary>Consent flow finished (form shown if required). Ads may still be blocked.</summary>
        public bool ConsentDone { get; private set; }
        /// <summary>The region requires a "privacy options" entry point in the settings (EEA/UK/CH).</summary>
        public bool PrivacyOptionsRequired { get; private set; }
        /// <summary>Short status for the diagnostics overlay / settings.</summary>
        public string Status { get; private set; } = "idle";

        public bool InterstitialReady { get { return interstitialLoaded; } }
        public bool RewardedReady { get { return rewardedLoaded; } }
        public bool Available { get { return Initialized; } }

        int matchesSinceInterstitial;
        float lastFullscreenAdTime = -10000f;
        bool interstitialLoaded, rewardedLoaded;
        bool showing;
        bool audioWasPaused;
        Action pendingClosed;
        Action<bool> pendingReward;
        bool rewardEarned;

#if DERBY_ADS
        InterstitialAd interstitial;
        RewardedAd rewarded;
        float interstitialRetryAt, rewardedRetryAt;
        bool interstitialLoading, rewardedLoading;
#endif

        /// <summary>Creates the singleton (DontDestroyOnLoad) and starts consent + initialisation once.</summary>
        public static AdsManager Ensure()
        {
            if (Instance != null) return Instance;
            var go = new GameObject("AdsManager");
            DontDestroyOnLoad(go);
            Instance = go.AddComponent<AdsManager>();
            Instance.Begin();
            return Instance;
        }

        void Begin()
        {
#if DERBY_ADS
            try
            {
                MobileAds.RaiseAdEventsOnUnityMainThread = true;
                var request = new ConsentRequestParameters();
                request.TagForUnderAgeOfConsent = false;
                if (AdsConfig.TestMode && AdsConfig.UmpTestDeviceHashedIds.Length > 0)
                {
                    request.ConsentDebugSettings.DebugGeography = DebugGeography.EEA;
                    foreach (var id in AdsConfig.UmpTestDeviceHashedIds) request.ConsentDebugSettings.TestDeviceHashedIds.Add(id);
                }
                Status = "consent";
                // Ads may be requested while the consent information updates (returning users).
                if (ConsentInformation.CanRequestAds()) InitializeSdk();
                ConsentInformation.Update(request, error =>
                {
                    if (error != null)
                    {
                        Debug.LogWarning("[ads] consent update failed: " + error.Message);
                        FinishConsent();
                        return;
                    }
                    ConsentForm.LoadAndShowConsentFormIfRequired(formError =>
                    {
                        if (formError != null) Debug.LogWarning("[ads] consent form: " + formError.Message);
                        FinishConsent();
                    });
                });
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[ads] not started: " + ex.Message);
                Status = "error";
                ConsentDone = true;
            }
#else
            Status = "no plugin";
            ConsentDone = true;
#endif
        }

#if DERBY_ADS
        void FinishConsent()
        {
            ConsentDone = true;
            try { PrivacyOptionsRequired = ConsentInformation.PrivacyOptionsRequirementStatus == PrivacyOptionsRequirementStatus.Required; }
            catch (Exception) { PrivacyOptionsRequired = false; }
            if (ConsentInformation.CanRequestAds()) InitializeSdk();
            else Status = "no consent";
        }

        void InitializeSdk()
        {
            if (Initialized || showingInit) return;
            showingInit = true;
            Status = "init";
            try
            {
                // 13+ audience: never request mature ads.
                var cfg = new RequestConfiguration();
                cfg.MaxAdContentRating = MaxAdContentRating.T;
                MobileAds.SetRequestConfiguration(cfg);
                MobileAds.Initialize(status =>
                {
                    Initialized = true;
                    Status = "ready";
                    LoadInterstitial();
                    LoadRewarded();
                });
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[ads] init failed: " + ex.Message);
                Status = "error";
                showingInit = false;
            }
        }
        bool showingInit;

        static string InterstitialId
        {
            get
            {
#if UNITY_IOS
                return AdsConfig.IosInterstitial;
#else
                return AdsConfig.AndroidInterstitial;
#endif
            }
        }

        static string RewardedId
        {
            get
            {
#if UNITY_IOS
                return AdsConfig.IosRewarded;
#else
                return AdsConfig.AndroidRewarded;
#endif
            }
        }

        void LoadInterstitial()
        {
            if (!Initialized || interstitialLoading || interstitialLoaded) return;
            interstitialLoading = true;
            if (interstitial != null) { interstitial.Destroy(); interstitial = null; }
            InterstitialAd.Load(InterstitialId, new AdRequest(), (ad, error) =>
            {
                interstitialLoading = false;
                if (error != null || ad == null)
                {
                    Debug.LogWarning("[ads] interstitial load failed: " + (error != null ? error.GetMessage() : "null"));
                    interstitialRetryAt = Time.unscaledTime + 30f;
                    return;
                }
                interstitial = ad;
                interstitialLoaded = true;
                ad.OnAdFullScreenContentOpened += OnFullscreenOpened;
                ad.OnAdFullScreenContentClosed += () => { interstitialLoaded = false; OnFullscreenClosed(); LoadInterstitial(); };
                ad.OnAdFullScreenContentFailed += err => { interstitialLoaded = false; OnFullscreenClosed(); LoadInterstitial(); };
            });
        }

        void LoadRewarded()
        {
            if (!Initialized || rewardedLoading || rewardedLoaded) return;
            rewardedLoading = true;
            if (rewarded != null) { rewarded.Destroy(); rewarded = null; }
            RewardedAd.Load(RewardedId, new AdRequest(), (ad, error) =>
            {
                rewardedLoading = false;
                if (error != null || ad == null)
                {
                    Debug.LogWarning("[ads] rewarded load failed: " + (error != null ? error.GetMessage() : "null"));
                    rewardedRetryAt = Time.unscaledTime + 30f;
                    return;
                }
                rewarded = ad;
                rewardedLoaded = true;
                ad.OnAdFullScreenContentOpened += OnFullscreenOpened;
                ad.OnAdFullScreenContentClosed += () => { rewardedLoaded = false; OnFullscreenClosed(); LoadRewarded(); };
                ad.OnAdFullScreenContentFailed += err => { rewardedLoaded = false; OnFullscreenClosed(); LoadRewarded(); };
            });
        }

        void Update()
        {
            if (!Initialized) return;
            if (!interstitialLoaded && !interstitialLoading && Time.unscaledTime >= interstitialRetryAt) LoadInterstitial();
            if (!rewardedLoaded && !rewardedLoading && Time.unscaledTime >= rewardedRetryAt) LoadRewarded();
        }
#endif

        void OnFullscreenOpened()
        {
            showing = true;
            audioWasPaused = AudioListener.pause;
            AudioListener.pause = true;
        }

        void OnFullscreenClosed()
        {
            // a failed show calls this without a matching "opened": only restore audio we actually paused
            if (showing) AudioListener.pause = audioWasPaused;
            showing = false;
            lastFullscreenAdTime = Time.unscaledTime;
            // On some devices the reward callback arrives just after the close event: wait a moment
            // (real time, the game may be paused) before telling the game whether the reward was earned.
            StartCoroutine(ResolveAfterClose());
        }

        System.Collections.IEnumerator ResolveAfterClose()
        {
            // up to 1 s for the reward callback; resolve at once if it is already in
            float waited = 0f;
            while (!rewardEarned && pendingReward != null && waited < 1f) { waited += Time.unscaledDeltaTime; yield return null; }
            var closed = pendingClosed; pendingClosed = null;
            var reward = pendingReward; pendingReward = null;
            bool earned = rewardEarned; rewardEarned = false;
            if (closed != null) closed();
            if (reward != null) reward(earned);
        }

        // ------------------------------------------------------------------ game-facing API

        /// <summary>Call once per finished match. Returns true when the next transition should show an interstitial.</summary>
        public bool MatchFinished(int levelNumber)
        {
            matchesSinceInterstitial++;
            if (!Initialized || !interstitialLoaded) return false;
            if (levelNumber <= AdsConfig.InterstitialFreeLevels) return false;
            if (matchesSinceInterstitial < AdsConfig.InterstitialEveryMatches) return false;
            if (Time.unscaledTime - lastFullscreenAdTime < AdsConfig.InterstitialMinGapSeconds) return false;
            return true;
        }

        /// <summary>Shows the interstitial if one is loaded, then calls <paramref name="closed"/> (immediately when nothing can be shown).</summary>
        public void ShowInterstitial(Action closed)
        {
#if DERBY_ADS
            if (!showing && interstitialLoaded && interstitial != null && interstitial.CanShowAd())
            {
                matchesSinceInterstitial = 0;
                pendingClosed = closed;
                try { interstitial.Show(); return; }
                catch (Exception ex)
                {
                    Debug.LogWarning("[ads] interstitial show failed: " + ex.Message);
                    pendingClosed = null;
                    interstitialLoaded = false;
                }
            }
#endif
            if (closed != null) closed();
        }

        /// <summary>Shows a rewarded ad; <paramref name="result"/> gets true only when the reward was earned.</summary>
        public void ShowRewarded(Action<bool> result)
        {
#if DERBY_ADS
            if (!showing && rewardedLoaded && rewarded != null && rewarded.CanShowAd())
            {
                pendingReward = result;
                rewardEarned = false;
                try
                {
                    rewarded.Show(reward => { rewardEarned = true; });
                    return;
                }
                catch (Exception ex)
                {
                    Debug.LogWarning("[ads] rewarded show failed: " + ex.Message);
                    pendingReward = null;
                    rewardedLoaded = false;
                }
            }
#endif
            if (result != null) result(false);
        }

        /// <summary>Opens the UMP privacy options form (EEA/UK/CH). Calls back with an error message or null.</summary>
        public void ShowPrivacyOptions(Action<string> done)
        {
#if DERBY_ADS
            try
            {
                ConsentForm.ShowPrivacyOptionsForm(error =>
                {
                    try { PrivacyOptionsRequired = ConsentInformation.PrivacyOptionsRequirementStatus == PrivacyOptionsRequirementStatus.Required; }
                    catch (Exception) { }
                    if (done != null) done(error != null ? error.Message : null);
                });
                return;
            }
            catch (Exception ex)
            {
                if (done != null) done(ex.Message);
                return;
            }
#else
            if (done != null) done("no plugin");
#endif
        }

        void OnDestroy()
        {
#if DERBY_ADS
            if (interstitial != null) interstitial.Destroy();
            if (rewarded != null) rewarded.Destroy();
#endif
            if (Instance == this) Instance = null;
        }
    }
}
