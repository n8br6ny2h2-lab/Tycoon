using System;
using FactoryKing.Core.Content;
using FactoryKing.Core.Economy;
using FactoryKing.Core.Numerics;

namespace FactoryKing.Core.Simulation
{
    /// <summary>
    /// Deterministic discrete production simulation. Every unit that is sold was bought as raw material, travelled
    /// through every machine's buffer and was shipped — the visuals read this exact state.
    ///
    /// Each line is advanced in sub-steps no longer than its fastest machine cycle, processing machines upstream-first,
    /// so a unit can flow through several stages inside one frame without artificial buffer starvation.
    /// </summary>
    public sealed class ProductionSimulator
    {
        /// <summary>Hard cap on sub-steps per line per tick; protects frame time if someone passes a huge dt.</summary>
        public const int MaxSubSteps = 4096;

        private readonly GameContent _content;
        private readonly EconomyTuning _tuning;
        private readonly ModifierStack _modifiers;
        private readonly Wallet _wallet;
        private readonly GameState _state;
        private readonly EconomyLedger _ledger;

        // Scratch buffers reused every tick (no per-frame allocations).
        private MachineDef[] _defs = new MachineDef[8];
        private double[] _cycle = new double[8];
        private int[] _capacity = new int[8];

        public ProductionSimulator(GameContent content, EconomyTuning tuning, ModifierStack modifiers, Wallet wallet,
            GameState state, EconomyLedger ledger)
        {
            _content = content;
            _tuning = tuning;
            _modifiers = modifiers;
            _wallet = wallet;
            _state = state;
            _ledger = ledger;
        }

        /// <summary>Raised when a shipping machine sells a load: factory id, product id, units, revenue.</summary>
        public event Action<string, string, int, BigDouble> GoodsSold;

        public void Tick(double dt)
        {
            if (dt <= 0 || double.IsNaN(dt) || double.IsInfinity(dt)) return;
            foreach (var fs in _state.Factories)
            {
                if (!fs.Unlocked) continue;
                if (!_content.TryGetFactory(fs.FactoryId, out var def)) continue;
                for (int l = 0; l < fs.Lines.Count; l++)
                {
                    var line = fs.Lines[l];
                    if (line.Unlocked) TickLine(def, line, dt);
                }
            }
        }

        private void EnsureScratch(int n)
        {
            if (_defs.Length >= n) return;
            _defs = new MachineDef[n];
            _cycle = new double[n];
            _capacity = new int[n];
        }

        private void TickLine(FactoryDef factory, LineState line, double dt)
        {
            int n = factory.MachineChain.Count;
            EnsureScratch(n);

            double speedMod = _modifiers.Get(ModifierStat.ProductionSpeed, factory.Id);
            double capMod = _modifiers.Get(ModifierStat.StorageCapacity, factory.Id);
            double costMod = _modifiers.Get(ModifierStat.OperatingCost, factory.Id);

            double minCycle = double.MaxValue;
            for (int i = 0; i < n; i++)
            {
                var def = _content.GetMachine(factory.MachineChain[i]);
                int level = line.Machines[i].Level;
                _defs[i] = def;
                _cycle[i] = UpgradeMath.CycleSeconds(def, level, _tuning, speedMod);
                _capacity[i] = UpgradeMath.Capacity(def, level, _tuning, capMod);
                if (_cycle[i] < minCycle) minCycle = _cycle[i];
            }

            int steps = (int)Math.Ceiling(dt / minCycle);
            steps = Math.Max(1, Math.Min(MaxSubSteps, steps));
            double sub = dt / steps;

            // Accumulate costs per tick and pay once per category to keep the hot loop cheap.
            double workedElectricity = 0, workedWages = 0, workedMaintenance = 0;

            for (int s = 0; s < steps; s++)
            {
                for (int i = 0; i < n; i++)
                {
                    double worked = StepMachine(factory, line, i, sub, costMod);
                    if (worked <= 0) continue;
                    var def = _defs[i];
                    workedElectricity += worked * def.PowerKw * factory.ElectricityPricePerKwSecond * costMod;
                    workedWages += worked * factory.WagePerMachineSecond * costMod;
                    workedMaintenance += worked * UpgradeMath.MaintenancePerSecond(def, line.Machines[i].Level, _tuning) * costMod;
                }
            }

            Charge(LedgerCategory.Electricity, workedElectricity);
            Charge(LedgerCategory.Wages, workedWages);
            Charge(LedgerCategory.Maintenance, workedMaintenance);
        }

        private void Charge(LedgerCategory category, double amount)
        {
            if (amount <= 0) return;
            _wallet.PayOperating(amount);
            _ledger.Record(category, amount);
        }

        /// <summary>Advances one machine by <paramref name="dt"/>; returns seconds spent working.</summary>
        private double StepMachine(FactoryDef factory, LineState line, int i, double dt, double costMod)
        {
            var ms = line.Machines[i];
            double cycle = _cycle[i];
            double remaining = dt;
            double worked = 0;
            int guard = 0;

            while (remaining > 1e-12 && guard++ < 64)
            {
                if (!ms.InCycle && !TryStart(factory, line, i, costMod)) break;

                double need = (1d - ms.Progress) * cycle;
                if (remaining + 1e-12 >= need)
                {
                    remaining -= need;
                    worked += need;
                    ms.Progress = 1d;
                    if (!TryComplete(factory, line, i)) break;
                }
                else
                {
                    ms.Progress += remaining / cycle;
                    worked += remaining;
                    remaining = 0;
                    ms.Status = MachineStatus.Working;
                }
            }
            return worked;
        }

        private bool TryStart(FactoryDef factory, LineState line, int i, double costMod)
        {
            var def = _defs[i];
            var ms = line.Machines[i];

            if (def.Stage != StageType.Shipping && ms.OutputBuffer + def.OutputPerCycle > _capacity[i])
            {
                ms.Status = MachineStatus.Blocked;
                return false;
            }

            if (i == 0 || def.InputProductId == null)
            {
                // Raw input: buy one raw unit per output unit.
                double cost = factory.RawMaterialCost * def.OutputPerCycle * costMod;
                _wallet.PayOperating(cost);
                _ledger.Record(LedgerCategory.RawMaterials, cost);
                ms.InProcess = def.OutputPerCycle;
            }
            else
            {
                var upstream = line.Machines[i - 1];
                if (def.Stage == StageType.Shipping)
                {
                    if (upstream.OutputBuffer < 1) { ms.Status = MachineStatus.Starved; return false; }
                    int load = Math.Min(UpgradeMath.ShippingBatch(def, ms.Level, _tuning), upstream.OutputBuffer);
                    upstream.OutputBuffer -= load;
                    ms.InProcess = load;
                }
                else
                {
                    if (upstream.OutputBuffer < def.InputPerCycle) { ms.Status = MachineStatus.Starved; return false; }
                    upstream.OutputBuffer -= def.InputPerCycle;
                    ms.InProcess = def.InputPerCycle;
                }
            }

            ms.InCycle = true;
            ms.Progress = 0;
            ms.Status = MachineStatus.Working;
            return true;
        }

        private bool TryComplete(FactoryDef factory, LineState line, int i)
        {
            var def = _defs[i];
            var ms = line.Machines[i];

            if (def.Stage == StageType.Shipping)
            {
                int units = ms.InProcess;
                var product = _content.GetProduct(def.OutputProductId);
                double priceMod = _modifiers.Get(ModifierStat.SellPrice, factory.Id);
                BigDouble revenue = BigDouble.FromDouble(product.BaseValue) * (units * priceMod);
                _wallet.Earn(revenue);
                _ledger.Record(LedgerCategory.Revenue, revenue.ToDouble());
                _ledger.RecordUnitsSold(units);
                _state.TotalUnitsSold += units;
                FinishCycle(ms);
                GoodsSold?.Invoke(factory.Id, product.Id, units, revenue);
                return true;
            }

            if (ms.OutputBuffer + def.OutputPerCycle > _capacity[i])
            {
                ms.Status = MachineStatus.Blocked;
                return false;
            }

            ms.OutputBuffer += def.OutputPerCycle;
            if (def.Stage != StageType.Storage) // storage only moves goods; it does not create them
            {
                _state.UnitsProduced.TryGetValue(def.OutputProductId, out long produced);
                _state.UnitsProduced[def.OutputProductId] = produced + def.OutputPerCycle;
            }
            FinishCycle(ms);
            return true;
        }

        private static void FinishCycle(MachineState ms)
        {
            ms.InProcess = 0;
            ms.InCycle = false;
            ms.Progress = 0;
            ms.CyclesCompleted++;
        }
    }
}
