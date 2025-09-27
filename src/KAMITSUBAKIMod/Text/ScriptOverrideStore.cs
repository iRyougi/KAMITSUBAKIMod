using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using BepInEx;

namespace KAMITSUBAKIMod.Text
{
    // 读取 script/<bookName>.override.tsv
    // 按“行号（从1起，对应数据行，不含表头） -> 字符串值”缓存覆盖文本。
    // 优先列：SimplifiedChinese；若不存在则 Text；两者都无则整行不覆盖。
    public static class ScriptOverrideStore
    {
        private static readonly Dictionary<string, OverrideData> _cache = new Dictionary<string, OverrideData>(StringComparer.OrdinalIgnoreCase);

        public static string ScriptDir => Path.Combine(Paths.PluginPath, "KAMITSUBAKIMod", "script");

        public class OverrideData
        {
            public Dictionary<int, string> LineToText = new Dictionary<int, string>(); // 行号 -> 覆盖文本
            public string TargetColumnName = "SimplifiedChinese"; // 记录命中的列名（便于日志）
        }

        public static OverrideData LoadFor(string bookName)
        {
            if (_cache.TryGetValue(bookName, out var d)) return d;

            var path = Path.Combine(ScriptDir, bookName + ".override.tsv");
            var data = new OverrideData();

            if (!File.Exists(path))
            {
                _cache[bookName] = data; // 空也缓存，避免频繁 IO
                return data;
            }

            try
            {
                using (var sr = new StreamReader(path, Encoding.UTF8, true))
                {
                    string headerLine = sr.ReadLine();
                    if (string.IsNullOrEmpty(headerLine))
                    {
                        _cache[bookName] = data; return data;
                    }
                    var headers = headerLine.Split('\t');
                    int colZh = IndexOf(headers, "SimplifiedChinese");
                    int colText = IndexOf(headers, "Text");
                    int target = colZh >= 0 ? colZh : colText;
                    data.TargetColumnName = colZh >= 0 ? "SimplifiedChinese" : (colText >= 0 ? "Text" : "");

                    string line;
                    int lineNo = 0; // 数据行号，从 1 开始
                    while ((line = sr.ReadLine()) != null)
                    {
                        lineNo++;
                        if (string.IsNullOrEmpty(line)) continue;

                        var cells = line.Split('\t');
                        if (target >= 0 && target < cells.Length)
                        {
                            var v = cells[target];
                            // 反转义 \n -> 换行
                            if (!string.IsNullOrEmpty(v))
                                v = v.Replace("\\n", "\n");
                            if (!string.IsNullOrEmpty(v))
                                data.LineToText[lineNo] = v; // 只有非空才作为覆盖
                        }
                    }
                }
                BepInEx.Logging.Logger.CreateLogSource("KAMITSUBAKIMod")
                    .LogInfo($"[Script] Loaded override for {bookName}: {data.LineToText.Count} lines (col={data.TargetColumnName})");
            }
            catch (Exception e)
            {
                BepInEx.Logging.Logger.CreateLogSource("KAMITSUBAKIMod")
                    .LogWarning($"[Script] Load override failed for {bookName}: {e.Message}");
            }

            _cache[bookName] = data;
            return data;
        }

        private static int IndexOf(string[] arr, string name)
        {
            for (int i = 0; i < arr.Length; i++)
                if (string.Equals(arr[i], name, StringComparison.OrdinalIgnoreCase))
                    return i;
            return -1;
        }
    }
}
