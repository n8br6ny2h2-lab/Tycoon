using System;
using FactoryKing.Core.Simulation;

namespace FactoryKing.Core.TimeKeeping
{
    public interface IClock
    {
        DateTime UtcNow { get; }
    }

    public sealed class SystemClock : IClock
    {
        public DateTime UtcNow => DateTime.UtcNow;
    }

    /// <summary>Test/manual clock.</summary>
    public sealed class ManualClock : IClock
    {
        public DateTime UtcNow { get; set; } = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);
        public void Advance(TimeSpan span) => UtcNow = UtcNow + span;
    }

    /// <summary>
    /// Device-clock tamper protection without a server.
    /// - Time never moves backwards for the game: "trusted now" is max(device now, highest time ever seen).
    /// - Moving the clock back is detected (beyond a tolerance) and yields no offline time and no daily reset.
    /// - Moving it forward is bounded by the offline cap; daily rewards can only advance one day per claim.
    /// A server time source can later replace <see cref="IClock"/> without changing callers.
    /// </summary>
    public sealed class TimeGuard
    {
        /// <summary>Small backwards jumps (NTP corrections, time-zone/DST are UTC-neutral) are tolerated.</summary>
        public static readonly TimeSpan RollbackTolerance = TimeSpan.FromMinutes(5);

        private readonly IClock _clock;
        private readonly TimeState _state;

        public TimeGuard(IClock clock, TimeState state)
        {
            _clock = clock;
            _state = state;
            if (_state.FirstLaunchUtcTicks == 0) _state.FirstLaunchUtcTicks = clock.UtcNow.Ticks;
        }

        public IClock Clock => _clock;

        private long _serverOffsetTicks;

        /// <summary>True once the clock was checked against the game server (online features enabled).</summary>
        public bool ServerVerified { get; private set; }

        /// <summary>Device time corrected by the last server time check.</summary>
        private long Now => _clock.UtcNow.Ticks + _serverOffsetTicks;

        /// <summary>
        /// Uses the server's clock from now on (removes any manual change of the device clock). Offsets under a
        /// minute are ignored (network latency).
        /// </summary>
        public void ApplyServerTime(DateTime serverUtc)
        {
            long offset = serverUtc.Ticks - _clock.UtcNow.Ticks;
            _serverOffsetTicks = Math.Abs(offset) < TimeSpan.TicksPerMinute ? 0 : offset;
            ServerVerified = true;
        }

        /// <summary>True when the device clock is earlier than a time the game has already seen.</summary>
        public bool IsRolledBack => _state.MaxSeenUtcTicks > 0 &&
                                    Now < _state.MaxSeenUtcTicks - RollbackTolerance.Ticks;

        /// <summary>Monotonic game time: never earlier than anything seen before.</summary>
        public DateTime TrustedNow
        {
            get
            {
                long now = Now;
                if (now > _state.MaxSeenUtcTicks) _state.MaxSeenUtcTicks = now;
                return new DateTime(Math.Max(now, _state.MaxSeenUtcTicks), DateTimeKind.Utc);
            }
        }

        public long TrustedDayIndex => TrustedNow.Ticks / TimeSpan.TicksPerDay;

        /// <summary>ISO-like week index (weeks starting Monday) of trusted time.</summary>
        public long TrustedWeekIndex => TrustedDayIndex / 7; // day 0 (0001-01-01) was a Monday, so weeks start on Monday

        /// <summary>
        /// Seconds away since the last save, validated: 0 if the clock went backwards. Records the rollback.
        /// </summary>
        public double ConsumeElapsedSinceSave()
        {
            if (_state.LastSavedUtcTicks <= 0) return 0;
            long now = Now;
            if (IsRolledBack)
            {
                _state.ClockRollbacksDetected++;
                return 0;
            }
            double seconds = (now - _state.LastSavedUtcTicks) / (double)TimeSpan.TicksPerSecond;
            if (now > _state.MaxSeenUtcTicks) _state.MaxSeenUtcTicks = now;
            return Math.Max(0, seconds);
        }
    }
}
