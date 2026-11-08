using System;
using FactoryKing.Core.Numerics;
using NUnit.Framework;

namespace FactoryKing.Tests
{
    public class BigDoubleTests
    {
        [Test]
        public void Normalizes_Mantissa_Into_One_To_Ten()
        {
            var v = BigDouble.FromDouble(12345);
            Assert.That(v.Mantissa, Is.EqualTo(1.2345).Within(1e-12));
            Assert.That(v.Exponent, Is.EqualTo(4));
            Assert.That(BigDouble.FromDouble(0.004).Exponent, Is.EqualTo(-3));
        }

        [Test]
        public void Arithmetic_Matches_Double_In_Range()
        {
            var rnd = new Random(1234);
            for (int i = 0; i < 2000; i++)
            {
                double a = (rnd.NextDouble() - 0.3) * Math.Pow(10, rnd.Next(-5, 12));
                double b = (rnd.NextDouble() - 0.3) * Math.Pow(10, rnd.Next(-5, 12));
                Close(a + b, BigDouble.FromDouble(a) + b);
                Close(a - b, BigDouble.FromDouble(a) - b);
                Close(a * b, BigDouble.FromDouble(a) * b);
                if (b != 0) Close(a / b, BigDouble.FromDouble(a) / b);
                Assert.That(BigDouble.FromDouble(a).CompareTo(b), Is.EqualTo(a.CompareTo(b)), $"{a} vs {b}");
            }
        }

        private static void Close(double expected, BigDouble actual)
        {
            double tol = Math.Max(1e-9, Math.Abs(expected) * 1e-9);
            Assert.That(actual.ToDouble(), Is.EqualTo(expected).Within(tol));
        }

        [Test]
        public void Handles_Numbers_Far_Beyond_Double()
        {
            var huge = BigDouble.FromLog10(500);
            var product = huge * huge;
            Assert.That(product.Exponent, Is.EqualTo(1000));
            Assert.That((product / huge).ApproximatelyEquals(huge), Is.True);
            Assert.That(huge.ToDouble(), Is.EqualTo(double.MaxValue), "ToDouble clamps instead of returning Infinity");
            Assert.That(huge > BigDouble.FromDouble(double.MaxValue), Is.True);
        }

        [Test]
        public void Adding_Negligible_Value_Keeps_Larger()
        {
            var big = BigDouble.FromLog10(40);
            Assert.That(big + 1, Is.EqualTo(big));
            Assert.That(1 + big, Is.EqualTo(big));
        }

        [Test]
        public void Never_Produces_NaN_Or_Infinity()
        {
            Assert.That(BigDouble.FromDouble(double.NaN).IsZero, Is.True);
            Assert.That(BigDouble.FromDouble(double.PositiveInfinity), Is.EqualTo(BigDouble.MaxValue));
            var max = BigDouble.MaxValue * BigDouble.MaxValue;
            Assert.That(max, Is.EqualTo(BigDouble.MaxValue));
            Assert.Throws<DivideByZeroException>(() => { var _ = BigDouble.One / BigDouble.Zero; });
        }

        [Test]
        public void Negative_Comparisons()
        {
            Assert.That(BigDouble.FromDouble(-5) < BigDouble.FromDouble(-2), Is.True);
            Assert.That(BigDouble.FromDouble(-500) < BigDouble.FromDouble(-2), Is.True);
            Assert.That(BigDouble.FromDouble(-1) < BigDouble.Zero, Is.True);
            Assert.That(BigDouble.Zero < BigDouble.FromDouble(1e-20), Is.True);
        }

        [Test]
        public void Pow_And_Floor()
        {
            Assert.That(BigDouble.Pow(BigDouble.FromDouble(2), 10).ToDouble(), Is.EqualTo(1024).Within(1e-9));
            Assert.That(BigDouble.FromDouble(12.9).Floor().ToDouble(), Is.EqualTo(12));
            var huge = BigDouble.FromLog10(30.5);
            Assert.That(huge.Floor(), Is.EqualTo(huge));
        }

        [TestCase("0")]
        [TestCase("1.5e3")]
        [TestCase("-7.25e-4")]
        [TestCase("3.14159e123456")]
        public void String_RoundTrip(string text)
        {
            var v = BigDouble.Parse(text);
            Assert.That(BigDouble.Parse(v.ToInvariantString()), Is.EqualTo(v));
        }

        [Test]
        public void TryParse_Rejects_Garbage()
        {
            Assert.That(BigDouble.TryParse("abc", out _), Is.False);
            Assert.That(BigDouble.TryParse("1e", out _), Is.False);
            Assert.That(BigDouble.TryParse("", out _), Is.False);
            Assert.That(BigDouble.TryParse("NaN", out _), Is.False);
        }
    }
}
