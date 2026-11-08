using System;
using System.Globalization;

namespace FactoryKing.Core.Numerics
{
    /// <summary>
    /// Large-number value type for idle-game currency: mantissa in [1,10) (or 0) times 10^exponent.
    /// Never produces NaN or Infinity: invalid inputs collapse to zero and overflows clamp to <see cref="MaxValue"/>.
    /// </summary>
    [Serializable]
    public readonly struct BigDouble : IComparable<BigDouble>, IEquatable<BigDouble>
    {
        /// <summary>Above this many decimal orders of difference, the smaller operand is negligible in add/sub.</summary>
        private const int PrecisionOrders = 17;
        private const long MaxExponent = long.MaxValue / 4;

        public readonly double Mantissa;
        public readonly long Exponent;

        public static readonly BigDouble Zero = new BigDouble(0d, 0L, true);
        public static readonly BigDouble One = new BigDouble(1d, 0L, true);
        public static readonly BigDouble MaxValue = new BigDouble(9.999999999999999d, MaxExponent, true);

        private static readonly double[] Pow10Cache = BuildPow10Cache();

        private BigDouble(double mantissa, long exponent, bool alreadyNormalized)
        {
            Mantissa = mantissa;
            Exponent = exponent;
        }

        public BigDouble(double mantissa, long exponent)
        {
            this = Normalize(mantissa, exponent);
        }

        public bool IsZero => Mantissa == 0d;
        public bool IsNegative => Mantissa < 0d;
        public bool IsPositive => Mantissa > 0d;
        public int Sign => Math.Sign(Mantissa);

        public static BigDouble FromDouble(double value) => Normalize(value, 0);

        /// <summary>Builds 10^log10Value. Useful for geometric cost curves that would overflow a double.</summary>
        public static BigDouble FromLog10(double log10Value)
        {
            if (double.IsNaN(log10Value) || double.IsNegativeInfinity(log10Value)) return Zero;
            if (double.IsPositiveInfinity(log10Value) || log10Value > MaxExponent) return MaxValue;
            double e = Math.Floor(log10Value);
            return Normalize(Math.Pow(10d, log10Value - e), (long)e);
        }

        public double ToDouble()
        {
            if (IsZero) return 0d;
            if (Exponent > 308) return Mantissa > 0 ? double.MaxValue : -double.MaxValue;
            if (Exponent < -323) return 0d;
            double result = Mantissa * Pow10(Exponent);
            if (double.IsInfinity(result)) return Mantissa > 0 ? double.MaxValue : -double.MaxValue;
            return result;
        }

        /// <summary>log10(|value|). Returns negative infinity for zero.</summary>
        public double Log10() => IsZero ? double.NegativeInfinity : Math.Log10(Math.Abs(Mantissa)) + Exponent;

        public BigDouble Abs() => Mantissa < 0 ? new BigDouble(-Mantissa, Exponent, true) : this;

        public BigDouble Floor()
        {
            if (Exponent >= PrecisionOrders || IsZero) return this;
            return FromDouble(Math.Floor(ToDouble()));
        }

        /// <summary>Raises a non-negative value to a real power.</summary>
        public static BigDouble Pow(BigDouble value, double power)
        {
            if (value.IsZero) return power == 0 ? One : Zero;
            if (value.IsNegative) throw new ArgumentOutOfRangeException(nameof(value), "Pow supports non-negative bases only.");
            return FromLog10(value.Log10() * power);
        }

        public static BigDouble Max(BigDouble a, BigDouble b) => a >= b ? a : b;
        public static BigDouble Min(BigDouble a, BigDouble b) => a <= b ? a : b;

        public bool ApproximatelyEquals(BigDouble other, double relativeTolerance = 1e-9)
        {
            if (IsZero || other.IsZero) return Math.Abs((this - other).ToDouble()) <= relativeTolerance;
            BigDouble diff = (this - other).Abs();
            BigDouble scale = Max(Abs(), other.Abs());
            return (diff / scale).ToDouble() <= relativeTolerance;
        }

        // ---------- Arithmetic ----------

        public static BigDouble operator +(BigDouble a, BigDouble b)
        {
            if (a.IsZero) return b;
            if (b.IsZero) return a;
            long diff = a.Exponent - b.Exponent;
            if (diff > PrecisionOrders) return a;
            if (diff < -PrecisionOrders) return b;
            if (diff >= 0) return Normalize(a.Mantissa + b.Mantissa * Pow10(-diff), a.Exponent);
            return Normalize(b.Mantissa + a.Mantissa * Pow10(diff), b.Exponent);
        }

        public static BigDouble operator -(BigDouble a) => new BigDouble(-a.Mantissa, a.Exponent, true);
        public static BigDouble operator -(BigDouble a, BigDouble b) => a + (-b);

        public static BigDouble operator *(BigDouble a, BigDouble b)
        {
            if (a.IsZero || b.IsZero) return Zero;
            return Normalize(a.Mantissa * b.Mantissa, SafeAdd(a.Exponent, b.Exponent));
        }

        public static BigDouble operator /(BigDouble a, BigDouble b)
        {
            if (b.IsZero) throw new DivideByZeroException("BigDouble division by zero.");
            if (a.IsZero) return Zero;
            return Normalize(a.Mantissa / b.Mantissa, SafeAdd(a.Exponent, -b.Exponent));
        }

        public static BigDouble operator *(BigDouble a, double b) => a * FromDouble(b);
        public static BigDouble operator *(double a, BigDouble b) => FromDouble(a) * b;
        public static BigDouble operator /(BigDouble a, double b) => a / FromDouble(b);

        public static implicit operator BigDouble(double value) => FromDouble(value);
        public static implicit operator BigDouble(long value) => FromDouble(value);
        public static implicit operator BigDouble(int value) => FromDouble(value);

        // ---------- Comparison ----------

        public int CompareTo(BigDouble other)
        {
            int sa = Sign, sb = other.Sign;
            if (sa != sb) return sa.CompareTo(sb);
            if (sa == 0) return 0;
            int magnitude = Exponent != other.Exponent
                ? Exponent.CompareTo(other.Exponent)
                : Math.Abs(Mantissa).CompareTo(Math.Abs(other.Mantissa));
            return sa > 0 ? magnitude : -magnitude;
        }

        public static bool operator <(BigDouble a, BigDouble b) => a.CompareTo(b) < 0;
        public static bool operator >(BigDouble a, BigDouble b) => a.CompareTo(b) > 0;
        public static bool operator <=(BigDouble a, BigDouble b) => a.CompareTo(b) <= 0;
        public static bool operator >=(BigDouble a, BigDouble b) => a.CompareTo(b) >= 0;
        public static bool operator ==(BigDouble a, BigDouble b) => a.Equals(b);
        public static bool operator !=(BigDouble a, BigDouble b) => !a.Equals(b);

        public bool Equals(BigDouble other) => Mantissa.Equals(other.Mantissa) && Exponent == other.Exponent;
        public override bool Equals(object obj) => obj is BigDouble other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(Mantissa, Exponent);

        // ---------- Serialization ----------

        /// <summary>Round-trippable invariant text, e.g. "1.2345e67".</summary>
        public string ToInvariantString() =>
            Mantissa.ToString("R", CultureInfo.InvariantCulture) + "e" + Exponent.ToString(CultureInfo.InvariantCulture);

        public static BigDouble Parse(string text)
        {
            if (TryParse(text, out BigDouble value)) return value;
            throw new FormatException($"Invalid BigDouble: '{text}'");
        }

        public static bool TryParse(string text, out BigDouble value)
        {
            value = Zero;
            if (string.IsNullOrWhiteSpace(text)) return false;
            text = text.Trim();
            int e = text.IndexOfAny(new[] { 'e', 'E' });
            if (e < 0)
            {
                if (!double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out double d) || double.IsNaN(d) || double.IsInfinity(d)) return false;
                value = FromDouble(d);
                return true;
            }
            if (!double.TryParse(text.Substring(0, e), NumberStyles.Float, CultureInfo.InvariantCulture, out double m) || double.IsNaN(m) || double.IsInfinity(m)) return false;
            if (!long.TryParse(text.Substring(e + 1), NumberStyles.Integer, CultureInfo.InvariantCulture, out long exp)) return false;
            value = Normalize(m, exp);
            return true;
        }

        public override string ToString() => ToInvariantString();

        // ---------- Internals ----------

        private static BigDouble Normalize(double mantissa, long exponent)
        {
            if (mantissa == 0d || double.IsNaN(mantissa)) return Zero;
            if (double.IsInfinity(mantissa)) return mantissa > 0 ? MaxValue : -MaxValue;

            double abs = Math.Abs(mantissa);
            int shift = (int)Math.Floor(Math.Log10(abs));
            mantissa /= Pow10(shift);
            exponent = SafeAdd(exponent, shift);

            // Correct floating-point drift at the boundaries.
            abs = Math.Abs(mantissa);
            if (abs >= 10d) { mantissa /= 10d; exponent = SafeAdd(exponent, 1); }
            else if (abs < 1d) { mantissa *= 10d; exponent = SafeAdd(exponent, -1); }

            if (exponent > MaxExponent) return mantissa > 0 ? MaxValue : -MaxValue;
            if (exponent < -MaxExponent) return Zero;
            return new BigDouble(mantissa, exponent, true);
        }

        private static long SafeAdd(long a, long b)
        {
            long r = a + b;
            if (((a ^ r) & (b ^ r)) < 0) return a > 0 ? long.MaxValue / 2 : long.MinValue / 2; // overflow -> clamp
            return r;
        }

        private static double Pow10(long exponent)
        {
            if (exponent >= -323 && exponent <= 308) return Pow10Cache[exponent + 323];
            return Math.Pow(10d, exponent);
        }

        private static double[] BuildPow10Cache()
        {
            var cache = new double[308 + 323 + 1];
            for (int i = 0; i < cache.Length; i++) cache[i] = Math.Pow(10d, i - 323);
            return cache;
        }
    }
}
