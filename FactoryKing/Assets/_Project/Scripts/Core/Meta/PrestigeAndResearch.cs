using System;
using System.Collections.Generic;
using FactoryKing.Core.Economy;
using FactoryKing.Core.Numerics;
using FactoryKing.Core.Simulation;

namespace FactoryKing.Core.Meta
{
    /// <summary>What the player gets and keeps; shown before confirming (nothing hidden).</summary>
    public sealed class PrestigePreview
    {
        public bool Available;
        public BigDouble RunEarnings;
        public BigDouble RequiredRunEarnings;
        public double CrownsGained;
        public double CrownsAfter;
        public double MultiplierNow;
        public double MultiplierAfter;
        public double StartingCashAfter;
        public string BadgeId;
        public string DecorId;
        public readonly List<string> Resets = new List<string> { "prestige.reset.cash", "prestige.reset.factories", "prestige.reset.levels", "prestige.reset.orders" };
        public readonly List<string> Keeps = new List<string> { "prestige.keep.diamonds", "prestige.keep.managers", "prestige.keep.research", "prestige.keep.achievements", "prestige.keep.collection", "prestige.keep.purchases" };
        public readonly List<string> Bonuses = new List<string>();
    }

    /// <summary>
    /// Prestige ("Yeniden Yapılanma"): converts run earnings into permanent crowns (+10% sale price each), badges,
    /// decorations and starting bonuses. Managers, research, diamonds, purchases and achievements are kept.
    /// </summary>
    public sealed class PrestigeService
    {
        public const string Source = "prestige";
        private readonly GameSession _s;

        public PrestigeService(GameSession session) => _s = session;

        public event Action Prestiged;

        public double CrownsFor(BigDouble runEarnings)
        {
            if (!runEarnings.IsPositive) return 0;
            double ratio = (runEarnings / _s.Tuning.PrestigeCrownDivisor).ToDouble();
            return Math.Floor(Math.Sqrt(Math.Max(0, ratio)));
        }

        public double MultiplierFor(double crowns) => 1d + crowns * _s.Tuning.PrestigeBonusPerCrown;

        public double StartingCashFor(double crowns) => _s.Tuning.StartingCash * (1d + crowns);

        public PrestigePreview Preview()
        {
            var p = _s.State.Prestige;
            var pv = new PrestigePreview
            {
                RunEarnings = _s.State.RunEarnings,
                RequiredRunEarnings = _s.Tuning.PrestigeMinRunEarnings,
                CrownsGained = CrownsFor(_s.State.RunEarnings),
                MultiplierNow = MultiplierFor(p.Crowns),
            };
            pv.CrownsAfter = p.Crowns + pv.CrownsGained;
            pv.MultiplierAfter = MultiplierFor(pv.CrownsAfter);
            pv.StartingCashAfter = StartingCashFor(pv.CrownsAfter);
            pv.Available = _s.State.RunEarnings >= pv.RequiredRunEarnings && pv.CrownsGained >= 1;
            int next = p.Count + 1;
            pv.BadgeId = "badge_prestige_" + Math.Min(next, 10);
            pv.DecorId = "decor_monument_" + Math.Min(next, 5);
            pv.Bonuses.Add("prestige.bonus.multiplier");
            pv.Bonuses.Add("prestige.bonus.cash");
            if (next >= 1) pv.Bonuses.Add("prestige.bonus.line2");
            if (next >= 3) pv.Bonuses.Add("prestige.bonus.food");
            return pv;
        }

        public bool TryPrestige()
        {
            var pv = Preview();
            if (!pv.Available) return false;
            var st = _s.State;
            st.Prestige.Count++;
            st.Prestige.Crowns = pv.CrownsAfter;
            if (!st.Prestige.Badges.Contains(pv.BadgeId)) st.Prestige.Badges.Add(pv.BadgeId);
            st.Collection.Add(pv.DecorId);

            // Reset the run.
            st.Factories.Clear();
            st.EnsureConsistent(_s.Content);
            st.Factories[0].Unlocked = true;
            st.Factories[0].Lines[0].Unlocked = true;
            st.Factories[0].Lines[1].Unlocked = true;                    // prestige bonus: 2nd steel line
            if (st.Prestige.Count >= 3 && st.Factories.Count > 1)        // prestige bonus: food factory owned
            {
                st.Factories[1].Unlocked = true;
                st.Factories[1].Lines[0].Unlocked = true;
            }
            st.ActiveFactoryId = st.Factories[0].FactoryId;
            st.Cash = pv.StartingCashAfter;
            st.Debt = BigDouble.Zero;
            st.RunEarnings = BigDouble.Zero;
            st.TotalInvested = BigDouble.Zero;
            st.Orders.Clear();
            foreach (var m in st.Managers)
                if (m.AssignedFactoryId != null && !st.GetFactory(m.AssignedFactoryId).Unlocked) m.AssignedFactoryId = null;

            Apply();
            _s.Managers.Apply();
            _s.Ledger.Reset();
            _s.Analytics.Track(AnalyticsEvents.Prestige, "count", st.Prestige.Count.ToString());
            Prestiged?.Invoke();
            return true;
        }

        public void Apply()
        {
            double crowns = _s.State.Prestige.Crowns;
            if (crowns > 0) _s.Modifiers.Set(Source, ModifierStat.SellPrice, MultiplierFor(crowns));
            else _s.Modifiers.Remove(Source);
        }
    }

    public sealed class ResearchDef
    {
        public string Id;
        public string NameKey;
        /// <summary>Null for special effects (offline cap).</summary>
        public ModifierStat? Stat;
        /// <summary>Per-level change: +0.05 = +5% (for cost stats it is a reduction).</summary>
        public double PerLevel;
        public int MaxLevel;
        public double BaseCost;
        public double CostGrowth;
        public int RequiredCompanyLevel;
    }

    /// <summary>Permanent research tree bought with cash (kept through prestige).</summary>
    public sealed class ResearchService
    {
        public const string SourcePrefix = "research.";
        public const string OfflineCapId = "res_offline_cap";

        public static readonly List<ResearchDef> Defs = new List<ResearchDef>
        {
            new ResearchDef { Id = "res_speed", NameKey = "research.res_speed", Stat = ModifierStat.ProductionSpeed, PerLevel = 0.05, MaxLevel = 20, BaseCost = 2500, CostGrowth = 3.2, RequiredCompanyLevel = 3 },
            new ResearchDef { Id = "res_price", NameKey = "research.res_price", Stat = ModifierStat.SellPrice, PerLevel = 0.05, MaxLevel = 20, BaseCost = 4000, CostGrowth = 3.4, RequiredCompanyLevel = 4 },
            new ResearchDef { Id = "res_cost", NameKey = "research.res_cost", Stat = ModifierStat.OperatingCost, PerLevel = 0.04, MaxLevel = 15, BaseCost = 1500, CostGrowth = 3.0, RequiredCompanyLevel = 2 },
            new ResearchDef { Id = "res_storage", NameKey = "research.res_storage", Stat = ModifierStat.StorageCapacity, PerLevel = 0.10, MaxLevel = 10, BaseCost = 1000, CostGrowth = 2.8, RequiredCompanyLevel = 2 },
            new ResearchDef { Id = "res_upgrade", NameKey = "research.res_upgrade", Stat = ModifierStat.UpgradeCost, PerLevel = 0.03, MaxLevel = 15, BaseCost = 6000, CostGrowth = 3.5, RequiredCompanyLevel = 6 },
            new ResearchDef { Id = OfflineCapId, NameKey = "research.res_offline_cap", Stat = null, PerLevel = 1, MaxLevel = 4, BaseCost = 50000, CostGrowth = 12, RequiredCompanyLevel = 8 },
        };

        private readonly GameSession _s;

        public ResearchService(GameSession session) => _s = session;

        public event Action Changed;

        public int Level(string id) => _s.State.Research.TryGetValue(id, out int l) ? l : 0;

        public double OfflineCapBonusHours => Level(OfflineCapId);

        public int TotalLevels
        {
            get { int t = 0; foreach (var v in _s.State.Research.Values) t += v; return t; }
        }

        public BigDouble Cost(ResearchDef def, int currentLevel) =>
            BigDouble.FromLog10(Math.Log10(def.BaseCost) + currentLevel * Math.Log10(def.CostGrowth));

        public bool IsUnlocked(ResearchDef def) => _s.CompanyLevel >= def.RequiredCompanyLevel;

        public bool TryBuy(string id)
        {
            var def = Defs.Find(d => d.Id == id);
            if (def == null || !IsUnlocked(def)) return false;
            int lv = Level(id);
            if (lv >= def.MaxLevel) return false;
            if (!_s.Wallet.TrySpend(Cost(def, lv))) return false;
            _s.State.Research[id] = lv + 1;
            _s.State.Stats.ResearchBought++;
            Apply();
            return true;
        }

        public static double Multiplier(ResearchDef def, int level)
        {
            double b = def.PerLevel * level;
            return def.Stat == ModifierStat.OperatingCost || def.Stat == ModifierStat.UpgradeCost ? 1d / (1d + b) : 1d + b;
        }

        public void Apply()
        {
            foreach (var def in Defs)
            {
                if (def.Stat == null) continue;
                int lv = Level(def.Id);
                if (lv > 0) _s.Modifiers.Set(SourcePrefix + def.Id, def.Stat.Value, Multiplier(def, lv));
                else _s.Modifiers.Remove(SourcePrefix + def.Id);
            }
            Changed?.Invoke();
        }
    }
}
