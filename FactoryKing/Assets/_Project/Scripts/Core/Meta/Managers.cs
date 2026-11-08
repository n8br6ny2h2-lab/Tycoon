using System;
using System.Collections.Generic;
using FactoryKing.Core.Economy;
using FactoryKing.Core.Numerics;
using FactoryKing.Core.Simulation;

namespace FactoryKing.Core.Meta
{
    public enum Rarity { Common = 0, Rare = 1, Epic = 2, Legendary = 3 }

    public sealed class ManagerDef
    {
        public string Id;
        public string NameKey;
        public string TitleKey;
        public Rarity Rarity;
        public ModifierStat Stat;
        public long HireDiamonds;
        public string ColorHex;

        public int MaxLevel => 10 + 5 * (int)Rarity;          // 10 / 15 / 20 / 25
        public double BaseBonus => 0.10 + 0.05 * (int)Rarity;  // 10% .. 25%
        public double BonusPerLevel => 0.02 + 0.01 * (int)Rarity;
    }

    /// <summary>Five manager characters with rarity, levels and factory assignment. Extensible: add defs.</summary>
    public sealed class ManagerService
    {
        public const string SourcePrefix = "manager.";

        public static readonly List<ManagerDef> Defs = new List<ManagerDef>
        {
            new ManagerDef { Id = "mgr_kemal", NameKey = "manager.mgr_kemal", TitleKey = "manager.title.cost", Rarity = Rarity.Common, Stat = ModifierStat.OperatingCost, HireDiamonds = 25, ColorHex = "#5B8DEF" },
            new ManagerDef { Id = "mgr_baris", NameKey = "manager.mgr_baris", TitleKey = "manager.title.storage", Rarity = Rarity.Common, Stat = ModifierStat.StorageCapacity, HireDiamonds = 40, ColorHex = "#43B581" },
            new ManagerDef { Id = "mgr_ayla", NameKey = "manager.mgr_ayla", TitleKey = "manager.title.speed", Rarity = Rarity.Rare, Stat = ModifierStat.ProductionSpeed, HireDiamonds = 120, ColorHex = "#F2994A" },
            new ManagerDef { Id = "mgr_selin", NameKey = "manager.mgr_selin", TitleKey = "manager.title.offline", Rarity = Rarity.Epic, Stat = ModifierStat.OfflineEarnings, HireDiamonds = 220, ColorHex = "#9B51E0" },
            new ManagerDef { Id = "mgr_deniz", NameKey = "manager.mgr_deniz", TitleKey = "manager.title.price", Rarity = Rarity.Legendary, Stat = ModifierStat.SellPrice, HireDiamonds = 380, ColorHex = "#F2C94C" },
        };

        private readonly GameSession _s;

        public ManagerService(GameSession session) => _s = session;

        public event Action Changed;

        public static ManagerDef GetDef(string id) => Defs.Find(d => d.Id == id);

        public ManagerState GetOwned(string id) => _s.State.Managers.Find(m => m.Id == id);
        public bool IsOwned(string id) => GetOwned(id) != null;

        public static double Bonus(ManagerDef def, int level) =>
            def.BaseBonus + def.BonusPerLevel * (Math.Max(1, Math.Min(def.MaxLevel, level)) - 1);

        /// <summary>The multiplier this manager applies (cost stats become reductions).</summary>
        public static double Multiplier(ManagerDef def, int level)
        {
            double b = Bonus(def, level);
            return def.Stat == ModifierStat.OperatingCost || def.Stat == ModifierStat.UpgradeCost ? 1d / (1d + b) : 1d + b;
        }

        public BigDouble LevelUpCost(ManagerDef def, int level) =>
            BigDouble.FromLog10(Math.Log10(2000d * (1 + (int)def.Rarity)) + (level - 1) * Math.Log10(2.6));

        public bool TryHire(string id)
        {
            var def = GetDef(id);
            if (def == null || IsOwned(id)) return false;
            if (!_s.Wallet.TrySpendDiamonds(def.HireDiamonds)) return false;
            Grant(id);
            return true;
        }

        /// <summary>Adds a manager without payment (starter pack, events). No-op if owned.</summary>
        public void Grant(string id)
        {
            if (GetDef(id) == null || IsOwned(id)) return;
            _s.State.Managers.Add(new ManagerState { Id = id, Level = 1 });
            Apply();
            _s.Analytics.Track(AnalyticsEvents.ManagerHired, "id", id);
        }

        public bool TryLevelUp(string id)
        {
            var def = GetDef(id);
            var m = GetOwned(id);
            if (def == null || m == null || m.Level >= def.MaxLevel) return false;
            if (!_s.Wallet.TrySpend(LevelUpCost(def, m.Level))) return false;
            m.Level++;
            _s.State.Stats.ManagerLevelUps++;
            Apply();
            return true;
        }

        /// <summary>Assigns to an owned factory (one manager per factory). Null factory unassigns.</summary>
        public bool Assign(string id, string factoryId)
        {
            var m = GetOwned(id);
            if (m == null) return false;
            if (factoryId != null && !_s.State.GetFactory(factoryId).Unlocked) return false;
            if (factoryId != null)
                foreach (var other in _s.State.Managers)
                    if (other != m && other.AssignedFactoryId == factoryId) other.AssignedFactoryId = null;
            m.AssignedFactoryId = factoryId;
            Apply();
            return true;
        }

        public ManagerState AssignedTo(string factoryId) => _s.State.Managers.Find(m => m.AssignedFactoryId == factoryId);

        /// <summary>Rebuilds every manager modifier from state.</summary>
        public void Apply()
        {
            foreach (var def in Defs) _s.Modifiers.Remove(SourcePrefix + def.Id);
            foreach (var m in _s.State.Managers)
            {
                var def = GetDef(m.Id);
                if (def == null || m.AssignedFactoryId == null) continue;
                _s.Modifiers.Set(SourcePrefix + def.Id, def.Stat, Multiplier(def, m.Level), m.AssignedFactoryId);
            }
            Changed?.Invoke();
        }
    }
}
