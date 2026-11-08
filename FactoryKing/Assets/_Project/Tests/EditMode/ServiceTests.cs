using FactoryKing.Core.Content;
using FactoryKing.Core.Economy;
using FactoryKing.Core.Numerics;
using FactoryKing.Core.Simulation;
using NUnit.Framework;

namespace FactoryKing.Tests
{
    public class ServiceTests
    {
        private GameSession _s;
        private static readonly MachineRef Furnace = new MachineRef(DefaultContent.Steel, 0, 1);

        [SetUp]
        public void SetUp() => _s = new GameSession(DefaultContent.Create());

        [Test]
        public void Wallet_Debt_Is_Repaid_Before_Cash()
        {
            _s.State.Cash = 10;
            _s.Wallet.PayOperating(25);
            Assert.That(_s.Wallet.Cash.IsZero, Is.True);
            Assert.That(_s.Wallet.Debt.ToDouble(), Is.EqualTo(15).Within(1e-9));
            _s.Wallet.Earn(20);
            Assert.That(_s.Wallet.Debt.IsZero, Is.True);
            Assert.That(_s.Wallet.Cash.ToDouble(), Is.EqualTo(5).Within(1e-9));
        }

        [Test]
        public void Wallet_Purchases_Require_Cash()
        {
            _s.State.Cash = 10;
            Assert.That(_s.Wallet.TrySpend(11), Is.False);
            Assert.That(_s.Wallet.TrySpend(10), Is.True);
            Assert.That(_s.Wallet.TrySpend(-1), Is.False);
            Assert.That(_s.Wallet.TrySpendDiamonds(_s.Wallet.Diamonds + 1), Is.False);
        }

        [Test]
        public void Upgrade_Spends_Exact_Quote_And_Raises_Level()
        {
            _s.State.Cash = 1000;
            var quote = _s.Upgrades.GetQuote(Furnace, 3);
            Assert.That(quote.Levels, Is.EqualTo(3));
            BigDouble before = _s.Wallet.Cash;
            Assert.That(_s.Upgrades.TryUpgrade(Furnace, 3), Is.True);
            Assert.That(_s.State.GetMachine(Furnace).Level, Is.EqualTo(4));
            Assert.That((before - _s.Wallet.Cash).ApproximatelyEquals(quote.Cost), Is.True);
            Assert.That(_s.State.TotalInvested.ApproximatelyEquals(quote.Cost), Is.True);
        }

        [Test]
        public void Upgrade_Fails_Without_Money_And_Changes_Nothing()
        {
            _s.State.Cash = 1;
            Assert.That(_s.Upgrades.TryUpgrade(Furnace), Is.False);
            Assert.That(_s.State.GetMachine(Furnace).Level, Is.EqualTo(1));
            Assert.That(_s.Wallet.Cash.ToDouble(), Is.EqualTo(1));
        }

        [Test]
        public void Upgrade_Max_Buys_As_Many_As_Affordable_And_Stops_At_100()
        {
            _s.State.Cash = BigDouble.FromLog10(300);
            Assert.That(_s.Upgrades.TryUpgrade(Furnace, UpgradeService.Max), Is.True);
            Assert.That(_s.State.GetMachine(Furnace).Level, Is.EqualTo(100));
            Assert.That(_s.Upgrades.GetQuote(Furnace, 1).IsMaxLevel, Is.True);
            Assert.That(_s.Upgrades.TryUpgrade(Furnace), Is.False);
        }

        [Test]
        public void Upgrade_Event_Fires()
        {
            _s.State.Cash = 1000;
            int newLevel = 0;
            _s.Upgrades.MachineUpgraded += (r, o, n) => newLevel = n;
            _s.Upgrades.TryUpgrade(Furnace, 2);
            Assert.That(newLevel, Is.EqualTo(3));
        }

        [Test]
        public void Cannot_Upgrade_Machines_On_Locked_Lines()
        {
            _s.State.Cash = 1e9;
            Assert.That(_s.Upgrades.TryUpgrade(new MachineRef(DefaultContent.Steel, 1, 0)), Is.False);
            Assert.That(_s.Upgrades.TryUpgrade(new MachineRef(DefaultContent.Food, 0, 0)), Is.False);
        }

        [Test]
        public void Lines_And_Factories_Unlock_In_Order()
        {
            _s.State.Cash = 1e15;
            Assert.That(_s.Factories.TryUnlockLine(DefaultContent.Steel, 2), Is.False, "line 3 needs line 2");
            Assert.That(_s.Factories.TryUnlockLine(DefaultContent.Steel, 1), Is.True);
            Assert.That(_s.Factories.TryUnlockLine(DefaultContent.Steel, 2), Is.True);
            Assert.That(_s.Factories.TryUnlockFactory(DefaultContent.Plastic), Is.False, "plastic needs food");
            Assert.That(_s.Factories.TryUnlockFactory(DefaultContent.Food), Is.True);
            Assert.That(_s.Factories.IsLineUnlocked(DefaultContent.Food, 0), Is.True);
            Assert.That(_s.Factories.TryUnlockFactory(DefaultContent.Food), Is.False, "already owned");
            Assert.That(_s.Factories.SetActiveFactory(DefaultContent.Food), Is.True);
            Assert.That(_s.State.ActiveFactoryId, Is.EqualTo(DefaultContent.Food));
        }

        [Test]
        public void Unlock_Charges_The_Configured_Price()
        {
            var def = _s.Content.GetFactory(DefaultContent.Steel);
            _s.State.Cash = def.LineUnlockCosts[1];
            Assert.That(_s.Factories.TryUnlockLine(DefaultContent.Steel, 1), Is.True);
            Assert.That(_s.Wallet.Cash.IsZero, Is.True);
        }

        [Test]
        public void Modifiers_Combine_Multiplicatively_And_Respect_Scope()
        {
            var m = _s.Modifiers;
            foreach (var f in _s.Content.Factories) m.Remove("market." + f.Id); // isolate from the living market
            m.Remove(FactoryKing.Core.Meta.SeasonService.MomentumSource);
            m.Set("prestige", ModifierStat.SellPrice, 2);
            m.Set("manager.steel", ModifierStat.SellPrice, 1.5, DefaultContent.Steel);
            Assert.That(m.Get(ModifierStat.SellPrice, DefaultContent.Steel), Is.EqualTo(3).Within(1e-12));
            Assert.That(m.Get(ModifierStat.SellPrice, DefaultContent.Food), Is.EqualTo(2).Within(1e-12));
            m.Remove("prestige");
            Assert.That(m.Get(ModifierStat.SellPrice, DefaultContent.Food), Is.EqualTo(1));
        }

        [Test]
        public void Sell_Price_Modifier_Raises_Revenue()
        {
            var baseRevenue = _s.AnalyzeLine(DefaultContent.Steel, 0).RevenuePerSecond;
            _s.Modifiers.Set("boost", ModifierStat.SellPrice, 2);
            Assert.That(_s.AnalyzeLine(DefaultContent.Steel, 0).RevenuePerSecond, Is.EqualTo(baseRevenue * 2).Within(1e-9));
        }

        [Test]
        public void Company_Value_And_Level_Grow_With_Progress()
        {
            var v0 = _s.CompanyValue;
            int l0 = _s.CompanyLevel;
            _s.Wallet.Earn(1e6);
            Assert.That(_s.CompanyValue > v0, Is.True);
            Assert.That(_s.CompanyLevel, Is.GreaterThan(l0));
        }

        [Test]
        public void State_Repairs_Missing_Or_Corrupt_Structure()
        {
            var content = DefaultContent.Create();
            var state = GameState.CreateNew(content, EconomyTuning.CreateDefault());
            state.Factories.RemoveAt(4);
            state.Factories[0].Lines[0].Machines.RemoveAt(5);
            state.Factories[0].Lines[0].Machines[0].Level = -3;
            state.Factories[0].Lines[0].Machines[1].Progress = double.NaN;
            state.ActiveFactoryId = "deleted_factory";
            state.EnsureConsistent(content);
            Assert.That(state.Factories.Count, Is.EqualTo(DefaultContent.FactoryCount + 3));
            Assert.That(state.Factories[0].Lines[0].Machines.Count, Is.EqualTo(6));
            Assert.That(state.Factories[0].Lines[0].Machines[0].Level, Is.EqualTo(1));
            Assert.That(state.Factories[0].Lines[0].Machines[1].Progress, Is.EqualTo(0));
            Assert.That(state.ActiveFactoryId, Is.EqualTo(DefaultContent.Steel));
        }
    }
}
