using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace NeuroPilotXR.Training
{
    /// <summary>
    /// 极简 JSON 解析（零第三方依赖）。
    /// 解析融合层下行信封：顶层值支持 string / number / bool / null / 嵌套对象，
    /// 数组按深度跳过。契约信封为 {type, ts, seq, payload:{...}}（主仓库 app/unity/README.md），
    /// payload 子对象经 Obj("payload") 单独取用，其内部字段为扁平标量。
    /// </summary>
    public sealed class FusionJson
    {
        private readonly Dictionary<string, string> _raw = new Dictionary<string, string>();
        private readonly Dictionary<string, FusionJson> _nested = new Dictionary<string, FusionJson>();

        /// <summary>取数值；缺失或不可解析返回 fallback（默认 NaN）。</summary>
        public double Num(string key, double fallback = double.NaN)
        {
            return _raw.TryGetValue(key, out string raw)
                   && double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out double value)
                ? value
                : fallback;
        }

        /// <summary>取字符串（自动处理 \" \\ \\n 等常规转义）；缺失返回 fallback。</summary>
        public string Str(string key, string fallback = null)
        {
            if (!_raw.TryGetValue(key, out string raw) || raw == null)
            {
                return fallback;
            }

            var sb = new StringBuilder(raw.Length);
            for (int i = 0; i < raw.Length; i++)
            {
                char c = raw[i];
                if (c == '\\' && i + 1 < raw.Length)
                {
                    char next = raw[++i];
                    switch (next)
                    {
                        case '"': sb.Append('"'); break;
                        case '\\': sb.Append('\\'); break;
                        case 'n': sb.Append('\n'); break;
                        case 'r': sb.Append('\r'); break;
                        case 't': sb.Append('\t'); break;
                        default: sb.Append(next); break;
                    }
                }
                else
                {
                    sb.Append(c);
                }
            }

            return sb.ToString();
        }

        public bool Has(string key) => _raw.ContainsKey(key);

        /// <summary>取嵌套子对象；缺失返回 null。</summary>
        public FusionJson Obj(string key)
        {
            return _nested.TryGetValue(key, out FusionJson value) ? value : null;
        }

        /// <summary>解析信封；type 缺失视为无效帧。</summary>
        public static bool TryParseEnvelope(string json, out string type, out FusionJson fields)
        {
            type = null;
            fields = TryParse(json);
            if (fields == null)
            {
                return false;
            }

            type = fields.Str("type");
            return type != null;
        }

        public static FusionJson TryParse(string json)
        {
            int i = 0;
            SkipWhitespace(json, ref i);
            if (i >= json.Length || json[i] != '{')
            {
                return null;
            }

            i++;
            var result = new FusionJson();
            while (true)
            {
                SkipWhitespace(json, ref i);
                if (i >= json.Length) return null;
                if (json[i] == '}') return result;
                if (json[i] != '"') return null;

                string key = ReadStringRaw(json, ref i);
                SkipWhitespace(json, ref i);
                if (i >= json.Length || json[i] != ':') return null;
                i++;
                SkipWhitespace(json, ref i);

                if (i < json.Length && json[i] == '"')
                {
                    result._raw[key] = ReadStringRaw(json, ref i);
                }
                else if (i < json.Length && json[i] == '{')
                {
                    int start = i;
                    if (!SkipBalanced(json, ref i)) return null;
                    result._nested[key] = TryParse(json.Substring(start, i - start));
                }
                else if (i < json.Length && json[i] == '[')
                {
                    if (!SkipBalanced(json, ref i)) return null;
                }
                else
                {
                    int start = i;
                    while (i < json.Length && json[i] != ',' && json[i] != '}') i++;
                    if (i >= json.Length) return null;
                    result._raw[key] = json.Substring(start, i - start).Trim();
                }

                SkipWhitespace(json, ref i);
                if (i < json.Length && json[i] == ',') { i++; continue; }
                if (i < json.Length && json[i] == '}') return result;
                return null;
            }
        }

        private static void SkipWhitespace(string json, ref int i)
        {
            while (i < json.Length && char.IsWhiteSpace(json[i])) i++;
        }

        /// <summary>从开头引号读字符串，保留转义序列原样（Str() 时再反转义）。</summary>
        private static string ReadStringRaw(string json, ref int i)
        {
            i++; // 跳过开头引号
            var sb = new StringBuilder();
            while (i < json.Length)
            {
                char c = json[i];
                if (c == '\\' && i + 1 < json.Length)
                {
                    sb.Append(c);
                    sb.Append(json[i + 1]);
                    i += 2;
                    continue;
                }

                if (c == '"')
                {
                    i++;
                    return sb.ToString();
                }

                sb.Append(c);
                i++;
            }

            return sb.ToString();
        }

        /// <summary>跳过完整的对象/数组（含其中字符串），i 停在结束符之后。</summary>
        private static bool SkipBalanced(string json, ref int i)
        {
            char open = json[i];
            char close = open == '{' ? '}' : ']';
            int depth = 0;
            while (i < json.Length)
            {
                char c = json[i];
                if (c == '"')
                {
                    i++;
                    while (i < json.Length)
                    {
                        if (json[i] == '\\') { i += 2; continue; }
                        if (json[i] == '"') break;
                        i++;
                    }

                    if (i >= json.Length) return false;
                }
                else if (c == open)
                {
                    depth++;
                }
                else if (c == close)
                {
                    depth--;
                    if (depth == 0)
                    {
                        i++;
                        return true;
                    }
                }

                i++;
            }

            return false;
        }
    }
}
