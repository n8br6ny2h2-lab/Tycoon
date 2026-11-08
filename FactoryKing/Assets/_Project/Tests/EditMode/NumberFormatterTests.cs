using FactoryKing.Core.Numerics;
using NUnit.Framework;

namespace FactoryKing.Tests
{
    public class NumberFormatterTests
    {
        [TestCase(0, "0")]
        [TestCase(7, "7")]
        [TestCase(7.456, "7.45")]
        [TestCase(42.99, "42.9")]
        [TestCase(999.9, "999")]
        [TestCase(1000, "1K")]
        [TestCase(1234, "1.23K")]
        [TestCase(1239, "1.23K")]
        [TestCase(12345, "12.3K")]
        [TestCase(123456, "123K")]
        [TestCase(1.5e6, "1.5M")]
        [TestCase(2e9, "2B")]
        [TestCase(3.33e12, "3.33T")]
        [TestCase(-1500, "-1.5K")]
        public void English(double value, string expected) =>
            Assert.That(NumberFormatter.Format(value), Is.EqualTo(expected));

        [Test]
        public void Turkish_Uses_Comma_And_Turkish_Suffixes()
        {
            Assert.That(NumberFormatter.Format(1234, NumberStyle.Turkish), Is.EqualTo("1,23 B"));
            Assert.That(NumberFormatter.Format(2.5e6, NumberStyle.Turkish), Is.EqualTo("2,5 Mn"));
            Assert.That(NumberFormatter.Format(7.25, NumberStyle.Turkish), Is.EqualTo("7,25"));
        }

        [Test]
        public void Alphabetic_Suffixes_After_Named_Ones()
        {
            Assert.That(NumberFormatter.Format(BigDouble.FromLog10(36)), Is.EqualTo("1aa"));
            Assert.That(NumberFormatter.Format(BigDouble.FromLog10(39)), Is.EqualTo("1ab"));
            Assert.That(NumberFormatter.Format(BigDouble.FromLog10(36 + 3 * 26)), Is.EqualTo("1ba"));
        }

        [Test]
        public void Duration()
        {
            Assert.That(NumberFormatter.FormatDuration(45), Is.EqualTo("45s"));
            Assert.That(NumberFormatter.FormatDuration(125), Is.EqualTo("2m 05s"));
            Assert.That(NumberFormatter.FormatDuration(8 * 3600 + 300), Is.EqualTo("8h 05m"));
        }
    }
}
