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
    public class LiveOpsTests
    {
        private ManualClock _clock;
        private GameSession _s;

        [SetUp]
        public void SetUp()
        {
            _clock = new ManualClock { UtcNow = new DateTime(2027, 3, 1, 9, 0, 0, DateTimeKind.Utc) }; // Monday
            _s = new GameSession(DefaultContent.Create(), clock: _clock);
        }

        private void OwnRegular(int n)
        {
            for (int i = 0; i < n; i++) { var f = _s.State.Factories[i]; f.Unlocked = true; f.Lines[0].Unlocked = true; }
        }

        // ---------------- A1 drone ----------------
        [Test]
        public void Drone_Appears_Can_Be_Caught_Once_And_Returns_Later()
        {
            _s.MetaTick();
            Assert.That(_s.Drone.IsVisible, Is.False);
            _clock.Advance(TimeSpan.FromMinutes(2));
            _s.MetaTick();
            Assert.That(_s.Drone.IsVisible, Is.True);
            var cash = _s.Wallet.Cash; long dia = _s.Wallet.Diamonds;
            Assert.That(_s.Drone.TryCatch(out var r), Is.True);
            Assert.That(_s.Wallet.Cash > cash || _s.Wallet.Diamonds > dia || _s.Boosts.IsActive, Is.True);
            Assert.That(_s.Drone.TryCatch(out _), Is.False);
            Assert.That(_s.State.Stats.DronesCaught, Is.EqualTo(1));
            _clock.Advance(TimeSpan.FromMinutes(10));
            _s.MetaTick();
            Assert.That(_s.Drone.IsVisible, Is.True, "comes back");
            _clock.Advance(TimeSpan.FromSeconds(DroneService.VisibleSeconds + 1));
            Assert.That(_s.Drone.IsVisible, Is.False, "flies away if ignored");
        }

        // ---------------- A2/A3 event factory ----------------
        [Test]
        public void Event_Factory_Opens_Thursday_Scales_Pays_And_Closes_Monday()
        {
            OwnRegular(3);
            _clock.UtcNow = new DateTime(2027, 3, 4, 1, 0, 0, DateTimeKind.Utc); // Thursday
            _s.MetaTick();
            Assert.That(_s.EventFactory.IsActive, Is.True);
            var ev = _s.EventFactory.Current;
            Assert.That(_s.State.GetFactory(ev.FactoryId).Unlocked, Is.True);
            Assert.That(ev.Scale, Is.GreaterThan(1), "scaled to the player's progress");
            Assert.That(_s.OwnedRegularFactories, Is.EqualTo(3), "event factory does not count as a regular factory");
            Assert.That(_s.Factories.TryUnlockFactory(ev.FactoryId), Is.False, "cannot be bought");

            for (int i = 0; i < 4000; i++) _s.Tick(0.25); // ~17 minutes of production
            Assert.That(_s.EventFactory.Tokens, Is.GreaterThan(25));
            long d = _s.Wallet.Diamonds;
            Assert.That(_s.EventFactory.TryClaim(0), Is.True);
            Assert.That(_s.EventFactory.TryClaim(0), Is.False);
            Assert.That(_s.Wallet.Diamonds, Is.GreaterThanOrEqualTo(d + 15));

            _clock.UtcNow = new DateTime(2027, 3, 8, 0, 30, 0, DateTimeKind.Utc); // Monday
            _s.MetaTick();
            Assert.That(_s.EventFactory.Current, Is.Null);
            Assert.That(_s.State.GetFactory(ev.FactoryId).Unlocked, Is.False);
        }

        [Test]
        public void Events_Need_A_Second_Factory_And_Rotate_Weekly()
        {
            _clock.UtcNow = new DateTime(2027, 3, 4, 1, 0, 0, DateTimeKind.Utc);
            _s.MetaTick();
            Assert.That(_s.EventFactory.Current, Is.Null, "new players are not overwhelmed");
            var a = _s.EventFactory.Window(new DateTime(2027, 3, 4, 1, 0, 0, DateTimeKind.Utc)).Value.factoryId;
            var b = _s.EventFactory.Window(new DateTime(2027, 3, 11, 1, 0, 0, DateTimeKind.Utc)).Value.factoryId;
            Assert.That(a, Is.Not.EqualTo(b));
            Assert.That(_s.EventFactory.Window(new DateTime(2027, 3, 9, 1, 0, 0, DateTimeKind.Utc)), Is.Null, "Tuesday: no event");
        }

        [Test]
        public void Final_Event_Tier_Grants_A_Badge_With_Permanent_Bonus()
        {
            OwnRegular(2);
            _clock.UtcNow = new DateTime(2027, 3, 4, 1, 0, 0, DateTimeKind.Utc);
            _s.MetaTick();
            var ev = _s.EventFactory.Current;
            _s.State.Stats.SoldByProduct[_s.EventFactory.ProductId(ev.FactoryId)] = ev.BaselineSold + 5000;
            for (int i = 0; i < EventFactoryService.Tiers.Length; i++) Assert.That(_s.EventFactory.TryClaim(i), Is.True);
            Assert.That(_s.State.Live.EventBadges.Count, Is.EqualTo(1));
            Assert.That(_s.Badges.Bonus, Is.EqualTo(0.01).Within(1e-9));
        }

        // ---------------- A4 cards & crates ----------------
        [Test]
        public void Crates_Give_Cards_With_Shown_Odds_And_Cards_Recruit_Managers()
        {
            foreach (var c in CrateService.Defs) Assert.That(c.Odds.Sum(), Is.EqualTo(1).Within(1e-9), c.Id);
            _s.Crates.Add("crate_big", 3);
            int total = 0;
            for (int i = 0; i < 3; i++) total += _s.Crates.Open("crate_big", new Random(i)).Values.Sum();
            Assert.That(total, Is.EqualTo(24));
            Assert.That(_s.Crates.Open("crate_big"), Is.Null, "none left");

            Assert.That(_s.Managers.TryRecruitWithCards("mgr_mert"), Is.False);
            _s.Managers.AddCards("mgr_mert", ManagerService.CardsToUnlock(Rarity.Common));
            Assert.That(_s.Managers.TryRecruitWithCards("mgr_mert"), Is.True);
            Assert.That(_s.Managers.TryHire("mgr_mert"), Is.False, "card-only managers have no diamond price");
            Assert.That(ManagerService.Defs.Count, Is.EqualTo(20));
        }

        [Test]
        public void Crate_Can_Be_Bought_With_Diamonds()
        {
            _s.State.Diamonds = 39;
            Assert.That(_s.Crates.TryBuy("crate_small"), Is.False);
            _s.State.Diamonds = 40;
            Assert.That(_s.Crates.TryBuy("crate_small"), Is.True);
            Assert.That(_s.Crates.Count("crate_small"), Is.EqualTo(1));
        }

        // ---------------- A5 expeditions ----------------
        [Test]
        public void Expeditions_Run_In_Slots_And_Pay_After_Return()
        {
            Assert.That(_s.Expeditions.TryStart(0, 1), Is.True);
            Assert.That(_s.Expeditions.TryStart(0, 0), Is.False, "slot busy");
            Assert.That(_s.Expeditions.TryStart(3, 0), Is.False, "only 3 slots");
            Assert.That(_s.Expeditions.TryClaim(0), Is.False, "still away");
            _clock.Advance(TimeSpan.FromHours(4.01));
            long d = _s.Wallet.Diamonds;
            Assert.That(_s.Expeditions.TryClaim(0, new Random(1)), Is.True);
            Assert.That(_s.Wallet.Diamonds, Is.EqualTo(d + 5));
            Assert.That(_s.State.Live.ManagerCards.Values.Sum(), Is.EqualTo(3));
            Assert.That(_s.Expeditions.InSlot(0), Is.Null);
        }

        // ---------------- A6 supply chain, A7 fleet ----------------
        [Test]
        public void Supplier_Factory_Lowers_Costs_And_Raises_Prices()
        {
            OwnRegular(5); // includes plastic (supplier of electronics)
            _s.ApplyAllModifiers();
            Assert.That(_s.Supply.IsSupplied("electronics"), Is.True);
            Assert.That(_s.Modifiers.HasSource("supply.electronics"), Is.True);
            _s.State.GetFactory("plastic").Unlocked = false;
            _s.Supply.Apply();
            Assert.That(_s.Modifiers.HasSource("supply.electronics"), Is.False);
        }

        [Test]
        public void Fleet_Upgrade_Carries_More_Per_Trip()
        {
            var def = _s.Content.GetMachine("steel_flatbed_dock");
            double before = _s.AnalyzeLine("steel", 0).MachineCapacityPerSecond[5];
            _s.State.Cash = 1e9;
            Assert.That(_s.Fleet.TryUpgrade("steel"), Is.True);
            Assert.That(_s.Fleet.Level("steel"), Is.EqualTo(2));
            Assert.That(UpgradeMath.ShippingBatch(def, 1, _s.Tuning, FleetService.Capacity(2)), Is.EqualTo(6));
            Assert.That(_s.AnalyzeLine("steel", 0).MachineCapacityPerSecond[5], Is.EqualTo(before * 1.5).Within(before * 1e-9));
            for (int i = 0; i < 10; i++) _s.Fleet.TryUpgrade("steel");
            Assert.That(_s.Fleet.Level("steel"), Is.EqualTo(FleetService.MaxLevel));
        }

        // ---------------- A8 engineering ----------------
        [Test]
        public void Engineering_Is_Permanent_And_Has_Effects()
        {
            _s.State.Diamonds = 10000;
            double cap = _s.Offline.CapSeconds;
            Assert.That(_s.Engineering.TryBuy(EngineeringService.Offline), Is.True);
            Assert.That(_s.Offline.CapSeconds, Is.EqualTo(cap + 3600));
            Assert.That(_s.Engineering.TryBuy(EngineeringService.Expedition), Is.True);
            Assert.That(_s.Expeditions.DurationSeconds(0), Is.EqualTo(3600 * 0.9).Within(1e-6));
            _s.Seasons.EndSeason();
            Assert.That(_s.Engineering.Level(EngineeringService.Offline), Is.EqualTo(1), "kept across seasons");
        }

        // ---------------- A10 season road ----------------
        [Test]
        public void Season_Road_Has_50_Tiers_Free_And_Premium()
        {
            Assert.That(_s.Road.TiersReached, Is.EqualTo(0));
            _s.Road.AddXp(SeasonRoadService.XpPerTier * 3);
            Assert.That(_s.Road.TiersReached, Is.EqualTo(3));
            Assert.That(_s.Road.TryClaim(0, false), Is.True);
            Assert.That(_s.Road.TryClaim(0, false), Is.False);
            Assert.That(_s.Road.TryClaim(3, false), Is.False, "not reached");
            Assert.That(_s.Road.TryClaim(0, true), Is.False, "premium needs the Season Pass");
            _s.Seasons.ActivatePremium();
            Assert.That(_s.Road.TryClaim(0, true), Is.True);
            _s.Road.AddXp(SeasonRoadService.XpPerTier * 100);
            Assert.That(_s.Road.TiersReached, Is.EqualTo(SeasonRoadService.TierCount));
            for (int i = 0; i < 50; i++) _s.Road.TryClaim(i, true);
            Assert.That(_s.Cosmetics.IsOwned("floor_carbon") && _s.Cosmetics.IsOwned("wall_aurora"), Is.True);
            _s.Seasons.EndSeason();
            Assert.That(_s.Road.Xp, Is.EqualTo(0), "road resets each season");
        }

        [Test]
        public void Actions_Give_Season_Xp()
        {
            _s.DailyRewards.TryClaim();
            Assert.That(_s.Road.Xp, Is.EqualTo(SeasonXp.DailyReward));
            _s.State.Cash = 1e9;
            _s.Factories.TryUnlockFactory("food");
            Assert.That(_s.Road.Xp, Is.EqualTo(SeasonXp.DailyReward + SeasonXp.Factory));
        }

        // ---------------- A11 convenience ----------------
        [Test]
        public void Auto_Upgrade_Spends_Within_Budget_Only_When_Enabled()
        {
            _s.State.Cash = 1000;
            Assert.That(_s.Convenience.AutoUpgradeStep(), Is.EqualTo(0), "off by default");
            _s.State.Settings.AutoUpgrade = true;
            Assert.That(_s.Convenience.AutoUpgradeStep(), Is.GreaterThan(0));
            Assert.That(_s.Wallet.Cash.ToDouble(), Is.GreaterThanOrEqualTo(1000 * (1 - ConvenienceService.BudgetShare) - 1e-6));
        }

        [Test]
        public void Claim_All_Collects_Everything_Ready()
        {
            _s.State.AchievementsUnlocked.Add("ach_first_upgrade");
            _s.Road.AddXp(SeasonRoadService.XpPerTier);
            int n = _s.Convenience.ClaimAll();
            Assert.That(n, Is.GreaterThanOrEqualTo(3)); // achievement, road tier, daily reward
            Assert.That(_s.Convenience.ClaimAll(), Is.EqualTo(0));
        }

        // ---------------- monetisation ----------------
        [Test]
        public void Piggy_Bank_Fills_From_Earned_Diamonds_And_Opens_With_Purchase()
        {
            var piggy = _s.Purchases.Catalog.ById("piggy_bank");
            Assert.That(_s.Purchases.CanPurchase(piggy), Is.False, "empty");
            _s.Piggy.Feed(2000);
            Assert.That(_s.Piggy.Diamonds, Is.EqualTo(PiggyBankService.Cap > 400 ? 400 : PiggyBankService.Cap));
            Assert.That(_s.Purchases.CanPurchase(piggy), Is.True);
            long d = _s.Wallet.Diamonds;
            Assert.That(_s.Purchases.Process(piggy.StoreId, "PB1", true, false), Is.EqualTo(PurchaseResult.Granted));
            Assert.That(_s.Wallet.Diamonds, Is.EqualTo(d + 400));
            Assert.That(_s.Piggy.Diamonds, Is.EqualTo(0));
        }

        [Test]
        public void Weekly_Vip_Subscription_Grants_Daily_Diamonds_Until_Expiry()
        {
            var vip = _s.Purchases.Catalog.ById("vip_weekly");
            Assert.That(_s.Purchases.Process(vip.StoreId, "S1", true, false), Is.EqualTo(PurchaseResult.Granted));
            Assert.That(_s.Offers.VipActive, Is.True);
            Assert.That(_s.Purchases.CanPurchase(vip), Is.False);
            Assert.That(_s.Offers.TryClaimVipDaily(), Is.True);
            Assert.That(_s.Offers.TryClaimVipDaily(), Is.False);
            _clock.Advance(TimeSpan.FromDays(1));
            Assert.That(_s.Offers.TryClaimVipDaily(), Is.True);
            _clock.Advance(TimeSpan.FromDays(7));
            Assert.That(_s.Offers.VipActive, Is.False, "expired without renewal");
            Assert.That(_s.Purchases.Process(vip.StoreId, "S0", true, true, _clock.UtcNow.AddDays(-1).Ticks), Is.EqualTo(PurchaseResult.AlreadyProcessed), "old period restore");
        }

        [Test]
        public void Starter_Offer_Is_Highlighted_For_48_Hours()
        {
            Assert.That(_s.Offers.StarterHighlighted, Is.True);
            _clock.Advance(TimeSpan.FromHours(49));
            Assert.That(_s.Offers.StarterHighlighted, Is.False);
        }

        [Test]
        public void Live_State_Survives_Save_Load()
        {
            _s.Crates.Add("crate_big", 2);
            _s.Managers.AddCards("mgr_ece", 7);
            _s.Expeditions.TryStart(1, 2);
            _s.Road.AddXp(777);
            _s.State.Live.Engineering["eng_drone"] = 2;
            _s.State.Settings.AutoUpgrade = true;
            _s.State.GetFactory("steel").FleetLevel = 3;
            var loaded = SaveService.TryDecode(SaveService.Encode(_s.State), _s.Content);
            Assert.That(SaveSerializer.ToJson(loaded).ToString(), Is.EqualTo(SaveSerializer.ToJson(_s.State).ToString()));
            Assert.That(loaded.Live.Crates["crate_big"], Is.EqualTo(2));
            Assert.That(loaded.GetFactory("steel").FleetLevel, Is.EqualTo(3));
        }
    }
}
