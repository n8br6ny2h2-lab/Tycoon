using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using FactoryKing.Core.Content;
using FactoryKing.Core.Economy;
using FactoryKing.Core.Localization;
using FactoryKing.Core.Meta;
using FactoryKing.Core.Simulation;
using NUnit.Framework;

namespace FactoryKing.Tests
{
    /// <summary>Every text the game can show must exist in English and Turkish.</summary>
    public class LocalizationCoverageTests
    {
        private static LocalizationTable Table() =>
            LocalizationTable.ParseTsv(File.ReadAllText(TestPaths.ProjectFile("Assets/_Project/Resources/Localization/strings.txt")));

        private static void AssertKey(LocalizationTable t, string key)
        {
            Assert.That(t.HasTranslation(key, Language.English), Is.True, "EN missing: " + key);
            Assert.That(t.HasTranslation(key, Language.Turkish), Is.True, "TR missing: " + key);
        }

        [Test]
        public void All_Literal_Ui_Keys_Exist()
        {
            var t = Table();
            string scripts = Path.GetDirectoryName(TestPaths.ProjectFile("Assets/_Project/Scripts/Runtime/FactoryKing.Runtime.asmdef"));
            var keyPattern = new Regex("\"((?:ui|screen|nav|hud|market|power|ability|season|cosmetics|onboard|pause|exp|cards|crate|eng|stats|coop|club|online|road|vip|piggy|offer|supply|fleet|event|badges|drone|tutorial|map|managers|missions|orders|warehouse|research|daily|achievements|collection|events|prestige|shop|ads|settings|menu|consent|offline|notif|gc|save|debug|achievement)\\.[a-z0-9_.]*[a-z0-9_])\"(?!\\s*\\+)");
            int found = 0;
            foreach (var file in Directory.GetFiles(scripts, "*.cs", SearchOption.AllDirectories))
            foreach (Match m in keyPattern.Matches(File.ReadAllText(file)))
            {
                found++;
                AssertKey(t, m.Groups[1].Value);
            }
            Assert.That(found, Is.GreaterThan(150), "scanner should find the UI keys");
        }

        [Test]
        public void All_Dynamic_Keys_Exist()
        {
            var t = Table();
            foreach (var m in ManagerService.Defs) { AssertKey(t, m.NameKey); AssertKey(t, m.TitleKey); AssertKey(t, m.AbilityKey); AssertKey(t, m.AbilityKey + ".desc"); AssertKey(t, "rarity." + m.Rarity); AssertKey(t, "stat." + m.Stat); }
            foreach (var r in ResearchService.Defs) { AssertKey(t, r.NameKey); AssertKey(t, r.NameKey + ".desc"); }
            foreach (var a in AchievementService.Defs) { AssertKey(t, a.NameKey); AssertKey(t, a.NameKey + ".desc"); }
            foreach (var p in StoreCatalog.CreateDefault().Products) { AssertKey(t, p.NameKey); AssertKey(t, p.DescriptionKey); }
            foreach (var f in typeof(MissionTypes).GetFields()) AssertKey(t, "mission." + f.GetValue(null));
            foreach (var s in Enum.GetNames(typeof(MachineStatus))) AssertKey(t, "status." + s);
            for (int i = 0; i < 3; i++) AssertKey(t, "tutorial.step" + i);
            var pv = new PrestigePreview();
            foreach (var k in pv.Keeps.Concat(pv.Resets)) AssertKey(t, k);
            foreach (var k in new[] { "prestige.bonus.multiplier", "prestige.bonus.cash", "prestige.bonus.line2", "prestige.bonus.food" }) AssertKey(t, k);
            foreach (var d in new[] { "decor_monument", "badge_prestige", "decor_weekly_flag", "decor_golden_gear", "decor_gold_trucks", "decor_gold_trim", "decor_gold_statue" })
                AssertKey(t, "decor." + d);
            foreach (var c in CosmeticService.Defs) AssertKey(t, c.NameKey);
            foreach (var slot in Enum.GetNames(typeof(CosmeticSlot))) AssertKey(t, "cosmetics.slot." + slot);
            for (int i = 1; i <= 6; i++) { AssertKey(t, "help.t" + i); AssertKey(t, "help.b" + i); }
            foreach (var c in CrateService.Defs) AssertKey(t, c.NameKey);
            foreach (var e in EngineeringService.Defs) { AssertKey(t, e.NameKey); AssertKey(t, e.NameKey + ".desc"); }
            for (int i = 0; i < 3; i++) { AssertKey(t, "exp.kind" + i); AssertKey(t, "settings.text" + i); }
            for (int i = 1; i <= FleetService.MaxLevel; i++) AssertKey(t, "fleet.tier" + i);
            foreach (var f in DefaultContent.Create().EventFactories) { AssertKey(t, f.NameKey); AssertKey(t, "decor.badge_event_" + f.Id.Substring(4)); }
            AssertKey(t, "event.weekend_rush");
            AssertKey(t, "event.weekend_rush.desc");
        }
    }
}
