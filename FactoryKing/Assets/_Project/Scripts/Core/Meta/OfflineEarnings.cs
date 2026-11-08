using System;
using System.Collections.Generic;
using FactoryKing.Core.Economy;
using FactoryKing.Core.Numerics;
using FactoryKing.Core.Simulation;

namespace FactoryKing.Core.Meta
{
    public sealed class OfflineReport
    {
        public double SecondsAway;
        public double SecondsCredited;
        public double CapSeconds;
        public bool Capped;
        public bool ClockRollbackDetected;
        public BigDouble Earnings;
        public long UnitsSold;
        public readonly Dictionary<string, BigDouble> ByFactory = new Dictionary<string, BigDouble>();
        public bool Claimed;

        public bool HasEarnings => Earnings.IsPositive;
    }

    /// <summary>
    /// Closed-form offline production: every owned line runs at its steady-state rate (bottleneck throughput) for the
    /// validated time away, capped (8 h base + research + VIP). No per-frame simulation is replayed, so the result is
    /// instant and identical to what the line would earn continuously.
    /// </summary>
    public sealed class OfflineEarningsService
    {
        private readonly GameSession _s;

        public OfflineEarningsService(GameSession session) => _s = session;

        /// <summary>Unclaimed report waiting for the summary screen.</summary>
        public OfflineReport Pending { get; private set; }

        public double CapSeconds
        {
            get
            {
                double hours = _s.Tuning.OfflineCapHours + _s.Research.OfflineCapBonusHours + _s.Engineering.Level(EngineeringService.Offline);
                if (_s.State.Entitlements.Contains(StoreCatalog.EntitlementVip)) hours += StoreCatalog.VipOfflineHours;
                return hours * 3600d;
            }
        }

        public OfflineReport Calculate(double secondsAway, bool rollback = false)
        {
            var report = new OfflineReport
            {
                SecondsAway = Math.Max(0, secondsAway),
                CapSeconds = CapSeconds,
                ClockRollbackDetected = rollback,
            };
            if (rollback || report.SecondsAway < _s.Tuning.OfflineMinimumSeconds) return report;

            report.SecondsCredited = Math.Min(report.SecondsAway, report.CapSeconds);
            report.Capped = report.SecondsAway > report.CapSeconds;

            BigDouble total = BigDouble.Zero;
            foreach (var fs in _s.State.Factories)
            {
                if (!fs.Unlocked || !_s.Content.TryGetFactory(fs.FactoryId, out var def)) continue;
                double offlineMod = _s.Modifiers.Get(ModifierStat.OfflineEarnings, def.Id);
                BigDouble factoryTotal = BigDouble.Zero;
                for (int l = 0; l < fs.Lines.Count; l++)
                {
                    if (!fs.Lines[l].Unlocked) continue;
                    var rep = LineAnalyzer.Analyze(_s.Content, _s.Tuning, _s.Modifiers, def, fs.Lines[l]);
                    if (rep.NetPerSecond <= 0) continue;
                    factoryTotal += BigDouble.FromDouble(rep.NetPerSecond) * (report.SecondsCredited * _s.Tuning.OfflineEfficiency * offlineMod);
                    report.UnitsSold += (long)Math.Floor(rep.FinalUnitsPerSecond * report.SecondsCredited * _s.Tuning.OfflineEfficiency);
                }
                if (factoryTotal.IsPositive) report.ByFactory[def.Id] = factoryTotal;
                total += factoryTotal;
            }
            report.Earnings = total;
            return report;
        }

        /// <summary>Called once on launch/resume with the validated time away.</summary>
        public OfflineReport Begin(double secondsAway, bool rollback)
        {
            var r = Calculate(secondsAway, rollback);
            Pending = r.HasEarnings || r.ClockRollbackDetected ? r : null;
            return r;
        }

        /// <summary>Credits the pending report (multiplier 2 after a completed rewarded ad).</summary>
        public BigDouble Claim(double multiplier = 1)
        {
            var r = Pending;
            if (r == null || r.Claimed) return BigDouble.Zero;
            r.Claimed = true;
            Pending = null;
            if (!r.HasEarnings) return BigDouble.Zero;
            var amount = r.Earnings * Math.Max(1, Math.Min(2, multiplier));
            _s.Wallet.Grant(amount);
            _s.State.Stats.UnitsSold += r.UnitsSold;
            _s.State.TotalUnitsSold += r.UnitsSold;
            return amount;
        }
    }
}
