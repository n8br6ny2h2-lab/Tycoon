using System.IO;
using System.Linq;
using FactoryKing.Core.Content;
using FactoryKing.Core.Economy;
using FactoryKing.Core.Localization;
using NUnit.Framework;

namespace FactoryKing.Tests
{
    public class ContentTests
    {
        private GameContent _content;
        private EconomyTuning _tuning;

        [SetUp]
        public void SetUp()
        {
            _content = DefaultContent.Create();
            _tuning = EconomyTuning.CreateDefault();
        }

        [Test]
        public void Default_Content_Is_Valid_And_Profitable_At_Every_Level()
        {
            var errors = ContentValidator.Validate(_content, _tuning);
            Assert.That(errors, Is.Empty, string.Join("\n", errors));
        }

        [Test]
        public void Has_Five_Factories_And_At_Least_25_Machines_In_All_Categories()
        {
            Assert.That(_content.Factories.Count, Is.EqualTo(DefaultContent.FactoryCount));
            Assert.That(_content.Machines.Count, Is.GreaterThanOrEqualTo(25));
            foreach (MachineCategory cat in System.Enum.GetValues(typeof(MachineCategory)))
                Assert.That(_content.Machines.Any(m => m.Category == cat), Is.True, $"No machine in category {cat}");
        }

        [Test]
        public void Every_Line_Has_All_Six_Stages_In_Order()
        {
            foreach (var f in _content.Factories)
            {
                var stages = f.MachineChain.Select(id => _content.GetMachine(id).Stage).ToArray();
                Assert.That(stages, Is.EqualTo(new[]
                {
                    StageType.RawInput, StageType.Processing, StageType.Production,
                    StageType.Packaging, StageType.Storage, StageType.Shipping,
                }), f.Id);
            }
        }

        [Test]
        public void Later_Factories_Cost_More_And_Earn_More()
        {
            var ordered = _content.Factories.OrderBy(f => f.Order).ToList();
            for (int i = 1; i < ordered.Count; i++)
            {
                Assert.That(ordered[i].UnlockCost, Is.GreaterThan(ordered[i - 1].UnlockCost));
                var prev = LineAnalyzer.Analyze(_content, _tuning, null, ordered[i - 1], Enumerable.Repeat(1, 6).ToArray());
                var cur = LineAnalyzer.Analyze(_content, _tuning, null, ordered[i], Enumerable.Repeat(1, 6).ToArray());
                Assert.That(cur.NetPerSecond, Is.GreaterThan(prev.NetPerSecond * 5), ordered[i].Id);
            }
        }

        [Test]
        public void Factory_Payback_Time_Is_Reasonable()
        {
            // A new factory's first line at level 1 should repay its purchase price in 2 min - 7 days at level 1:
            // late factories are week-long goals of a month-long season (prestige and momentum shorten them).
            foreach (var f in _content.Factories.Where(f => f.UnlockCost > 0 && f.Order < 5)) // later ones: see BalanceTests (season pacing)
            {
                var r = LineAnalyzer.Analyze(_content, _tuning, null, f, Enumerable.Repeat(1, 6).ToArray());
                double minutes = f.UnlockCost / r.NetPerSecond / 60;
                Assert.That(minutes, Is.InRange(2, 1440), $"{f.Id} payback {minutes:0.0} min");
            }
        }

        [Test]
        public void Every_Content_Key_Is_Translated_In_Both_Languages()
        {
            var table = LocalizationTable.ParseTsv(File.ReadAllText(TestPaths.ProjectFile("Assets/_Project/Resources/Localization/strings.txt")));
            var keys = _content.Factories.Select(f => f.NameKey)
                .Concat(_content.Machines.Select(m => m.NameKey))
                .Concat(_content.Products.Select(p => p.NameKey))
                .Concat(System.Enum.GetNames(typeof(StageType)).Select(s => "stage." + s));
            foreach (var key in keys)
            {
                Assert.That(table.HasTranslation(key, Language.English), Is.True, "EN missing: " + key);
                Assert.That(table.HasTranslation(key, Language.Turkish), Is.True, "TR missing: " + key);
            }
        }
    }
}
