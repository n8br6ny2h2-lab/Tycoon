using System;
using System.Collections.Generic;
using FactoryKing.Core.Economy;
using FactoryKing.Core.Numerics;
using FactoryKing.Core.Simulation;

namespace FactoryKing.Core.Meta
{
    // =====================================================================================================
    // Cosmetics
    // =====================================================================================================

    public enum CosmeticSlot { Floor = 0, Wall = 1, Ground = 2, Sky = 3 }

    public enum CosmeticPattern { Solid, Tiles, Stripes, Checker }

    /// <summary>How a cosmetic is obtained. Only <see cref="Diamonds"/> and <see cref="Purchase"/> items can be bought.</summary>
    public enum CosmeticSource { Default, Diamonds, Season, SeasonPremium, Achievement, Purchase }

    public sealed class CosmeticDef
    {
        public string Id;
        public CosmeticSlot Slot;
        public string NameKey;
        public string ColorHex;
        public string SecondaryHex;
        public CosmeticPattern Pattern;
        public CosmeticSource Source;
        public long DiamondPrice;
        /// <summary>For Achievement / Purchase sources: which achievement or store product grants it.</summary>
        public string SourceRef;
    }

    /// <summary>
    /// Factory appearance (floor, walls, outdoor ground, sky). Owned items are permanent across seasons.
    /// </summary>
    public sealed class CosmeticService
    {
        public static readonly List<CosmeticDef> Defs = new List<CosmeticDef>
        {
            // Floors
            C("floor_default", CosmeticSlot.Floor, null, null, CosmeticPattern.Solid, CosmeticSource.Default),
            C("floor_graphite", CosmeticSlot.Floor, "#4A4E57", null, CosmeticPattern.Solid, CosmeticSource.Diamonds, 60),
            C("floor_blue_tiles", CosmeticSlot.Floor, "#9FC5E8", "#7EA8CC", CosmeticPattern.Tiles, CosmeticSource.Diamonds, 90),
            C("floor_checker", CosmeticSlot.Floor, "#EDEDED", "#2F2F2F", CosmeticPattern.Checker, CosmeticSource.Season),
            C("floor_marble", CosmeticSlot.Floor, "#F2EEE8", "#D8D0C6", CosmeticPattern.Tiles, CosmeticSource.Achievement, 0, "ach_five_factories"),
            C("floor_gold", CosmeticSlot.Floor, "#E8C25A", "#C99A2E", CosmeticPattern.Tiles, CosmeticSource.Purchase, 0, "cosmetic_gold"),
            C("floor_neon", CosmeticSlot.Floor, "#1B1F3B", "#00F5D4", CosmeticPattern.Checker, CosmeticSource.Purchase, 0, "cosmetic_neon"),
            // Walls
            C("wall_default", CosmeticSlot.Wall, null, null, CosmeticPattern.Solid, CosmeticSource.Default),
            C("wall_brick", CosmeticSlot.Wall, "#A44A3F", "#8A3B32", CosmeticPattern.Tiles, CosmeticSource.Diamonds, 60),
            C("wall_navy", CosmeticSlot.Wall, "#22355C", null, CosmeticPattern.Solid, CosmeticSource.Diamonds, 90),
            C("wall_teal_stripes", CosmeticSlot.Wall, "#2A9D8F", "#E9F5F3", CosmeticPattern.Stripes, CosmeticSource.Season),
            C("wall_glass", CosmeticSlot.Wall, "#BFE3F2", "#8CC8E0", CosmeticPattern.Stripes, CosmeticSource.Achievement, 0, "ach_ten_factories"),
            C("wall_gold", CosmeticSlot.Wall, "#F0D27A", null, CosmeticPattern.Solid, CosmeticSource.Purchase, 0, "cosmetic_gold"),
            C("wall_neon", CosmeticSlot.Wall, "#2B2D42", "#F72585", CosmeticPattern.Stripes, CosmeticSource.Purchase, 0, "cosmetic_neon"),
            // Outdoor ground
            C("ground_default", CosmeticSlot.Ground, null, null, CosmeticPattern.Solid, CosmeticSource.Default),
            C("ground_desert", CosmeticSlot.Ground, "#E3C58F", null, CosmeticPattern.Solid, CosmeticSource.Diamonds, 60),
            C("ground_snow", CosmeticSlot.Ground, "#F4F8FB", null, CosmeticPattern.Solid, CosmeticSource.Diamonds, 90),
            C("ground_autumn", CosmeticSlot.Ground, "#C9733A", null, CosmeticPattern.Solid, CosmeticSource.Season),
            C("ground_city", CosmeticSlot.Ground, "#6B6F76", "#5A5E64", CosmeticPattern.Tiles, CosmeticSource.Achievement, 0, "ach_first_season"),
            C("ground_sakura", CosmeticSlot.Ground, "#F7C6D9", null, CosmeticPattern.Solid, CosmeticSource.SeasonPremium),
            // Sky
            C("sky_default", CosmeticSlot.Sky, null, null, CosmeticPattern.Solid, CosmeticSource.Default),
            C("sky_sunset", CosmeticSlot.Sky, "#FF8C61", null, CosmeticPattern.Solid, CosmeticSource.Diamonds, 60),
            C("sky_deep_blue", CosmeticSlot.Sky, "#3A6EA5", null, CosmeticPattern.Solid, CosmeticSource.Diamonds, 90),
            C("sky_aurora", CosmeticSlot.Sky, "#5CE1B8", null, CosmeticPattern.Solid, CosmeticSource.Season),
            C("sky_galaxy", CosmeticSlot.Sky, "#5B2A86", null, CosmeticPattern.Solid, CosmeticSource.SeasonPremium),
            C("sky_gold", CosmeticSlot.Sky, "#FFD166", null, CosmeticPattern.Solid, CosmeticSource.Purchase, 0, "cosmetic_gold"),
            C("sky_neon", CosmeticSlot.Sky, "#7209B7", null, CosmeticPattern.Solid, CosmeticSource.Purchase, 0, "cosmetic_neon"),
        };

        private static CosmeticDef C(string id, CosmeticSlot slot, string color, string secondary, CosmeticPattern pattern,
            CosmeticSource source, long price = 0, string sourceRef = null) => new CosmeticDef
        {
            Id = id, Slot = slot, NameKey = "cosmetic." + id, ColorHex = color, SecondaryHex = secondary,
            Pattern = pattern, Source = source, DiamondPrice = price, SourceRef = sourceRef,
        };

        private readonly GameSession _s;

        public CosmeticService(GameSession session) => _s = session;

        public event Action Changed;

        public static CosmeticDef Get(string id) => Defs.Find(d => d.Id == id);

        public static IEnumerable<CosmeticDef> ForSlot(CosmeticSlot slot)
        {
            foreach (var d in Defs) if (d.Slot == slot) yield return d;
        }

        public bool IsOwned(string id)
        {
            var d = Get(id);
            return d != null && (d.Source == CosmeticSource.Default || _s.State.Collection.Contains(id));
        }

        public string Equipped(CosmeticSlot slot) =>
            _s.State.EquippedCosmetics.TryGetValue(slot.ToString(), out var id) && IsOwned(id) ? id : DefaultFor(slot);

        public static string DefaultFor(CosmeticSlot slot) => Defs.Find(d => d.Slot == slot && d.Source == CosmeticSource.Default).Id;

        public CosmeticDef EquippedDef(CosmeticSlot slot) => Get(Equipped(slot));

        public bool TryBuy(string id)
        {
            var d = Get(id);
            if (d == null || d.Source != CosmeticSource.Diamonds || IsOwned(id)) return false;
            if (!_s.Wallet.TrySpendDiamonds(d.DiamondPrice)) return false;
            Grant(id);
            return true;
        }

        /// <summary>Adds to the permanent collection (purchases, rewards). Returns false if already owned.</summary>
        public bool Grant(string id)
        {
            if (Get(id) == null || IsOwned(id)) return false;
            _s.State.Collection.Add(id);
            Changed?.Invoke();
            return true;
        }

        public bool Equip(string id)
        {
            var d = Get(id);
            if (d == null || !IsOwned(id)) return false;
            _s.State.EquippedCosmetics[d.Slot.ToString()] = id;
            Changed?.Invoke();
            return true;
        }
    }

    // =====================================================================================================
    // Seasons
    // =====================================================================================================

    public sealed class SeasonTierDef
    {
        public int Factories;
        public long Diamonds;
        public int LegacyStars;
        public string CosmeticId;
        public string PremiumCosmeticId;
    }

    /// <summary>
    /// 30-day seasons. Each season is a fresh run towards all 20 factories; reaching factory milestones pays
    /// permanent rewards (diamonds, legacy stars, cosmetics). When the season ends — or early, once all factories are
    /// owned — the run resets. Kept forever: diamonds, collection/cosmetics, achievements, purchases, managers (level
    /// resets), legacy stars, badges and stats.
    /// </summary>
    public sealed class SeasonService
    {
        public const string LegacySource = "legacy";
        public const string MomentumSource = "season_momentum";
        public const double LegacyBonusPerStar = 0.05;

        public static readonly SeasonTierDef[] Tiers =
        {
            new SeasonTierDef { Factories = 2, Diamonds = 20 },
            new SeasonTierDef { Factories = 3, Diamonds = 30, LegacyStars = 1 },
            new SeasonTierDef { Factories = 4, Diamonds = 40, CosmeticId = "floor_checker" },
            new SeasonTierDef { Factories = 5, Diamonds = 50, LegacyStars = 1 },
            new SeasonTierDef { Factories = 6, Diamonds = 60, PremiumCosmeticId = "ground_sakura" },
            new SeasonTierDef { Factories = 8, Diamonds = 70, LegacyStars = 1, CosmeticId = "wall_teal_stripes" },
            new SeasonTierDef { Factories = 10, Diamonds = 80, LegacyStars = 1 },
            new SeasonTierDef { Factories = 12, Diamonds = 90, CosmeticId = "ground_autumn" },
            new SeasonTierDef { Factories = 14, Diamonds = 100, LegacyStars = 1 },
            new SeasonTierDef { Factories = 16, Diamonds = 120, LegacyStars = 1, PremiumCosmeticId = "sky_galaxy" },
            new SeasonTierDef { Factories = 18, Diamonds = 140, LegacyStars = 1 },
            new SeasonTierDef { Factories = 20, Diamonds = 200, LegacyStars = 2, CosmeticId = "sky_aurora" },
        };

        private readonly GameSession _s;

        public SeasonService(GameSession session) => _s = session;

        /// <summary>Raised after a season ended and the new one started (old season number, factories reached).</summary>
        public event Action<int, int> SeasonEnded;

        private SeasonState St => _s.State.Season;

        public int Number => St.Number;
        public int LegacyStars => St.LegacyStars;
        public bool HasPremium => St.PremiumPass;

        public DateTime Start => new DateTime(St.StartUtcTicks, DateTimeKind.Utc);
        public DateTime End => Start.AddDays(_s.Tuning.SeasonDays);

        public double SecondsLeft => Math.Max(0, (End - _s.Time.TrustedNow).TotalSeconds);

        public int OwnedFactories => _s.State.Factories.FindAll(f => f.Unlocked).Count;

        public bool IsReached(int tier) => OwnedFactories >= Tiers[tier].Factories || St.ReachedTiers.Contains(tier);
        public bool IsClaimed(int tier) => St.ClaimedTiers.Contains(tier);
        public bool CanEndEarly => OwnedFactories >= _s.Content.Factories.Count;

        public int ClaimableCount
        {
            get { int n = 0; for (int i = 0; i < Tiers.Length; i++) if (IsReached(i) && !IsClaimed(i)) n++; return n; }
        }

        /// <summary>Starts the very first season (new game) without resetting anything.</summary>
        public void EnsureStarted()
        {
            if (St.StartUtcTicks == 0) St.StartUtcTicks = _s.Time.TrustedNow.Ticks;
        }

        /// <summary>Records reached tiers (so a reset never loses them) and ends the season when time is up.</summary>
        public void Tick()
        {
            EnsureStarted();
            for (int i = 0; i < Tiers.Length; i++)
                if (OwnedFactories >= Tiers[i].Factories) St.ReachedTiers.Add(i);
            if (OwnedFactories > St.BestFactories) St.BestFactories = OwnedFactories;
            if (_s.Time.TrustedNow >= End) EndSeason();
        }

        public bool TryClaim(int tier)
        {
            if (tier < 0 || tier >= Tiers.Length || !IsReached(tier) || IsClaimed(tier)) return false;
            var t = Tiers[tier];
            St.ClaimedTiers.Add(tier);
            _s.Wallet.AddDiamonds(t.Diamonds * (St.PremiumPass ? 2 : 1));
            if (t.LegacyStars > 0)
            {
                St.LegacyStars += t.LegacyStars;
                Apply();
            }
            if (t.CosmeticId != null && !_s.Cosmetics.Grant(t.CosmeticId)) _s.Wallet.AddDiamonds(50);
            if (St.PremiumPass && t.PremiumCosmeticId != null && !_s.Cosmetics.Grant(t.PremiumCosmeticId)) _s.Wallet.AddDiamonds(50);
            return true;
        }

        /// <summary>Premium track for the current season (granted by the "season_pass" purchase).</summary>
        public bool ActivatePremium()
        {
            if (St.PremiumPass) return false;
            St.PremiumPass = true;
            // Retroactive: premium extras of tiers already claimed this season.
            foreach (int tier in St.ClaimedTiers)
            {
                var t = Tiers[tier];
                _s.Wallet.AddDiamonds(t.Diamonds);
                if (t.PremiumCosmeticId != null) _s.Cosmetics.Grant(t.PremiumCosmeticId);
            }
            return true;
        }

        /// <summary>Ends the current season now: pays unclaimed rewards, archives it, resets the run.</summary>
        public void EndSeason()
        {
            for (int i = 0; i < Tiers.Length; i++) if (IsReached(i)) TryClaim(i);
            int old = St.Number, reached = St.BestFactories;
            St.History.Add(old + ":" + reached);
            if (St.History.Count > 24) St.History.RemoveAt(0);

            var st = _s.State;
            st.Factories.Clear();
            st.EnsureConsistent(_s.Content);
            st.Factories[0].Unlocked = true;
            st.Factories[0].Lines[0].Unlocked = true;
            st.ActiveFactoryId = st.Factories[0].FactoryId;
            st.Cash = BigDouble.FromDouble(_s.Tuning.StartingCash * (1 + St.LegacyStars));
            st.Debt = BigDouble.Zero;
            st.RunEarnings = BigDouble.Zero;
            st.TotalInvested = BigDouble.Zero;
            st.Orders.Clear();
            st.Research.Clear();
            st.Prestige.Count = 0;
            st.Prestige.Crowns = 0;
            st.BoostEndUtcTicks = 0;
            foreach (var m in st.Managers)
            {
                m.Level = 1;
                m.AssignedFactoryId = null;
                m.AbilityEndUtcTicks = 0;
                m.AbilityReadyUtcTicks = 0;
            }

            St.Number = old + 1;
            St.StartUtcTicks = _s.Time.TrustedNow.Ticks;
            St.ClaimedTiers.Clear();
            St.ReachedTiers.Clear();
            St.BestFactories = 1;
            St.PremiumPass = false;
            st.Stats.SeasonsCompleted++;

            _s.Ledger.Reset();
            _s.ApplyAllModifiers();
            SeasonEnded?.Invoke(old, reached);
        }

        public double DaysElapsed => Math.Max(0, (_s.Time.TrustedNow - Start).TotalDays);

        public double Momentum => 1 + _s.Tuning.SeasonMomentumPerDay * Math.Min(_s.Tuning.SeasonDays, DaysElapsed);

        public void Apply()
        {
            _s.Modifiers.SetQuiet(MomentumSource, ModifierStat.SellPrice, Momentum);
            if (St.LegacyStars > 0) _s.Modifiers.Set(LegacySource, ModifierStat.SellPrice, 1 + LegacyBonusPerStar * St.LegacyStars);
            else _s.Modifiers.Remove(LegacySource);
        }
    }
}
