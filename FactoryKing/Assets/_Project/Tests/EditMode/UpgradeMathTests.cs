using FactoryKing.Core.Content;
using FactoryKing.Core.Economy;
using FactoryKing.Core.Numerics;
using NUnit.Framework;

namespace FactoryKing.Tests
{
    public class UpgradeMathTests
    {
        private EconomyTuning _t;
        private MachineDef _def;

        [SetUp]
        public void SetUp()
        {
            _t = EconomyTuning.CreateDefault();
            _def = new MachineDef
            {
                Id = "m", Stage = StageType.Processing, InputProductId = "a", OutputProductId = "b",
                BaseCycleSeconds = 2, BaseCapacity = 10, BaseUpgradeCost = 10, UpgradeCostGrowth = 1.15,
                MaintenancePerSecond = 0.01,
            };
        }

        [Test]
        public void Speed_Increases_Monotonically_And_Doubles_At_Milestones()
        {
            double prev = 0;
            for (int l = 1; l <= 100; l++)
            {
                double s = UpgradeMath.SpeedMultiplier(l, _t);
                Assert.That(s, Is.GreaterThan(prev), $"level {l}");
                prev = s;
            }
            double before = UpgradeMath.SpeedMultiplier(9, _t) / (1 + 0.1 * 8);
            double after = UpgradeMath.SpeedMultiplier(10, _t) / (1 + 0.1 * 9);
            Assert.That(after / before, Is.EqualTo(2).Within(1e-9));
        }

        [Test]
        public void Level_Is_Clamped_To_1_100()
        {
            Assert.That(UpgradeMath.SpeedMultiplier(0, _t), Is.EqualTo(UpgradeMath.SpeedMultiplier(1, _t)));
            Assert.That(UpgradeMath.SpeedMultiplier(500, _t), Is.EqualTo(UpgradeMath.SpeedMultiplier(100, _t)));
        }

        [Test]
        public void Cost_Grows_Faster_Than_Speed_So_Economy_Is_Bounded()
        {
            // Marginal cost per unit of extra speed must rise over the level range.
            double early = UpgradeMath.UpgradeCost(_def, 2, _t).ToDouble() /
                           (UpgradeMath.SpeedMultiplier(3, _t) - UpgradeMath.SpeedMultiplier(2, _t));
            double late = UpgradeMath.UpgradeCost(_def, 95, _t).ToDouble() /
                          (UpgradeMath.SpeedMultiplier(96, _t) - UpgradeMath.SpeedMultiplier(95, _t));
            Assert.That(late, Is.GreaterThan(early * 100));
        }

        [Test]
        public void Bulk_Cost_Equals_Sum_Of_Single_Levels()
        {
            BigDouble sum = BigDouble.Zero;
            for (int l = 5; l < 15; l++) sum += UpgradeMath.UpgradeCost(_def, l, _t);
            var bulk = UpgradeMath.UpgradeCostForLevels(_def, 5, 10, _t);
            Assert.That(bulk.ApproximatelyEquals(sum, 1e-9), Is.True, $"{bulk} vs {sum}");
        }

        [Test]
        public void Bulk_Cost_Never_Exceeds_Max_Level()
        {
            var all = UpgradeMath.UpgradeCostForLevels(_def, 95, 50, _t);
            var five = UpgradeMath.UpgradeCostForLevels(_def, 95, 5, _t);
            Assert.That(all, Is.EqualTo(five));
            Assert.That(UpgradeMath.UpgradeCostForLevels(_def, 100, 1, _t).IsZero, Is.True);
        }

        [Test]
        public void Max_Affordable_Is_Exact()
        {
            BigDouble budget = 1000;
            int n = UpgradeMath.MaxAffordableLevels(_def, 1, budget, _t);
            Assert.That(UpgradeMath.UpgradeCostForLevels(_def, 1, n, _t) <= budget, Is.True);
            Assert.That(UpgradeMath.UpgradeCostForLevels(_def, 1, n + 1, _t) > budget, Is.True);
            Assert.That(UpgradeMath.MaxAffordableLevels(_def, 1, BigDouble.FromLog10(400), _t), Is.EqualTo(99));
        }

        [Test]
        public void Capacity_Grows_And_Shipping_Has_None()
        {
            Assert.That(UpgradeMath.Capacity(_def, 50, _t), Is.GreaterThan(UpgradeMath.Capacity(_def, 1, _t)));
            var dock = new MachineDef { Stage = StageType.Shipping, InputPerCycle = 4, OutputPerCycle = 4, BaseCapacity = 0 };
            Assert.That(UpgradeMath.Capacity(dock, 10, _t), Is.EqualTo(0));
        }

        [Test]
        public void Visual_Tiers()
        {
            Assert.That(UpgradeMath.VisualTier(1, _t), Is.EqualTo(0));
            Assert.That(UpgradeMath.VisualTier(9, _t), Is.EqualTo(0));
            Assert.That(UpgradeMath.VisualTier(10, _t), Is.EqualTo(1));
            Assert.That(UpgradeMath.VisualTier(100, _t), Is.EqualTo(5));
        }
    }
}
