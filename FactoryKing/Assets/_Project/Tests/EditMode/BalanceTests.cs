using System;
using System.Linq;
using FactoryKing.Core.Content;
using FactoryKing.Core.Meta;
using FactoryKing.Core.Numerics;
using FactoryKing.Core.Simulation;
using FactoryKing.Core.TimeKeeping;
using NUnit.Framework;

namespace FactoryKing.Tests
{
    /// <summary>
    /// Season pacing guard rail. An "average player" bot plays 4 sessions of 15 minutes a day (offline earnings in
    /// between, doubled half the time), spends greedily, uses manager skills and prestiges sensibly. Income during a
    /// session is credited analytically (the live simulation is verified separately to match the analyzer), which
    /// lets a whole 30-day season run in seconds.
    /// Design target: the 20th factory near the end of the season — never early, never unreachable.
    /// </summary>
    public class BalanceTests
    {
        public static double[] PlaySeason(int sessionsPerDay = 4, int sessionMinutes = 15, int stepMinutes = 3, int days = 32)
        {
            var clock = new ManualClock { UtcNow = new DateTime(2027, 1, 1, 7, 0, 0, DateTimeKind.Utc) };
            var s = new GameSession(DefaultContent.Create(), clock: clock);
            var reached = new double[DefaultContent.FactoryCount + 1];
            var origin = new DateTime(2027, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            long day0 = clock.UtcNow.Ticks;
            double[] hours = Enumerable.Range(0, sessionsPerDay).Select(i => 8.0 + i * (14.0 / Math.Max(1, sessionsPerDay - 1))).ToArray();

            for (int day = 0; day < days && reached[DefaultContent.FactoryCount] == 0; day++)
            foreach (var h in hours)
            {
                var start = origin.AddDays(day).AddHours(h);
                if (start <= clock.UtcNow) continue;
                double gap = (start - clock.UtcNow).TotalSeconds;
                clock.UtcNow = start;
                s.Wallet.Grant(s.Offline.Calculate(gap).Earnings * 1.5);
                s.MetaTick();
                if (s.Seasons.Number > 1) return reached; // season ended
                for (int m = 0; m < sessionMinutes; m += stepMinutes)
                {
                    s.Wallet.Grant(BigDouble.FromDouble(Math.Max(0, s.PotentialNetPerSecond) * 60 * stepMinutes));
                    clock.Advance(TimeSpan.FromMinutes(stepMinutes));
                    s.MetaTick();
                    Spend(s);
                    int owned = s.OwnedRegularFactories;
                    for (int k = 1; k <= owned; k++)
                        if (reached[k] == 0) reached[k] = (clock.UtcNow.Ticks - day0) / (double)TimeSpan.TicksPerDay;
                }
            }
            return reached;
        }

        private static void Spend(GameSession s)
        {
            for (int pass = 0; pass < 40; pass++)
            {
                bool bought = false;
                foreach (var f in s.Content.Factories) bought |= s.Factories.TryUnlockFactory(f.Id);
                foreach (var fs in s.State.Factories)
                {
                    if (!fs.Unlocked) continue;
                    if (s.Power.Throttle(fs.FactoryId) < 0.999) bought |= s.Power.TryUpgrade(fs.FactoryId);
                    for (int l = 0; l < fs.Lines.Count; l++)
                    {
                        if (!fs.Lines[l].Unlocked) { bought |= s.Factories.TryUnlockLine(fs.FactoryId, l); continue; }
                        bought |= s.Upgrades.TryUpgrade(new MachineRef(fs.FactoryId, l, s.AnalyzeLine(fs.FactoryId, l).BottleneckIndex), 5);
                    }
                }
                foreach (var r in ResearchService.Defs) bought |= s.Research.TryBuy(r.Id);
                if (!bought) break;
            }
            foreach (var d in ManagerService.Defs) s.Managers.TryHire(d.Id);
            var owned = s.State.Factories.FindAll(f => f.Unlocked);
            for (int i = 0; i < s.State.Managers.Count && i < owned.Count; i++)
                s.Managers.Assign(s.State.Managers[i].Id, owned[owned.Count - 1 - i].FactoryId);
            foreach (var m in s.State.Managers) s.Managers.TryUseAbility(m.Id);
            for (int i = 0; i < SeasonService.Tiers.Length; i++) s.Seasons.TryClaim(i);
            foreach (var a in AchievementService.Defs) s.Achievements.TryClaim(a.Id);
            s.DailyRewards.TryClaim();
            var pv = s.Prestige.Preview();
            if (pv.Available && pv.CrownsGained >= Math.Max(3, s.State.Prestige.Crowns * 0.5)) s.Prestige.TryPrestige();
        }

        [Test]
        public void Average_Player_Finishes_The_Season_In_About_A_Month()
        {
            var r = PlaySeason();
            TestContext.WriteLine($"5 @ {r[5]:0.0} d, 10 @ {r[10]:0.0} d, 15 @ {r[15]:0.0} d, 20 @ {r[20]:0.0} d");
            Assert.That(r[2], Is.InRange(0.01, 1.0), "second factory on the first day");
            Assert.That(r[5], Is.InRange(1.0, 7.0));
            Assert.That(r[10], Is.InRange(3.0, 14.0));
            Assert.That(r[15], Is.InRange(8.0, 24.0));
            Assert.That(r[20], Is.InRange(18.0, 30.0), "20th factory near the end of the 30-day season");
        }
    }
}

namespace FactoryKing.Tests
{
    public class BalanceExtremesTests
    {
        [Test]
        public void Light_Player_Still_Gets_Far()
        {
            var r = BalanceTests.PlaySeason(sessionsPerDay: 2, sessionMinutes: 9);
            TestContext.WriteLine($"light: 10 @ {r[10]:0.0} d, 15 @ {r[15]:0.0} d, 20 @ {r[20]:0.0} d");
            Assert.That(r[12], Is.GreaterThan(0), "a player with ~20 minutes a day reaches at least 12 factories");
        }

        [Test]
        public void Hardcore_Player_Cannot_Rush_The_Season()
        {
            var r = BalanceTests.PlaySeason(sessionsPerDay: 12, sessionMinutes: 45);
            TestContext.WriteLine($"hardcore: 10 @ {r[10]:0.0} d, 15 @ {r[15]:0.0} d, 20 @ {r[20]:0.0} d");
            Assert.That(r[20] == 0 || r[20] > 14, Is.True, "even 9 hours a day takes more than two weeks");
        }
    }
}
