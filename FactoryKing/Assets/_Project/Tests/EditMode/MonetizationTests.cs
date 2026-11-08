using System;
using System.Collections.Generic;
using FactoryKing.Core.Content;
using FactoryKing.Core.Economy;
using FactoryKing.Core.Meta;
using FactoryKing.Core.Simulation;
using FactoryKing.Core.TimeKeeping;
using NUnit.Framework;

namespace FactoryKing.Tests
{
    public class MonetizationTests
    {
        private sealed class FakeAds : IAdsProvider
        {
            public bool Ready = true;
            public AdResult Result = AdResult.Completed;
            public int Shown;
            public bool IsReady(string placement) => Ready;
            public void Show(string placement, Action<AdResult> done) { Shown++; done(Result); }
        }

        private sealed class Sink : IAnalyticsSink
        {
            public readonly List<string> Events = new List<string>();
            public void Send(string name, IReadOnlyDictionary<string, string> p) => Events.Add(name);
        }

        private ManualClock _clock;
        private GameSession _s;
        private StoreCatalog Catalog => _s.Purchases.Catalog;
        private string Sid(string id) => Catalog.ById(id).StoreId;

        [SetUp]
        public void SetUp()
        {
            _clock = new ManualClock();
            _s = new GameSession(DefaultContent.Create(), clock: _clock);
        }

        [Test]
        public void Catalog_Has_All_Required_Products()
        {
            foreach (var id in new[] { "diamonds_small", "diamonds_medium", "diamonds_large", "no_ads", "starter_pack", "vip", "cosmetic_gold", "cosmetic_neon", "season_pass" })
                Assert.That(Catalog.ById(id), Is.Not.Null, id);
            var custom = StoreCatalog.CreateDefault("com.acme.fk.");
            Assert.That(custom.ById("vip").StoreId, Is.EqualTo("com.acme.fk.vip"));
        }

        [Test]
        public void Consumable_Granted_Once_Per_Transaction()
        {
            long d = _s.Wallet.Diamonds;
            Assert.That(_s.Purchases.Process(Sid("diamonds_small"), "T1", true, false), Is.EqualTo(PurchaseResult.Granted));
            Assert.That(_s.Purchases.Process(Sid("diamonds_small"), "T1", true, false), Is.EqualTo(PurchaseResult.AlreadyProcessed));
            Assert.That(_s.Purchases.Process(Sid("diamonds_small"), "T2", true, false), Is.EqualTo(PurchaseResult.Granted));
            Assert.That(_s.Wallet.Diamonds, Is.EqualTo(d + 160));
        }

        [Test]
        public void Invalid_Receipt_Or_Unknown_Product_Grants_Nothing()
        {
            long d = _s.Wallet.Diamonds;
            Assert.That(_s.Purchases.Process(Sid("diamonds_large"), "T9", false, false), Is.EqualTo(PurchaseResult.InvalidReceipt));
            Assert.That(_s.Purchases.Process("com.other.app.gems", "T10", true, false), Is.EqualTo(PurchaseResult.UnknownProduct));
            Assert.That(_s.Purchases.Process(Sid("diamonds_large"), null, true, false), Is.EqualTo(PurchaseResult.InvalidReceipt));
            Assert.That(_s.Wallet.Diamonds, Is.EqualTo(d));
        }

        [Test]
        public void NonConsumable_Cannot_Be_Bought_Twice_And_Restore_Works()
        {
            var vip = Catalog.ById("vip");
            double priceBefore = _s.Modifiers.Get(ModifierStat.SellPrice, "steel");
            Assert.That(_s.Purchases.CanPurchase(vip), Is.True);
            Assert.That(_s.Purchases.Process(vip.StoreId, "V1", true, false), Is.EqualTo(PurchaseResult.Granted));
            Assert.That(_s.Purchases.CanPurchase(vip), Is.False);
            Assert.That(_s.Purchases.Process(vip.StoreId, "V2", true, false), Is.EqualTo(PurchaseResult.AlreadyOwned));
            Assert.That(_s.Modifiers.Get(ModifierStat.SellPrice, "steel") / priceBefore, Is.EqualTo(StoreCatalog.VipPriceBonus).Within(1e-12));
            Assert.That(_s.Offline.CapSeconds, Is.EqualTo((8 + StoreCatalog.VipOfflineHours) * 3600));

            // New device: restore re-grants entitlements but never consumables.
            var fresh = new GameSession(DefaultContent.Create(), clock: _clock);
            long d = fresh.Wallet.Diamonds;
            Assert.That(fresh.Purchases.Process(vip.StoreId, "V1", true, true), Is.EqualTo(PurchaseResult.Granted));
            Assert.That(fresh.Purchases.Process(Sid("diamonds_large"), "T1", true, true), Is.EqualTo(PurchaseResult.AlreadyProcessed));
            Assert.That(fresh.Purchases.Owns(StoreCatalog.EntitlementVip), Is.True);
            Assert.That(fresh.Wallet.Diamonds, Is.EqualTo(d));
        }

        [Test]
        public void Starter_Pack_Grants_Contents()
        {
            long d = _s.Wallet.Diamonds;
            _s.Purchases.Process(Sid("starter_pack"), "S1", true, false);
            Assert.That(_s.Wallet.Diamonds, Is.EqualTo(d + 300));
            Assert.That(_s.Managers.IsOwned("mgr_ayla"), Is.True);
        }

        [Test]
        public void Rewarded_Ad_Grants_Only_When_Completed()
        {
            var ads = new FakeAds();
            _s.Ads.Provider = ads;
            int rewards = 0;
            ads.Result = AdResult.Skipped;
            _s.Ads.Request(AdPlacements.Boost, () => rewards++);
            ads.Result = AdResult.Failed;
            _s.Ads.Request(AdPlacements.Boost, () => rewards++);
            Assert.That(rewards, Is.EqualTo(0));
            ads.Result = AdResult.Completed;
            _s.Ads.Request(AdPlacements.Boost, () => rewards++);
            Assert.That(rewards, Is.EqualTo(1));
            Assert.That(_s.State.Stats.AdsWatched, Is.EqualTo(1));
        }

        [Test]
        public void Ads_Respect_Daily_Caps_And_Cooldowns()
        {
            var ads = new FakeAds();
            _s.Ads.Provider = ads;
            int rewards = 0;
            AdResult last = AdResult.Completed;
            _s.Ads.Request(AdPlacements.FreeDiamonds, () => rewards++, r => last = r);
            _s.Ads.Request(AdPlacements.FreeDiamonds, () => rewards++, r => last = r);
            Assert.That(last, Is.EqualTo(AdResult.Capped), "cooldown");
            for (int i = 0; i < 10; i++)
            {
                _clock.Advance(TimeSpan.FromMinutes(11));
                _s.Ads.Request(AdPlacements.FreeDiamonds, () => rewards++);
            }
            Assert.That(rewards, Is.EqualTo(AdPlacements.DailyCap(AdPlacements.FreeDiamonds)));
            _clock.Advance(TimeSpan.FromDays(1));
            Assert.That(_s.Ads.RemainingToday(AdPlacements.FreeDiamonds), Is.EqualTo(AdPlacements.DailyCap(AdPlacements.FreeDiamonds)));
        }

        [Test]
        public void Without_Sdk_No_Fake_Ads_Are_Shown()
        {
            int rewards = 0;
            AdResult r = AdResult.Completed;
            _s.Ads.Request(AdPlacements.DoubleOffline, () => rewards++, x => r = x);
            Assert.That(r, Is.EqualTo(AdResult.NotReady));
            Assert.That(rewards, Is.EqualTo(0));
        }

        [Test]
        public void No_Ads_Purchase_Grants_Rewards_Without_Watching()
        {
            var ads = new FakeAds { Ready = false };
            _s.Ads.Provider = ads;
            _s.Purchases.Process(Sid("no_ads"), "N1", true, false);
            int rewards = 0;
            _s.Ads.Request(AdPlacements.Boost, () => rewards++);
            Assert.That(rewards, Is.EqualTo(1));
            Assert.That(ads.Shown, Is.EqualTo(0));
        }

        [Test]
        public void Analytics_Sends_Nothing_Without_Consent()
        {
            var sink = new Sink();
            _s.Analytics.AddSink(sink);
            _s.Analytics.Track(AnalyticsEvents.AppOpen);
            Assert.That(sink.Events, Is.Empty);
            _s.State.Settings.AnalyticsConsent = true;
            _s.Analytics.Track(AnalyticsEvents.AppOpen);
            Assert.That(sink.Events, Is.EqualTo(new[] { AnalyticsEvents.AppOpen }));
        }
    }
}
