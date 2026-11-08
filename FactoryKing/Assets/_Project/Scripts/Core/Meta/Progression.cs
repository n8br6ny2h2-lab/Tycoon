using System;
using System.Collections.Generic;
using FactoryKing.Core.Economy;
using FactoryKing.Core.Numerics;
using FactoryKing.Core.Simulation;

namespace FactoryKing.Core.Meta
{
    public static class MissionTypes
    {
        public const string Produce = "produce";
        public const string Sell = "sell";
        public const string Upgrade = "upgrade";
        public const string Earn = "earn";
        public const string Orders = "orders";
        public const string ManagerLevels = "manager_levels";
        public const string ReachLevel = "reach_level";
    }

    /// <summary>Daily (3) and weekly (3) missions. Progress = growth of a cumulative counter since assignment.</summary>
    public sealed class MissionService
    {
        private readonly GameSession _s;

        public MissionService(GameSession session) => _s = session;

        public event Action Changed;

        public double Current(string type)
        {
            var st = _s.State.Stats;
            switch (type)
            {
                case MissionTypes.Produce: return st.UnitsProduced;
                case MissionTypes.Sell: return st.UnitsSold;
                case MissionTypes.Upgrade: return st.Upgrades;
                case MissionTypes.Earn: return _s.State.LifetimeEarnings.ToDouble();
                case MissionTypes.Orders: return st.OrdersCompleted;
                case MissionTypes.ManagerLevels: return st.ManagerLevelUps;
                case MissionTypes.ReachLevel: return st.HighestMachineLevel;
                default: return 0;
            }
        }

        public double Progress(MissionState m) =>
            m.Type == MissionTypes.ReachLevel ? Current(m.Type) : Math.Max(0, Current(m.Type) - m.Baseline);

        public bool IsComplete(MissionState m) => Progress(m) >= m.Target;

        /// <summary>Generates new boards when the (trusted) day/week changes.</summary>
        public void Refresh()
        {
            var board = _s.State.Missions;
            long day = _s.Time.TrustedDayIndex, week = _s.Time.TrustedWeekIndex;
            bool changed = false;
            if (board.DailyDay != day)
            {
                board.DailyDay = day;
                board.Daily.Clear();
                var rnd = new Random((int)(day % int.MaxValue));
                var pool = new List<string> { MissionTypes.Produce, MissionTypes.Sell, MissionTypes.Upgrade, MissionTypes.Earn, MissionTypes.Orders };
                for (int i = 0; i < 3; i++)
                {
                    int k = rnd.Next(pool.Count);
                    board.Daily.Add(Create("d" + day + "_" + i, pool[k], daily: true, rnd));
                    pool.RemoveAt(k);
                }
                changed = true;
            }
            if (board.WeeklyWeek != week)
            {
                board.WeeklyWeek = week;
                board.Weekly.Clear();
                var rnd = new Random((int)(week % int.MaxValue) + 7919);
                board.Weekly.Add(Create("w" + week + "_0", MissionTypes.ReachLevel, false, rnd));
                board.Weekly.Add(Create("w" + week + "_1", MissionTypes.Sell, false, rnd));
                board.Weekly.Add(Create("w" + week + "_2", _s.State.Managers.Count > 0 ? MissionTypes.ManagerLevels : MissionTypes.Upgrade, false, rnd));
                changed = true;
            }
            if (changed) Changed?.Invoke();
        }

        private MissionState Create(string id, string type, bool daily, Random rnd)
        {
            double soldPerSec = 0, netPerSec = 0;
            foreach (var fs in _s.State.Factories)
            {
                if (!fs.Unlocked) continue;
                for (int l = 0; l < fs.Lines.Count; l++)
                {
                    if (!fs.Lines[l].Unlocked) continue;
                    var r = _s.AnalyzeLine(fs.FactoryId, l);
                    soldPerSec += r.FinalUnitsPerSecond;
                    netPerSec += Math.Max(0, r.NetPerSecond);
                }
            }
            double minutes = daily ? 12 + rnd.Next(10) : 240;
            double target;
            switch (type)
            {
                case MissionTypes.Produce: target = Math.Max(40, Math.Round(soldPerSec * 4 * 60 * minutes)); break;
                case MissionTypes.Sell: target = Math.Max(20, Math.Round(soldPerSec * 60 * minutes)); break;
                case MissionTypes.Upgrade: target = daily ? 10 + rnd.Next(3) * 5 : 120; break;
                case MissionTypes.Earn: target = Math.Max(500, Math.Round(netPerSec * 60 * minutes)); break;
                case MissionTypes.Orders: target = daily ? 2 : 10; break;
                case MissionTypes.ManagerLevels: target = 3; break;
                case MissionTypes.ReachLevel: target = Math.Min(100, Math.Max(30, _s.State.Stats.HighestMachineLevel + 25)); break;
                default: target = 1; break;
            }
            return new MissionState
            {
                Id = id, Type = type, Target = target, Baseline = Current(type),
                RewardDiamonds = daily ? 5 + rnd.Next(4) : 40,
                RewardCashSeconds = daily ? 300 : 900,
                RewardCash = Math.Max(_s.Tuning.StartingCash, netPerSec * (daily ? 300 : 900)),
            };
        }

        public bool TryClaim(MissionState m)
        {
            if (m == null || m.Claimed || !IsComplete(m)) return false;
            m.Claimed = true;
            _s.Wallet.AddDiamonds(m.RewardDiamonds);
            _s.Wallet.Grant(m.RewardCash > 0 ? BigDouble.FromDouble(m.RewardCash) : _s.IncomeReward(m.RewardCashSeconds));
            _s.Analytics.Track(AnalyticsEvents.MissionClaimed, "type", m.Type);
            Changed?.Invoke();
            return true;
        }

        public int ClaimableCount
        {
            get
            {
                int n = 0;
                foreach (var m in _s.State.Missions.Daily) if (!m.Claimed && IsComplete(m)) n++;
                foreach (var m in _s.State.Missions.Weekly) if (!m.Claimed && IsComplete(m)) n++;
                return n;
            }
        }
    }

    /// <summary>Customer orders: sell N units of a product before the deadline for a bonus payment.</summary>
    public sealed class OrderService
    {
        public const int MaxOrders = 3;
        public static readonly TimeSpan Duration = TimeSpan.FromMinutes(45);
        private readonly GameSession _s;
        private int _counter;

        public OrderService(GameSession session) => _s = session;

        public event Action Changed;

        public long Progress(OrderState o)
        {
            _s.State.Stats.SoldByProduct.TryGetValue(o.ProductId, out long sold);
            return Math.Max(0, sold - o.Baseline);
        }

        public bool IsComplete(OrderState o) => Progress(o) >= o.Quantity;
        public bool IsExpired(OrderState o) => _s.Time.TrustedNow.Ticks > o.ExpiresUtcTicks;

        public BigDouble Reward(OrderState o)
        {
            var def = _s.Content.GetFactory(o.FactoryId);
            var product = _s.Content.GetProduct(_s.Content.GetMachine(def.MachineChain[def.MachineChain.Count - 1]).OutputProductId);
            return BigDouble.FromDouble(product.BaseValue) * (o.Quantity * o.RewardMultiplier * _s.Modifiers.Get(ModifierStat.SellPrice, o.FactoryId));
        }

        public void Refresh()
        {
            bool changed = _s.State.Orders.RemoveAll(o => o.Claimed || (IsExpired(o) && !IsComplete(o))) > 0;
            var owned = _s.State.Factories.FindAll(f => f.Unlocked);
            if (owned.Count == 0) return;
            var rnd = new Random(unchecked((int)(_s.Time.TrustedNow.Ticks / TimeSpan.TicksPerSecond) + _counter));
            while (_s.State.Orders.Count < MaxOrders)
            {
                var fs = owned[rnd.Next(owned.Count)];
                var def = _s.Content.GetFactory(fs.FactoryId);
                string product = _s.Content.GetMachine(def.MachineChain[def.MachineChain.Count - 1]).OutputProductId;
                double rate = 0;
                for (int l = 0; l < fs.Lines.Count; l++)
                    if (fs.Lines[l].Unlocked) rate += _s.AnalyzeLine(fs.FactoryId, l).FinalUnitsPerSecond;
                _s.State.Stats.SoldByProduct.TryGetValue(product, out long sold);
                _s.State.Orders.Add(new OrderState
                {
                    Id = "o" + _s.Time.TrustedNow.Ticks + "_" + (_counter++),
                    FactoryId = fs.FactoryId,
                    ProductId = product,
                    Quantity = Math.Max(8, (long)Math.Round(rate * (240 + rnd.Next(600)))),
                    Baseline = sold,
                    RewardMultiplier = 0.3 + rnd.NextDouble() * 0.3, // bonus on top of the normal sale price
                    ExpiresUtcTicks = _s.Time.TrustedNow.Add(Duration).Ticks,
                });
                changed = true;
            }
            if (changed) Changed?.Invoke();
        }

        public bool TryClaim(OrderState o)
        {
            if (o == null || o.Claimed || !IsComplete(o)) return false;
            o.Claimed = true;
            _s.Wallet.Grant(Reward(o));
            _s.Wallet.AddDiamonds(2);
            _s.State.Stats.OrdersCompleted++;
            Refresh();
            Changed?.Invoke();
            return true;
        }
    }

    public sealed class AchievementDef
    {
        public string Id;
        public string NameKey;
        public long RewardDiamonds;
        public Func<GameSession, double> Value;
        public double Target;
        /// <summary>Game Center achievement id (configure in App Store Connect with the same id).</summary>
        public string GameCenterId;
    }

    public sealed class AchievementService
    {
        public static readonly List<AchievementDef> Defs = new List<AchievementDef>
        {
            A("ach_first_upgrade", 5, s => s.State.Stats.Upgrades, 1),
            A("ach_first_factory", 20, s => s.State.Stats.FactoriesBought, 1),
            A("ach_ten_machines", 15, s => s.OwnedMachineCount, 10),
            A("ach_first_million", 25, s => s.State.LifetimeEarnings.ToDouble(), 1e6),
            A("ach_first_prestige", 50, s => s.State.Prestige.Count, 1),
            A("ach_five_factories", 100, s => s.State.Factories.FindAll(f => f.Unlocked).Count, 5),
            A("ach_first_billion", 60, s => s.State.LifetimeEarnings.ToDouble(), 1e9),
            A("ach_level_100", 40, s => s.State.Stats.HighestMachineLevel, 100),
            A("ach_orders_10", 20, s => s.State.Stats.OrdersCompleted, 10),
            A("ach_first_manager", 10, s => s.State.Managers.Count, 1),
            A("ach_research_10", 25, s => s.Research.TotalLevels, 10),
            A("ach_streak_7", 30, s => s.State.Stats.DailyRewardsClaimed, 7),
        };

        private static AchievementDef A(string id, long dia, Func<GameSession, double> value, double target) => new AchievementDef
        {
            Id = id, NameKey = "achievement." + id, RewardDiamonds = dia, Value = value, Target = target,
            GameCenterId = "factoryking." + id.Substring(4),
        };

        private readonly GameSession _s;

        public AchievementService(GameSession session) => _s = session;

        /// <summary>Raised once when an achievement is reached (Game Center reporting hooks here).</summary>
        public event Action<AchievementDef> Unlocked;

        public double Progress01(AchievementDef d) => Math.Max(0, Math.Min(1, d.Value(_s) / d.Target));

        public void Evaluate()
        {
            foreach (var d in Defs)
            {
                if (_s.State.AchievementsUnlocked.Contains(d.Id)) continue;
                if (d.Value(_s) >= d.Target)
                {
                    _s.State.AchievementsUnlocked.Add(d.Id);
                    Unlocked?.Invoke(d);
                }
            }
        }

        public bool TryClaim(string id)
        {
            var d = Defs.Find(x => x.Id == id);
            if (d == null || !_s.State.AchievementsUnlocked.Contains(id) || !_s.State.AchievementsClaimed.Add(id)) return false;
            _s.Wallet.AddDiamonds(d.RewardDiamonds);
            return true;
        }
    }

    public sealed class DailyRewardDef
    {
        public long Diamonds;
        public double CashSeconds;
        public double BoostMinutes;
        public string DecorId;
    }

    /// <summary>7-day login calendar. Missing a day never resets progress (non-punishing).</summary>
    public sealed class DailyRewardService
    {
        public static readonly DailyRewardDef[] Cycle =
        {
            new DailyRewardDef { CashSeconds = 300 },
            new DailyRewardDef { Diamonds = 5 },
            new DailyRewardDef { CashSeconds = 900 },
            new DailyRewardDef { Diamonds = 10 },
            new DailyRewardDef { BoostMinutes = 30 },
            new DailyRewardDef { CashSeconds = 1800 },
            new DailyRewardDef { Diamonds = 30, DecorId = "decor_weekly_flag" },
        };

        private readonly GameSession _s;

        public DailyRewardService(GameSession session) => _s = session;

        public bool CanClaim => !_s.Time.IsRolledBack && _s.Time.TrustedDayIndex > _s.State.Daily.LastClaimDay;

        public int NextIndex => _s.State.Daily.NextIndex % Cycle.Length;

        public bool TryClaim()
        {
            if (!CanClaim) return false;
            var r = Cycle[NextIndex];
            if (r.Diamonds > 0) _s.Wallet.AddDiamonds(r.Diamonds);
            if (r.CashSeconds > 0) _s.Wallet.Grant(_s.IncomeReward(r.CashSeconds));
            if (r.BoostMinutes > 0) _s.Boosts.Activate(r.BoostMinutes);
            if (r.DecorId != null) _s.State.Collection.Add(r.DecorId);
            _s.State.Daily.LastClaimDay = _s.Time.TrustedDayIndex;
            _s.State.Daily.NextIndex = (NextIndex + 1) % Cycle.Length;
            _s.State.Stats.DailyRewardsClaimed++;
            return true;
        }
    }

    public sealed class LiveEventDef
    {
        public string Id;
        public string NameKey;
        public DateTime StartUtc;
        public DateTime EndUtc;
        public string GoalType; // a MissionTypes value
        public double TargetMinutesOfOutput;
        public long RewardDiamonds;
        public string RewardDecorId;
    }

    /// <summary>
    /// Timed events. The built-in schedule is a recurring weekend event; a remote JSON schedule can be appended to
    /// <see cref="Schedule"/> later without code changes elsewhere.
    /// </summary>
    public sealed class LiveEventService
    {
        private readonly GameSession _s;
        public readonly List<LiveEventDef> Schedule = new List<LiveEventDef>();

        public LiveEventService(GameSession session) => _s = session;

        /// <summary>Weekend Rush: Friday 00:00 UTC to Monday 00:00 UTC every week.</summary>
        public LiveEventDef WeekendEvent(DateTime now)
        {
            int dow = ((int)now.DayOfWeek + 6) % 7; // Monday = 0
            var monday = now.Date.AddDays(-dow);
            var start = monday.AddDays(4);
            return new LiveEventDef
            {
                Id = "evt_weekend_" + start.ToString("yyyyMMdd"),
                NameKey = "event.weekend_rush",
                StartUtc = DateTime.SpecifyKind(start, DateTimeKind.Utc),
                EndUtc = DateTime.SpecifyKind(monday.AddDays(7), DateTimeKind.Utc),
                GoalType = MissionTypes.Sell,
                TargetMinutesOfOutput = 180,
                RewardDiamonds = 60,
                RewardDecorId = "decor_golden_gear",
            };
        }

        public List<LiveEventDef> Active()
        {
            var now = _s.Time.TrustedNow;
            var list = new List<LiveEventDef>();
            var weekend = WeekendEvent(now);
            if (now >= weekend.StartUtc && now < weekend.EndUtc) list.Add(weekend);
            foreach (var e in Schedule) if (now >= e.StartUtc && now < e.EndUtc) list.Add(e);
            return list;
        }

        public EventProgress Progress(LiveEventDef e)
        {
            if (!_s.State.Events.TryGetValue(e.Id, out var p))
            {
                double rate = 0;
                foreach (var fs in _s.State.Factories)
                    if (fs.Unlocked)
                        for (int l = 0; l < fs.Lines.Count; l++)
                            if (fs.Lines[l].Unlocked) rate += _s.AnalyzeLine(fs.FactoryId, l).FinalUnitsPerSecond;
                p = new EventProgress
                {
                    EventId = e.Id,
                    Baseline = _s.Missions.Current(e.GoalType),
                    Target = Math.Max(100, Math.Round(rate * 60 * e.TargetMinutesOfOutput)),
                };
                _s.State.Events[e.Id] = p;
            }
            return p;
        }

        public double Value(LiveEventDef e) => Math.Max(0, _s.Missions.Current(e.GoalType) - Progress(e).Baseline);

        public bool TryClaim(LiveEventDef e)
        {
            var p = Progress(e);
            if (p.Claimed || Value(e) < p.Target) return false;
            p.Claimed = true;
            long dia = e.RewardDiamonds;
            if (_s.State.Entitlements.Contains(StoreCatalog.EntitlementSeasonPass)) dia *= 2;
            _s.Wallet.AddDiamonds(dia);
            if (e.RewardDecorId != null) _s.State.Collection.Add(e.RewardDecorId);
            return true;
        }
    }

    /// <summary>Temporary production boost (x2 speed), e.g. from a rewarded ad or the daily calendar.</summary>
    public sealed class BoostService
    {
        public const string Source = "boost";
        public const double Multiplier = 2.0;
        public const double MaxStackMinutes = 240;
        private readonly GameSession _s;

        public BoostService(GameSession session) => _s = session;

        public double RemainingSeconds => Math.Max(0, (_s.State.BoostEndUtcTicks - _s.Time.TrustedNow.Ticks) / (double)TimeSpan.TicksPerSecond);
        public bool IsActive => RemainingSeconds > 0;

        public void Activate(double minutes)
        {
            long now = _s.Time.TrustedNow.Ticks;
            long start = Math.Max(now, _s.State.BoostEndUtcTicks);
            long end = start + (long)(minutes * TimeSpan.TicksPerMinute);
            _s.State.BoostEndUtcTicks = Math.Min(end, now + (long)(MaxStackMinutes * TimeSpan.TicksPerMinute));
            Apply();
        }

        public void Apply()
        {
            if (IsActive) { if (!_s.Modifiers.HasSource(Source)) _s.Modifiers.Set(Source, ModifierStat.ProductionSpeed, Multiplier); }
            else if (_s.Modifiers.HasSource(Source)) _s.Modifiers.Remove(Source);
        }
    }
}
