using System;
using FactoryKing.Core.Content;
using FactoryKing.Core.Economy;
using FactoryKing.Core.Meta;
using FactoryKing.Core.Numerics;
using FactoryKing.Core.TimeKeeping;

namespace FactoryKing.Core.Simulation
{
    /// <summary>
    /// Composition root of the pure game model. Owns the services; contains no gameplay rules itself.
    /// The Unity layer creates one session and calls <see cref="Tick"/> from a single driver.
    /// </summary>
    public sealed class GameSession
    {
        public GameContent Content { get; }
        public EconomyTuning Tuning { get; }
        public GameState State { get; }
        public ModifierStack Modifiers { get; }
        public Wallet Wallet { get; }
        public EconomyLedger Ledger { get; }
        public ProductionSimulator Simulator { get; }
        public UpgradeService Upgrades { get; }
        public FactoryService Factories { get; }

        public TimeGuard Time { get; }
        public AnalyticsService Analytics { get; }
        public OfflineEarningsService Offline { get; }
        public ManagerService Managers { get; }
        public PrestigeService Prestige { get; }
        public ResearchService Research { get; }
        public MissionService Missions { get; }
        public OrderService Orders { get; }
        public AchievementService Achievements { get; }
        public DailyRewardService DailyRewards { get; }
        public LiveEventService Events { get; }
        public BoostService Boosts { get; }
        public PurchaseService Purchases { get; }
        public RewardedAdService Ads { get; }
        public PowerService Power { get; }
        public MarketService Market { get; }
        public CosmeticService Cosmetics { get; }
        public SeasonService Seasons { get; }

        private double _metaTimer;

        public GameSession(GameContent content, EconomyTuning tuning = null, GameState state = null, IClock clock = null,
            StoreCatalog catalog = null)
        {
            Content = content ?? throw new ArgumentNullException(nameof(content));
            Tuning = tuning ?? EconomyTuning.CreateDefault();
            State = state ?? GameState.CreateNew(Content, Tuning);
            State.EnsureConsistent(Content);

            Modifiers = new ModifierStack();
            Wallet = new Wallet(State);
            Ledger = new EconomyLedger(Tuning.StatsWindowSeconds);
            Simulator = new ProductionSimulator(Content, Tuning, Modifiers, Wallet, State, Ledger);
            Upgrades = new UpgradeService(Content, Tuning, State, Wallet, Modifiers);
            Factories = new FactoryService(Content, State, Wallet);

            Time = new TimeGuard(clock ?? new SystemClock(), State.Time);
            Analytics = new AnalyticsService(this);
            Research = new ResearchService(this);
            Offline = new OfflineEarningsService(this);
            Managers = new ManagerService(this);
            Prestige = new PrestigeService(this);
            Missions = new MissionService(this);
            Orders = new OrderService(this);
            Achievements = new AchievementService(this);
            DailyRewards = new DailyRewardService(this);
            Events = new LiveEventService(this);
            Boosts = new BoostService(this);
            Purchases = new PurchaseService(this, catalog ?? StoreCatalog.CreateDefault());
            Ads = new RewardedAdService(this);
            Power = new PowerService(this);
            Market = new MarketService(this);
            Cosmetics = new CosmeticService(this);
            Seasons = new SeasonService(this);
            Seasons.EnsureStarted();

            ApplyAllModifiers();

            Upgrades.MachineUpgraded += (r, from, to) => Power.Apply();
            Factories.LineUnlocked += (f, l) => Power.Apply();
            Factories.FactoryUnlocked += f => Power.Apply();
            Upgrades.MachineUpgraded += (r, from, to) =>
            {
                if (State.Stats.Upgrades == to - from) Analytics.Track(AnalyticsEvents.FirstUpgrade, "machine", r.ToString());
            };
            Factories.FactoryUnlocked += id => Analytics.Track(AnalyticsEvents.FactoryUnlocked, "factory", id);
        }

        /// <summary>Re-derives every modifier from persistent state (after load, prestige, purchases...).</summary>
        public void ApplyAllModifiers()
        {
            Prestige.Apply();
            Research.Apply();
            Managers.Apply();
            Purchases.ApplyEntitlements();
            Boosts.Apply();
            Managers.ApplyAbilities();
            Market.Apply();
            Seasons.Apply();
            Power.Apply(); // last: demand depends on the other modifiers only through utilisation ratios
        }

        /// <summary>
        /// Call once after loading (and on every resume from background): validates time away, prepares the offline
        /// summary, refreshes missions/orders. Returns the offline report (may have zero earnings).
        /// </summary>
        public OfflineReport Resume()
        {
            bool rollback = Time.IsRolledBack;
            double away = Time.ConsumeElapsedSinceSave();
            Seasons.Tick(); // a season that ended while away resets before offline earnings are valued
            // Expire boosts/abilities and refresh market prices before valuing the time away.
            Boosts.Apply();
            Managers.ApplyAbilities();
            Market.Apply();
            Power.Apply();
            var report = Offline.Begin(away, rollback);
            State.Stats.SessionCount++;
            MetaTick();
            return report;
        }

        /// <summary>Advances the live simulation. Call with real (unscaled) frame time or a fixed step.</summary>
        public void Tick(double dt)
        {
            if (dt <= 0 || double.IsNaN(dt) || double.IsInfinity(dt)) return;
            Simulator.Tick(dt);
            Ledger.Advance(dt);
            State.PlayTimeSeconds += dt;
            _metaTimer += dt;
            if (_metaTimer >= 1d)
            {
                _metaTimer = 0;
                MetaTick();
            }
        }

        /// <summary>Once-per-second bookkeeping: boosts, missions, orders, achievements.</summary>
        public void MetaTick()
        {
            Boosts.Apply();
            Managers.ApplyAbilities();
            Market.Apply();
            Power.Apply();
            Missions.Refresh();
            Orders.Refresh();
            Achievements.Evaluate();
            Seasons.Tick();
        }

        public LineReport AnalyzeLine(string factoryId, int line) =>
            LineAnalyzer.Analyze(Content, Tuning, Modifiers, Content.GetFactory(factoryId), State.GetFactory(factoryId).Lines[line]);

        /// <summary>Theoretical steady-state net income of everything owned.</summary>
        public double PotentialNetPerSecond => LineAnalyzer.TotalNetPerSecond(Content, Tuning, Modifiers, State);

        /// <summary>Cash reward worth <paramref name="seconds"/> of current income (never less than a small floor).</summary>
        public BigDouble IncomeReward(double seconds) =>
            BigDouble.FromDouble(Math.Max(Tuning.StartingCash, Math.Max(0, PotentialNetPerSecond) * seconds));

        public int OwnedMachineCount
        {
            get
            {
                int n = 0;
                foreach (var f in State.Factories)
                    if (f.Unlocked)
                        foreach (var l in f.Lines)
                            if (l.Unlocked) n += l.Machines.Count;
                return n;
            }
        }

        public BigDouble CompanyValue
        {
            get
            {
                double income = Math.Max(0d, PotentialNetPerSecond);
                return State.Cash - State.Debt
                       + State.TotalInvested * Tuning.InvestedAssetWeight
                       + BigDouble.FromDouble(income * Tuning.IncomeValuationSeconds);
            }
        }

        /// <summary>1 at start, +1 for every half order of magnitude of lifetime earnings.</summary>
        public int CompanyLevel
        {
            get
            {
                double log = State.LifetimeEarnings.IsPositive ? State.LifetimeEarnings.Log10() : 0;
                return 1 + (int)Math.Floor(Math.Max(0, log) * 2);
            }
        }
    }
}
