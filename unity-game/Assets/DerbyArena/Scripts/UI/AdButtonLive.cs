using System;
using DerbyArena.Ads;
using DerbyArena.Core;
using UnityEngine;

namespace DerbyArena.UI
{
    /// <summary>
    /// Keeps a rewarded-ad button honest while it is on screen (4.0.2). In 4.0.1 the garage and result buttons were
    /// built once: if the ad finished loading a second later the button still said "ad loading…" and stayed dead.
    /// This polls the ads manager a few times a second, asks it for an ad when none is coming, and switches the
    /// caption, colour and tap state as the ad becomes available or goes away.
    /// </summary>
    public sealed class AdButtonLive : MonoBehaviour
    {
        public UiButton Button;
        /// <summary>Caption while an ad can be shown.</summary>
        public Func<string> ReadyCaption;
        /// <summary>Extra condition (daily limit, reward already doubled…); false shows <see cref="BlockedCaption"/>.</summary>
        public Func<bool> Allowed;
        public string BlockedCaption = "";
        public Color ReadyColor = new Color(0.2f, 0.45f, 1f, 0.95f);
        public Color WaitColor = new Color(0.25f, 0.25f, 0.27f, 0.9f);
        /// <summary>Set while an ad is showing or after it was used: the component stops touching the button.</summary>
        public bool Frozen;
        /// <summary>Leave the button as it is until this time (unscaled), e.g. to keep "ad not completed" readable.</summary>
        public float HoldUntil;

        float next;
        bool requested;

        void Update()
        {
            if (Frozen || Button == null || Time.unscaledTime < next || Time.unscaledTime < HoldUntil) return;
            next = Time.unscaledTime + 0.3f;
            var ads = AdsManager.Instance;
            bool allowed = Allowed == null || Allowed();
            bool ready = ads != null && ads.RewardedReady;
            if (allowed && !ready && ads != null && !requested) { ads.RequestRewarded(); requested = true; }
            if (ready) requested = false;

            string caption;
            Color color;
            if (!allowed) { caption = BlockedCaption; color = WaitColor; }
            else if (ready) { caption = ReadyCaption != null ? ReadyCaption() : ""; color = ReadyColor; }
            else
            {
                // the ad service gave up (no plugin, no consent, start error): say so instead of "starting…" forever
                bool unavailable = ads == null || (ads.ConsentDone && !ads.Initialized && (ads.Status == "no consent" || ads.Status == "no plugin" || ads.Status == "error"));
                caption = unavailable ? Loc.T("REKLAM ŞU AN KULLANILAMIYOR", "ADS UNAVAILABLE RIGHT NOW")
                    : !ads.Initialized ? Loc.T("REKLAM SERVİSİ BAŞLIYOR…", "AD SERVICE STARTING…")
                    : Loc.T("REKLAM HAZIRLANIYOR…", "AD LOADING…");
                color = WaitColor;
            }
            bool interactable = allowed && ready;
            if (Button.Caption != null && Button.Caption.text != caption) Button.Caption.text = caption;
            if (Button.BaseColor != color) Button.SetColor(color);
            Button.Interactable = interactable;
        }
    }
}
