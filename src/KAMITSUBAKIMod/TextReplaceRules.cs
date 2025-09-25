using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using BepInEx;

namespace KAMITSUBAKIMod
{
    public enum MatchType { Exact, Contains, Regex }

    public sealed class ReplaceRule
    {
        public MatchType Type;
        public bool IgnoreCase;
        public string From = "";
        public string To = "";
        // 预编译的正则（仅 Regex 规则）
        public Regex Compiled;

        public bool TryInit()
        {
            if (Type == MatchType.Regex)
            {
                var opts = RegexOptions.Compiled | RegexOptions.CultureInvariant;
                if (IgnoreCase) opts |= RegexOptions.IgnoreCase;
                try
                {
                    Compiled = new Regex(From, opts);
                }
                catch (Exception e)
                {
                    Plugin.Log?.LogWarning($"Regex compile failed: /{From}/ -> {e.Message}");
                    return false;
                }
            }
            return true;
        }

        public string Apply(string input)
        {
            if (string.IsNullOrEmpty(input)) return input;

            switch (Type)
            {
                case MatchType.Exact:
                    return string.Equals(input, From, IgnoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal)
                        ? To : input;

                case MatchType.Contains:
                    if (IgnoreCase)
                    {
                        var idx = input.IndexOf(From, StringComparison.OrdinalIgnoreCase);
                        return idx >= 0 ? ReplaceFirstOrdinalIgnoreCase(input, From, To) : input;
                    }
                    else
                    {
                        return input.Contains(From) ? input.Replace(From, To) : input;
                    }

                case MatchType.Regex:
                    return Compiled != null ? Compiled.Replace(input, To) : input;
            }
            return input;
        }

        private static string ReplaceFirstOrdinalIgnoreCase(string text, string from, string to)
        {
            var idx = text.IndexOf(from, StringComparison.OrdinalIgnoreCase);
            if (idx < 0) return text;
            var sb = new StringBuilder(text.Length - from.Length + to.Length);
            sb.Append(text, 0, idx);
            sb.Append(to);
            sb.Append(text, idx + from.Length, text.Length - idx - from.Length);
            return sb.ToString();
        }
    }

    public static class TextReplaceRules
    {
        public static readonly List<ReplaceRule> Rules = new List<ReplaceRule>();
        public static string RulesFilePath => Path.Combine(Paths.PluginPath, "KAMITSUBAKIMod", "text", "replace.csv");

        public static void EnsureFile()
        {
            var dir = Path.GetDirectoryName(RulesFilePath);
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            if (!File.Exists(RulesFilePath))
            {
                File.WriteAllText(RulesFilePath,
@"MatchType,IgnoreCase,From,To
Exact,true,开始,Start
Contains,false,按任意键,Press any key
Regex,true,\b等级\s*(\d+),Lv $1
", new UTF8Encoding(false));
            }
        }

        public static bool Load()
        {
            try
            {
                Rules.Clear();
                if (!File.Exists(RulesFilePath)) return false;

                using (var sr = new StreamReader(RulesFilePath, Encoding.UTF8, true))
                {
                    string? line;
                    bool first = true;
                    while ((line = sr.ReadLine()) != null)
                    {
                        if (first) { first = false; if (line.StartsWith("MatchType", StringComparison.OrdinalIgnoreCase)) continue; }
                        if (string.IsNullOrWhiteSpace(line)) continue;

                        var cols = SplitCsv(line);
                        if (cols.Count < 4) continue;

                        if (!Enum.TryParse<MatchType>(cols[0].Trim(), true, out var type)) continue;
                        bool ignore = cols[1].Trim().Equals("true", StringComparison.OrdinalIgnoreCase);

                        var rule = new ReplaceRule
                        {
                            Type = type,
                            IgnoreCase = ignore,
                            From = cols[2],
                            To = cols[3]
                        };
                        if (rule.TryInit()) Rules.Add(rule);
                    }
                }
                Plugin.Log?.LogInfo($"Loaded {Rules.Count} text replace rule(s).");
                return true;
            }
            catch (Exception e)
            {
                Plugin.Log?.LogWarning($"Failed to load replace rules: {e}");
                return false;
            }
        }

        public static string ApplyAll(string input)
        {
            if (string.IsNullOrEmpty(input) || Rules.Count == 0) return input;
            string cur = input;
            for (int i = 0; i < Rules.Count; i++)
                cur = Rules[i].Apply(cur);
            return cur;
        }

        private static List<string> SplitCsv(string line)
        {
            // 简单 CSV：逗号分隔，支持用双引号包裹，双引号内的双引号用 "" 转义
            var res = new List<string>(4);
            var sb = new StringBuilder();
            bool inQuotes = false;
            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];
                if (inQuotes)
                {
                    if (c == '"')
                    {
                        if (i + 1 < line.Length && line[i + 1] == '"') { sb.Append('"'); i++; }
                        else inQuotes = false;
                    }
                    else sb.Append(c);
                }
                else
                {
                    if (c == ',') { res.Add(sb.ToString()); sb.Clear(); }
                    else if (c == '"') inQuotes = true;
                    else sb.Append(c);
                }
            }
            res.Add(sb.ToString());
            return res;
        }
    }
}
