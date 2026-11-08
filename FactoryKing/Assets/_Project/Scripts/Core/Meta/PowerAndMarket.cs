using System;
using FactoryKing.Core.Content;
using FactoryKing.Core.Economy;
using FactoryKing.Core.Numerics;
using FactoryKing.Core.Simulation;

namespace FactoryKing.Core.Meta
{
    /// <summary>
    /// Factory power grid (inspired by energy-management idle games). Every running machine draws power, more after
    /// each upgrade. When the factory's demand exceeds its power plant capacity the whole factory slows down
    /// proportionally, so the plant becomes a second bottleneck the player has to keep upgrading.
    /// </summary>
    public sealed class PowerService
    {
        public const string SourcePrefix = "power.";
        private readonly GameSession _s;

        public PowerService(GameSession session) => _s = session;

        public int Level(string factoryId) => _s.State.GetFactory(factoryId).PowerLevel;

        public double CapacityKw(string factoryId)
        {
            var def = _s.Content.GetFactory(factoryId);
            return def.BasePowerCapacityKw * (1d + _s.Tuning.PowerCapacityPerLevel * (Level(factoryId) - 1));
        }

        /// <summary>Steady-state demand of every unlocked line (independent of the throttle itself).</summary>
        public double DemandKw(string factoryId)
        {
            var fs = _s.State.GetFactory(factoryId);
            if (!fs.Unlocked) return 0;
            var def = _s.Content.GetFactory(factoryId);
            double demand = 0;
            foreach (var line in fs.Lines)
                if (line.Unlocked) demand += LineAnalyzer.Analyze(_s.Content, _s.Tuning, _s.Modifiers, def, line).PowerDemandKw;
            return demand;
        }

        /// <summary>1 when the plant covers demand, otherwise capacity / demand.</summary>
        public double Throttle(string factoryId)
        {
            double demand = DemandKw(factoryId);
            return demand <= 0 ? 1d : Math.Min(1d, CapacityKw(factoryId) / demand);
        }

        public BigDouble UpgradeCost(string factoryId)
        {
            var def = _s.Content.GetFactory(factoryId);
            return BigDouble.FromLog10(Math.Log10(def.PowerUpgradeBaseCost) + (Level(factoryId) - 1) * Math.Log10(def.PowerUpgradeGrowth));
        }

        public bool IsMax(string factoryId) => Level(factoryId) >= _s.Tuning.MaxPowerLevel;

        public bool TryUpgrade(string factoryId)
        {
            var fs = _s.State.GetFactory(factoryId);
            if (!fs.Unlocked || IsMax(factoryId)) return false;
            var cost = UpgradeCost(factoryId);
            if (!_s.Wallet.TrySpend(cost)) return false;
            fs.PowerLevel++;
            _s.State.TotalInvested += cost;
            Apply();
            return true;
        }

        public void Apply()
        {
            foreach (var fs in _s.State.Factories)
            {
                string source = SourcePrefix + fs.FactoryId;
                double t = fs.Unlocked ? Throttle(fs.FactoryId) : 1d;
                if (t < 0.9999) _s.Modifiers.Set(source, ModifierStat.ProductionSpeed, Math.Max(0.05, t), fs.FactoryId);
                else if (_s.Modifiers.HasSource(source)) _s.Modifiers.Remove(source);
            }
        }
    }

    /// <summary>
    /// Living market: each finished product's price drifts on its own slow cycle (±15%), and one factory's product is
    /// "in demand today" (+25%). Deterministic from trusted time, so offline play and all devices agree.
    /// </summary>
    public sealed class MarketService
    {
        public const string Source = "market";
        public const double Amplitude = 0.15;
        public const double HotBonus = 0.25;
        private readonly GameSession _s;

        public MarketService(GameSession session) => _s = session;

        /// <summary>Factory whose product gets the daily bonus.</summary>
        public string HotFactoryId(DateTime utc)
        {
            long day = utc.Ticks / TimeSpan.TicksPerDay;
            var list = _s.Content.Factories;
            return list[(int)(day * 7919 % list.Count)].Id;
        }

        public double PriceFactor(string factoryId, DateTime utc)
        {
            var def = _s.Content.GetFactory(factoryId);
            double hours = utc.Ticks / (double)TimeSpan.TicksPerHour;
            double period = 5 + def.Order * 1.5;                 // 5 h .. 11 h cycles
            double phase = (def.Order * 0.37) % 1d;
            double wave = Math.Sin((hours / period + phase) * Math.PI * 2d);
            double factor = 1d + Amplitude * wave;
            if (HotFactoryId(utc) == factoryId) factor += HotBonus;
            return factor;
        }

        public double PriceFactor(string factoryId) => PriceFactor(factoryId, _s.Time.TrustedNow);

        public void Apply()
        {
            var now = _s.Time.TrustedNow;
            foreach (var f in _s.Content.Factories)
                _s.Modifiers.SetQuiet(Source + "." + f.Id, ModifierStat.SellPrice, PriceFactor(f.Id, now), f.Id);
        }
    }
}
