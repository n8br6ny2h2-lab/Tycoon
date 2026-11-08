using System.Linq;
using FactoryKing.Core.Content;
using FactoryKing.Core.Economy;
using FactoryKing.Core.Numerics;
using FactoryKing.Core.Simulation;
using NUnit.Framework;

namespace FactoryKing.Tests
{
    public class ProductionTests
    {
        private static GameSession NewSession() => new GameSession(DefaultContent.Create());

        private static void Run(GameSession s, double seconds, double step = 0.1)
        {
            for (double t = 0; t < seconds; t += step) s.Tick(step);
        }

        [Test]
        public void New_Game_Starts_With_One_Factory_One_Line()
        {
            var s = NewSession();
            Assert.That(s.State.Factories.Count(f => f.Unlocked), Is.EqualTo(1));
            Assert.That(s.State.Factories[0].Lines.Count(l => l.Unlocked), Is.EqualTo(1));
            Assert.That(s.State.ActiveFactoryId, Is.EqualTo(DefaultContent.Steel));
        }

        [Test]
        public void Line_Produces_And_Sells_Goods_From_Real_Flow()
        {
            var s = NewSession();
            BigDouble start = s.Wallet.Cash;
            Run(s, 120);
            Assert.That(s.State.TotalUnitsSold, Is.GreaterThan(0));
            Assert.That(s.Wallet.Cash > start, Is.True, "line should be profitable");
            Assert.That(s.State.UnitsProduced["iron_ore"], Is.GreaterThanOrEqualTo(s.State.UnitsProduced["steel_bundle"] * 2));
            Assert.That(s.Ledger.RevenuePerSecond, Is.GreaterThan(0));
            Assert.That(s.Ledger.CostsPerSecond, Is.GreaterThan(0));
        }

        [Test]
        public void Units_Are_Conserved_Through_The_Chain()
        {
            var s = NewSession();
            Run(s, 300);
            var m = s.State.Factories[0].Lines[0].Machines;

            // Ore: every unit the unloader produced is either waiting, inside the furnace, or was smelted (2 per cycle).
            long ore = m[0].CyclesCompleted;
            Assert.That(ore, Is.EqualTo(m[0].OutputBuffer + m[1].InProcess + 2 * m[1].CyclesCompleted));

            // Bundles: every bundle the packer made is in its buffer, in the yard, on a truck, or sold.
            long bundles = m[3].CyclesCompleted;
            long accounted = m[3].OutputBuffer + m[4].InProcess + m[4].OutputBuffer + m[5].InProcess + s.State.TotalUnitsSold;
            Assert.That(bundles, Is.EqualTo(accounted));
        }

        [Test]
        public void Buffers_Never_Exceed_Capacity()
        {
            var s = NewSession();
            var t = s.Tuning;
            var f = s.Content.GetFactory(DefaultContent.Steel);
            var line = s.State.Factories[0].Lines[0];
            for (int step = 0; step < 3000; step++)
            {
                s.Tick(0.1);
                for (int i = 0; i < line.Machines.Count; i++)
                {
                    var def = s.Content.GetMachine(f.MachineChain[i]);
                    if (def.Stage == StageType.Shipping) continue;
                    Assert.That(line.Machines[i].OutputBuffer, Is.LessThanOrEqualTo(UpgradeMath.Capacity(def, line.Machines[i].Level, t)));
                    Assert.That(line.Machines[i].OutputBuffer, Is.GreaterThanOrEqualTo(0));
                }
            }
        }

        [Test]
        public void Simulation_Is_Deterministic()
        {
            var a = NewSession();
            var b = NewSession();
            Run(a, 200);
            Run(b, 200);
            Assert.That(a.Wallet.Cash, Is.EqualTo(b.Wallet.Cash));
            Assert.That(a.State.TotalUnitsSold, Is.EqualTo(b.State.TotalUnitsSold));
        }

        [TestCase(1)]
        [TestCase(10)]
        [TestCase(25)]
        [TestCase(60)]
        [TestCase(100)]
        public void Simulation_Agrees_With_Analyzer_In_Steady_State(int level)
        {
            foreach (var factory in DefaultContent.Create().Factories)
            {
                var s = NewSession();
                var fs = s.State.GetFactory(factory.Id);
                fs.Unlocked = true;
                fs.Lines[0].Unlocked = true;
                foreach (var m in fs.Lines[0].Machines) m.Level = level;
                var report = s.AnalyzeLine(factory.Id, 0);

                // Isolate this factory.
                foreach (var other in s.State.Factories) if (other != fs) other.Unlocked = false;

                Run(s, 120, 0.05); // warm-up: fill buffers
                long soldBefore = s.State.TotalUnitsSold;
                double seconds = 600;
                Run(s, seconds, 0.05);
                double measured = (s.State.TotalUnitsSold - soldBefore) / seconds;
                Assert.That(measured, Is.EqualTo(report.FinalUnitsPerSecond).Within(report.FinalUnitsPerSecond * 0.05 + 0.01),
                    $"{factory.Id} L{level}: sim {measured:0.###}/s vs analyzer {report.FinalUnitsPerSecond:0.###}/s");
            }
        }

        [Test]
        public void Frame_Rate_Does_Not_Change_Results()
        {
            var fine = NewSession();
            var coarse = NewSession();
            Run(fine, 600, 1.0 / 60);
            Run(coarse, 600, 0.5);
            double a = fine.State.TotalUnitsSold, b = coarse.State.TotalUnitsSold;
            Assert.That(b, Is.EqualTo(a).Within(a * 0.05 + 2));
        }

        [Test]
        public void Zero_Cash_Never_Softlocks_The_Factory()
        {
            var s = NewSession();
            s.State.Cash = BigDouble.Zero;
            Run(s, 300);
            Assert.That(s.State.TotalUnitsSold, Is.GreaterThan(0));
            Assert.That(s.Wallet.Cash.IsPositive, Is.True);
            Assert.That(s.Wallet.Debt.IsZero, Is.True, "debt is repaid from sales");
        }

        [Test]
        public void Upgrading_The_Bottleneck_Increases_Throughput()
        {
            var s = NewSession();
            var before = s.AnalyzeLine(DefaultContent.Steel, 0);
            s.State.Cash = 1e6;
            Assert.That(s.Upgrades.TryUpgrade(new MachineRef(DefaultContent.Steel, 0, before.BottleneckIndex), 5), Is.True);
            var after = s.AnalyzeLine(DefaultContent.Steel, 0);
            Assert.That(after.FinalUnitsPerSecond, Is.GreaterThan(before.FinalUnitsPerSecond));
        }

        [Test]
        public void Machine_Status_Reflects_Real_State()
        {
            var s = NewSession();
            var line = s.State.Factories[0].Lines[0];
            s.Tick(0.05);
            Assert.That(line.Machines[0].Status, Is.EqualTo(MachineStatus.Working));
            Assert.That(line.Machines[1].Status, Is.EqualTo(MachineStatus.Starved));
            // Starve the dock's downstream: make the dock very slow so the yard fills and the line backs up.
            s.Modifiers.Set("test", ModifierStat.StorageCapacity, 0.01);
            Run(s, 400);
            Assert.That(line.Machines.Any(m => m.Status == MachineStatus.Blocked), Is.True);
        }

        [Test]
        public void Huge_Tick_Is_Bounded_And_Safe()
        {
            var s = NewSession();
            Assert.DoesNotThrow(() => s.Tick(1e6));
            Assert.DoesNotThrow(() => s.Tick(double.NaN));
            Assert.DoesNotThrow(() => s.Tick(-5));
            Assert.That(double.IsNaN(s.Wallet.Cash.Mantissa), Is.False);
        }
    }
}
