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

        /// <summary>Active skill (tap to trigger): a strong temporary multiplier with a cooldown.</summary>
        public string AbilityKey;
        public ModifierStat AbilityStat;
        public double AbilityMultiplier;
        public double AbilitySeconds;
        public double AbilityCooldownSeconds;
        /// <summary>For an instant cash ability: seconds of current income paid out (0 = timed multiplier).</summary>
        public double AbilityInstantIncomeSeconds;

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
            new ManagerDef { Id = "mgr_kemal", NameKey = "manager.mgr_kemal", TitleKey = "manager.title.cost", Rarity = Rarity.Common, Stat = ModifierStat.OperatingCost, HireDiamonds = 25, ColorHex = "#5B8DEF",
                AbilityKey = "ability.cost_freeze", AbilityStat = ModifierStat.OperatingCost, AbilityMultiplier = 0.3, AbilitySeconds = 180, AbilityCooldownSeconds = 1800 },
            new ManagerDef { Id = "mgr_baris", NameKey = "manager.mgr_baris", TitleKey = "manager.title.storage", Rarity = Rarity.Common, Stat = ModifierStat.StorageCapacity, HireDiamonds = 40, ColorHex = "#43B581",
                AbilityKey = "ability.express", AbilityStat = ModifierStat.StorageCapacity, AbilityMultiplier = 3, AbilitySeconds = 300, AbilityCooldownSeconds = 1800 },
            new ManagerDef { Id = "mgr_ayla", NameKey = "manager.mgr_ayla", TitleKey = "manager.title.speed", Rarity = Rarity.Rare, Stat = ModifierStat.ProductionSpeed, HireDiamonds = 120, ColorHex = "#F2994A",
                AbilityKey = "ability.overdrive", AbilityStat = ModifierStat.ProductionSpeed, AbilityMultiplier = 3, AbilitySeconds = 120, AbilityCooldownSeconds = 2400 },
            new ManagerDef { Id = "mgr_selin", NameKey = "manager.mgr_selin", TitleKey = "manager.title.offline", Rarity = Rarity.Epic, Stat = ModifierStat.OfflineEarnings, HireDiamonds = 220, ColorHex = "#9B51E0",
                AbilityKey = "ability.overtime", AbilityInstantIncomeSeconds = 900, AbilityCooldownSeconds = 7200 },
            new ManagerDef { Id = "mgr_deniz", NameKey = "manager.mgr_deniz", TitleKey = "manager.title.price", Rarity = Rarity.Legendary, Stat = ModifierStat.SellPrice, HireDiamonds = 380, ColorHex = "#F2C94C",
                AbilityKey = "ability.flash_sale", AbilityStat = ModifierStat.SellPrice, AbilityMultiplier = 2, AbilitySeconds = 150, AbilityCooldownSeconds = 2700 },
        };

        // 15 more managers, collected with cards (from crates, expeditions, events and the season road).
        static ManagerService()
        {
            (string id, Rarity r, ModifierStat stat)[] extra =
            {
                ("mgr_mert", Rarity.Common, ModifierStat.ProductionSpeed), ("mgr_elif", Rarity.Common, ModifierStat.SellPrice),
                ("mgr_can", Rarity.Common, ModifierStat.OperatingCost), ("mgr_zeynep", Rarity.Common, ModifierStat.OfflineEarnings),
                ("mgr_emre", Rarity.Rare, ModifierStat.StorageCapacity), ("mgr_derya", Rarity.Rare, ModifierStat.SellPrice),
                ("mgr_burak", Rarity.Rare, ModifierStat.OperatingCost), ("mgr_ece", Rarity.Rare, ModifierStat.ProductionSpeed),
                ("mgr_oguz", Rarity.Epic, ModifierStat.ProductionSpeed), ("mgr_seda", Rarity.Epic, ModifierStat.SellPrice),
                ("mgr_tolga", Rarity.Epic, ModifierStat.StorageCapacity), ("mgr_nil", Rarity.Epic, ModifierStat.OperatingCost),
                ("mgr_kaan", Rarity.Legendary, ModifierStat.ProductionSpeed), ("mgr_ipek", Rarity.Legendary, ModifierStat.OfflineEarnings),
                ("mgr_arda", Rarity.Legendary, ModifierStat.OperatingCost),
            };
            string[] colors = { "#6C8EAD", "#E07A5F", "#81B29A", "#F2CC8F", "#3D405B" };
            int i = 0;
            foreach (var (id, r, stat) in extra)
            {
                var template = Defs.Find(d => d.Stat == stat);
                Defs.Add(new ManagerDef
                {
                    Id = id, NameKey = "manager." + id, TitleKey = template.TitleKey, Rarity = r, Stat = stat,
                    HireDiamonds = 0, ColorHex = colors[i++ % colors.Length],
                    AbilityKey = template.AbilityKey, AbilityStat = template.AbilityStat,
                    AbilityMultiplier = template.AbilityMultiplier, AbilityInstantIncomeSeconds = template.AbilityInstantIncomeSeconds,
                    AbilitySeconds = template.AbilitySeconds * (1 + 0.15 * (int)r),
                    AbilityCooldownSeconds = template.AbilityCooldownSeconds * (1 - 0.08 * (int)r),
                });
            }
        }

        /// <summary>Cards needed to recruit a manager of this rarity without diamonds.</summary>
        public static int CardsToUnlock(Rarity r) => 10 * (1 << (int)r);   // 10 / 20 / 40 / 80
        /// <summary>Cards consumed to go from <paramref name="level"/> to level + 1.</summary>
        public static int CardsForLevel(int level) => level;

        private readonly GameSession _s;

        public ManagerService(GameSession session) => _s = session;

        public int Cards(string id) => _s.State.Live.ManagerCards.TryGetValue(id, out int n) ? n : 0;

        public void AddCards(string id, int count)
        {
            if (GetDef(id) == null || count <= 0) return;
            _s.State.Live.ManagerCards[id] = Cards(id) + count;
            Changed?.Invoke();
        }

        /// <summary>Recruit with collected cards (managers without a diamond price can only be recruited this way).</summary>
        public bool TryRecruitWithCards(string id)
        {
            var def = GetDef(id);
            if (def == null || IsOwned(id)) return false;
            int need = CardsToUnlock(def.Rarity);
            if (Cards(id) < need) return false;
            _s.State.Live.ManagerCards[id] = Cards(id) - need;
            Grant(id);
            return true;
        }

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
            if (def == null || IsOwned(id) || def.HireDiamonds <= 0) return false;
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
            int cards = CardsForLevel(m.Level);
            if (Cards(id) < cards) return false;
            if (!_s.Wallet.TrySpend(LevelUpCost(def, m.Level))) return false;
            _s.State.Live.ManagerCards[id] = Cards(id) - cards;
            m.Level++;
            _s.State.Stats.ManagerLevelUps++;
            Apply();
            return true;
        }

        public const string AbilityPrefix = "ability.";

        /// <summary>Ability duration grows 4% per manager level.</summary>
        public static double AbilityDuration(ManagerDef def, int level) => def.AbilitySeconds * (1d + 0.04 * (level - 1));

        public double AbilityCooldownRemaining(string id)
        {
            var m = GetOwned(id);
            if (m == null) return 0;
            return Math.Max(0, (m.AbilityReadyUtcTicks - _s.Time.TrustedNow.Ticks) / (double)TimeSpan.TicksPerSecond);
        }

        public double AbilityActiveRemaining(string id)
        {
            var m = GetOwned(id);
            if (m == null) return 0;
            return Math.Max(0, (m.AbilityEndUtcTicks - _s.Time.TrustedNow.Ticks) / (double)TimeSpan.TicksPerSecond);
        }

        /// <summary>Manager must be assigned to a factory; the effect applies to that factory.</summary>
        public bool CanUseAbility(string id)
        {
            var m = GetOwned(id);
            return m != null && m.AssignedFactoryId != null && AbilityCooldownRemaining(id) <= 0;
        }

        public bool TryUseAbility(string id)
        {
            if (!CanUseAbility(id)) return false;
            var def = GetDef(id);
            var m = GetOwned(id);
            long now = _s.Time.TrustedNow.Ticks;
            double cooldown = def.AbilityCooldownSeconds * (1 - 0.08 * _s.Engineering.Level(EngineeringService.Cooldown));
            m.AbilityReadyUtcTicks = now + (long)(cooldown * TimeSpan.TicksPerSecond);
            if (def.AbilityInstantIncomeSeconds > 0)
            {
                double factoryNet = 0;
                var fs = _s.State.GetFactory(m.AssignedFactoryId);
                for (int l = 0; l < fs.Lines.Count; l++)
                    if (fs.Lines[l].Unlocked) factoryNet += Math.Max(0, _s.AnalyzeLine(fs.FactoryId, l).NetPerSecond);
                double bonus = 1d + 0.05 * (m.Level - 1);
                _s.Wallet.Grant(BigDouble.FromDouble(Math.Max(_s.Tuning.StartingCash, factoryNet * def.AbilityInstantIncomeSeconds * bonus)));
            }
            else
            {
                m.AbilityEndUtcTicks = now + (long)(AbilityDuration(def, m.Level) * TimeSpan.TicksPerSecond);
            }
            ApplyAbilities();
            Changed?.Invoke();
            return true;
        }

        /// <summary>Adds/removes timed ability modifiers (called every second).</summary>
        public void ApplyAbilities()
        {
            long now = _s.Time.TrustedNow.Ticks;
            foreach (var def in Defs)
            {
                string source = AbilityPrefix + def.Id;
                var m = GetOwned(def.Id);
                bool active = m != null && m.AssignedFactoryId != null && m.AbilityEndUtcTicks > now && def.AbilityInstantIncomeSeconds <= 0;
                if (active) { if (!_s.Modifiers.HasSource(source)) _s.Modifiers.Set(source, def.AbilityStat, def.AbilityMultiplier, m.AssignedFactoryId); }
                else if (_s.Modifiers.HasSource(source)) _s.Modifiers.Remove(source);
            }
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
