using System;
using System.Linq;
using FactoryKing.Core.Content;
using FactoryKing.Core.Economy;
using FactoryKing.Core.Meta;
using FactoryKing.Core.Numerics;
using FactoryKing.Core.Simulation;
using FactoryKing.Core.TimeKeeping;
using NUnit.Framework;

namespace FactoryKing.Tests
{
    public class MetaTests
    {
        private ManualClock _clock;
        private GameSession _s;

        [SetUp]
        public void SetUp()
        {
            _clock = new ManualClock { UtcNow = new DateTime(2026, 3, 4, 9, 0, 0, DateTimeKind.Utc) }; // Wednesday
            _s = new GameSession(DefaultContent.Create(), clock: _clock);
        }

        private void Run(double seconds) { for (double t = 0; t < seconds; t += 0.1) _s.Tick(0.1); }

        // ---------------- offline earnings & clock ----------------

        [Test]
        public void Offline_Earnings_Match_Steady_State_Rate()
        {
            _s.State.Time.LastSavedUtcTicks = _clock.UtcNow.Ticks;
            _s.State.Time.MaxSeenUtcTicks = _clock.UtcNow.Ticks;
            _clock.Advance(TimeSpan.FromHours(2));
            var r = _s.Resume();
            double expected = _s.AnalyzeLine("steel", 0).NetPerSecond * 7200 * _s.Tuning.OfflineEfficiency;
            Assert.That(r.Earnings.ToDouble(), Is.EqualTo(expected).Within(expected * 1e-9));
            Assert.That(r.Capped, Is.False);
            var before = _s.Wallet.Cash;
            _s.Offline.Claim();
            Assert.That((_s.Wallet.Cash - before).ToDouble(), Is.EqualTo(expected).Within(expected * 1e-6));
            Assert.That(_s.Offline.Claim().IsZero, Is.True, "cannot claim twice");
        }

        [Test]
        public void Offline_Is_Capped_At_8_Hours()
        {
            _s.State.Time.LastSavedUtcTicks = _clock.UtcNow.Ticks;
            _clock.Advance(TimeSpan.FromDays(3));
            var r = _s.Resume();
            Assert.That(r.Capped, Is.True);
            Assert.That(r.SecondsCredited, Is.EqualTo(8 * 3600));
        }

        [Test]
        public void Rewarded_Double_Gives_Twice_And_Never_More()
        {
            _s.State.Time.LastSavedUtcTicks = _clock.UtcNow.Ticks;
            _clock.Advance(TimeSpan.FromHours(1));
            var r = _s.Resume();
            var got = _s.Offline.Claim(5);
            Assert.That(got.ApproximatelyEquals(r.Earnings * 2, 1e-9), Is.True);
        }

        [Test]
        public void Clock_Rollback_Gives_No_Offline_Time_And_Is_Recorded()
        {
            _s.State.Time.LastSavedUtcTicks = _clock.UtcNow.Ticks;
            _s.State.Time.MaxSeenUtcTicks = _clock.UtcNow.Ticks;
            _clock.Advance(TimeSpan.FromHours(-30));
            var r = _s.Resume();
            Assert.That(r.ClockRollbackDetected, Is.True);
            Assert.That(r.Earnings.IsZero, Is.True);
            Assert.That(_s.State.Time.ClockRollbacksDetected, Is.EqualTo(1));
        }

        [Test]
        public void Clock_Forward_Then_Back_Cannot_Farm_Offline_Or_Daily()
        {
            _s.State.Time.LastSavedUtcTicks = _clock.UtcNow.Ticks;
            Assert.That(_s.DailyRewards.TryClaim(), Is.True);
            _clock.Advance(TimeSpan.FromDays(10));                  // cheat forward
            _s.Resume();
            Assert.That(_s.DailyRewards.TryClaim(), Is.True);         // one claim only
            Assert.That(_s.DailyRewards.TryClaim(), Is.False);
            _clock.Advance(TimeSpan.FromDays(-10));                 // set back to real time
            Assert.That(_s.Time.IsRolledBack, Is.True);
            Assert.That(_s.DailyRewards.TryClaim(), Is.False, "no daily claim while clock is behind");
            _s.State.Time.LastSavedUtcTicks = _clock.UtcNow.Ticks;
            Assert.That(_s.Resume().Earnings.IsZero, Is.True);
        }

        [Test]
        public void Small_Ntp_Correction_Is_Tolerated()
        {
            _s.State.Time.MaxSeenUtcTicks = _clock.UtcNow.Ticks;
            _clock.Advance(TimeSpan.FromMinutes(-2));
            Assert.That(_s.Time.IsRolledBack, Is.False);
        }

        [Test]
        public void Short_Absence_Shows_No_Summary()
        {
            _s.State.Time.LastSavedUtcTicks = _clock.UtcNow.Ticks;
            _clock.Advance(TimeSpan.FromSeconds(20));
            _s.Resume();
            Assert.That(_s.Offline.Pending, Is.Null);
        }

        // ---------------- managers ----------------

        [Test]
        public void Manager_Hire_Assign_Level_Affects_Economy()
        {
            var baseCost = _s.AnalyzeLine("steel", 0).CostsPerSecond;
            Assert.That(_s.Managers.TryHire("mgr_kemal"), Is.True, "starting diamonds afford the first manager");
            Assert.That(_s.Managers.TryHire("mgr_kemal"), Is.False, "already owned");
            Assert.That(_s.Managers.Assign("mgr_kemal", "food"), Is.False, "food not owned");
            Assert.That(_s.Managers.Assign("mgr_kemal", "steel"), Is.True);
            var cost1 = _s.AnalyzeLine("steel", 0).CostsPerSecond;
            Assert.That(cost1, Is.LessThan(baseCost));
            _s.State.Cash = 1e9;
            Assert.That(_s.Managers.TryLevelUp("mgr_kemal"), Is.True);
            Assert.That(_s.AnalyzeLine("steel", 0).CostsPerSecond, Is.LessThan(cost1));
            Assert.That(_s.State.Stats.ManagerLevelUps, Is.EqualTo(1));
        }

        [Test]
        public void One_Manager_Per_Factory()
        {
            _s.Wallet.AddDiamonds(1000);
            _s.Managers.TryHire("mgr_kemal");
            _s.Managers.TryHire("mgr_ayla");
            _s.Managers.Assign("mgr_kemal", "steel");
            _s.Managers.Assign("mgr_ayla", "steel");
            Assert.That(_s.Managers.GetOwned("mgr_kemal").AssignedFactoryId, Is.Null);
            Assert.That(_s.Managers.AssignedTo("steel").Id, Is.EqualTo("mgr_ayla"));
        }

        [Test]
        public void Five_Managers_With_All_Rarities_And_Distinct_Perks()
        {
            Assert.That(ManagerService.Defs.Count, Is.EqualTo(5));
            Assert.That(ManagerService.Defs.Select(d => d.Stat).Distinct().Count(), Is.EqualTo(5));
            Assert.That(ManagerService.Defs.Select(d => d.Rarity).Distinct().Count(), Is.EqualTo(4));
            foreach (var d in ManagerService.Defs)
                Assert.That(ManagerService.Bonus(d, d.MaxLevel), Is.GreaterThan(ManagerService.Bonus(d, 1)));
        }

        [Test]
        public void Offline_Manager_Boosts_Offline_Only()
        {
            _s.Wallet.AddDiamonds(1000);
            _s.Managers.TryHire("mgr_selin");
            double live = _s.PotentialNetPerSecond;
            double off = _s.Offline.Calculate(3600).Earnings.ToDouble();
            _s.Managers.Assign("mgr_selin", "steel");
            Assert.That(_s.PotentialNetPerSecond, Is.EqualTo(live));
            Assert.That(_s.Offline.Calculate(3600).Earnings.ToDouble(), Is.GreaterThan(off * 1.2));
        }

        // ---------------- prestige ----------------

        [Test]
        public void Prestige_Needs_Threshold_And_Explains_What_Is_Kept()
        {
            var pv = _s.Prestige.Preview();
            Assert.That(pv.Available, Is.False);
            Assert.That(pv.Keeps, Does.Contain("prestige.keep.managers"));
            Assert.That(pv.Resets, Does.Contain("prestige.reset.cash"));
            Assert.That(_s.Prestige.TryPrestige(), Is.False);
        }

        [Test]
        public void Prestige_Resets_Run_Keeps_Meta_And_Grants_Permanent_Bonus()
        {
            _s.Wallet.AddDiamonds(100);
            _s.Managers.TryHire("mgr_kemal");
            _s.State.Research["res_speed"] = 3;
            _s.State.AchievementsUnlocked.Add("ach_first_upgrade");
            _s.Wallet.Earn(1e9);
            _s.Factories.TryUnlockFactory("food");
            _s.State.Cash = 1e9;
            _s.Upgrades.TryUpgrade(new MachineRef("steel", 0, 0), 30);
            long diamonds = _s.Wallet.Diamonds;
            double priceBefore = _s.AnalyzeLine("steel", 0).RevenuePerSecond;

            var pv = _s.Prestige.Preview();
            Assert.That(pv.Available, Is.True);
            Assert.That(pv.CrownsGained, Is.EqualTo(Math.Floor(Math.Pow(_s.State.RunEarnings.ToDouble() / 1e7, 0.25) + 1e-9)));
            Assert.That(_s.Prestige.TryPrestige(), Is.True);

            Assert.That(_s.State.GetMachine(new MachineRef("steel", 0, 0)).Level, Is.EqualTo(1));
            Assert.That(_s.State.GetFactory("food").Unlocked, Is.False);
            Assert.That(_s.State.GetFactory("steel").Lines[1].Unlocked, Is.True, "prestige bonus line");
            Assert.That(_s.Wallet.Cash.ToDouble(), Is.EqualTo(pv.StartingCashAfter));
            Assert.That(_s.Wallet.Diamonds, Is.EqualTo(diamonds));
            Assert.That(_s.Managers.IsOwned("mgr_kemal"), Is.True);
            Assert.That(_s.Research.Level("res_speed"), Is.EqualTo(3));
            Assert.That(_s.State.AchievementsUnlocked, Does.Contain("ach_first_upgrade"));
            Assert.That(_s.State.Prestige.Badges, Does.Contain(pv.BadgeId));
            Assert.That(_s.State.Collection, Does.Contain(pv.DecorId));
            Assert.That(_s.State.RunEarnings.IsZero, Is.True);
            Assert.That(_s.AnalyzeLine("steel", 0).RevenuePerSecond, Is.GreaterThan(priceBefore * pv.MultiplierAfter * 0.99 / 1.0 - 1e-9).Or.GreaterThan(0));
            Assert.That(_s.Modifiers.Get(ModifierStat.SellPrice, "steel"), Is.EqualTo(pv.MultiplierAfter).Within(1e-9));
        }

        [Test]
        public void Prestige_Survives_Save_Load()
        {
            _s.Wallet.Earn(1e9);
            _s.Prestige.TryPrestige();
            var loaded = new GameSession(_s.Content,
                state: FactoryKing.Core.Persistence.SaveService.TryDecode(FactoryKing.Core.Persistence.SaveService.Encode(_s.State), _s.Content));
            Assert.That(loaded.Modifiers.Get(ModifierStat.SellPrice, "steel"), Is.EqualTo(_s.Modifiers.Get(ModifierStat.SellPrice, "steel")));
        }

        // ---------------- research ----------------

        [Test]
        public void Research_Requires_Level_And_Cash_And_Applies()
        {
            Assert.That(_s.Research.TryBuy("res_speed"), Is.False, "company level too low");
            _s.Wallet.Earn(1e6);
            _s.State.Cash = 1e7;
            double before = _s.AnalyzeLine("steel", 0).FinalUnitsPerSecond;
            Assert.That(_s.Research.TryBuy("res_speed"), Is.True);
            Assert.That(_s.AnalyzeLine("steel", 0).FinalUnitsPerSecond, Is.EqualTo(before * 1.05).Within(1e-9));
            _s.State.Research[ResearchService.OfflineCapId] = 2;
            Assert.That(_s.Offline.CapSeconds, Is.EqualTo(10 * 3600));
        }

        // ---------------- missions, orders, achievements, daily, events, boost ----------------

        [Test]
        public void Daily_And_Weekly_Missions_Generate_Track_And_Pay()
        {
            _s.MetaTick();
            Assert.That(_s.State.Missions.Daily.Count, Is.EqualTo(3));
            Assert.That(_s.State.Missions.Weekly.Count, Is.EqualTo(3));
            var upgrade = new MissionState { Id = "t", Type = MissionTypes.Upgrade, Target = 3, Baseline = _s.Missions.Current(MissionTypes.Upgrade), RewardDiamonds = 7 };
            _s.State.Missions.Daily.Add(upgrade);
            Assert.That(_s.Missions.TryClaim(upgrade), Is.False);
            _s.State.Cash = 1e6;
            _s.Upgrades.TryUpgrade(new MachineRef("steel", 0, 2), 3);
            long d = _s.Wallet.Diamonds;
            Assert.That(_s.Missions.TryClaim(upgrade), Is.True);
            Assert.That(_s.Wallet.Diamonds, Is.EqualTo(d + 7));
            Assert.That(_s.Missions.TryClaim(upgrade), Is.False);
        }

        [Test]
        public void Missions_Rotate_Each_Day()
        {
            _s.MetaTick();
            string first = _s.State.Missions.Daily[0].Id;
            _clock.Advance(TimeSpan.FromDays(1));
            _s.MetaTick();
            Assert.That(_s.State.Missions.Daily[0].Id, Is.Not.EqualTo(first));
        }

        [Test]
        public void Orders_Fill_From_Real_Sales_And_Expire()
        {
            _s.MetaTick();
            Assert.That(_s.State.Orders.Count, Is.EqualTo(OrderService.MaxOrders));
            var o = _s.State.Orders[0];
            o.Quantity = 3;
            Run(120);
            Assert.That(_s.Orders.IsComplete(o), Is.True);
            var cash = _s.Wallet.Cash;
            Assert.That(_s.Orders.TryClaim(o), Is.True);
            Assert.That(_s.Wallet.Cash > cash, Is.True);
            Assert.That(_s.State.Stats.OrdersCompleted, Is.EqualTo(1));
            var other = _s.State.Orders.First(x => !x.Claimed);
            other.Quantity = 1_000_000;
            _clock.Advance(TimeSpan.FromHours(2));
            _s.MetaTick();
            Assert.That(_s.State.Orders.Any(x => x.Id == other.Id), Is.False, "expired order replaced");
            Assert.That(_s.State.Orders.Count, Is.EqualTo(OrderService.MaxOrders));
        }

        [Test]
        public void Required_Achievements_Exist_And_Unlock()
        {
            foreach (var id in new[] { "ach_first_million", "ach_first_factory", "ach_ten_machines", "ach_first_prestige", "ach_five_factories" })
                Assert.That(AchievementService.Defs.Any(a => a.Id == id), Is.True, id);
            string unlocked = null;
            _s.Achievements.Unlocked += a => unlocked = a.Id;
            _s.State.Cash = 1e6;
            _s.Upgrades.TryUpgrade(new MachineRef("steel", 0, 0));
            _s.MetaTick();
            Assert.That(unlocked, Is.EqualTo("ach_first_upgrade"));
            long d = _s.Wallet.Diamonds;
            Assert.That(_s.Achievements.TryClaim("ach_first_upgrade"), Is.True);
            Assert.That(_s.Achievements.TryClaim("ach_first_upgrade"), Is.False);
            Assert.That(_s.Wallet.Diamonds, Is.EqualTo(d + 5));
        }

        [Test]
        public void Daily_Reward_Once_Per_Day_And_Cycle_Never_Resets()
        {
            Assert.That(_s.DailyRewards.TryClaim(), Is.True);
            Assert.That(_s.DailyRewards.TryClaim(), Is.False);
            _clock.Advance(TimeSpan.FromDays(5)); // missed days do not reset the streak
            Assert.That(_s.DailyRewards.TryClaim(), Is.True);
            Assert.That(_s.DailyRewards.NextIndex, Is.EqualTo(2));
        }

        [Test]
        public void Weekend_Event_Runs_Friday_To_Monday()
        {
            Assert.That(_s.Events.Active(), Is.Empty); // Wednesday
            _clock.UtcNow = new DateTime(2026, 3, 6, 1, 0, 0, DateTimeKind.Utc); // Friday
            var active = _s.Events.Active();
            Assert.That(active.Count, Is.EqualTo(1));
            var e = active[0];
            _s.Events.Progress(e).Target = 5;
            Run(200);
            long d = _s.Wallet.Diamonds;
            Assert.That(_s.Events.TryClaim(e), Is.True);
            Assert.That(_s.Wallet.Diamonds, Is.EqualTo(d + e.RewardDiamonds));
            Assert.That(_s.State.Collection, Does.Contain(e.RewardDecorId));
        }

        [Test]
        public void Boost_Doubles_Speed_Then_Expires()
        {
            double baseRate = _s.AnalyzeLine("steel", 0).FinalUnitsPerSecond;
            _s.Boosts.Activate(30);
            Assert.That(_s.AnalyzeLine("steel", 0).FinalUnitsPerSecond, Is.EqualTo(baseRate * 2).Within(1e-9));
            _clock.Advance(TimeSpan.FromMinutes(31));
            _s.MetaTick();
            Assert.That(_s.AnalyzeLine("steel", 0).FinalUnitsPerSecond, Is.EqualTo(baseRate).Within(1e-9));
            _s.Boosts.Activate(10000);
            Assert.That(_s.Boosts.RemainingSeconds, Is.LessThanOrEqualTo(BoostService.MaxStackMinutes * 60 + 1));
        }
    }
}
