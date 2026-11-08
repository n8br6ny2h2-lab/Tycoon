using System;
using FactoryKing.Core.Localization;
using FactoryKing.Core.Meta;
using FactoryKing.Core.Numerics;
using FactoryKing.Core.Simulation;
using FactoryKing.Runtime.Audio;
using FactoryKing.Runtime.Platform;
using UnityEngine;
using UnityEngine.UIElements;

namespace FactoryKing.Runtime.UI
{
    // ============================================================== Shop
    public sealed class ShopScreen : UiScreen
    {
        public override string Id => "shop";
        public override string TitleKey => "screen.shop";

        protected override void Build(VisualElement root)
        {
            var s = C.Session;
            Section(root, "shop.free");
            AdOffer(root, AdPlacements.Boost, "shop.ad.boost", () => s.Boosts.Activate(30));
            AdOffer(root, AdPlacements.FreeDiamonds, "shop.ad.diamonds", () => s.Wallet.AddDiamonds(5));
            AdOffer(root, AdPlacements.FreeCash, "shop.ad.cash", () => s.Wallet.Grant(s.IncomeReward(600)));

            Section(root, "shop.premium");
            foreach (var p in s.Purchases.Catalog.Products)
            {
                var product = p;
                var card = Card(root);
                var head = UiKit.Row(0, Justify.SpaceBetween);
                head.Add(UiKit.Label(T(product.NameKey), 36, true));
                if (product.Diamonds > 0) head.Add(UiKit.Label("◆ " + product.Diamonds, 34, true, UiKit.Diamond));
                card.Add(head);
                card.Add(UiKit.Label(T(product.DescriptionKey), 28, false, UiKit.Muted, true));
                bool owned = !s.Purchases.CanPurchase(product);
                var buy = UiKit.Button(owned ? T("shop.owned") : C.Store.PriceText(product), () => C.Store.Buy(product), UiKit.Good);
                UiKit.SetEnabled(buy, !owned);
                card.Add(buy);
            }

            root.Add(UiKit.Button(T("shop.restore"), () => { Click(); C.Store.Restore(); }, UiKit.CardHi));
            root.Add(UiKit.Label(T("shop.legal"), 24, false, UiKit.Muted, true));
            var links = UiKit.Row();
            links.Add(UiKit.Button(T("settings.privacy"), () => Application.OpenURL(C.PrivacyPolicyUrl), UiKit.Card, 26, 84));
            links.Add(UiKit.Button(T("settings.support"), () => Application.OpenURL(C.SupportUrl), UiKit.Card, 26, 84));
            root.Add(links);
        }

        private void AdOffer(VisualElement root, string placement, string key, Action reward)
        {
            var s = C.Session;
            var card = Card(root);
            card.Add(UiKit.Label(T(key), 32, true, null, true));
            card.Add(B.Text(UiKit.Label("", 26, false, UiKit.Muted), () => TF("shop.ad.remaining", s.Ads.RemainingToday(placement))));
            var b = UiKit.Button("", () =>
            {
                s.Ads.Request(placement, () => { reward(); C.Audio?.Play(Sfx.Success); }, r =>
                {
                    if (r == AdResult.NotReady) C.Ui.Toast(T("ads.not_ready"));
                    else if (r == AdResult.Skipped) C.Ui.Toast(T("ads.skipped"));
                    else if (r == AdResult.Failed) C.Ui.Toast(T("ads.failed"));
                    else if (r == AdResult.Completed) C.Ui.Toast(T("ads.thanks"), UiKit.Good);
                });
            }, UiKit.Diamond);
            B.Button(b, () =>
            {
                double cd = s.Ads.CooldownRemainingSeconds(placement);
                if (cd > 0) return TF("shop.ad.cooldown", NumberFormatter.FormatDuration(cd));
                return s.Purchases.Owns(StoreCatalog.EntitlementNoAds) ? T("shop.ad.claim_free") : T("shop.ad.watch");
            }, () => s.Ads.IsAvailable(placement));
            card.Add(b);
        }
    }

    // ============================================================== Settings
    public sealed class SettingsScreen : UiScreen
    {
        public override string Id => "settings";
        public override string TitleKey => "screen.settings";

        protected override void Build(VisualElement root)
        {
            var s = C.Session;
            var st = s.State.Settings;

            Section(root, "settings.sound");
            root.Add(Slider(T("settings.music"), st.MusicVolume, v => { st.MusicVolume = v; C.Audio?.SetVolumes(st.SfxVolume, st.MusicVolume); }));
            root.Add(Slider(T("settings.sfx"), st.SfxVolume, v => { st.SfxVolume = v; C.Audio?.SetVolumes(st.SfxVolume, st.MusicVolume); }));

            Section(root, "settings.language");
            var langs = UiKit.Row();
            var en = UiKit.Button("English", () => SetLanguage(Language.English), C.Loc.Current == Language.English ? UiKit.Accent : UiKit.CardHi);
            var tr = UiKit.Button("Türkçe", () => SetLanguage(Language.Turkish), C.Loc.Current == Language.Turkish ? UiKit.Accent : UiKit.CardHi);
            en.style.flexGrow = 1; tr.style.flexGrow = 1;
            langs.Add(en); langs.Add(tr);
            root.Add(langs);

            Section(root, "settings.privacy_title");
            root.Add(Toggle(T("settings.notifications"), st.Notifications, v => C.Notifications?.SetEnabled(v)));
            root.Add(Toggle(T("settings.analytics"), st.AnalyticsConsent, v => { st.AnalyticsConsent = v; st.ConsentAsked = true; }));
            root.Add(UiKit.Label(T("settings.analytics_note"), 24, false, UiKit.Muted, true));
            root.Add(Toggle(T("settings.quality"), st.HighQuality, v => { st.HighQuality = v; QualityProfile.Apply(v); }));
            root.Add(Toggle(T("settings.haptics"), st.Haptics, v => st.Haptics = v));
            root.Add(Toggle(T("settings.battery"), st.BatterySaver, v => st.BatterySaver = v));
            root.Add(UiKit.Label(T("settings.battery_note"), 24, false, UiKit.Muted, true));

            Section(root, "settings.company");
            var nameRow = UiKit.Row();
            var nameField = new TextField { maxLength = CompanyNameRules.MaxLength, value = s.State.CompanyName ?? "" };
            nameField.style.fontSize = 32;
            nameField.style.flexGrow = 1;
            nameField.style.minHeight = 90;
            nameRow.Add(nameField);
            nameRow.Add(UiKit.Button(T("settings.rename"), () =>
            {
                string n = CompanyNameRules.Sanitize(nameField.value);
                if (n == null) { C.Ui.Toast(T("onboard.name_invalid"), UiKit.Bad); return; }
                s.State.CompanyName = n;
                nameField.value = n;
                C.Ui.Toast(n, UiKit.Good);
            }, UiKit.CardHi, 28, 90));
            root.Add(nameRow);

            Section(root, "settings.account");
            root.Add(UiKit.Button(T("shop.restore"), () => C.Store.Restore(), UiKit.CardHi));
            if (C.GameCenter != null)
                root.Add(UiKit.Button(C.GameCenter.Authenticated ? T("gc.leaderboards") : T("gc.sign_in"),
                    () => { if (C.GameCenter.Authenticated) C.GameCenter.ShowLeaderboards(); else C.GameCenter.SignIn(); }, UiKit.CardHi));
            root.Add(UiKit.Button(T("settings.privacy"), () => Application.OpenURL(C.PrivacyPolicyUrl), UiKit.CardHi));
            root.Add(UiKit.Button(T("settings.support"), () => Application.OpenURL(C.SupportUrl), UiKit.CardHi));
            root.Add(UiKit.Button(T("settings.reset"), ConfirmReset, UiKit.Bad));

            root.Add(UiKit.Label($"Factory King {Application.version} · {C.Store.Backend.Name}", 24, false, UiKit.Muted));

            if (Debug.isDebugBuild) DebugTools(root);
        }

        private void SetLanguage(Language l)
        {
            C.Session.State.Settings.Language = (int)l;
            C.Loc.SetLanguage(l); // triggers a full UI rebuild
        }

        private void ConfirmReset()
        {
            C.Ui.ShowModal((box, close) =>
            {
                box.Add(UiKit.Label(T("settings.reset_confirm"), 32, true, null, true));
                box.Add(UiKit.Button(T("settings.reset_yes"), () =>
                {
                    close();
                    C.Save.DeleteAll();
                    C.Ui.Toast(T("settings.reset_done"));
                    Bootstrap.GameBootstrap.Instance?.RestartFresh();
                }, UiKit.Bad));
                box.Add(UiKit.Button(T("ui.cancel"), close, UiKit.CardHi));
            });
        }

        private void DebugTools(VisualElement root)
        {
            var s = C.Session;
            Section(root, "debug.title");
            var row = UiKit.Row();
            row.style.flexWrap = Wrap.Wrap;
            row.Add(UiKit.Button("+1M $", () => s.Wallet.Grant(1e6), UiKit.CardHi, 26, 84));
            row.Add(UiKit.Button("+1B $", () => s.Wallet.Grant(1e9), UiKit.CardHi, 26, 84));
            row.Add(UiKit.Button("+500 ◆", () => s.Wallet.AddDiamonds(500), UiKit.CardHi, 26, 84));
            row.Add(UiKit.Button("Offline 3h", () =>
            {
                s.Offline.Begin(3 * 3600, false);
                C.Ui.ShowOfflineSummary();
            }, UiKit.CardHi, 26, 84));
            row.Add(UiKit.Button("IAP: cancel next", () => TestStoreBackend.NextOutcome = StoreFailure.Cancelled, UiKit.CardHi, 26, 84));
            row.Add(UiKit.Button("IAP: decline next", () => TestStoreBackend.NextOutcome = StoreFailure.PaymentDeclined, UiKit.CardHi, 26, 84));
            row.Add(UiKit.Button("Ad: skip next", () => TestAdsProvider.NextResult = AdResult.Skipped, UiKit.CardHi, 26, 84));
            row.Add(UiKit.Button("Ad: fail next", () => TestAdsProvider.NextResult = AdResult.Failed, UiKit.CardHi, 26, 84));
            root.Add(row);
        }

        private static VisualElement Slider(string label, float value, Action<float> changed)
        {
            var box = new VisualElement();
            box.Add(UiKit.Label(label, 30));
            var slider = new Slider(0f, 1f) { value = value };
            slider.style.height = 70;
            slider.RegisterValueChangedCallback(e => changed(e.newValue));
            box.Add(slider);
            return box;
        }

        private static VisualElement Toggle(string label, bool value, Action<bool> changed)
        {
            var row = UiKit.Row(0, Justify.SpaceBetween);
            row.style.minHeight = 96;
            row.Add(UiKit.Label(label, 30, false, null, true));
            bool state = value;
            Button b = null;
            b = UiKit.Button(state ? "ON" : "OFF", () =>
            {
                state = !state;
                b.text = state ? "ON" : "OFF";
                b.style.backgroundColor = state ? UiKit.Good : UiKit.Disabled;
                b.userData = state ? UiKit.Good : UiKit.Disabled;
                changed(state);
            }, state ? UiKit.Good : UiKit.Disabled, 28, 84);
            b.style.width = 170;
            row.Add(b);
            return row;
        }
    }

    /// <summary>Two rendering profiles for older devices (Settings toggle).</summary>
    public static class QualityProfile
    {
        public static void Apply(bool high)
        {
            Application.targetFrameRate = 60;
            QualitySettings.shadows = high ? ShadowQuality.All : ShadowQuality.HardOnly;
            QualitySettings.shadowDistance = high ? 70f : 35f;
            QualitySettings.antiAliasing = high ? 4 : 0;
            QualitySettings.lodBias = high ? 1.2f : 0.7f;
            QualitySettings.pixelLightCount = high ? 4 : 1;
        }

        /// <summary>Low profile by default on devices with little memory.</summary>
        public static bool RecommendedHigh => SystemInfo.systemMemorySize >= 3000 && SystemInfo.processorCount >= 4;
    }
}
