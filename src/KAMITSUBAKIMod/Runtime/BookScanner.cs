using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using KAMITSUBAKIMod.Text;

namespace KAMITSUBAKIMod.Runtime
{
    public class BookScanner : MonoBehaviour
    {
        private readonly HashSet<int> _dumped = new HashSet<int>();

        [Serializable] class Book { public List<Grid> importGridList; }
        [Serializable] class Grid { public List<Row> rows; }
        [Serializable] class Row { public int rowIndex; public List<string> strings; }

        private void Start()
        {
            DontDestroyOnLoad(gameObject);
            StartCoroutine(ScanLoop());
        }

        private IEnumerator ScanLoop()
        {
            var wait = new WaitForSeconds(2f);
            while (true)
            {
                ScanOnce();
                yield return wait;
            }
        }

        private void ScanOnce()
        {
            var objs = Resources.FindObjectsOfTypeAll<UnityEngine.Object>();
            foreach (var o in objs)
            {
                if (o == null) continue;
                var nm = o.name ?? "";
                if (!nm.EndsWith(".book", StringComparison.OrdinalIgnoreCase)) continue;
                if (_dumped.Contains(o.GetInstanceID())) continue;

                var typeName = o.GetType().FullName ?? o.GetType().Name;

                // 1) 先保存原始 JSON
                try
                {
                    string json = JsonUtility.ToJson(o);
                    if (!string.IsNullOrEmpty(json))
                    {
                        var safe = DumpUtil.MakeSafe(nm);
                        DumpUtil.SaveJson("scan_" + safe + "_" + o.GetType().Name, json);
                        Debug.Log($"[Dump] saved JSON: {safe}.json ({o.GetType().Name})");
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[Dump] ToJson failed for {nm}: {ex.Message}");
                }

                // 2) 如果是 AdvImportBook，尝试解析导出 TSV
                if (typeName.IndexOf("AdvImportBook", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    TryExportTsv(nm, o);
                }

                _dumped.Add(o.GetInstanceID());
            }
        }

        private void TryExportTsv(string assetName, UnityEngine.Object o)
        {
            try
            {
                string json = JsonUtility.ToJson(o);
                if (string.IsNullOrEmpty(json)) return;

                // 1) 从 json 中“轻解析”出所有 strings 数组（每个 array = 一行）
                List<List<string>> rows = ExtractAllStringsArrays(json);
                if (rows == null || rows.Count == 0) return;

                // 2) 第一个 strings 作为表头（如果不是表头也没关系，下面会兜底）
                List<string> header = rows[0];
                int startRow = 1;
                if (header == null || header.Count == 0)
                {
                    // 没有表头就把第一行也当数据
                    header = new List<string>();
                    startRow = 0;
                }

                // 3) 尝试定位常见列
                int colText = IndexOfIgnoreCase(header, "Text");
                int colZh = IndexOfIgnoreCase(header, "SimplifiedChinese");
                int colEn = IndexOfIgnoreCase(header, "English");
                int colName = IndexOfIgnoreCase(header, "CharacterName");

                // 4) 组织 TSV
                var sb = new StringBuilder();

                // 写导出的表头（优先导出这几列；没找到就整体导出）
                List<string> outHeader = new List<string>();
                if (colName >= 0) outHeader.Add("CharacterName");
                if (colText >= 0) outHeader.Add("Text");
                if (colZh >= 0) outHeader.Add("SimplifiedChinese");
                if (colEn >= 0) outHeader.Add("English");
                if (outHeader.Count == 0)
                {
                    // 兜底：把原 header 整行写出来
                    outHeader = header ?? new List<string>();
                }
                sb.AppendLine(string.Join("\t", outHeader.ToArray()));

                // 写每一行
                for (int i = startRow; i < rows.Count; i++)
                {
                    var r = rows[i] ?? new List<string>();

                    // === 新增：检查是否整行为空 ===
                    bool allEmpty = true;
                    foreach (var cell in r)
                    {
                        if (!string.IsNullOrEmpty(cell))
                        {
                            allEmpty = false;
                            break;
                        }
                    }
                    if (allEmpty) continue; // 跳过整行空的

                    List<string> outRow = new List<string>();

                    if (outHeader.Count > 0 && (colText >= 0 || colZh >= 0 || colEn >= 0 || colName >= 0))
                    {
                        if (colName >= 0) outRow.Add(GetIndexSafe(r, colName));
                        if (colText >= 0) outRow.Add(GetIndexSafe(r, colText));
                        if (colZh >= 0) outRow.Add(GetIndexSafe(r, colZh));
                        if (colEn >= 0) outRow.Add(GetIndexSafe(r, colEn));
                    }
                    else
                    {
                        // 兜底：整行输出
                        outRow = r;
                    }

                    // 清理换行
                    for (int k = 0; k < outRow.Count; k++)
                    {
                        if (outRow[k] != null)
                            outRow[k] = outRow[k].Replace("\r\n", "\\n").Replace("\n", "\\n");
                    }

                    // 再检查输出列是否全空
                    bool outEmpty = true;
                    foreach (var cell in outRow)
                    {
                        if (!string.IsNullOrEmpty(cell))
                        {
                            outEmpty = false;
                            break;
                        }
                    }
                    if (outEmpty) continue; // 跳过全空的输出行

                    sb.AppendLine(string.Join("\t", outRow.ToArray()));
                }


                var safe = DumpUtil.MakeSafe(assetName);
                var file = Path.Combine(DumpUtil.DumpDir, safe + ".tsv");
                File.WriteAllText(file, sb.ToString(), new UTF8Encoding(false));
                Debug.Log($"[Dump] exported TSV (strings-scan): {safe}.tsv  (rows={rows.Count - startRow})");
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[Dump] Export TSV failed for {assetName}: {ex.Message}");
            }
        }

        private static int IndexOfIgnoreCase(List<string> header, string name)
        {
            if (header == null) return -1;
            for (int i = 0; i < header.Count; i++)
            {
                var h = header[i];
                if (!string.IsNullOrEmpty(h) && string.Equals(h, name, StringComparison.OrdinalIgnoreCase))
                    return i;
            }
            return -1;
        }

        private static string GetIndexSafe(List<string> arr, int idx)
        {
            if (arr == null || idx < 0 || idx >= arr.Count) return "";
            return arr[idx] ?? "";
        }

        // 从 JSON 字符串中提取所有 "strings":[ ... ] 的数组（每个数组都是一行的单元格列表）
        private static List<List<string>> ExtractAllStringsArrays(string json)
        {
            var result = new List<List<string>>();
            if (string.IsNullOrEmpty(json)) return result;

            string key = "\"strings\":";
            int pos = 0;

            while (true)
            {
                int idx = json.IndexOf(key, pos, StringComparison.OrdinalIgnoreCase);
                if (idx < 0) break;

                // 找到后面第一个 '['
                int lb = json.IndexOf('[', idx + key.Length);
                if (lb < 0) break;

                // 解析一个 JSON 字符串数组
                int p = lb + 1;
                var row = new List<string>();
                var sb = new StringBuilder();
                bool inString = false;
                bool escape = false;

                while (p < json.Length)
                {
                    char c = json[p++];

                    if (inString)
                    {
                        if (escape)
                        {
                            // 处理常见转义（只保留 \n、\r、\t、\"、\\）
                            switch (c)
                            {
                                case 'n': sb.Append('\n'); break;
                                case 'r': sb.Append('\r'); break;
                                case 't': sb.Append('\t'); break;
                                case '"': sb.Append('\"'); break;
                                case '\\': sb.Append('\\'); break;
                                default: sb.Append(c); break;
                            }
                            escape = false;
                        }
                        else if (c == '\\')
                        {
                            escape = true;
                        }
                        else if (c == '"')
                        {
                            // 结束一个字符串
                            row.Add(sb.ToString());
                            sb.Length = 0;
                            inString = false;
                        }
                        else
                        {
                            sb.Append(c);
                        }
                    }
                    else
                    {
                        if (c == '"')
                        {
                            inString = true;
                        }
                        else if (c == ']')
                        {
                            // 结束这个数组
                            break;
                        }
                    }
                }

                if (row.Count > 0)
                    result.Add(row);

                pos = p;
            }

            return result;
        }


        private static int FindCol(List<string> header, string name)
        {
            for (int i = 0; i < header.Count; i++)
            {
                var h = header[i];
                if (string.IsNullOrEmpty(h)) continue;
                if (string.Equals(h, name, StringComparison.OrdinalIgnoreCase)) return i;
            }
            return -1;
        }

        private static string GetSafe(List<string> arr, int idx)
        {
            if (idx < 0 || idx >= arr.Count) return "";
            return arr[idx] ?? "";
        }
    }
}
