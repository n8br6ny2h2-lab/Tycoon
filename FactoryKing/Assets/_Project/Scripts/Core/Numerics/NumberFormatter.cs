using System;
using System.Globalization;
using System.Text;

namespace FactoryKing.Core.Numerics
{
    /// <summary>Locale-specific number presentation (suffixes and decimal separator).</summary>
    public sealed class NumberStyle
    {
        public readonly string[] Suffixes;
        public readonly string DecimalSeparator;
        public readonly string SuffixSeparator;

        public NumberStyle(string[] suffixes, string decimalSeparator, string suffixSeparator)
        {
            Suffixes = suffixes ?? throw new ArgumentNullException(nameof(suffixes));
            DecimalSeparator = decimalSeparator;
            SuffixSeparator = suffixSeparator;
        }

        public static readonly NumberStyle English = new NumberStyle(
            new[] { "", "K", "M", "B", "T", "Qa", "Qi", "Sx", "Sp", "Oc", "No", "Dc" }, ".", "");

        /// <summary>Turkish: Bin, Milyon, Milyar, Trilyon, Katrilyon, Kentilyon ... with comma decimals.</summary>
        public static readonly NumberStyle Turkish = new NumberStyle(
            new[] { "", "B", "Mn", "Mr", "Tn", "Ktr", "Ktl", "Sks", "Spt", "Okt", "Non", "Des" }, ",", " ");
    }

    public static class NumberFormatter
    {
        /// <summary>
        /// Formats with three significant digits and a magnitude suffix: 999, 1.23K, 45.6M, 789B, then aa, ab ...
        /// Values are truncated (never rounded up) so a displayed balance never looks larger than it is.
        /// </summary>
        public static string Format(BigDouble value, NumberStyle style = null)
        {
            style ??= NumberStyle.English;
            if (value.IsZero) return "0";
            string sign = value.IsNegative ? "-" : "";
            BigDouble abs = value.Abs();

            if (abs.Exponent < 3)
            {
                double d = abs.ToDouble();
                string small = d >= 100 || Math.Abs(d - Math.Floor(d)) < 1e-9
                    ? Math.Floor(d).ToString("0", CultureInfo.InvariantCulture)
                    : TruncateTo(d, d >= 10 ? 1 : 2).ToString(d >= 10 ? "0.#" : "0.##", CultureInfo.InvariantCulture);
                return sign + small.Replace(".", style.DecimalSeparator);
            }

            long tier = abs.Exponent / 3;
            int digitsBeforePoint = (int)(abs.Exponent % 3) + 1;
            double scaled = abs.Mantissa * Math.Pow(10, digitsBeforePoint - 1);
            int decimals = 3 - digitsBeforePoint;
            double truncated = TruncateTo(scaled, decimals);
            string format = decimals == 0 ? "0" : "0." + new string('#', decimals);
            string number = truncated.ToString(format, CultureInfo.InvariantCulture).Replace(".", style.DecimalSeparator);
            return sign + number + style.SuffixSeparator + Suffix(tier, style);
        }

        public static string Format(double value, NumberStyle style = null) => Format(BigDouble.FromDouble(value), style);

        /// <summary>Formats seconds as compact duration, e.g. "2h 05m", "45s".</summary>
        public static string FormatDuration(double seconds)
        {
            if (seconds < 0 || double.IsNaN(seconds)) seconds = 0;
            long s = (long)Math.Floor(seconds);
            long h = s / 3600, m = (s % 3600) / 60, sec = s % 60;
            if (h > 0) return $"{h}h {m:00}m";
            if (m > 0) return $"{m}m {sec:00}s";
            return $"{sec}s";
        }

        private static string Suffix(long tier, NumberStyle style)
        {
            if (tier < style.Suffixes.Length) return style.Suffixes[tier];
            // Alphabetic suffixes after the named ones: aa, ab, ... az, ba, ... zz, aaa ...
            long index = tier - style.Suffixes.Length;
            var sb = new StringBuilder();
            int letters = 2;
            long block = 26 * 26;
            while (index >= block && letters < 6) { index -= block; letters++; block *= 26; }
            for (int i = 0; i < letters; i++)
            {
                sb.Insert(0, (char)('a' + index % 26));
                index /= 26;
            }
            return sb.ToString();
        }

        private static double TruncateTo(double value, int decimals)
        {
            double factor = Math.Pow(10, decimals);
            // Small epsilon protects against 1.23 being stored as 1.2299999.
            return Math.Floor(value * factor + 1e-7) / factor;
        }
    }
}
