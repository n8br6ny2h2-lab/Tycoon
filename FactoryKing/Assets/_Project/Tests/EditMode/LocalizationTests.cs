using System.IO;
using FactoryKing.Core.Localization;
using NUnit.Framework;

namespace FactoryKing.Tests
{
    public class LocalizationTests
    {
        private static LocalizationTable Load() =>
            LocalizationTable.ParseTsv(File.ReadAllText(TestPaths.ProjectFile("Assets/_Project/Resources/Localization/strings.txt")));

        [Test]
        public void Switching_Language_Changes_Text_And_Fires_Event()
        {
            var t = Load();
            int fired = 0;
            t.LanguageChanged += () => fired++;
            Assert.That(t.Get("game.title"), Is.EqualTo("Factory King"));
            t.SetLanguage(Language.Turkish);
            Assert.That(t.Get("game.title"), Is.EqualTo("Fabrika Kralı"));
            Assert.That(fired, Is.EqualTo(1));
            t.SetLanguage(Language.Turkish);
            Assert.That(fired, Is.EqualTo(1), "no event when unchanged");
        }

        [Test]
        public void Turkish_Characters_Survive_Parsing()
        {
            var t = Load();
            t.SetLanguage(Language.Turkish);
            Assert.That(t.Get("factory.steel"), Is.EqualTo("Demir Çelik Fabrikası"));
            Assert.That(t.Get("machine.textile_loom"), Does.Contain("Tezgâhı"));
            Assert.That(t.Get("ui.net_per_hour"), Does.Contain("kâr"));
            foreach (char c in "ğüşıöçĞÜŞİÖÇ") Assert.That(c, Is.Not.EqualTo('?'));
        }

        [Test]
        public void Every_Key_Has_Both_Languages()
        {
            var t = Load();
            Assert.That(t.Count, Is.GreaterThan(50));
            foreach (var key in t.Keys)
            {
                Assert.That(t.HasTranslation(key, Language.English), Is.True, "EN " + key);
                Assert.That(t.HasTranslation(key, Language.Turkish), Is.True, "TR " + key);
            }
        }

        [Test]
        public void Missing_Keys_Fall_Back_To_Key_And_Format_Is_Safe()
        {
            var t = LocalizationTable.ParseTsv("a\tHello {0}\tMerhaba {0}\nb\tOnly English\t\n# comment\n");
            Assert.That(t.Get("nope"), Is.EqualTo("nope"));
            t.SetLanguage(Language.Turkish);
            Assert.That(t.Format("a", "Ali"), Is.EqualTo("Merhaba Ali"));
            Assert.That(t.Get("b"), Is.EqualTo("Only English"));
            var bad = LocalizationTable.ParseTsv("x\tBroken {0\tBozuk {0");
            Assert.That(bad.Format("x", 1), Is.EqualTo("Broken {0"));
        }

        [Test]
        public void Number_Style_Follows_Language()
        {
            var t = Load();
            Assert.That(t.Money(1500), Is.EqualTo("1.5K"));
            t.SetLanguage(Language.Turkish);
            Assert.That(t.Money(1500), Is.EqualTo("1,5 B"));
        }
    }
}
