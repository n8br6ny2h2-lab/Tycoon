using System;

namespace FactoryKing.Core.Economy
{
    /// <summary>Global economy knobs. Serializable so the Unity layer can mirror it in a ScriptableObject.</summary>
    [Serializable]
    public sealed class EconomyTuning
    {
        public int MaxMachineLevel = 100;

        /// <summary>Linear speed gain per level (0.1 = +10% per level).</summary>
        public double SpeedPerLevel = 0.10;
        /// <summary>Levels that double speed when reached.</summary>
        public int[] SpeedMilestones = { 10, 25, 50, 75, 100 };
        public double MilestoneSpeedMultiplier = 2.0;

        /// <summary>Linear output-buffer growth per level (0.05 = +5% per level).</summary>
        public double CapacityPerLevel = 0.05;
        /// <summary>Linear growth of a machine's power draw per level.</summary>
        public double PowerPerLevel = 0.06;
        /// <summary>Power plant capacity gained per level (0.5 = +50% of base).</summary>
        public double PowerCapacityPerLevel = 0.5;
        public int MaxPowerLevel = 60;

        /// <summary>Linear maintenance growth per level.</summary>
        public double MaintenancePerLevel = 0.06;

        /// <summary>Levels at which a machine's visual tier changes (index = tier).</summary>
        public int[] VisualTierLevels = { 1, 10, 25, 50, 75, 100 };

        public double StartingCash = 60;
        public long StartingDiamonds = 25;

        /// <summary>Company value = cash + invested * InvestedAssetWeight + netIncomePerSecond * IncomeValuationSeconds.</summary>
        public double InvestedAssetWeight = 0.6;
        public double IncomeValuationSeconds = 600;

        /// <summary>Rolling window for income statistics, seconds.</summary>
        public int StatsWindowSeconds = 60;

        /// <summary>Base offline earnings window (hours).</summary>
        public double OfflineCapHours = 8;
        /// <summary>Share of steady-state profit credited while away (the rewarded ad doubles it).</summary>
        public double OfflineEfficiency = 0.5;
        /// <summary>Away time below this is not reported as offline earnings (seconds).</summary>
        public double OfflineMinimumSeconds = 60;

        /// <summary>Run earnings needed before prestige is offered.</summary>
        public double PrestigeMinRunEarnings = 1e9;
        /// <summary>Crowns = floor(PrestigeLogFactor * log10(runEarnings / PrestigeCrownDivisor)).</summary>
        public double PrestigeCrownDivisor = 1e8;
        /// <summary>
        /// Logarithmic crowns: with 20 factories earnings span ~30 orders of magnitude, so any power curve (square or
        /// fourth root) explodes; a log keeps prestige a steady multiplier (verified by the season pacing test).
        /// </summary>
        public double PrestigeLogFactor = 2;
        /// <summary>Permanent sell-price bonus per crown (0.1 = +10%).</summary>
        public double PrestigeBonusPerCrown = 0.10;

        /// <summary>Length of a season; the run resets when it ends (permanent rewards are kept).</summary>
        public double SeasonDays = 30;
        /// <summary>
        /// Season momentum: +X sale price per elapsed season day. Catch-up so late factories stay reachable for
        /// players with less time, and the pace stays smooth instead of hitting a wall.
        /// </summary>
        public double SeasonMomentumPerDay = 0.15;

        public static EconomyTuning CreateDefault() => new EconomyTuning();
    }
}
