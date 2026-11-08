using System;
using System.Collections.Generic;
using FactoryKing.Core.Numerics;

namespace FactoryKing.Core.Localization
{
    public enum Language
    {
        English = 0,
        Turkish = 1,
    }

    /// <summary>
    /// Key -> text table loaded from a UTF-8 TSV file: <c>key \t en \t tr</c>. Lines starting with '#' are comments.
    /// Supports {0}-style placeholders and "\n" escapes. Missing translations fall back to English, then to the key.
    /// </summary>
    public sealed class LocalizationTable
    {
        private readonly Dictionary<string, string[]> _entries = new Dictionary<string, string[]>(StringComparer.Ordinal);

        public Language Current { get; private set; } = Language.English;
        public event Action LanguageChanged;

        public int Count => _entries.Count;
        public IEnumerable<string> Keys => _entries.Keys;

        public NumberStyle NumberStyle => Current == Language.Turkish ? NumberStyle.Turkish : NumberStyle.English;

        public static LocalizationTable ParseTsv(string text)
        {
            var table = new LocalizationTable();
            if (string.IsNullOrEmpty(text)) return table;
            if (text[0] == '﻿') text = text.Substring(1);

            var lines = text.Split('\n');
            for (int n = 0; n < lines.Length; n++)
            {
                string line = lines[n].TrimEnd('\r');
                if (line.Length == 0 || line[0] == '#') continue;
                var cols = line.Split('\t');
                if (cols.Length < 2) continue;
                string key = cols[0].Trim();
                if (key.Length == 0) continue;
                var values = new string[2];
                values[0] = Unescape(cols[1]);
                values[1] = cols.Length > 2 ? Unescape(cols[2]) : null;
                table._entries[key] = values;
            }
            return table;
        }

        public void SetLanguage(Language language)
        {
            if (Current == language) return;
            Current = language;
            LanguageChanged?.Invoke();
        }

        public bool Has(string key) => key != null && _entries.ContainsKey(key);

        public bool HasTranslation(string key, Language language)
        {
            if (key == null || !_entries.TryGetValue(key, out var v)) return false;
            string s = v[(int)language];
            return !string.IsNullOrEmpty(s);
        }

        public string Get(string key)
        {
            if (key == null) return string.Empty;
            if (!_entries.TryGetValue(key, out var values)) return key;
            string s = values[(int)Current];
            if (string.IsNullOrEmpty(s)) s = values[0];
            return string.IsNullOrEmpty(s) ? key : s;
        }

        public string Format(string key, params object[] args)
        {
            string pattern = Get(key);
            try { return string.Format(pattern, args); }
            catch (FormatException) { return pattern; }
        }

        public string Money(BigDouble value) => NumberFormatter.Format(value, NumberStyle);

        private static string Unescape(string s) => s.Replace("\\n", "\n").Replace("\\t", "\t").Trim();
    }
}
