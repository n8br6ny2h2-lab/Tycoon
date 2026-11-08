using System;
using System.Collections.Generic;
using FactoryKing.Core.Economy;

namespace FactoryKing.Core.Content
{
    /// <summary>Static checks that catch broken content before it reaches players. Returns human-readable errors.</summary>
    public static class ContentValidator
    {
        public static List<string> Validate(GameContent content, EconomyTuning tuning)
        {
            var errors = new List<string>();
            var productIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (var p in content.Products)
            {
                if (string.IsNullOrEmpty(p.Id)) errors.Add("Product with empty id.");
                else if (!productIds.Add(p.Id)) errors.Add($"Duplicate product id '{p.Id}'.");
                if (p.BaseValue < 0) errors.Add($"Product '{p.Id}' has negative value.");
            }

            var machineIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (var m in content.Machines)
            {
                if (!machineIds.Add(m.Id)) errors.Add($"Duplicate machine id '{m.Id}'.");
                if (m.BaseCycleSeconds <= 0) errors.Add($"Machine '{m.Id}' has non-positive cycle time.");
                if (m.InputPerCycle < 1 || m.OutputPerCycle < 1) errors.Add($"Machine '{m.Id}' has invalid per-cycle amounts.");
                if (m.UpgradeCostGrowth < 1) errors.Add($"Machine '{m.Id}' has upgrade growth < 1 (prices would fall).");
                if (m.BaseUpgradeCost <= 0) errors.Add($"Machine '{m.Id}' has non-positive upgrade cost.");
                if (m.OutputProductId == null || !productIds.Contains(m.OutputProductId))
                    errors.Add($"Machine '{m.Id}' outputs unknown product '{m.OutputProductId}'.");
                if (m.InputProductId != null && !productIds.Contains(m.InputProductId))
                    errors.Add($"Machine '{m.Id}' consumes unknown product '{m.InputProductId}'.");
            }

            var factoryIds = new HashSet<string>(StringComparer.Ordinal);
            var usedMachines = new HashSet<string>(StringComparer.Ordinal);
            foreach (var f in content.Factories)
            {
                if (!factoryIds.Add(f.Id)) errors.Add($"Duplicate factory id '{f.Id}'.");
                if (f.LineCount < 1) errors.Add($"Factory '{f.Id}' has no lines.");
                if (f.LineUnlockCosts.Length > 0 && f.LineUnlockCosts[0] != 0) errors.Add($"Factory '{f.Id}' first line must be free.");
                ValidateChain(content, tuning, f, errors, usedMachines);
            }
            return errors;
        }

        private static void ValidateChain(GameContent content, EconomyTuning tuning, FactoryDef f, List<string> errors, HashSet<string> used)
        {
            var chain = f.MachineChain;
            if (chain.Count < 2) { errors.Add($"Factory '{f.Id}' chain too short."); return; }

            var defs = new List<MachineDef>();
            foreach (var id in chain)
            {
                try { defs.Add(content.GetMachine(id)); }
                catch (KeyNotFoundException) { errors.Add($"Factory '{f.Id}' references unknown machine '{id}'."); return; }
                if (!used.Add(id)) errors.Add($"Machine '{id}' is used by more than one chain slot (each slot needs its own variant).");
            }

            if (defs[0].Stage != StageType.RawInput || defs[0].InputProductId != null)
                errors.Add($"Factory '{f.Id}' must start with a RawInput machine without input product.");
            if (defs[defs.Count - 1].Stage != StageType.Shipping)
                errors.Add($"Factory '{f.Id}' must end with a Shipping machine.");

            for (int i = 1; i < defs.Count; i++)
            {
                if (defs[i].InputProductId != defs[i - 1].OutputProductId)
                    errors.Add($"Factory '{f.Id}': '{defs[i].Id}' expects '{defs[i].InputProductId}' but '{defs[i - 1].Id}' makes '{defs[i - 1].OutputProductId}'.");
                if ((int)defs[i].Stage < (int)defs[i - 1].Stage)
                    errors.Add($"Factory '{f.Id}': stage order goes backwards at '{defs[i].Id}'.");
                // The upstream buffer must hold at least one downstream batch at level 1, or the line deadlocks.
                int upstreamCap = UpgradeMath.Capacity(defs[i - 1], 1, tuning);
                if (upstreamCap < defs[i].InputPerCycle)
                    errors.Add($"Factory '{f.Id}': buffer of '{defs[i - 1].Id}' ({upstreamCap}) is smaller than the batch of '{defs[i].Id}'.");
            }

            var final = content.GetProduct(defs[defs.Count - 1].OutputProductId);
            if (final.BaseValue <= 0) errors.Add($"Factory '{f.Id}' sells '{final.Id}' with no value.");

            // Every line must be profitable at every uniform level: no "upgrade makes you poorer" traps.
            var levels = new int[defs.Count];
            for (int level = 1; level <= tuning.MaxMachineLevel; level++)
            {
                for (int i = 0; i < levels.Length; i++) levels[i] = level;
                var report = LineAnalyzer.Analyze(content, tuning, null, f, levels);
                if (report.NetPerSecond <= 0)
                {
                    errors.Add($"Factory '{f.Id}' is not profitable at uniform level {level} (net {report.NetPerSecond:0.###}/s).");
                    break;
                }
            }
        }
    }
}
