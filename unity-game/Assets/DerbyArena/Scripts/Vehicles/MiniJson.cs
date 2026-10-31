using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace DerbyArena.Vehicles
{
    /// <summary>
    /// Tiny JSON reader used for glTF. Objects become Dictionary&lt;string, object&gt;, arrays
    /// List&lt;object&gt;, numbers double, plus string / bool / null. No dependencies, no reflection.
    /// </summary>
    public static class MiniJson
    {
        public static object Parse(string json)
        {
            var p = new Reader(json);
            return p.ReadValue();
        }

        public static Dictionary<string, object> Obj(object o)
        {
            return o as Dictionary<string, object>;
        }

        public static List<object> Arr(object o)
        {
            return o as List<object>;
        }

        public static Dictionary<string, object> Obj(Dictionary<string, object> d, string key)
        {
            object v;
            return d != null && d.TryGetValue(key, out v) ? v as Dictionary<string, object> : null;
        }

        public static List<object> Arr(Dictionary<string, object> d, string key)
        {
            object v;
            return d != null && d.TryGetValue(key, out v) ? v as List<object> : null;
        }

        public static double Num(Dictionary<string, object> d, string key, double fallback)
        {
            object v;
            if (d != null && d.TryGetValue(key, out v) && v is double) return (double)v;
            return fallback;
        }

        public static string Str(Dictionary<string, object> d, string key)
        {
            object v;
            return d != null && d.TryGetValue(key, out v) ? v as string : null;
        }

        public static float[] Floats(Dictionary<string, object> d, string key)
        {
            var list = Arr(d, key);
            if (list == null) return null;
            var result = new float[list.Count];
            for (int i = 0; i < list.Count; i++)
                result[i] = list[i] is double ? (float)(double)list[i] : 0f;
            return result;
        }

        sealed class Reader
        {
            readonly string s;
            int i;

            public Reader(string text) { s = text; }

            void Skip()
            {
                while (i < s.Length && char.IsWhiteSpace(s[i])) i++;
            }

            public object ReadValue()
            {
                Skip();
                if (i >= s.Length) throw new FormatException("Unexpected end of JSON");
                char c = s[i];
                if (c == '{') return ReadObject();
                if (c == '[') return ReadArray();
                if (c == '"') return ReadString();
                if (c == 't') { i += 4; return true; }
                if (c == 'f') { i += 5; return false; }
                if (c == 'n') { i += 4; return null; }
                return ReadNumber();
            }

            Dictionary<string, object> ReadObject()
            {
                var d = new Dictionary<string, object>();
                i++; // {
                Skip();
                if (s[i] == '}') { i++; return d; }
                while (true)
                {
                    Skip();
                    string key = ReadString();
                    Skip();
                    i++; // :
                    d[key] = ReadValue();
                    Skip();
                    if (s[i] == ',') { i++; continue; }
                    i++; // }
                    return d;
                }
            }

            List<object> ReadArray()
            {
                var l = new List<object>();
                i++; // [
                Skip();
                if (s[i] == ']') { i++; return l; }
                while (true)
                {
                    l.Add(ReadValue());
                    Skip();
                    if (s[i] == ',') { i++; continue; }
                    i++; // ]
                    return l;
                }
            }

            string ReadString()
            {
                i++; // opening quote
                var sb = new StringBuilder();
                while (s[i] != '"')
                {
                    char c = s[i++];
                    if (c != '\\') { sb.Append(c); continue; }
                    char e = s[i++];
                    switch (e)
                    {
                        case 'n': sb.Append('\n'); break;
                        case 't': sb.Append('\t'); break;
                        case 'r': sb.Append('\r'); break;
                        case 'b': sb.Append('\b'); break;
                        case 'f': sb.Append('\f'); break;
                        case 'u':
                            sb.Append((char)Convert.ToInt32(s.Substring(i, 4), 16));
                            i += 4;
                            break;
                        default: sb.Append(e); break;
                    }
                }
                i++; // closing quote
                return sb.ToString();
            }

            double ReadNumber()
            {
                int start = i;
                while (i < s.Length && "+-0123456789.eE".IndexOf(s[i]) >= 0) i++;
                return double.Parse(s.Substring(start, i - start), NumberStyles.Float, CultureInfo.InvariantCulture);
            }
        }
    }
}
