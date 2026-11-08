using System.Text;

namespace FactoryKing.Core.Simulation
{
    /// <summary>
    /// Validation for the player's company name (shown in the HUD and menus; stored only on the device / iCloud).
    /// Letters (including Turkish), digits, space, dot, dash, ampersand and apostrophe; 3–20 characters.
    /// </summary>
    public static class CompanyNameRules
    {
        public const int MinLength = 3;
        public const int MaxLength = 20;

        public static string Sanitize(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return null;
            var sb = new StringBuilder(raw.Length);
            bool lastSpace = false;
            foreach (char c in raw.Trim())
            {
                if (sb.Length >= MaxLength) break;
                bool ok = char.IsLetterOrDigit(c) || c == '.' || c == '-' || c == '&' || c == '\'';
                if (c == ' ' && !lastSpace && sb.Length > 0) { sb.Append(' '); lastSpace = true; continue; }
                if (!ok) continue;
                sb.Append(c);
                lastSpace = false;
            }
            string s = sb.ToString().Trim();
            return s.Length >= MinLength ? s : null;
        }

        public static bool IsValid(string raw) => Sanitize(raw) != null;
    }
}
