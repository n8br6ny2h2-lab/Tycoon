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
        public double OfflineEfficiency = 0.6;
        /// <summary>Away time below this is not reported as offline earnings (seconds).</summary>
        public double OfflineMinimumSeconds = 60;

        /// <summary>Run earnings needed before prestige is offered.</summary>
        public double PrestigeMinRunEarnings = 1e8;
        /// <summary>Crowns = floor((runEarnings / PrestigeCrownDivisor) ^ PrestigeExponent).</summary>
        public double PrestigeCrownDivisor = 1e7;
        /// <summary>
        /// Fourth root: each extra crown needs much more income, so prestige multiplies progress without the
        /// runaway loop a square root causes (verified by the 24 h progression probe).
        /// </summary>
        public double PrestigeExponent = 0.25;
        /// <summary>Permanent sell-price bonus per crown (0.1 = +10%).</summary>
        public double PrestigeBonusPerCrown = 0.10;

        public static EconomyTuning CreateDefault() => new EconomyTuning();
    }
}
