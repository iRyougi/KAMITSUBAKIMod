using KAMITSUBAKIMod.Text;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace KAMITSUBAKIMod.Runtime
{
    /// <summary>
    /// ��ע���鱾ʱ���� script/<book>.override.tsv ����кš��ı�Ӧ�õ������ JSON��"strings":[...] ��
    /// </summary>
    public static class BookOverrideRuntime
    {
        // --- ��� ---
        public static void TryApplyOnRegister(string bookName, UnityEngine.Object obj)
        {
            try
            {
                if (obj == null || string.IsNullOrEmpty(bookName)) return;

                // 1) ��ȡ override���к� -> �ı���
                var ov = ScriptOverrideStore.LoadFor(bookName);
                if (ov == null || ov.LineToText == null || ov.LineToText.Count == 0)
                {
                    Debug.Log($"[Override] No override for {bookName}");
                    return;
                }

                // 2) ��ƽ����Ϊ JSON
                var json = JsonUtility.ToJson(obj);
                if (string.IsNullOrEmpty(json)) return;

                // 3) �������� "strings":[ ... ]����һ���� header��
                List<ArraySpan> spans;
                var rows = ExtractAllStringsArrays(json, out spans);
                if (rows == null || rows.Count == 0)
                {
                    Debug.Log($"[Override] No strings[] found in {bookName}");
                    return;
                }

                var header = rows[0];
                int colText = IndexOf(header, "Text");
                int colZh = IndexOf(header, "SimplifiedChinese");
                int target = (colZh >= 0) ? colZh : colText; // ��༭������һ�£����ȼ��У����� Text
                if (target < 0)
                {
                    Debug.Log($"[Override] No target column in {bookName}");
                    return;
                }

                // 4) ���� override���кŴ� 1 ��ʼ����Ӧ rows[1..]
                foreach (var kv in ov.LineToText)
                {
                    int line = kv.Key;             // 1-based�������У�
                    int idx = line;               // header �� 0
                    if (idx <= 0 || idx >= rows.Count) continue;
                    SetCell(rows[idx], target, kv.Value ?? "");
                }

                // 5) �� rows д�� JSON �����Ƕ���
                var patched = RebuildJson(json, spans, rows);
                if (!string.IsNullOrEmpty(patched) && !ReferenceEquals(patched, json))
                {
                    JsonUtility.FromJsonOverwrite(patched, obj);
                    Debug.Log($"[Override] Applied to {bookName} (rows={rows.Count - 1})");
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[Override] Apply failed: " + ex);
            }
        }

        // ---------- ���������� JSON ����/��д���ߣ���༭����ͬ˼·�� ----------
        struct ArraySpan { public int start; public int end; }

        static List<List<string>> ExtractAllStringsArrays(string json, out List<ArraySpan> spans)
        {
            spans = new List<ArraySpan>();
            var result = new List<List<string>>();
            if (string.IsNullOrEmpty(json)) return result;

            string key = "\"strings\":";
            int pos = 0;
            while (true)
            {
                int idx = json.IndexOf(key, pos, StringComparison.OrdinalIgnoreCase);
                if (idx < 0) break;

                int lb = json.IndexOf('[', idx + key.Length);
                if (lb < 0) break;

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
                            else if (c == 'r') sb.Append('\r');
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
                // ԭ������ '[' ֮ǰ
                sb.Append(json, cur, sp.start - cur);
                // ��д��ǰ strings ����
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
            // ʣ��β��
            sb.Append(json, cur, json.Length - cur);
            return sb.ToString();
        }

        static string Escape(string s)
        {
            if (string.IsNullOrEmpty(s)) return string.Empty;
            var sb = new StringBuilder(s.Length + 8);
            foreach (var c in s)
            {
                switch (c)
                {
                    case '\\': sb.Append("\\\\"); break;
                    case '\"': sb.Append("\\\""); break;
                    case '\n': sb.Append("\\n"); break;
                    case '\r': /* drop */ break;
                    case '\t': sb.Append("\\t"); break;
                    default: sb.Append(c); break;
                }
            }
            return sb.ToString();
        }

        static int IndexOf(List<string> header, string name)
        {
            if (header == null) return -1;
            for (int i = 0; i < header.Count; i++)
            {
                var s = header[i];
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