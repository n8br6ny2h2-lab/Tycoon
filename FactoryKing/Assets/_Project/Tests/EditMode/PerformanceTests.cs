using System.Diagnostics;
using FactoryKing.Core.Content;
using FactoryKing.Core.Persistence;
using FactoryKing.Core.Simulation;
using NUnit.Framework;

namespace FactoryKing.Tests
{
    /// <summary>
    /// Worst-case economy cost: all 5 factories, all 15 lines, every machine at level 100 (fastest cycles, most
    /// sub-steps). The simulation must stay far below a 60 FPS frame budget (16.6 ms) on a desktop CPU; mobile CPUs
    /// are roughly 3-5x slower, so we require < 1.5 ms per 50 ms tick here.
    /// </summary>
    public class PerformanceTests
    {
        private static GameSession MaxedGame()
        {
            var s = new GameSession(DefaultContent.Create());
            foreach (var f in s.State.Factories)
            {
                f.Unlocked = true;
                foreach (var l in f.Lines)
                {
                    l.Unlocked = true;
                    foreach (var m in l.Machines) m.Level = 100;
                }
            }
            return s;
        }

        [Test]
        public void Simulation_Tick_Fits_Frame_Budget_At_Max_Load()
        {
            var s = MaxedGame();
            for (int i = 0; i < 100; i++) s.Tick(0.05); // warm-up / JIT
            var sw = Stopwatch.StartNew();
            const int ticks = 600; // 30 simulated seconds
            for (int i = 0; i < ticks; i++) s.Tick(0.05);
            sw.Stop();
            double msPerTick = sw.Elapsed.TotalMilliseconds / ticks;
            TestContext.WriteLine($"max-load tick: {msPerTick:0.000} ms");
            Assert.That(msPerTick, Is.LessThan(1.5));
            Assert.That(s.State.TotalUnitsSold, Is.GreaterThan(0));
        }

        [Test]
        public void Save_And_Load_Are_Fast()
        {
            var s = MaxedGame();
            for (int i = 0; i < 200; i++) s.Tick(0.05);
            var sw = Stopwatch.StartNew();
            string text = null;
            for (int i = 0; i < 20; i++) text = SaveService.Encode(s.State);
            for (int i = 0; i < 20; i++) Assert.That(SaveService.TryDecode(text, s.Content), Is.Not.Null);
            sw.Stop();
            TestContext.WriteLine($"save+load: {sw.Elapsed.TotalMilliseconds / 20:0.00} ms, {text.Length / 1024.0:0.0} KB");
            Assert.That(sw.Elapsed.TotalMilliseconds / 20, Is.LessThan(25));
            Assert.That(text.Length, Is.LessThan(200_000), "fits easily in iCloud key-value storage (1 MB)");
        }

        [Test]
        public void Offline_Calculation_Is_Instant()
        {
            var s = MaxedGame();
            var sw = Stopwatch.StartNew();
            for (int i = 0; i < 100; i++) s.Offline.Calculate(8 * 3600);
            sw.Stop();
            Assert.That(sw.Elapsed.TotalMilliseconds / 100, Is.LessThan(2));
        }
    }
}
