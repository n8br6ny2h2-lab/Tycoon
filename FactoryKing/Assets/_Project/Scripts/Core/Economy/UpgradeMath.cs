using System;
using FactoryKing.Core.Content;
using FactoryKing.Core.Numerics;

namespace FactoryKing.Core.Economy
{
    /// <summary>Pure formulas for machine stats per level. No state, fully unit-testable.</summary>
    public static class UpgradeMath
    {
        public static int ClampLevel(int level, EconomyTuning t) => Math.Max(1, Math.Min(t.MaxMachineLevel, level));

        public static double SpeedMultiplier(int level, EconomyTuning t)
        {
            level = ClampLevel(level, t);
            double linear = 1d + t.SpeedPerLevel * (level - 1);
            int reached = 0;
            foreach (int m in t.SpeedMilestones) if (level >= m) reached++;
            return linear * Math.Pow(t.MilestoneSpeedMultiplier, reached);
        }

        public static double CycleSeconds(MachineDef def, int level, EconomyTuning t, double extraSpeedMultiplier = 1d)
        {
            double speed = SpeedMultiplier(level, t) * Math.Max(1e-6, extraSpeedMultiplier);
            return def.BaseCycleSeconds / speed;
        }

        /// <summary>Output buffer capacity. Shipping machines have no buffer (they sell directly).</summary>
        public static int Capacity(MachineDef def, int level, EconomyTuning t, double extraCapacityMultiplier = 1d)
        {
            if (def.Stage == StageType.Shipping) return 0;
            level = ClampLevel(level, t);
            double cap = def.BaseCapacity * (1d + t.CapacityPerLevel * (level - 1)) * Math.Max(0.01, extraCapacityMultiplier);
            // Buffers must hold at least two batches of the downstream consumer to avoid artificial starvation.
            return Math.Max(def.OutputPerCycle * 2, (int)Math.Floor(cap));
        }

        /// <summary>Maximum units loaded per shipping cycle (truck size). Partial loads are allowed.</summary>
        public static int ShippingBatch(MachineDef def, int level, EconomyTuning t, double capacityMultiplier = 1d) =>
            Math.Max(1, (int)Math.Floor(def.InputPerCycle * Math.Max(1d, capacityMultiplier) + 1e-9));

        /// <summary>Electrical load while running; bigger (upgraded) machines draw more power.</summary>
        public static double PowerKw(MachineDef def, int level, EconomyTuning t)
        {
            level = ClampLevel(level, t);
            return def.PowerKw * (1d + t.PowerPerLevel * (level - 1));
        }

        public static double MaintenancePerSecond(MachineDef def, int level, EconomyTuning t)
        {
            level = ClampLevel(level, t);
            return def.MaintenancePerSecond * (1d + t.MaintenancePerLevel * (level - 1));
        }

        /// <summary>Price to go from <paramref name="level"/> to level + 1.</summary>
        public static BigDouble UpgradeCost(MachineDef def, int level, EconomyTuning t, double costMultiplier = 1d)
        {
            double log = Math.Log10(def.BaseUpgradeCost * costMultiplier) + (level - 1) * Math.Log10(def.UpgradeCostGrowth);
            return BigDouble.FromLog10(log);
        }

        /// <summary>Total price for <paramref name="count"/> consecutive levels starting at <paramref name="level"/> (geometric series).</summary>
        public static BigDouble UpgradeCostForLevels(MachineDef def, int level, int count, EconomyTuning t, double costMultiplier = 1d)
        {
            count = Math.Min(count, t.MaxMachineLevel - level);
            if (count <= 0) return BigDouble.Zero;
            BigDouble first = UpgradeCost(def, level, t, costMultiplier);
            double g = def.UpgradeCostGrowth;
            if (Math.Abs(g - 1d) < 1e-12) return first * count;
            // first * (g^count - 1) / (g - 1)
            BigDouble gPow = BigDouble.FromLog10(count * Math.Log10(g));
            return first * (gPow - BigDouble.One) / (g - 1d);
        }

        /// <summary>How many levels can be bought with <paramref name="budget"/>, capped by the max level.</summary>
        public static int MaxAffordableLevels(MachineDef def, int level, BigDouble budget, EconomyTuning t, double costMultiplier = 1d)
        {
            int remaining = t.MaxMachineLevel - level;
            if (remaining <= 0 || budget <= BigDouble.Zero) return 0;
            // Binary search over the closed-form series.
            int lo = 0, hi = remaining;
            while (lo < hi)
            {
                int mid = (lo + hi + 1) / 2;
                if (UpgradeCostForLevels(def, level, mid, t, costMultiplier) <= budget) lo = mid;
                else hi = mid - 1;
            }
            return lo;
        }

        /// <summary>Total spent on a machine to bring it from level 1 to <paramref name="level"/>.</summary>
        public static BigDouble InvestedUpTo(MachineDef def, int level, EconomyTuning t) =>
            UpgradeCostForLevels(def, 1, level - 1, t);

        public static int VisualTier(int level, EconomyTuning t)
        {
            int tier = 0;
            for (int i = 0; i < t.VisualTierLevels.Length; i++) if (level >= t.VisualTierLevels[i]) tier = i;
            return tier;
        }
    }
}
