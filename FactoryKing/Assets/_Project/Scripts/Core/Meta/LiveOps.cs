using System;
using System.Collections.Generic;
using FactoryKing.Core.Content;
using FactoryKing.Core.Economy;
using FactoryKing.Core.Numerics;
using FactoryKing.Core.Simulation;

namespace FactoryKing.Core.Meta
{
    // =====================================================================================================
    // Season XP helper: every rewarding action feeds the 50-tier season road.
    // =====================================================================================================
    public static class SeasonXp
    {
        public const int DailyMission = 120, WeeklyMission = 400, Order = 60, Factory = 250, Achievement = 100,
            DailyReward = 80, Expedition = 50, EventTier = 150, Drone = 10, Crate = 20;
    }

    // =====================================================================================================
    // A1 — Cargo drone: a tappable bonus that flies by every few minutes.
    // =====================================================================================================
    public enum DroneRewardKind { Cash, Diamonds, Boost }

    public readonly struct DroneReward
    {
        public readonly DroneRewardKind Kind;
        public readonly BigDouble Cash;
        public readonly long Diamonds;
        public readonly double BoostMinutes;

        public DroneReward(DroneRewardKind kind, BigDouble cash, long diamonds, double boostMinutes)
        {
            Kind = kind; Cash = cash; Diamonds = diamonds; BoostMinutes = boostMinutes;
        }
    }

    public sealed class DroneService
    {
        public const double VisibleSeconds = 25;
        private readonly GameSession _s;

        public DroneService(GameSession session) => _s = session;

        private LiveOpsState L => _s.State.Live;

        public double IntervalSeconds => 240 * (1 - 0.1 * _s.Engineering.Level(EngineeringService.Drone));

        public bool IsVisible => _s.Time.TrustedNow.Ticks < L.DroneVisibleUntilUtcTicks;

        public void Tick()
        {
            long now = _s.Time.TrustedNow.Ticks;
            if (L.DroneNextUtcTicks == 0) L.DroneNextUtcTicks = now + (long)(60 * TimeSpan.TicksPerSecond);
            if (now >= L.DroneNextUtcTicks && !IsVisible)
            {
                L.DroneVisibleUntilUtcTicks = now + (long)(VisibleSeconds * TimeSpan.TicksPerSecond);
                L.DroneSeed = unchecked((int)(now / TimeSpan.TicksPerSecond));
                var rnd = new Random(L.DroneSeed);
                L.DroneNextUtcTicks = now + (long)((IntervalSeconds * (0.75 + 0.5 * rnd.NextDouble())) * TimeSpan.TicksPerSecond);
            }
        }

        /// <summary>What the current drone carries (deterministic per drone).</summary>
        public DroneReward Peek()
        {
            var rnd = new Random(L.DroneSeed);
            double roll = rnd.NextDouble();
            if (roll < 0.80) return new DroneReward(DroneRewardKind.Cash, _s.IncomeReward(60 + rnd.Next(120)), 0, 0);
            if (roll < 0.95) return new DroneReward(DroneRewardKind.Diamonds, BigDouble.Zero, 2 + rnd.Next(4), 0);
            return new DroneReward(DroneRewardKind.Boost, BigDouble.Zero, 0, 5);
        }

        /// <summary>Tap on the drone. Multiplier 2 after an optional rewarded video.</summary>
        public bool TryCatch(out DroneReward reward, int multiplier = 1)
        {
            reward = default;
            if (!IsVisible) return false;
            reward = Peek();
            L.DroneVisibleUntilUtcTicks = 0;
            multiplier = Math.Max(1, Math.Min(2, multiplier));
            switch (reward.Kind)
            {
                case DroneRewardKind.Cash: _s.Wallet.Grant(reward.Cash * multiplier); break;
                case DroneRewardKind.Diamonds: _s.Wallet.AddDiamonds(reward.Diamonds * multiplier); _s.Piggy.Feed(reward.Diamonds); break;
                case DroneRewardKind.Boost: _s.Boosts.Activate(reward.BoostMinutes * multiplier); break;
            }
            _s.State.Stats.DronesCaught++;
            _s.Road.AddXp(SeasonXp.Drone);
            return true;
        }
    }

    // =====================================================================================================
    // A4 — Crates of manager cards (earned in play; can also be bought with diamonds, odds always shown).
    // =====================================================================================================
    public sealed class CrateDef
    {
        public string Id;
        public string NameKey;
        public int Cards;
        public long DiamondPrice;
        /// <summary>Odds per rarity (Common, Rare, Epic, Legendary), sum = 1. Displayed in the UI before opening.</summary>
        public double[] Odds;
    }

    public sealed class CrateService
    {
        public static readonly List<CrateDef> Defs = new List<CrateDef>
        {
            new CrateDef { Id = "crate_small", NameKey = "crate.small", Cards = 3, DiamondPrice = 40, Odds = new[] { 0.70, 0.25, 0.045, 0.005 } },
            new CrateDef { Id = "crate_big", NameKey = "crate.big", Cards = 8, DiamondPrice = 120, Odds = new[] { 0.55, 0.32, 0.11, 0.02 } },
            new CrateDef { Id = "crate_epic", NameKey = "crate.epic", Cards = 12, DiamondPrice = 300, Odds = new[] { 0.30, 0.40, 0.24, 0.06 } },
        };

        private readonly GameSession _s;

        public CrateService(GameSession session) => _s = session;

        public static CrateDef Get(string id) => Defs.Find(d => d.Id == id);

        public int Count(string id) => _s.State.Live.Crates.TryGetValue(id, out int n) ? n : 0;

        public void Add(string id, int n = 1)
        {
            if (Get(id) == null || n <= 0) return;
            _s.State.Live.Crates[id] = Count(id) + n;
        }

        public bool TryBuy(string id)
        {
            var d = Get(id);
            if (d == null || !_s.Wallet.TrySpendDiamonds(d.DiamondPrice)) return false;
            Add(id);
            return true;
        }

        /// <summary>Opens an owned crate; returns manager id -> cards received.</summary>
        public Dictionary<string, int> Open(string id, Random rnd = null)
        {
            var d = Get(id);
            if (d == null || Count(id) <= 0) return null;
            _s.State.Live.Crates[id] = Count(id) - 1;
            rnd ??= new Random(unchecked((int)(_s.Time.TrustedNow.Ticks % int.MaxValue)) ^ _s.State.Stats.CratesOpened.GetHashCode());
            var result = new Dictionary<string, int>(StringComparer.Ordinal);
            int cards = d.Cards + _s.Engineering.Level(EngineeringService.Cards);
            for (int i = 0; i < cards; i++)
            {
                double roll = rnd.NextDouble(), acc = 0;
                int rarity = 0;
                for (int r = 0; r < d.Odds.Length; r++) { acc += d.Odds[r]; if (roll < acc) { rarity = r; break; } rarity = r; }
                var pool = ManagerService.Defs.FindAll(m => (int)m.Rarity == rarity);
                var pick = pool[rnd.Next(pool.Count)];
                result.TryGetValue(pick.Id, out int n);
                result[pick.Id] = n + 1;
                _s.Managers.AddCards(pick.Id, 1);
            }
            _s.State.Stats.CratesOpened++;
            _s.Road.AddXp(SeasonXp.Crate);
            return result;
        }
    }

    // =====================================================================================================
    // A5 — Expeditions: send a truck on a timed trip for rewards.
    // =====================================================================================================
    public sealed class ExpeditionService
    {
        public const int Slots = 3;
        public static readonly double[] Hours = { 1, 4, 8 };
        private readonly GameSession _s;

        public ExpeditionService(GameSession session) => _s = session;

        public List<ExpeditionState> Active => _s.State.Live.Expeditions;

        public ExpeditionState InSlot(int slot) => Active.Find(e => e.Slot == slot);

        public double DurationSeconds(int kind) => Hours[kind] * 3600 * (1 - 0.1 * _s.Engineering.Level(EngineeringService.Expedition));

        public bool TryStart(int slot, int kind)
        {
            if (slot < 0 || slot >= Slots || kind < 0 || kind >= Hours.Length || InSlot(slot) != null) return false;
            long now = _s.Time.TrustedNow.Ticks;
            var rnd = new Random(unchecked((int)(now % int.MaxValue)) + slot);
            double incomeSeconds = kind == 0 ? 1800 : kind == 1 ? 7200 : 14400;
            Active.Add(new ExpeditionState
            {
                Slot = slot, Kind = kind, StartUtcTicks = now,
                EndUtcTicks = now + (long)(DurationSeconds(kind) * TimeSpan.TicksPerSecond),
                RewardCash = _s.IncomeReward(incomeSeconds).ToDouble(),
                RewardCards = kind == 0 ? 1 : kind == 1 ? 3 : 6,
                RewardDiamonds = kind == 0 ? 0 : kind == 1 ? 5 : 15,
                RewardCrate = kind == 2 && rnd.NextDouble() < 0.25,
            });
            return true;
        }

        public double Remaining(ExpeditionState e) => Math.Max(0, (e.EndUtcTicks - _s.Time.TrustedNow.Ticks) / (double)TimeSpan.TicksPerSecond);

        public bool TryClaim(int slot, Random rnd = null)
        {
            var e = InSlot(slot);
            if (e == null || Remaining(e) > 0) return false;
            Active.Remove(e);
            _s.Wallet.Grant(BigDouble.FromDouble(e.RewardCash));
            _s.Wallet.AddDiamonds(e.RewardDiamonds);
            _s.Piggy.Feed(e.RewardDiamonds);
            rnd ??= new Random(unchecked((int)(e.EndUtcTicks % int.MaxValue)));
            for (int i = 0; i < e.RewardCards; i++)
            {
                var m = ManagerService.Defs[rnd.Next(ManagerService.Defs.Count)];
                _s.Managers.AddCards(m.Id, 1);
            }
            if (e.RewardCrate) _s.Crates.Add("crate_small");
            _s.State.Stats.ExpeditionsDone++;
            _s.Road.AddXp(SeasonXp.Expedition);
            return true;
        }
    }

    // =====================================================================================================
    // A2/A3 — Weekly event factory (Thursday 00:00 – Monday 00:00 UTC), with its own token reward track.
    // =====================================================================================================
    public sealed class EventFactoryService
    {
        public static readonly (int tokens, long diamonds, string crate, int cards)[] Tiers =
        {
            (25, 15, null, 2), (100, 25, "crate_small", 3), (300, 40, null, 5), (800, 60, "crate_big", 6), (2000, 100, "crate_epic", 10),
        };

        private readonly GameSession _s;

        public EventFactoryService(GameSession session) => _s = session;

        public EventFactoryState Current => _s.State.Live.EventFactory;

        /// <summary>The event window containing <paramref name="now"/>, or null if none.</summary>
        public (DateTime start, DateTime end, string factoryId)? Window(DateTime now)
        {
            int dow = ((int)now.DayOfWeek + 6) % 7; // Monday = 0
            var monday = DateTime.SpecifyKind(now.Date.AddDays(-dow), DateTimeKind.Utc);
            var start = monday.AddDays(3);           // Thursday
            var end = monday.AddDays(7);             // next Monday
            if (now < start || now >= end) return null;
            long week = monday.Ticks / TimeSpan.TicksPerDay / 7;
            var events = _s.Content.EventFactories;
            return (start, end, events[(int)(week % events.Count)].Id);
        }

        public bool IsActive => Current != null && _s.Time.TrustedNow.Ticks < Current.EndUtcTicks;

        /// <summary>Events unlock once the player owns a second regular factory (after the first steps).</summary>
        public const int RequiredFactories = 2;
        public bool IsUnlockedForPlayer => _s.OwnedRegularFactories >= RequiredFactories;

        public string ProductId(string factoryId)
        {
            var def = _s.Content.GetFactory(factoryId);
            return _s.Content.GetMachine(def.MachineChain[def.MachineChain.Count - 1]).OutputProductId;
        }

        public long Tokens
        {
            get
            {
                var c = Current;
                if (c == null) return 0;
                _s.State.Stats.SoldByProduct.TryGetValue(ProductId(c.FactoryId), out long sold);
                return Math.Max(0, sold - c.BaselineSold);
            }
        }

        public bool IsTierReached(int tier) => Tokens >= Tiers[tier].tokens;

        public void Tick()
        {
            var now = _s.Time.TrustedNow;
            var c = Current;
            if (c != null && now.Ticks >= c.EndUtcTicks) Finish();
            var window = IsUnlockedForPlayer ? Window(now) : null;
            if (window == null || (Current != null && Current.FactoryId == window.Value.factoryId))
            {
                if (Current != null)
                {
                    // A season reset may have closed the event factory: reopen it for the rest of the event.
                    var open = _s.State.GetFactory(Current.FactoryId);
                    if (!open.Unlocked) { open.Unlocked = true; open.Lines[0].Unlocked = true; }
                }
                ApplyScale();
                return;
            }

            // Start a new event: open the event factory fresh and scale its economy to the player's progress.
            string fid = window.Value.factoryId;
            var fs = _s.State.GetFactory(fid);
            var def = _s.Content.GetFactory(fid);
            fs.Lines.Clear();
            fs.PowerLevel = 1;
            fs.FleetLevel = 1;
            _s.State.EnsureConsistent(_s.Content);
            fs = _s.State.GetFactory(fid);
            fs.Unlocked = true;
            fs.Lines[0].Unlocked = true;
            int best = 0;
            foreach (var f in _s.State.Factories)
                if (f.Unlocked && _s.Content.TryGetFactory(f.FactoryId, out var d) && !d.IsEvent) best = Math.Max(best, d.Order);
            _s.State.Stats.SoldByProduct.TryGetValue(ProductId(fid), out long sold);
            _s.State.Live.EventFactory = new EventFactoryState
            {
                FactoryId = fid, StartUtcTicks = window.Value.start.Ticks, EndUtcTicks = window.Value.end.Ticks,
                BaselineSold = sold, Scale = Math.Pow(DefaultContent.ScaleRatio, Math.Max(0, best - 1)),
            };
            ApplyScale();
        }

        private void ApplyScale()
        {
            var c = Current;
            if (c == null) return;
            // The event factory is a fresh factory at the player's tier: prices and costs scale together.
            _s.Modifiers.SetQuiet("event.scale.price", ModifierStat.SellPrice, c.Scale, c.FactoryId);
            _s.Modifiers.SetQuiet("event.scale.cost", ModifierStat.OperatingCost, c.Scale, c.FactoryId);
            _s.Modifiers.SetQuiet("event.scale.upgrade", ModifierStat.UpgradeCost, c.Scale, c.FactoryId);
        }

        public bool TryClaim(int tier)
        {
            var c = Current;
            if (c == null || tier < 0 || tier >= Tiers.Length || !IsTierReached(tier) || !c.ClaimedTiers.Add(tier)) return false;
            var t = Tiers[tier];
            long dia = t.diamonds * (_s.Seasons.HasPremium ? 2 : 1);
            _s.Wallet.AddDiamonds(dia);
            _s.Piggy.Feed(dia);
            if (t.crate != null) _s.Crates.Add(t.crate);
            var rnd = new Random(tier + (int)(c.StartUtcTicks % 1000));
            for (int i = 0; i < t.cards; i++) _s.Managers.AddCards(ManagerService.Defs[rnd.Next(ManagerService.Defs.Count)].Id, 1);
            if (tier == Tiers.Length - 1)
            {
                string badge = "badge_event_" + c.FactoryId.Substring(4);
                if (!_s.State.Live.EventBadges.Contains(badge)) _s.State.Live.EventBadges.Add(badge);
                _s.Badges.Apply();
            }
            _s.Road.AddXp(SeasonXp.EventTier);
            return true;
        }

        /// <summary>Ends the event: pays unclaimed tiers, closes the factory.</summary>
        public void Finish()
        {
            var c = Current;
            if (c == null) return;
            for (int i = 0; i < Tiers.Length; i++) if (IsTierReached(i)) TryClaim(i);
            if (Tokens > _s.State.Stats.EventTokensBest) _s.State.Stats.EventTokensBest = Tokens;
            var fs = _s.State.GetFactory(c.FactoryId);
            fs.Unlocked = false;
            foreach (var l in fs.Lines) l.Unlocked = false;
            foreach (var m in _s.State.Managers) if (m.AssignedFactoryId == c.FactoryId) m.AssignedFactoryId = null;
            if (_s.State.ActiveFactoryId == c.FactoryId) _s.State.ActiveFactoryId = _s.State.Factories[0].FactoryId;
            foreach (var key in new[] { "event.scale.price", "event.scale.cost", "event.scale.upgrade" }) _s.Modifiers.Remove(key);
            _s.State.Live.EventFactory = null;
            _s.Managers.Apply();
        }
    }

    // =====================================================================================================
    // A6 supply chains, A7 truck fleet, A9 badge bonus
    // =====================================================================================================
    public sealed class SupplyChainService
    {
        public const double CostFactor = 0.8, PriceFactor = 1.1;
        private readonly GameSession _s;

        public SupplyChainService(GameSession session) => _s = session;

        public bool IsSupplied(string factoryId)
        {
            var def = _s.Content.GetFactory(factoryId);
            return def.SupplierId != null && _s.State.GetFactory(def.SupplierId).Unlocked;
        }

        public void Apply()
        {
            foreach (var f in _s.Content.Factories)
            {
                string src = "supply." + f.Id;
                if (f.SupplierId != null && IsSupplied(f.Id))
                {
                    if (!_s.Modifiers.HasSource(src))
                    {
                        _s.Modifiers.Set(src, ModifierStat.OperatingCost, CostFactor, f.Id);
                        _s.Modifiers.Add(src, ModifierStat.SellPrice, PriceFactor, f.Id);
                    }
                }
                else if (_s.Modifiers.HasSource(src)) _s.Modifiers.Remove(src);
            }
        }
    }

    public sealed class FleetService
    {
        public const int MaxLevel = 5;
        private readonly GameSession _s;

        public FleetService(GameSession session) => _s = session;

        public int Level(string factoryId) => _s.State.GetFactory(factoryId).FleetLevel;

        /// <summary>Truck load multiplier: van, truck, semi, road train, freight train.</summary>
        public static double Capacity(int level) => 1 + 0.5 * (level - 1);

        public BigDouble UpgradeCost(string factoryId)
        {
            var def = _s.Content.GetFactory(factoryId);
            double upgradeMod = _s.Modifiers.Get(ModifierStat.UpgradeCost, factoryId);
            return BigDouble.FromLog10(Math.Log10(def.PowerUpgradeBaseCost * 20 * upgradeMod) + (Level(factoryId) - 1) * Math.Log10(9));
        }

        public bool TryUpgrade(string factoryId)
        {
            var fs = _s.State.GetFactory(factoryId);
            if (!fs.Unlocked || fs.FleetLevel >= MaxLevel) return false;
            var cost = UpgradeCost(factoryId);
            if (!_s.Wallet.TrySpend(cost)) return false;
            fs.FleetLevel++;
            _s.State.TotalInvested += cost;
            Apply();
            return true;
        }

        public void Apply()
        {
            foreach (var fs in _s.State.Factories)
            {
                string src = "fleet." + fs.FactoryId;
                if (fs.FleetLevel > 1) _s.Modifiers.SetQuiet(src, ModifierStat.ShippingCapacity, Capacity(fs.FleetLevel), fs.FactoryId);
                else if (_s.Modifiers.HasSource(src)) _s.Modifiers.Remove(src);
            }
        }
    }

    /// <summary>Every badge (prestige and event) adds +1% sale price permanently, up to +25%.</summary>
    public sealed class BadgeService
    {
        public const string Source = "badges";
        public const double PerBadge = 0.01, Cap = 0.25;
        private readonly GameSession _s;

        public BadgeService(GameSession session) => _s = session;

        public int Count => _s.State.Prestige.Badges.Count + _s.State.Live.EventBadges.Count;
        public double Bonus => Math.Min(Cap, Count * PerBadge);

        public void Apply()
        {
            if (Count > 0) _s.Modifiers.SetQuiet(Source, ModifierStat.SellPrice, 1 + Bonus);
            else if (_s.Modifiers.HasSource(Source)) _s.Modifiers.Remove(Source);
        }
    }

    // =====================================================================================================
    // A8 — Engineering: permanent upgrades bought with diamonds, kept across seasons.
    // =====================================================================================================
    public sealed class EngineeringDef
    {
        public string Id;
        public string NameKey;
        public int MaxLevel;
        public long BaseDiamonds;
    }

    public sealed class EngineeringService
    {
        public const string Offline = "eng_offline", StartCash = "eng_startcash", Cooldown = "eng_cooldown",
            Expedition = "eng_expedition", Drone = "eng_drone", Cards = "eng_cards";

        public static readonly List<EngineeringDef> Defs = new List<EngineeringDef>
        {
            new EngineeringDef { Id = Offline, NameKey = "eng." + Offline, MaxLevel = 4, BaseDiamonds = 100 },
            new EngineeringDef { Id = StartCash, NameKey = "eng." + StartCash, MaxLevel = 5, BaseDiamonds = 60 },
            new EngineeringDef { Id = Cooldown, NameKey = "eng." + Cooldown, MaxLevel = 5, BaseDiamonds = 80 },
            new EngineeringDef { Id = Expedition, NameKey = "eng." + Expedition, MaxLevel = 3, BaseDiamonds = 80 },
            new EngineeringDef { Id = Drone, NameKey = "eng." + Drone, MaxLevel = 3, BaseDiamonds = 60 },
            new EngineeringDef { Id = Cards, NameKey = "eng." + Cards, MaxLevel = 3, BaseDiamonds = 150 },
        };

        private readonly GameSession _s;

        public EngineeringService(GameSession session) => _s = session;

        public int Level(string id) => _s.State.Live.Engineering.TryGetValue(id, out int l) ? l : 0;

        public long Cost(EngineeringDef d) => d.BaseDiamonds * (1L << Level(d.Id));

        public bool TryBuy(string id)
        {
            var d = Defs.Find(x => x.Id == id);
            if (d == null || Level(id) >= d.MaxLevel) return false;
            if (!_s.Wallet.TrySpendDiamonds(Cost(d))) return false;
            _s.State.Live.Engineering[id] = Level(id) + 1;
            return true;
        }
    }

    // =====================================================================================================
    // A10 — Season road: 50 XP tiers with a free and a premium (Season Pass) track.
    // =====================================================================================================
    public sealed class RoadReward
    {
        public long Diamonds;
        public string Crate;
        public double BoostMinutes;
        public string Cosmetic;
        public int Cards;
    }

    public sealed class SeasonRoadService
    {
        public const int TierCount = 50;
        public const int XpPerTier = 300;
        private readonly GameSession _s;

        public SeasonRoadService(GameSession session) => _s = session;

        public int Xp => _s.State.Live.SeasonXp;
        public int TiersReached => Math.Min(TierCount, Xp / XpPerTier);

        public void AddXp(int xp)
        {
            if (xp > 0) _s.State.Live.SeasonXp += xp;
        }

        public static RoadReward Free(int tier)
        {
            int n = tier + 1;
            if (n % 10 == 0) return new RoadReward { Crate = "crate_big" };
            if (n % 5 == 0) return new RoadReward { Crate = "crate_small" };
            if (n % 3 == 0) return new RoadReward { BoostMinutes = 15 };
            if (n % 2 == 0) return new RoadReward { Cards = 2 };
            return new RoadReward { Diamonds = 10 };
        }

        public static RoadReward Premium(int tier)
        {
            int n = tier + 1;
            if (n == 25) return new RoadReward { Cosmetic = "floor_carbon" };
            if (n == 50) return new RoadReward { Cosmetic = "wall_aurora", Crate = "crate_epic" };
            if (n % 10 == 0) return new RoadReward { Crate = "crate_epic" };
            if (n % 4 == 0) return new RoadReward { Crate = "crate_big" };
            if (n % 2 == 0) return new RoadReward { Cards = 4 };
            return new RoadReward { Diamonds = 25 };
        }

        public bool CanClaimFree(int tier) => tier < TiersReached && !_s.State.Live.RoadClaimedFree.Contains(tier);
        public bool CanClaimPremium(int tier) => _s.Seasons.HasPremium && tier < TiersReached && !_s.State.Live.RoadClaimedPremium.Contains(tier);

        public int ClaimableCount
        {
            get
            {
                int n = 0;
                for (int i = 0; i < TierCount; i++) { if (CanClaimFree(i)) n++; if (CanClaimPremium(i)) n++; }
                return n;
            }
        }

        public bool TryClaim(int tier, bool premium)
        {
            if (premium ? !CanClaimPremium(tier) : !CanClaimFree(tier)) return false;
            (premium ? _s.State.Live.RoadClaimedPremium : _s.State.Live.RoadClaimedFree).Add(tier);
            var r = premium ? Premium(tier) : Free(tier);
            if (r.Diamonds > 0) { _s.Wallet.AddDiamonds(r.Diamonds); _s.Piggy.Feed(r.Diamonds); }
            if (r.Crate != null) _s.Crates.Add(r.Crate);
            if (r.BoostMinutes > 0) _s.Boosts.Activate(r.BoostMinutes);
            if (r.Cosmetic != null && !_s.Cosmetics.Grant(r.Cosmetic)) _s.Wallet.AddDiamonds(50);
            if (r.Cards > 0)
            {
                var rnd = new Random(tier * 31 + (premium ? 7 : 0) + _s.Seasons.Number);
                for (int i = 0; i < r.Cards; i++) _s.Managers.AddCards(ManagerService.Defs[rnd.Next(ManagerService.Defs.Count)].Id, 1);
            }
            return true;
        }

        /// <summary>Season end: unclaimed reached rewards are paid, progress resets.</summary>
        public void OnSeasonEnd()
        {
            for (int i = 0; i < TierCount; i++) { TryClaim(i, false); TryClaim(i, true); }
            _s.State.Live.SeasonXp = 0;
            _s.State.Live.RoadClaimedFree.Clear();
            _s.State.Live.RoadClaimedPremium.Clear();
        }
    }

    // =====================================================================================================
    // A11 — Auto-upgrade and claim-all
    // =====================================================================================================
    public sealed class ConvenienceService
    {
        /// <summary>Auto-upgrade never spends more than this share of current cash per step.</summary>
        public const double BudgetShare = 0.25;
        private readonly GameSession _s;

        public ConvenienceService(GameSession session) => _s = session;

        /// <summary>Called every couple of seconds: upgrades bottlenecks (and the power plant) within a cash budget.</summary>
        public int AutoUpgradeStep()
        {
            if (!_s.State.Settings.AutoUpgrade) return 0;
            int bought = 0;
            var budget = _s.Wallet.Cash * BudgetShare;
            var spent = BigDouble.Zero;
            foreach (var fs in _s.State.Factories)
            {
                if (!fs.Unlocked) continue;
                if (_s.Power.Throttle(fs.FactoryId) < 0.999 && spent + _s.Power.UpgradeCost(fs.FactoryId) <= budget)
                {
                    var c = _s.Power.UpgradeCost(fs.FactoryId);
                    if (_s.Power.TryUpgrade(fs.FactoryId)) { spent += c; bought++; }
                }
                for (int l = 0; l < fs.Lines.Count; l++)
                {
                    if (!fs.Lines[l].Unlocked) continue;
                    var r = new MachineRef(fs.FactoryId, l, _s.AnalyzeLine(fs.FactoryId, l).BottleneckIndex);
                    var q = _s.Upgrades.GetQuote(r, 1);
                    if (q.IsMaxLevel || spent + q.Cost > budget) continue;
                    if (_s.Upgrades.TryUpgrade(r, 1)) { spent += q.Cost; bought++; }
                }
            }
            return bought;
        }

        /// <summary>Collects every finished mission, order, achievement, season/road/event reward and expedition.</summary>
        public int ClaimAll()
        {
            int n = 0;
            foreach (var m in _s.State.Missions.Daily) if (_s.Missions.TryClaim(m)) n++;
            foreach (var m in _s.State.Missions.Weekly) if (_s.Missions.TryClaim(m)) n++;
            foreach (var o in _s.State.Orders.ToArray()) if (_s.Orders.TryClaim(o)) n++;
            foreach (var a in AchievementService.Defs) if (_s.Achievements.TryClaim(a.Id)) n++;
            for (int i = 0; i < SeasonService.Tiers.Length; i++) if (_s.Seasons.TryClaim(i)) n++;
            for (int i = 0; i < SeasonRoadService.TierCount; i++) { if (_s.Road.TryClaim(i, false)) n++; if (_s.Road.TryClaim(i, true)) n++; }
            for (int i = 0; i < EventFactoryService.Tiers.Length; i++) if (_s.EventFactory.TryClaim(i)) n++;
            for (int i = 0; i < ExpeditionService.Slots; i++) if (_s.Expeditions.TryClaim(i)) n++;
            if (_s.DailyRewards.TryClaim()) n++;
            return n;
        }
    }

    // =====================================================================================================
    // Monetisation helpers: piggy bank, starter offer, weekly VIP subscription.
    // =====================================================================================================
    public sealed class PiggyBankService
    {
        public const double Share = 0.2;
        public const long Cap = 1000, MinToBreak = 200;
        private readonly GameSession _s;

        public PiggyBankService(GameSession session) => _s = session;

        public long Diamonds => _s.State.Live.PiggyDiamonds;
        public bool CanBreak => Diamonds >= MinToBreak;

        /// <summary>A share of earned (not purchased) diamonds is also saved into the piggy bank.</summary>
        public void Feed(long earnedDiamonds)
        {
            if (earnedDiamonds <= 0) return;
            _s.State.Live.PiggyDiamonds = Math.Min(Cap, Diamonds + Math.Max(1, (long)Math.Round(earnedDiamonds * Share)));
        }

        /// <summary>Called by the purchase of the "piggy_bank" product.</summary>
        public long Break()
        {
            long d = Diamonds;
            _s.State.Live.PiggyDiamonds = 0;
            _s.Wallet.AddDiamonds(d);
            return d;
        }
    }

    public sealed class OfferService
    {
        public static readonly TimeSpan StarterWindow = TimeSpan.FromHours(48);
        public const long VipDailyDiamonds = 30;
        private readonly GameSession _s;

        public OfferService(GameSession session) => _s = session;

        public void EnsureStarted()
        {
            if (_s.State.Live.StarterOfferStartUtcTicks == 0) _s.State.Live.StarterOfferStartUtcTicks = _s.Time.TrustedNow.Ticks;
        }

        /// <summary>The starter pack is highlighted (with a timer) for the first 48 hours.</summary>
        public double StarterSecondsLeft =>
            Math.Max(0, (_s.State.Live.StarterOfferStartUtcTicks + StarterWindow.Ticks - _s.Time.TrustedNow.Ticks) / (double)TimeSpan.TicksPerSecond);

        public bool StarterHighlighted => StarterSecondsLeft > 0 && !_s.Purchases.Owns(StoreCatalog.EntitlementStarter);

        public bool VipActive => _s.State.Live.SubscriptionExpiresUtcTicks > _s.Time.TrustedNow.Ticks;

        public bool CanClaimVipDaily => VipActive && _s.Time.TrustedDayIndex > _s.State.Live.SubscriptionLastDailyDay;

        public bool TryClaimVipDaily()
        {
            if (!CanClaimVipDaily) return false;
            _s.State.Live.SubscriptionLastDailyDay = _s.Time.TrustedDayIndex;
            _s.Wallet.AddDiamonds(VipDailyDiamonds);
            return true;
        }
    }
}
