using System;
using FactoryKing.Core.Meta;
using FactoryKing.Core.Numerics;
using FactoryKing.Runtime.Audio;
using UnityEngine;
using UnityEngine.UIElements;

namespace FactoryKing.Runtime.UI
{
    /// <summary>Splash, main menu, consent and offline summary dialogs.</summary>
    public static class UiModals
    {
        public static void ShowOfflineSummary(this UiManager ui)
        {
            var C = ui.Context;
            var s = C.Session;
            var report = s.Offline.Pending;
            if (report == null) return;
            ui.ShowModal((box, close) =>
            {
                box.Add(UiKit.Label(C.T("offline.title"), 46, true, UiKit.Accent));
                if (report.ClockRollbackDetected)
                {
                    box.Add(UiKit.Label(C.T("offline.rollback"), 30, false, UiKit.Warn, true));
                    box.Add(UiKit.Button(C.T("ui.ok"), () => { s.Offline.Claim(); close(); }));
                    return;
                }
                box.Add(UiKit.Label(C.TF("offline.away", NumberFormatter.FormatDuration(report.SecondsAway)), 32));
                if (report.Capped)
                    box.Add(UiKit.Label(C.TF("offline.capped", NumberFormatter.FormatDuration(report.CapSeconds)), 28, false, UiKit.Warn, true));
                box.Add(UiKit.Label(C.TF("offline.sold", report.UnitsSold), 28, false, UiKit.Muted));
                foreach (var kv in report.ByFactory)
                    box.Add(UiKit.Label($"{C.T(s.Content.GetFactory(kv.Key).NameKey)}: {C.M(kv.Value)}", 28, false, UiKit.Muted));
                box.Add(UiKit.Spacer(10));
                var total = UiKit.Label("$ " + C.M(report.Earnings), 64, true, UiKit.Good);
                box.Add(total);

                var collect = UiKit.Button(C.T("offline.collect"), () =>
                {
                    s.Offline.Claim();
                    C.Audio?.Play(Sfx.Cash);
                    close();
                }, UiKit.Good, 38, 120);
                box.Add(collect);
                var doubleIt = UiKit.Button(s.Purchases.Owns(StoreCatalog.EntitlementNoAds) ? C.T("offline.double_free") : C.T("offline.double_ad"), () =>
                {
                    s.Ads.Request(AdPlacements.DoubleOffline, () =>
                    {
                        s.Offline.Claim(2);
                        C.Audio?.Play(Sfx.Cash);
                        close();
                    }, r => { if (r != AdResult.Completed) ui.Toast(C.T(r == AdResult.NotReady ? "ads.not_ready" : "ads.skipped")); });
                }, UiKit.Diamond, 34, 110);
                UiKit.SetEnabled(doubleIt, s.Ads.IsAvailable(AdPlacements.DoubleOffline));
                box.Add(doubleIt);
                // Count-up animation of the total.
                double target = report.Earnings.ToDouble();
                float start = Time.unscaledTime;
                total.schedule.Execute(() =>
                {
                    float k = Mathf.Clamp01((Time.unscaledTime - start) / 1.2f);
                    total.text = "$ " + C.M(BigDouble.FromDouble(target * (1 - Math.Pow(1 - k, 3))));
                }).Every(33).Until(() => Time.unscaledTime - start > 1.3f);
            }, dismissible: false);
        }

        public static void ShowSplash(this UiManager ui, Action done)
        {
            var C = ui.Context;
            ui.ShowModal((box, close) =>
            {
                box.style.backgroundColor = new Color(0, 0, 0, 0);
                box.style.alignItems = Align.Center;
                box.Add(UiKit.Label("♛", 180, true, UiKit.Accent));
                box.Add(UiKit.Label("FACTORY KING", 84, true));
                box.Add(UiKit.Label(C.T("game.title"), 40, false, UiKit.Muted));
                var bar = UiKit.ProgressBar(out var fill, UiKit.Accent, 20);
                bar.style.width = 600;
                box.Add(bar);
                float start = Time.unscaledTime;
                box.schedule.Execute(() =>
                {
                    float k = Mathf.Clamp01((Time.unscaledTime - start) / 1.4f);
                    UiKit.SetProgress(fill, k);
                    if (k >= 1f) { close(); done?.Invoke(); }
                }).Every(30).Until(() => Time.unscaledTime - start > 1.5f);
            }, dismissible: false);
        }

        public static void ShowMainMenu(this UiManager ui, Action play)
        {
            var C = ui.Context;
            ui.ShowModal((box, close) =>
            {
                box.style.alignItems = Align.Stretch;
                box.Add(UiKit.Label("♛ FACTORY KING", 72, true, UiKit.Accent));
                box.Add(UiKit.Label(C.T("menu.subtitle"), 32, false, UiKit.Muted, true));
                box.Add(UiKit.Spacer(20));
                bool fresh = C.Session.State.LifetimeEarnings.IsZero;
                box.Add(UiKit.Button(fresh ? C.T("menu.play") : C.T("menu.continue"), () => { close(); play?.Invoke(); }, UiKit.Accent, 44, 140));
                box.Add(UiKit.Button(C.T("screen.settings"), () => { close(); ui.Open("settings"); play?.Invoke(); }, UiKit.CardHi));
                box.Add(UiKit.Label(C.TF("ui.company_level", C.Session.CompanyLevel), 28, false, UiKit.Muted));
            }, dismissible: false);
        }

        /// <summary>Asked once, after the tutorial start. Declining changes nothing in gameplay.</summary>
        public static void ShowConsent(this UiManager ui)
        {
            var C = ui.Context;
            var st = C.Session.State.Settings;
            if (st.ConsentAsked) return;
            ui.ShowModal((box, close) =>
            {
                box.Add(UiKit.Label(C.T("consent.title"), 40, true));
                box.Add(UiKit.Label(C.T("consent.body"), 28, false, UiKit.Muted, true));
                box.Add(UiKit.Button(C.T("consent.yes"), () => { st.AnalyticsConsent = true; st.ConsentAsked = true; close(); }, UiKit.Good));
                box.Add(UiKit.Button(C.T("consent.no"), () => { st.AnalyticsConsent = false; st.ConsentAsked = true; close(); }, UiKit.CardHi));
                box.Add(UiKit.Button(C.T("settings.privacy"), () => Application.OpenURL(C.PrivacyPolicyUrl), UiKit.Card, 26, 80));
            }, dismissible: false);
        }
    }
}
