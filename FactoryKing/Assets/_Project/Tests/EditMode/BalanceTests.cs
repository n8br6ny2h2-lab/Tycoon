using System;
using FactoryKing.Core.Content;
using FactoryKing.Core.Meta;
using FactoryKing.Core.Simulation;
using FactoryKing.Core.TimeKeeping;
using NUnit.Framework;

namespace FactoryKing.Tests
{
    /// <summary>
    /// Progression guard rails using an "ideal player" bot that spends greedily and claims every reward every 5 s.
    /// Real players are slower, so these bounds are the fastest acceptable pace; they catch runaway economies.
    /// </summary>
    public class BalanceTests
    {
        private sealed class Milestones
        {
            public double Food = -1, Plastic = -1, AllFive = -1, FirstPrestige = -1;
        }

        private static Milestones RunBot(double hours)
        {
            var clock = new ManualClock();
            var s = new GameSession(DefaultContent.Create(), clock: clock);
            var ms = new Milestones();
            double t = 0;
            while (t < hours * 3600)
            {
                for (int i = 0; i < 10; i++) { s.Tick(0.5); t += 0.5; }
                clock.Advance(TimeSpan.FromSeconds(5));
                foreach (var m in s.State.Missions.Daily) s.Missions.TryClaim(m);
                foreach (var m in s.State.Missions.Weekly) s.Missions.TryClaim(m);
                foreach (var a in AchievementService.Defs) s.Achievements.TryClaim(a.Id);
                s.DailyRewards.TryClaim();
                foreach (var o in s.State.Orders.ToArray()) s.Orders.TryClaim(o);
                foreach (var d in ManagerService.Defs) s.Managers.TryHire(d.Id);
                var owned = s.State.Factories.FindAll(f => f.Unlocked);
                for (int i = 0; i < s.State.Managers.Count && i < owned.Count; i++)
                    if (s.State.Managers[i].AssignedFactoryId == null) s.Managers.Assign(s.State.Managers[i].Id, owned[owned.Count - 1 - i].FactoryId);
                var pv = s.Prestige.Preview();
                if (pv.Available && pv.CrownsGained >= 3 && s.Prestige.TryPrestige() && ms.FirstPrestige < 0) ms.FirstPrestige = t / 60;
                bool bought = true;
                while (bought)
                {
                    bought = false;
                    foreach (var f in s.Content.Factories) bought |= s.Factories.TryUnlockFactory(f.Id);
                    foreach (var fs in s.State.Factories)
                    {
                        if (!fs.Unlocked) continue;
                        for (int l = 0; l < fs.Lines.Count; l++)
                        {
                            if (!fs.Lines[l].Unlocked) { bought |= s.Factories.TryUnlockLine(fs.FactoryId, l); continue; }
                            bought |= s.Upgrades.TryUpgrade(new MachineRef(fs.FactoryId, l, s.AnalyzeLine(fs.FactoryId, l).BottleneckIndex));
                        }
                    }
                    foreach (var r in ResearchService.Defs) bought |= s.Research.TryBuy(r.Id);
                }
                if (ms.Food < 0 && s.State.GetFactory("food").Unlocked) ms.Food = t / 60;
                if (ms.Plastic < 0 && s.State.GetFactory("plastic").Unlocked) ms.Plastic = t / 60;
                if (ms.AllFive < 0 && s.State.Factories.TrueForAll(f => f.Unlocked)) ms.AllFive = t / 60;
            }
            return ms;
        }

        [Test]
        public void Ideal_Player_Pace_Stays_Within_Design_Bounds()
        {
            var m = RunBot(4);
            TestContext.WriteLine($"food {m.Food:0} min, plastic {m.Plastic:0} min, prestige {m.FirstPrestige:0} min, all five {m.AllFive:0} min");
            Assert.That(m.Food, Is.InRange(3, 45), "second factory");
            Assert.That(m.Plastic, Is.InRange(20, 150), "third factory");
            Assert.That(m.FirstPrestige, Is.GreaterThan(45).Or.EqualTo(-1), "first prestige should not come too early");
            Assert.That(m.AllFive < 0 || m.AllFive > 120, Is.True, "all five factories must take more than 2 hours");
        }
    }
}
