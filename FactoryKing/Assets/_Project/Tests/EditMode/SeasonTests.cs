using System;
using System.Linq;
using FactoryKing.Core.Content;
using FactoryKing.Core.Economy;
using FactoryKing.Core.Meta;
using FactoryKing.Core.Persistence;
using FactoryKing.Core.Simulation;
using FactoryKing.Core.TimeKeeping;
using NUnit.Framework;

namespace FactoryKing.Tests
{
    public class SeasonTests
    {
        private ManualClock _clock;
        private GameSession _s;

        [SetUp]
        public void SetUp()
        {
            _clock = new ManualClock();
            _s = new GameSession(DefaultContent.Create(), clock: _clock);
        }

        private void Own(int count)
        {
            for (int i = 0; i < count; i++)
            {
                var f = _s.State.Factories[i];
                f.Unlocked = true;
                f.Lines[0].Unlocked = true;
            }
        }

        [Test]
        public void Season_Starts_With_The_Game_And_Lasts_30_Days()
        {
            Assert.That(_s.Seasons.Number, Is.EqualTo(1));
            Assert.That(_s.Seasons.SecondsLeft, Is.EqualTo(30 * 86400).Within(1));
            _clock.Advance(TimeSpan.FromDays(29));
            _s.MetaTick();
            Assert.That(_s.Seasons.Number, Is.EqualTo(1));
        }

        [Test]
        public void Season_End_Resets_The_Run_And_Keeps_Permanent_Things()
        {
            _s.Wallet.AddDiamonds(500);
            _s.Managers.TryHire("mgr_ayla");
            _s.State.Cash = 1e12;
            _s.Managers.TryLevelUp("mgr_ayla");
            _s.State.Entitlements.Add("vip");
            _s.State.AchievementsUnlocked.Add("ach_first_upgrade");
            _s.Cosmetics.TryBuy("floor_graphite");
            _s.Cosmetics.Equip("floor_graphite");
            _s.State.Research["res_speed"] = 4;
            _s.State.Prestige.Crowns = 30;
            Own(6);
            _s.MetaTick();
            long diamondsBefore = _s.Wallet.Diamonds;

            _clock.Advance(TimeSpan.FromDays(30.01));
            int endedSeason = 0, endedReached = 0;
            _s.Seasons.SeasonEnded += (n, reached) => { endedSeason = n; endedReached = reached; };
            _s.MetaTick();

            Assert.That(endedSeason, Is.EqualTo(1));
            Assert.That(endedReached, Is.EqualTo(6));
            Assert.That(_s.Seasons.Number, Is.EqualTo(2));
            // Reset
            Assert.That(_s.State.Factories.Count(f => f.Unlocked), Is.EqualTo(1));
            Assert.That(_s.State.Research, Is.Empty);
            Assert.That(_s.State.Prestige.Crowns, Is.EqualTo(0));
            Assert.That(_s.Managers.GetOwned("mgr_ayla").Level, Is.EqualTo(1));
            // Kept
            Assert.That(_s.Managers.IsOwned("mgr_ayla"), Is.True);
            Assert.That(_s.Wallet.Diamonds, Is.GreaterThan(diamondsBefore), "unclaimed tier rewards are paid at the end");
            Assert.That(_s.State.Entitlements, Does.Contain("vip"));
            Assert.That(_s.State.AchievementsUnlocked, Does.Contain("ach_first_upgrade"));
            Assert.That(_s.Cosmetics.Equipped(CosmeticSlot.Floor), Is.EqualTo("floor_graphite"));
            Assert.That(_s.Seasons.LegacyStars, Is.GreaterThan(0));
            Assert.That(_s.State.Stats.SeasonsCompleted, Is.EqualTo(1));
            Assert.That(_s.State.Season.History, Does.Contain("1:6"));
        }

        [Test]
        public void Tier_Rewards_Are_Permanent_And_Claimed_Once()
        {
            Own(4);
            _s.MetaTick();
            long d = _s.Wallet.Diamonds;
            Assert.That(_s.Seasons.ClaimableCount, Is.EqualTo(3)); // tiers 2, 3, 4 factories
            Assert.That(_s.Seasons.TryClaim(0), Is.True);
            Assert.That(_s.Seasons.TryClaim(0), Is.False);
            Assert.That(_s.Seasons.TryClaim(2), Is.True);
            Assert.That(_s.Cosmetics.IsOwned("floor_checker"), Is.True);
            Assert.That(_s.Seasons.TryClaim(5), Is.False, "8 factories not reached");
            Assert.That(_s.Wallet.Diamonds, Is.EqualTo(d + 20 + 40));
        }

        [Test]
        public void Legacy_Stars_Raise_Prices_In_Every_Later_Season()
        {
            Own(5);
            _s.MetaTick();
            for (int i = 0; i < SeasonService.Tiers.Length; i++) _s.Seasons.TryClaim(i);
            Assert.That(_s.Seasons.LegacyStars, Is.EqualTo(2));
            double withStars = _s.Modifiers.Get(ModifierStat.SellPrice, "steel") / _s.Market.PriceFactor("steel") / _s.Seasons.Momentum;
            Assert.That(withStars, Is.EqualTo(1 + 2 * SeasonService.LegacyBonusPerStar).Within(1e-9));
        }

        [Test]
        public void Momentum_Grows_Through_The_Season()
        {
            double m0 = _s.Seasons.Momentum;
            _clock.Advance(TimeSpan.FromDays(10));
            _s.MetaTick();
            Assert.That(_s.Seasons.Momentum, Is.EqualTo(m0 + 10 * _s.Tuning.SeasonMomentumPerDay).Within(1e-6));
        }

        [Test]
        public void Season_Pass_Doubles_Diamonds_Adds_Premium_Cosmetics_Retroactively()
        {
            Own(6);
            _s.MetaTick();
            for (int i = 0; i < 5; i++) _s.Seasons.TryClaim(i); // tiers up to 6 factories (incl. premium ground_sakura)
            Assert.That(_s.Cosmetics.IsOwned("ground_sakura"), Is.False);
            var pass = _s.Purchases.Catalog.ById("season_pass");
            long d = _s.Wallet.Diamonds;
            Assert.That(_s.Purchases.Process(pass.StoreId, "SP1", true, false), Is.EqualTo(PurchaseResult.Granted));
            Assert.That(_s.Cosmetics.IsOwned("ground_sakura"), Is.True);
            Assert.That(_s.Wallet.Diamonds, Is.EqualTo(d + 150 + 20 + 30 + 40 + 50 + 60));
            Assert.That(_s.Purchases.CanPurchase(pass), Is.False, "once per season");
            _clock.Advance(TimeSpan.FromDays(31));
            _s.MetaTick();
            Assert.That(_s.Seasons.HasPremium, Is.False);
            Assert.That(_s.Purchases.CanPurchase(pass), Is.True, "available again next season");
        }

        [Test]
        public void Finishing_All_Factories_Allows_Ending_Early()
        {
            Assert.That(_s.Seasons.CanEndEarly, Is.False);
            Own(DefaultContent.FactoryCount);
            Assert.That(_s.Seasons.CanEndEarly, Is.True);
            _s.Seasons.EndSeason();
            Assert.That(_s.Seasons.Number, Is.EqualTo(2));
            Assert.That(_s.Cosmetics.IsOwned("sky_aurora"), Is.True, "final tier cosmetic");
        }

        [Test]
        public void Season_State_Survives_Save_Load()
        {
            Own(4);
            _s.MetaTick();
            _s.Seasons.TryClaim(1);
            _s.Seasons.ActivatePremium();
            var loaded = SaveService.TryDecode(SaveService.Encode(_s.State), _s.Content);
            Assert.That(loaded.Season.StartUtcTicks, Is.EqualTo(_s.State.Season.StartUtcTicks));
            Assert.That(loaded.Season.ClaimedTiers, Does.Contain(1));
            Assert.That(loaded.Season.ReachedTiers.Count, Is.EqualTo(3));
            Assert.That(loaded.Season.LegacyStars, Is.EqualTo(1));
            Assert.That(loaded.Season.PremiumPass, Is.True);
        }
    }

    public class CosmeticTests
    {
        private GameSession _s;

        [SetUp]
        public void SetUp() => _s = new GameSession(DefaultContent.Create(), clock: new ManualClock());

        [Test]
        public void Every_Slot_Has_A_Free_Default_And_Several_Options()
        {
            foreach (CosmeticSlot slot in Enum.GetValues(typeof(CosmeticSlot)))
            {
                var defs = CosmeticService.ForSlot(slot).ToList();
                Assert.That(defs.Count(d => d.Source == CosmeticSource.Default), Is.EqualTo(1), slot.ToString());
                Assert.That(defs.Count, Is.GreaterThanOrEqualTo(6));
                Assert.That(defs.Any(d => d.Source == CosmeticSource.Diamonds), Is.True, "buyable");
                Assert.That(defs.Any(d => d.Source == CosmeticSource.Season || d.Source == CosmeticSource.Achievement || d.Source == CosmeticSource.SeasonPremium), Is.True, "earnable");
                Assert.That(_s.Cosmetics.Equipped(slot), Is.EqualTo(CosmeticService.DefaultFor(slot)));
            }
        }

        [Test]
        public void Buy_With_Diamonds_Then_Equip()
        {
            Assert.That(_s.Cosmetics.Equip("wall_navy"), Is.False, "not owned");
            _s.State.Diamonds = 89;
            Assert.That(_s.Cosmetics.TryBuy("wall_navy"), Is.False, "costs 90");
            _s.State.Diamonds = 200;
            Assert.That(_s.Cosmetics.TryBuy("wall_navy"), Is.True);
            Assert.That(_s.Cosmetics.TryBuy("wall_navy"), Is.False, "already owned");
            Assert.That(_s.State.Diamonds, Is.EqualTo(110));
            Assert.That(_s.Cosmetics.Equip("wall_navy"), Is.True);
            Assert.That(_s.Cosmetics.Equipped(CosmeticSlot.Wall), Is.EqualTo("wall_navy"));
        }

        [Test]
        public void Reward_Only_Items_Cannot_Be_Bought()
        {
            _s.State.Diamonds = 100000;
            foreach (var d in CosmeticService.Defs.Where(x => x.Source != CosmeticSource.Diamonds))
                Assert.That(_s.Cosmetics.TryBuy(d.Id), Is.False, d.Id);
        }

        [Test]
        public void Cosmetic_Packs_And_Achievements_Grant_And_Restore()
        {
            var neon = _s.Purchases.Catalog.ById("cosmetic_neon");
            _s.Purchases.Process(neon.StoreId, "N1", true, false);
            Assert.That(_s.Cosmetics.IsOwned("floor_neon"), Is.True);
            var fresh = new GameSession(DefaultContent.Create(), clock: new ManualClock());
            fresh.Purchases.Process(neon.StoreId, "N1", true, true);
            Assert.That(fresh.Cosmetics.IsOwned("sky_neon"), Is.True, "restore brings cosmetics back");

            _s.State.AchievementsUnlocked.Add("ach_five_factories");
            _s.Achievements.TryClaim("ach_five_factories");
            Assert.That(_s.Cosmetics.IsOwned("floor_marble"), Is.True);
        }

        [Test]
        public void Every_Reward_Reference_Points_To_A_Real_Cosmetic()
        {
            foreach (var t in SeasonService.Tiers)
            {
                if (t.CosmeticId != null) Assert.That(CosmeticService.Get(t.CosmeticId), Is.Not.Null, t.CosmeticId);
                if (t.PremiumCosmeticId != null) Assert.That(CosmeticService.Get(t.PremiumCosmeticId), Is.Not.Null);
            }
            foreach (var a in AchievementService.Defs.Where(a => a.RewardCosmetic != null))
                Assert.That(CosmeticService.Get(a.RewardCosmetic), Is.Not.Null);
            foreach (var p in StoreCatalog.CreateDefault().Products)
            foreach (var c in p.CosmeticIds)
                Assert.That(CosmeticService.Get(c), Is.Not.Null);
        }
    }
}
