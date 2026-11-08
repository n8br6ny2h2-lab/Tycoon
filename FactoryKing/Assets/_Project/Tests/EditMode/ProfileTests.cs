using FactoryKing.Core.Content;
using FactoryKing.Core.Persistence;
using FactoryKing.Core.Simulation;
using NUnit.Framework;

namespace FactoryKing.Tests
{
    public class ProfileTests
    {
        [TestCase("Demir Sanayi A.Ş.", "Demir Sanayi A.Ş.")]
        [TestCase("  Çağ   Holding  ", "Çağ Holding")]
        [TestCase("ab", null)]
        [TestCase("<script>!!", "script")]
        [TestCase("Bu çok çok uzun bir şirket adıdır", "Bu çok çok uzun bir")]
        [TestCase("", null)]
        [TestCase(null, null)]
        public void Company_Name_Is_Sanitised(string input, string expected) =>
            Assert.That(CompanyNameRules.Sanitize(input), Is.EqualTo(expected));

        [Test]
        public void Profile_And_Phone_Settings_Survive_Save()
        {
            var content = DefaultContent.Create();
            var s = new GameSession(content);
            s.State.CompanyName = "Kral Sanayi";
            s.State.Settings.Haptics = false;
            s.State.Settings.BatterySaver = true;
            s.State.Settings.OnboardingDone = true;
            var loaded = SaveService.TryDecode(SaveService.Encode(s.State), content);
            Assert.That(loaded.CompanyName, Is.EqualTo("Kral Sanayi"));
            Assert.That(loaded.Settings.Haptics, Is.False);
            Assert.That(loaded.Settings.BatterySaver, Is.True);
            Assert.That(loaded.Settings.OnboardingDone, Is.True);
        }

        [Test]
        public void New_Game_Needs_Onboarding()
        {
            var s = new GameSession(DefaultContent.Create());
            Assert.That(s.State.Settings.OnboardingDone, Is.False);
            Assert.That(s.State.CompanyName, Is.Null);
        }
    }
}
