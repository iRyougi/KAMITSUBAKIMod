using BepInEx;
using BepInEx.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

using KAMITSUBAKI.Framework;

namespace KAMITSUBAKI.Framework.Services
{
    internal class TextService : ITextService
    {
        readonly ManualLogSource _log;
        readonly string _scriptWorkspace;

        public TextService(ManualLogSource log)
        {
            _log = log;
            _scriptWorkspace = Path.Combine(Paths.PluginPath, "KAMITSUBAKIMod", "scripts");
            if (!Directory.Exists(_scriptWorkspace)) Directory.CreateDirectory(_scriptWorkspace);
        }

        public void ApplyOverrideForBook(string bookName, UnityEngine.Object obj)
        {
            try
            {
                if (obj == null || string.IsNullOrEmpty(bookName)) return;

                string path = TryFindOverrideTsv(bookName);
                if (path == null) { _log.LogInfo("[Texts] no override for " + bookName); return; }

                var json = JsonUtility.ToJson(obj);
                if (string.IsNullOrEmpty(json)) return;

                List<ArraySpan> spans;
                var rows = ExtractAllStringsArrays(json, out spans);
                if (rows == null || rows.Count == 0) return;

                var header = rows[0];
                int colText = IndexOf(header, "Text");
                int colZh = IndexOf(header, "SimplifiedChinese");
                int target = (colZh >= 0) ? colZh : colText;
                if (target < 0) { _log.LogWarning("[Texts] no target column for " + bookName); return; }

                var lineMap = LoadLineMap(path);
                foreach (var kv in lineMap)
                {
                    int idx = kv.Key; // 1..N
                    if (idx <= 0 || idx >= rows.Count) continue;
                    SetCell(rows[idx], target, kv.Value ?? "");
                }

                var patched = RebuildJson(json, spans, rows);
                if (!string.IsNullOrEmpty(patched) && !object.ReferenceEquals(patched, json))
                {
                    JsonUtility.FromJsonOverwrite(patched, obj);
                    _log.LogInfo("[Texts] applied override " + bookName + " (" + lineMap.Count + " lines)");
                }
            }
            catch (Exception ex) { _log.LogWarning("[Texts] apply failed: " + ex); }
        }

        string TryFindOverrideTsv(string bookName)
        {
            string[] candidates = {
                "scripts/" + bookName + ".override.tsv",
                "scripts/" + TrimBook(bookName) + ".override.tsv"
            };
            for (int i = 0; i < candidates.Length; i++)
            {
                string v = candidates[i];
                string full;
                if (Framework.FrameworkPlugin.Assets.TryGetOverrideFile(v, out full)) return full;

                var ws = Path.Combine(_scriptWorkspace, Path.GetFileName(v));
                if (File.Exists(ws)) return ws;
            }
            return null;
        }

        static string TrimBook(string s)
        {
            if (string.IsNullOrEmpty(s)) return s;
            return s.EndsWith(".book", StringComparison.OrdinalIgnoreCase) ? s.Substring(0, s.Length - 5) : s;
        }

        Dictionary<int, string> LoadLineMap(string path)
        {
            var map = new Dictionary<int, string>();
            var lines = File.ReadAllLines(path, new UTF8Encoding(false));
            if (lines.Length == 0) return map;

            for (int i = 1; i < lines.Length; i++)
            {
                var row = lines[i].Split('\t');
                if (row.Length == 0) continue;
                string last = row[row.Length - 1];
                map[i] = last.Replace("\\n", "\n");
            }
            return map;
        }

        struct ArraySpan { public int start; public int end; }

        static List<List<string>> ExtractAllStringsArrays(string json, out List<ArraySpan> spans)
        {
            spans = new List<ArraySpan>();
            var result = new List<List<string>>();
            if (string.IsNullOrEmpty(json)) return result;

            string key = "\"strings\":"; int pos = 0;
            while (true)
            {
                int idx = json.IndexOf(key, pos, StringComparison.OrdinalIgnoreCase);
                if (idx < 0) break;
                int lb = json.IndexOf('[', idx + key.Length); if (lb < 0) break;

                int p = lb + 1;
                var row = new List<string>();
                var sb = new StringBuilder();
                bool inString = false, escape = false;
                int startSpan = lb, endSpan = -1;

                while (p < json.Length)
                {
                    char c = json[p++];
                    if (inString)
                    {
                        if (escape)
                        {
                            escape = false;
                            if (c == 'n') sb.Append('\n');
                            else if (c == 'r') { }
                            else if (c == 't') sb.Append('\t');
                            else if (c == '"') sb.Append('\"');
                            else if (c == '\\') sb.Append('\\');
                            else sb.Append(c);
                        }
                        else if (c == '\\') escape = true;
                        else if (c == '"') { row.Add(sb.ToString()); sb.Length = 0; inString = false; }
                        else sb.Append(c);
                    }
                    else
                    {
                        if (c == '"') inString = true;
                        else if (c == ']') { endSpan = p; break; }
                    }
                }

                if (row.Count > 0) { result.Add(row); spans.Add(new ArraySpan { start = startSpan, end = endSpan }); }
                pos = (endSpan > 0 ? endSpan : p);
            }
            return result;
        }

        static string RebuildJson(string json, List<ArraySpan> spans, List<List<string>> rows)
        {
            if (spans == null || rows == null || spans.Count != rows.Count) return json;
            var sb = new StringBuilder(json.Length + 1024);
            int cur = 0;
            for (int i = 0; i < spans.Count; i++)
            {
                var sp = spans[i];
                sb.Append(json, cur, sp.start - cur);
                sb.Append('[');
                var arr = rows[i];
                for (int j = 0; j < arr.Count; j++)
                {
                    if (j > 0) sb.Append(',');
                    sb.Append('"').Append(Escape(arr[j])).Append('"');
                }
                sb.Append(']');
                cur = sp.end;
            }
            sb.Append(json, cur, json.Length - cur);
            return sb.ToString();
        }

        static string Escape(string s)
        {
            if (string.IsNullOrEmpty(s)) return string.Empty;
            var b = new StringBuilder(s.Length + 8);
            for (int i = 0; i < s.Length; i++)
            {
                char c = s[i];
                switch (c)
                {
                    case '\\': b.Append("\\\\"); break;
                    case '\"': b.Append("\\\""); break;
                    case '\n': b.Append("\\n"); break;
                    case '\r': break;
                    case '\t': b.Append("\\t"); break;
                    default: b.Append(c); break;
                }
            }
            return b.ToString();
        }

        static int IndexOf(List<string> header, string name)
        {
            if (header == null) return -1;
            for (int i = 0; i < header.Count; i++)
            {
                string s = header[i];
                if (!string.IsNullOrEmpty(s) && string.Equals(s, name, StringComparison.OrdinalIgnoreCase))
                    return i;
            }
            return -1;
        }

        static void SetCell(List<string> row, int idx, string val)
        {
            if (row == null || idx < 0) return;
            while (row.Count <= idx) row.Add("");
            row[idx] = val ?? "";
        }
    }
}
