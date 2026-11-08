using System;
using FactoryKing.Core.Content;
using FactoryKing.Core.Economy;
using FactoryKing.Core.Numerics;

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

        public GameSession(GameContent content, EconomyTuning tuning = null, GameState state = null)
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
        }

        /// <summary>Advances the live simulation. Call with real (unscaled) frame time or a fixed step.</summary>
        public void Tick(double dt)
        {
            if (dt <= 0 || double.IsNaN(dt) || double.IsInfinity(dt)) return;
            Simulator.Tick(dt);
            Ledger.Advance(dt);
            State.PlayTimeSeconds += dt;
        }

        public LineReport AnalyzeLine(string factoryId, int line) =>
            LineAnalyzer.Analyze(Content, Tuning, Modifiers, Content.GetFactory(factoryId), State.GetFactory(factoryId).Lines[line]);

        /// <summary>Theoretical steady-state net income of everything owned.</summary>
        public double PotentialNetPerSecond => LineAnalyzer.TotalNetPerSecond(Content, Tuning, Modifiers, State);

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
