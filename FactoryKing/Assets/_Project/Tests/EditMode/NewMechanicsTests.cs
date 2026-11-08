using System;
using FactoryKing.Core.Content;
using FactoryKing.Core.Economy;
using FactoryKing.Core.Meta;
using FactoryKing.Core.Simulation;
using FactoryKing.Core.TimeKeeping;
using NUnit.Framework;

namespace FactoryKing.Tests
{
    public class NewMechanicsTests
    {
        private ManualClock _clock;
        private GameSession _s;

        [SetUp]
        public void SetUp()
        {
            _clock = new ManualClock();
            _s = new GameSession(DefaultContent.Create(), clock: _clock);
        }

        // ---------------- power grid ----------------

        [Test]
        public void One_Line_Runs_At_Full_Power_At_Start()
        {
            Assert.That(_s.Power.Throttle("steel"), Is.EqualTo(1));
            Assert.That(_s.Power.DemandKw("steel"), Is.LessThan(_s.Power.CapacityKw("steel")));
        }

        [Test]
        public void Upgrades_And_Lines_Overload_The_Plant_And_Slow_The_Factory()
        {
            _s.State.Cash = 1e9;
            double before = _s.AnalyzeLine("steel", 0).FinalUnitsPerSecond;
            _s.Factories.TryUnlockLine("steel", 1);
            _s.Factories.TryUnlockLine("steel", 2);
            for (int i = 0; i < 6; i++) _s.Upgrades.TryUpgrade(new MachineRef("steel", 0, i), 30);
            double throttle = _s.Power.Throttle("steel");
            Assert.That(throttle, Is.LessThan(1));
            Assert.That(_s.Modifiers.HasSource(PowerService.SourcePrefix + "steel"), Is.True);
            // The upgraded line is faster than before but slowed by the grid.
            var report = _s.AnalyzeLine("steel", 0);
            Assert.That(report.FinalUnitsPerSecond, Is.GreaterThan(before));

            int guard = 0;
            while (_s.Power.Throttle("steel") < 1 && guard++ < 60) Assert.That(_s.Power.TryUpgrade("steel"), Is.True);
            Assert.That(_s.Power.Throttle("steel"), Is.EqualTo(1));
            Assert.That(_s.Modifiers.HasSource(PowerService.SourcePrefix + "steel"), Is.False);
            Assert.That(_s.AnalyzeLine("steel", 0).FinalUnitsPerSecond, Is.GreaterThan(report.FinalUnitsPerSecond));
        }

        [Test]
        public void Power_Upgrade_Needs_Cash_And_Is_Saved()
        {
            _s.State.Cash = 0;
            Assert.That(_s.Power.TryUpgrade("steel"), Is.False);
            _s.State.Cash = 1e6;
            Assert.That(_s.Power.TryUpgrade("steel"), Is.True);
            var loaded = FactoryKing.Core.Persistence.SaveService.TryDecode(FactoryKing.Core.Persistence.SaveService.Encode(_s.State), _s.Content);
            Assert.That(loaded.GetFactory("steel").PowerLevel, Is.EqualTo(2));
            Assert.That(_s.Power.TryUpgrade("food"), Is.False, "not owned");
        }

        [Test]
        public void Bigger_Machines_Draw_More_Power()
        {
            var def = _s.Content.GetMachine("steel_blast_furnace");
            Assert.That(UpgradeMath.PowerKw(def, 50, _s.Tuning), Is.GreaterThan(UpgradeMath.PowerKw(def, 1, _s.Tuning) * 3));
        }

        // ---------------- market ----------------

        [Test]
        public void Market_Prices_Move_Within_Bounds_And_One_Product_Is_Hot()
        {
            double min = 9, max = 0;
            var start = new DateTime(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc);
            for (int h = 0; h < 24 * 14; h++)
            {
                var t = start.AddHours(h);
                int hot = 0;
                foreach (var f in _s.Content.Factories)
                {
                    double p = _s.Market.PriceFactor(f.Id, t);
                    min = Math.Min(min, p);
                    max = Math.Max(max, p);
                    if (_s.Market.HotFactoryId(t) == f.Id) hot++;
                }
                Assert.That(hot, Is.EqualTo(1));
            }
            Assert.That(min, Is.GreaterThanOrEqualTo(1 - MarketService.Amplitude - 1e-9));
            Assert.That(max, Is.LessThanOrEqualTo(1 + MarketService.Amplitude + MarketService.HotBonus + 1e-9));
            Assert.That(max - min, Is.GreaterThan(0.3), "prices really move");
        }

        [Test]
        public void Market_Changes_Revenue_Over_Time()
        {
            double r0 = _s.AnalyzeLine("steel", 0).RevenuePerSecond;
            _clock.Advance(TimeSpan.FromHours(2.5));
            _s.MetaTick();
            double r1 = _s.AnalyzeLine("steel", 0).RevenuePerSecond;
            Assert.That(r1, Is.Not.EqualTo(r0).Within(r0 * 0.01));
        }

        // ---------------- manager abilities ----------------

        [Test]
        public void Ability_Needs_Assignment_Runs_Then_Cools_Down()
        {
            _s.Wallet.AddDiamonds(1000);
            _s.Managers.TryHire("mgr_ayla");
            Assert.That(_s.Managers.TryUseAbility("mgr_ayla"), Is.False, "must be assigned");
            _s.Managers.Assign("mgr_ayla", "steel");
            double baseRate = _s.AnalyzeLine("steel", 0).FinalUnitsPerSecond;
            Assert.That(_s.Managers.TryUseAbility("mgr_ayla"), Is.True);
            Assert.That(_s.AnalyzeLine("steel", 0).FinalUnitsPerSecond, Is.EqualTo(baseRate * 3).Within(baseRate * 1e-9));
            Assert.That(_s.Managers.TryUseAbility("mgr_ayla"), Is.False, "cooldown");
            _clock.Advance(TimeSpan.FromMinutes(3));
            _s.MetaTick();
            Assert.That(_s.AnalyzeLine("steel", 0).FinalUnitsPerSecond, Is.EqualTo(baseRate).Within(baseRate * 1e-9));
            _clock.Advance(TimeSpan.FromMinutes(40));
            Assert.That(_s.Managers.CanUseAbility("mgr_ayla"), Is.True);
        }

        [Test]
        public void Overtime_Ability_Pays_Income_Instantly()
        {
            _s.Wallet.AddDiamonds(1000);
            _s.Managers.TryHire("mgr_selin");
            _s.Managers.Assign("mgr_selin", "steel");
            var before = _s.Wallet.Cash;
            Assert.That(_s.Managers.TryUseAbility("mgr_selin"), Is.True);
            Assert.That((_s.Wallet.Cash - before).ToDouble(), Is.GreaterThan(_s.AnalyzeLine("steel", 0).NetPerSecond * 800));
        }

        [Test]
        public void Every_Manager_Has_An_Ability()
        {
            foreach (var d in ManagerService.Defs)
            {
                Assert.That(d.AbilityKey, Is.Not.Null);
                Assert.That(d.AbilityCooldownSeconds, Is.GreaterThan(d.AbilitySeconds));
            }
        }

        // ---------------- difficulty ----------------

        [Test]
        public void Later_Factories_Have_Steeper_Upgrade_Curves()
        {
            double prev = 0;
            foreach (var f in _s.Content.Factories)
            {
                double g = _s.Content.GetMachine(f.MachineChain[1]).UpgradeCostGrowth;
                Assert.That(g, Is.GreaterThan(prev));
                prev = g;
            }
        }
    }
}
