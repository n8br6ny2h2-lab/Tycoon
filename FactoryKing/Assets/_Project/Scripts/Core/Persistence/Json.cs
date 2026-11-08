using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace FactoryKing.Core.Persistence
{
    public enum JsonType { Null, Bool, Number, String, Array, Object }

    /// <summary>
    /// Minimal dependency-free JSON tree (Unity's JsonUtility cannot serialize dictionaries or our BigDouble, and the
    /// core must not depend on engine or third-party packages). Numbers are kept as double; strings are UTF-8 safe.
    /// </summary>
    public sealed class JsonValue
    {
        public JsonType Type { get; private set; }
        private bool _bool;
        private double _number;
        private string _string;
        private List<JsonValue> _array;
        private Dictionary<string, JsonValue> _object;
        private List<string> _order;

        public static JsonValue Null() => new JsonValue { Type = JsonType.Null };
        public static JsonValue From(bool b) => new JsonValue { Type = JsonType.Bool, _bool = b };
        public static JsonValue From(double d) => new JsonValue { Type = JsonType.Number, _number = double.IsNaN(d) || double.IsInfinity(d) ? 0 : d };
        public static JsonValue From(long l) => From((double)l);
        public static JsonValue From(string s) => s == null ? Null() : new JsonValue { Type = JsonType.String, _string = s };
        public static JsonValue NewArray() => new JsonValue { Type = JsonType.Array, _array = new List<JsonValue>() };
        public static JsonValue NewObject() => new JsonValue
        {
            Type = JsonType.Object, _object = new Dictionary<string, JsonValue>(StringComparer.Ordinal), _order = new List<string>(),
        };

        // ---------- object/array access ----------
        public JsonValue Set(string key, JsonValue value)
        {
            if (Type != JsonType.Object) throw new InvalidOperationException("Not an object");
            if (!_object.ContainsKey(key)) _order.Add(key);
            _object[key] = value ?? Null();
            return this;
        }
        public JsonValue Set(string key, string v) => Set(key, From(v));
        public JsonValue Set(string key, double v) => Set(key, From(v));
        public JsonValue Set(string key, long v) => Set(key, From(v));
        public JsonValue Set(string key, int v) => Set(key, From((double)v));
        public JsonValue Set(string key, bool v) => Set(key, From(v));

        public JsonValue Add(JsonValue v)
        {
            if (Type != JsonType.Array) throw new InvalidOperationException("Not an array");
            _array.Add(v ?? Null());
            return this;
        }

        public JsonValue Get(string key) => Type == JsonType.Object && _object.TryGetValue(key, out var v) ? v : null;
        public bool Has(string key) => Type == JsonType.Object && _object.ContainsKey(key);
        public IEnumerable<string> Keys => Type == JsonType.Object ? (IEnumerable<string>)_order : Array.Empty<string>();
        public int Count => Type == JsonType.Array ? _array.Count : Type == JsonType.Object ? _object.Count : 0;
        public JsonValue this[int i] => _array[i];
        public IEnumerable<JsonValue> Items => Type == JsonType.Array ? (IEnumerable<JsonValue>)_array : Array.Empty<JsonValue>();

        // ---------- typed reads with defaults (tolerant to missing/corrupt fields) ----------
        public string AsString(string def = null) => Type == JsonType.String ? _string : def;
        public double AsDouble(double def = 0) => Type == JsonType.Number ? _number : def;
        public long AsLong(long def = 0) => Type == JsonType.Number ? (long)Math.Round(_number) : def;
        public int AsInt(int def = 0) => Type == JsonType.Number ? (int)Math.Round(_number) : def;
        public bool AsBool(bool def = false) => Type == JsonType.Bool ? _bool : def;

        public string Str(string key, string def = null) => Get(key)?.AsString(def) ?? def;
        public double Dbl(string key, double def = 0) => Get(key)?.AsDouble(def) ?? def;
        public long Lng(string key, long def = 0) => Get(key)?.AsLong(def) ?? def;
        public int Int(string key, int def = 0) => Get(key)?.AsInt(def) ?? def;
        public bool Bool(string key, bool def = false) => Get(key)?.AsBool(def) ?? def;
        public JsonValue Arr(string key) { var v = Get(key); return v != null && v.Type == JsonType.Array ? v : NewArray(); }
        public JsonValue Obj(string key) { var v = Get(key); return v != null && v.Type == JsonType.Object ? v : NewObject(); }

        // ---------- writing ----------
        public override string ToString()
        {
            var sb = new StringBuilder(256);
            Write(sb);
            return sb.ToString();
        }

        private void Write(StringBuilder sb)
        {
            switch (Type)
            {
                case JsonType.Null: sb.Append("null"); break;
                case JsonType.Bool: sb.Append(_bool ? "true" : "false"); break;
                case JsonType.Number: sb.Append(_number.ToString("R", CultureInfo.InvariantCulture)); break;
                case JsonType.String: WriteString(sb, _string); break;
                case JsonType.Array:
                    sb.Append('[');
                    for (int i = 0; i < _array.Count; i++) { if (i > 0) sb.Append(','); _array[i].Write(sb); }
                    sb.Append(']');
                    break;
                case JsonType.Object:
                    sb.Append('{');
                    for (int i = 0; i < _order.Count; i++)
                    {
                        if (i > 0) sb.Append(',');
                        WriteString(sb, _order[i]);
                        sb.Append(':');
                        _object[_order[i]].Write(sb);
                    }
                    sb.Append('}');
                    break;
            }
        }

        private static void WriteString(StringBuilder sb, string s)
        {
            sb.Append('"');
            foreach (char c in s)
            {
                switch (c)
                {
                    case '"': sb.Append("\\\""); break;
                    case '\\': sb.Append("\\\\"); break;
                    case '\n': sb.Append("\\n"); break;
                    case '\r': sb.Append("\\r"); break;
                    case '\t': sb.Append("\\t"); break;
                    default:
                        if (c < 0x20) sb.Append("\\u").Append(((int)c).ToString("x4"));
                        else sb.Append(c);
                        break;
                }
            }
            sb.Append('"');
        }

        // ---------- parsing ----------
        public static JsonValue Parse(string text)
        {
            if (text == null) throw new FormatException("null json");
            int i = 0;
            var v = ParseValue(text, ref i, 0);
            SkipWs(text, ref i);
            if (i != text.Length) throw new FormatException("Trailing characters at " + i);
            return v;
        }

        public static bool TryParse(string text, out JsonValue value)
        {
            try { value = Parse(text); return true; }
            catch (Exception) { value = null; return false; }
        }

        private static void SkipWs(string s, ref int i)
        {
            while (i < s.Length && (s[i] == ' ' || s[i] == '\n' || s[i] == '\r' || s[i] == '\t')) i++;
        }

        private static JsonValue ParseValue(string s, ref int i, int depth)
        {
            if (depth > 64) throw new FormatException("Too deep");
            SkipWs(s, ref i);
            if (i >= s.Length) throw new FormatException("Unexpected end");
            char c = s[i];
            if (c == '{')
            {
                i++;
                var o = NewObject();
                SkipWs(s, ref i);
                if (i < s.Length && s[i] == '}') { i++; return o; }
                while (true)
                {
                    SkipWs(s, ref i);
                    if (i >= s.Length || s[i] != '"') throw new FormatException("Expected key at " + i);
                    string key = ParseString(s, ref i);
                    SkipWs(s, ref i);
                    if (i >= s.Length || s[i] != ':') throw new FormatException("Expected ':' at " + i);
                    i++;
                    o.Set(key, ParseValue(s, ref i, depth + 1));
                    SkipWs(s, ref i);
                    if (i < s.Length && s[i] == ',') { i++; continue; }
                    if (i < s.Length && s[i] == '}') { i++; return o; }
                    throw new FormatException("Expected ',' or '}' at " + i);
                }
            }
            if (c == '[')
            {
                i++;
                var a = NewArray();
                SkipWs(s, ref i);
                if (i < s.Length && s[i] == ']') { i++; return a; }
                while (true)
                {
                    a.Add(ParseValue(s, ref i, depth + 1));
                    SkipWs(s, ref i);
                    if (i < s.Length && s[i] == ',') { i++; continue; }
                    if (i < s.Length && s[i] == ']') { i++; return a; }
                    throw new FormatException("Expected ',' or ']' at " + i);
                }
            }
            if (c == '"') return From(ParseString(s, ref i));
            if (Match(s, ref i, "true")) return From(true);
            if (Match(s, ref i, "false")) return From(false);
            if (Match(s, ref i, "null")) return Null();
            int start = i;
            while (i < s.Length && "+-0123456789.eE".IndexOf(s[i]) >= 0) i++;
            if (start == i) throw new FormatException("Unexpected char '" + c + "' at " + i);
            if (!double.TryParse(s.Substring(start, i - start), NumberStyles.Float, CultureInfo.InvariantCulture, out double d))
                throw new FormatException("Bad number at " + start);
            return From(d);
        }

        private static bool Match(string s, ref int i, string word)
        {
            if (string.CompareOrdinal(s, i, word, 0, word.Length) != 0) return false;
            i += word.Length;
            return true;
        }

        private static string ParseString(string s, ref int i)
        {
            i++; // opening quote
            var sb = new StringBuilder();
            while (i < s.Length)
            {
                char c = s[i++];
                if (c == '"') return sb.ToString();
                if (c != '\\') { sb.Append(c); continue; }
                if (i >= s.Length) break;
                char e = s[i++];
                switch (e)
                {
                    case '"': sb.Append('"'); break;
                    case '\\': sb.Append('\\'); break;
                    case '/': sb.Append('/'); break;
                    case 'b': sb.Append('\b'); break;
                    case 'f': sb.Append('\f'); break;
                    case 'n': sb.Append('\n'); break;
                    case 'r': sb.Append('\r'); break;
                    case 't': sb.Append('\t'); break;
                    case 'u':
                        if (i + 4 > s.Length) throw new FormatException("Bad \\u escape");
                        sb.Append((char)int.Parse(s.Substring(i, 4), NumberStyles.HexNumber, CultureInfo.InvariantCulture));
                        i += 4;
                        break;
                    default: throw new FormatException("Bad escape");
                }
            }
            throw new FormatException("Unterminated string");
        }
    }
}
