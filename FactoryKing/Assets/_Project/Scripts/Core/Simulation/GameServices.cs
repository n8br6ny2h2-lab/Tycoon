using System;
using FactoryKing.Core.Content;
using FactoryKing.Core.Economy;
using FactoryKing.Core.Numerics;

namespace FactoryKing.Core.Simulation
{
    public readonly struct UpgradeQuote
    {
        public readonly int CurrentLevel;
        public readonly int Levels;
        public readonly BigDouble Cost;
        public readonly bool CanAfford;
        public readonly bool IsMaxLevel;

        public UpgradeQuote(int currentLevel, int levels, BigDouble cost, bool canAfford, bool isMaxLevel)
        {
            CurrentLevel = currentLevel;
            Levels = levels;
            Cost = cost;
            CanAfford = canAfford;
            IsMaxLevel = isMaxLevel;
        }

        public int TargetLevel => CurrentLevel + Levels;
    }

    /// <summary>Machine upgrades (levels 1..MaxMachineLevel).</summary>
    public sealed class UpgradeService
    {
        /// <summary>Pass as count to buy as many levels as affordable.</summary>
        public const int Max = -1;

        private readonly GameContent _content;
        private readonly EconomyTuning _tuning;
        private readonly GameState _state;
        private readonly Wallet _wallet;
        private readonly ModifierStack _modifiers;

        public UpgradeService(GameContent content, EconomyTuning tuning, GameState state, Wallet wallet, ModifierStack modifiers)
        {
            _content = content;
            _tuning = tuning;
            _state = state;
            _wallet = wallet;
            _modifiers = modifiers;
        }

        /// <summary>machine, old level, new level.</summary>
        public event Action<MachineRef, int, int> MachineUpgraded;

        public MachineDef GetDef(MachineRef r) => _content.GetMachine(_content.GetFactory(r.FactoryId).MachineChain[r.Index]);

        public UpgradeQuote GetQuote(MachineRef r, int count)
        {
            var ms = _state.GetMachine(r);
            var def = GetDef(r);
            double costMod = _modifiers.Get(ModifierStat.UpgradeCost, r.FactoryId);
            int remaining = _tuning.MaxMachineLevel - ms.Level;
            if (remaining <= 0) return new UpgradeQuote(ms.Level, 0, BigDouble.Zero, false, true);

            int levels;
            if (count == Max)
            {
                levels = UpgradeMath.MaxAffordableLevels(def, ms.Level, _wallet.Cash, _tuning, costMod);
                if (levels == 0) levels = 1; // show the price of the next level
            }
            else
            {
                levels = Math.Max(1, Math.Min(count, remaining));
            }
            var cost = UpgradeMath.UpgradeCostForLevels(def, ms.Level, levels, _tuning, costMod);
            return new UpgradeQuote(ms.Level, levels, cost, _wallet.CanAfford(cost), false);
        }

        public bool TryUpgrade(MachineRef r, int count = 1)
        {
            var fs = _state.GetFactory(r.FactoryId);
            if (!fs.Unlocked || !fs.Lines[r.Line].Unlocked) return false;
            var quote = GetQuote(r, count);
            if (quote.IsMaxLevel || !quote.CanAfford || !_wallet.TrySpend(quote.Cost)) return false;

            var ms = _state.GetMachine(r);
            int old = ms.Level;
            ms.Level = quote.TargetLevel;
            _state.TotalInvested += quote.Cost;
            _state.Stats.Upgrades += quote.Levels;
            if (ms.Level > _state.Stats.HighestMachineLevel) _state.Stats.HighestMachineLevel = ms.Level;
            MachineUpgraded?.Invoke(r, old, ms.Level);
            return true;
        }
    }

    /// <summary>Buying factories and production lines.</summary>
    public sealed class FactoryService
    {
        private readonly GameContent _content;
        private readonly GameState _state;
        private readonly Wallet _wallet;

        public FactoryService(GameContent content, GameState state, Wallet wallet)
        {
            _content = content;
            _state = state;
            _wallet = wallet;
        }

        public event Action<string> FactoryUnlocked;
        /// <summary>factory id, line index.</summary>
        public event Action<string, int> LineUnlocked;
        public event Action<string> ActiveFactoryChanged;

        public bool IsUnlocked(string factoryId) => _state.GetFactory(factoryId).Unlocked;
        public bool IsLineUnlocked(string factoryId, int line) => _state.GetFactory(factoryId).Lines[line].Unlocked;

        /// <summary>A factory can be bought once the previous one (by order) is owned.</summary>
        public bool CanUnlockFactory(string factoryId, out BigDouble cost)
        {
            var def = _content.GetFactory(factoryId);
            cost = def.UnlockCost;
            if (IsUnlocked(factoryId)) return false;
            int index = _state.Factories.FindIndex(f => f.FactoryId == factoryId);
            if (index > 0 && !_state.Factories[index - 1].Unlocked) return false;
            return _wallet.CanAfford(cost);
        }

        public bool TryUnlockFactory(string factoryId)
        {
            if (!CanUnlockFactory(factoryId, out var cost) || !_wallet.TrySpend(cost)) return false;
            var fs = _state.GetFactory(factoryId);
            fs.Unlocked = true;
            fs.Lines[0].Unlocked = true;
            _state.TotalInvested += cost;
            _state.Stats.FactoriesBought++;
            FactoryUnlocked?.Invoke(factoryId);
            return true;
        }

        /// <summary>Lines unlock in order inside an owned factory.</summary>
        public bool CanUnlockLine(string factoryId, int line, out BigDouble cost)
        {
            var def = _content.GetFactory(factoryId);
            cost = BigDouble.Zero;
            if (line < 0 || line >= def.LineCount) return false;
            cost = def.LineUnlockCosts[line];
            var fs = _state.GetFactory(factoryId);
            if (!fs.Unlocked || fs.Lines[line].Unlocked) return false;
            if (line > 0 && !fs.Lines[line - 1].Unlocked) return false;
            return _wallet.CanAfford(cost);
        }

        public bool TryUnlockLine(string factoryId, int line)
        {
            if (!CanUnlockLine(factoryId, line, out var cost) || !_wallet.TrySpend(cost)) return false;
            _state.GetFactory(factoryId).Lines[line].Unlocked = true;
            _state.TotalInvested += cost;
            _state.Stats.LinesBought++;
            LineUnlocked?.Invoke(factoryId, line);
            return true;
        }

        public bool SetActiveFactory(string factoryId)
        {
            if (!IsUnlocked(factoryId) || _state.ActiveFactoryId == factoryId) return false;
            _state.ActiveFactoryId = factoryId;
            ActiveFactoryChanged?.Invoke(factoryId);
            return true;
        }
    }
}
