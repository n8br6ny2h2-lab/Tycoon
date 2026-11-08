using System;
using FactoryKing.Core.Content;
using FactoryKing.Core.Simulation;

namespace FactoryKing.Core.Economy
{
    /// <summary>Steady-state economics of one production line at its current machine levels.</summary>
    public sealed class LineReport
    {
        /// <summary>Finished goods sold per second when the line runs continuously.</summary>
        public double FinalUnitsPerSecond;
        /// <summary>Index of the slowest machine in the chain.</summary>
        public int BottleneckIndex;
        /// <summary>Per machine: throughput expressed in finished-goods units per second at 100% utilisation.</summary>
        public double[] MachineCapacityPerSecond;
        /// <summary>Per machine: fraction of time busy in steady state (bottleneck = 1).</summary>
        public double[] Utilization;

        public double RevenuePerSecond;
        public double RawCostPerSecond;
        public double ElectricityPerSecond;
        public double WagesPerSecond;
        public double MaintenancePerSecond;
        /// <summary>Average electrical demand of the line in steady state (kW).</summary>
        public double PowerDemandKw;

        public double CostsPerSecond => RawCostPerSecond + ElectricityPerSecond + WagesPerSecond + MaintenancePerSecond;
        public double NetPerSecond => RevenuePerSecond - CostsPerSecond;
    }

    /// <summary>
    /// Closed-form throughput/profit analysis. Used for UI ("income per second"), upgrade previews, offline earnings
    /// and balance tests. Must agree with <see cref="ProductionSimulator"/> in the long run (verified by tests).
    /// </summary>
    public static class LineAnalyzer
    {
        public static LineReport Analyze(GameContent content, EconomyTuning tuning, ModifierStack modifiers,
            FactoryDef factory, LineState line)
        {
            int[] levels = new int[factory.MachineChain.Count];
            for (int i = 0; i < levels.Length; i++) levels[i] = line.Machines[i].Level;
            return Analyze(content, tuning, modifiers, factory, levels);
        }

        public static LineReport Analyze(GameContent content, EconomyTuning tuning, ModifierStack modifiers,
            FactoryDef factory, int[] levels)
        {
            int n = factory.MachineChain.Count;
            double speedMod = modifiers?.Get(ModifierStat.ProductionSpeed, factory.Id) ?? 1d;
            double costMod = modifiers?.Get(ModifierStat.OperatingCost, factory.Id) ?? 1d;
            double priceMod = modifiers?.Get(ModifierStat.SellPrice, factory.Id) ?? 1d;
            double shipMod = modifiers?.Get(ModifierStat.ShippingCapacity, factory.Id) ?? 1d;

            var defs = new MachineDef[n];
            for (int i = 0; i < n; i++) defs[i] = content.GetMachine(factory.MachineChain[i]);

            // finalPerOutput[i]: finished goods obtained from one output unit of machine i.
            var finalPerOutput = new double[n];
            finalPerOutput[n - 1] = 1d;
            for (int i = n - 2; i >= 0; i--)
            {
                var next = defs[i + 1];
                double ratio = next.Stage == StageType.Shipping ? 1d : (double)next.OutputPerCycle / next.InputPerCycle;
                finalPerOutput[i] = finalPerOutput[i + 1] * ratio;
            }

            var report = new LineReport
            {
                MachineCapacityPerSecond = new double[n],
                Utilization = new double[n],
            };

            double bottleneck = double.MaxValue;
            for (int i = 0; i < n; i++)
            {
                double cycle = UpgradeMath.CycleSeconds(defs[i], levels[i], tuning, speedMod);
                double outputsPerCycle = defs[i].Stage == StageType.Shipping
                    ? UpgradeMath.ShippingBatch(defs[i], levels[i], tuning, shipMod)
                    : defs[i].OutputPerCycle;
                double cap = outputsPerCycle / cycle * finalPerOutput[i];
                report.MachineCapacityPerSecond[i] = cap;
                if (cap < bottleneck)
                {
                    bottleneck = cap;
                    report.BottleneckIndex = i;
                }
            }

            report.FinalUnitsPerSecond = bottleneck;
            var product = content.GetProduct(defs[n - 1].OutputProductId);
            report.RevenuePerSecond = bottleneck * product.BaseValue * priceMod;
            report.RawCostPerSecond = bottleneck / finalPerOutput[0] * factory.RawMaterialCost * costMod;

            for (int i = 0; i < n; i++)
            {
                double util = Math.Min(1d, bottleneck / report.MachineCapacityPerSecond[i]);
                report.Utilization[i] = util;
                double kw = UpgradeMath.PowerKw(defs[i], levels[i], tuning);
                report.PowerDemandKw += util * kw;
                report.ElectricityPerSecond += util * kw * factory.ElectricityPricePerKwSecond * costMod;
                report.WagesPerSecond += util * factory.WagePerMachineSecond * costMod;
                report.MaintenancePerSecond += util * UpgradeMath.MaintenancePerSecond(defs[i], levels[i], tuning) * costMod;
            }
            return report;
        }

        /// <summary>Sum of steady-state net income of every unlocked line in every unlocked factory.</summary>
        public static double TotalNetPerSecond(GameContent content, EconomyTuning tuning, ModifierStack modifiers, GameState state)
        {
            double total = 0;
            foreach (var fs in state.Factories)
            {
                if (!fs.Unlocked || !content.TryGetFactory(fs.FactoryId, out var def)) continue;
                foreach (var line in fs.Lines)
                    if (line.Unlocked) total += Analyze(content, tuning, modifiers, def, line).NetPerSecond;
            }
            return total;
        }
    }
}
