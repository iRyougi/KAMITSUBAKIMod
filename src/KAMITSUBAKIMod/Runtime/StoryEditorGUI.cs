using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using BepInEx;
using UnityEngine;
using KAMITSUBAKIMod.Text;     // 用于读取/写入 .override.tsv（你之前的 ScriptOverrideStore.cs）

namespace KAMITSUBAKIMod.Runtime
{
    // F1 打开编辑器窗口；左侧选书，右侧分页编辑目标列（简中优先，其次 Text），保存到 script/<book>.override.tsv
    public class StoryEditorGUI : MonoBehaviour
    {
        // 显示
        private bool _show;
        private Rect _win = new Rect(30, 30, 980, 560);
        private float _alpha = 0.9f;
        private bool _resizing = false;

        // 列表/筛选/滚动
        private string _filter = "";
        private Vector2 _leftScroll;
        private Vector2 _rightScroll;

        // 当前书 & 数据
        private string _selected = null;
        private List<List<string>> _rows = null;  // rows[0] = header
        private int _colName = -1, _colText = -1, _colZh = -1, _targetCol = -1;

        // 分页
        private int _page = 0;
        private const int PageSize = 20;

        // 样式
        private GUIStyle _labelWrap;

        private void Start()
        {
            DontDestroyOnLoad(gameObject);
            _labelWrap = null; // 只能在 OnGUI 里基于 GUI.skin 创建
        }

        private void Update()
        {
            if (UnityEngine.Input.GetKeyDown(KeyCode.F1)) _show = !_show;
        }

        private void OnGUI()
        {
            if (!_show) return;

            if (_labelWrap == null)
                _labelWrap = new GUIStyle(GUI.skin.label) { wordWrap = true };

            var old = GUI.color;
            GUI.color = new Color(1f, 1f, 1f, _alpha);

            _win = GUI.Window(0xCAFE123, _win, DoWindow, "Story Editor (F1 toggle)");

            GUI.color = old;

            // 保证窗口在屏幕内
            _win.x = Mathf.Clamp(_win.x, 0, Screen.width - 80);
            _win.y = Mathf.Clamp(_win.y, 0, Screen.height - 60);
        }

        private void DoWindow(int id)
        {
            // 顶部操作条
            GUILayout.BeginHorizontal();
            GUILayout.Label("Opacity", GUILayout.Width(60));
            _alpha = GUILayout.HorizontalSlider(_alpha, 0.3f, 1f, GUILayout.Width(140));
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Close", GUILayout.Width(70))) _show = false;
            GUILayout.EndHorizontal();

            GUILayout.Space(6);

            GUILayout.BeginHorizontal();
            DrawLeftPanel();
            DrawRightPanel();
            GUILayout.EndHorizontal();

            // 拖动窗口
            GUI.DragWindow(new Rect(0, 0, _win.width, 24));

            // 右下角缩放
            var rh = new Rect(_win.width - 18, _win.height - 18, 18, 18);
            GUI.DrawTexture(rh, Texture2D.whiteTexture);
            EditorResizeHandle(rh);
        }

        private void EditorResizeHandle(Rect rh)
        {
            var e = Event.current;
            if (e.type == EventType.MouseDown && rh.Contains(e.mousePosition)) { _resizing = true; e.Use(); }
            if (_resizing && e.type == EventType.MouseDrag)
            {
                _win.width = Mathf.Clamp(_win.width + e.delta.x, 600, Screen.width - _win.x - 20);
                _win.height = Mathf.Clamp(_win.height + e.delta.y, 420, Screen.height - _win.y - 20);
                e.Use();
            }
            if (e.type == EventType.MouseUp) _resizing = false;
        }

        // ---------------- 左侧：书列表 ----------------
        private void DrawLeftPanel()
        {
            GUILayout.BeginVertical(GUILayout.Width(330));

            GUILayout.Label("Story Books");
            GUILayout.BeginHorizontal();
            GUILayout.Label("Filter:", GUILayout.Width(45));
            _filter = GUILayout.TextField(_filter ?? "");
            GUILayout.EndHorizontal();

            _leftScroll = GUILayout.BeginScrollView(_leftScroll, GUI.skin.box, GUILayout.ExpandHeight(true));
            var list = BookRegistry.List();
            foreach (var e in list)
            {
                if (e == null || string.IsNullOrEmpty(e.Name)) continue;
                if (!string.IsNullOrEmpty(_filter) &&
                    e.Name.IndexOf(_filter, StringComparison.OrdinalIgnoreCase) < 0) continue;

                bool sel = (e.Name == _selected);
                if (GUILayout.Toggle(sel, e.Name, "Button"))
                {
                    if (_selected != e.Name)
                    {
                        _selected = e.Name;
                        LoadRowsFromObject(e.Object, _selected);
                    }
                }
            }
            GUILayout.EndScrollView();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Refresh"))
            {
                if (!string.IsNullOrEmpty(_selected))
                {
                    var entry = BookRegistry.Get(_selected);
                    if (entry != null) LoadRowsFromObject(entry.Object, _selected);
                }
            }
            if (GUILayout.Button("Force Scan"))
            {
                int n = 0;
                try
                {
                    // 枚举所有已加载对象，凡是名字以 .book 结尾的都注册
                    var all = Resources.FindObjectsOfTypeAll<UnityEngine.Object>();
                    foreach (var o in all)
                    {
                        if (o == null) continue;
                        var nm = o.name;
                        if (!string.IsNullOrEmpty(nm) && nm.EndsWith(".book", StringComparison.OrdinalIgnoreCase))
                        {
                            KAMITSUBAKIMod.Runtime.BookRegistry.Register(nm, o);
                            n++;
                        }
                    }
                    Debug.Log($"[Editor] ForceScan registered {n} books");
                }
                catch (Exception ex)
                {
                    Debug.LogWarning("[Editor] ForceScan failed: " + ex.Message);
                }
            }
            if (!string.IsNullOrEmpty(_selected) && GUILayout.Button("Save Override (.tsv)"))
            {
                SaveOverrideTsv(_selected);
            }
            GUILayout.EndHorizontal();

            GUILayout.EndVertical();
        }

        // ---------------- 右侧：编辑面板 ----------------
        private void DrawRightPanel()
        {
            GUILayout.BeginVertical(GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));

            if (string.IsNullOrEmpty(_selected))
            {
                GUILayout.Label("Select a storyXXXX.book on the left.", _labelWrap);
                GUILayout.EndVertical();
                return;
            }
            if (_rows == null || _rows.Count == 0)
            {
                GUILayout.Label("No rows found in this book.", _labelWrap);
                GUILayout.EndVertical();
                return;
            }

            DrawHeader();
            DrawPager();

            _rightScroll = GUILayout.BeginScrollView(_rightScroll, GUI.skin.box, GUILayout.ExpandHeight(true));

            int start = 1 + _page * PageSize; // 跳过 header
            int end = Math.Min(_rows.Count - 1, start + PageSize - 1);
            if (start > end) { start = 1; end = Math.Min(_rows.Count - 1, PageSize); }

            for (int i = start; i <= end; i++)
            {
                var row = _rows[i] ?? new List<string>();
                GUILayout.BeginVertical(GUI.skin.box);

                // 行号 + 角色名
                GUILayout.BeginHorizontal();
                GUILayout.Label($"#{i}", GUILayout.Width(60));
                if (_colName >= 0 && _colName < row.Count)
                    GUILayout.Label(row[_colName] ?? "", _labelWrap);
                GUILayout.EndHorizontal();

                // 原文（优先 Text）
                string src = (_colText >= 0 && _colText < row.Count) ? (row[_colText] ?? "") :
                             (_colZh >= 0 && _colZh < row.Count) ? (row[_colZh] ?? "") : "";
                if (!string.IsNullOrEmpty(src))
                {
                    GUILayout.Label("Source:", GUILayout.Width(70));
                    GUILayout.Label(src.Replace("\n", "\\n"), _labelWrap);
                }

                // 目标列编辑（简中优先）
                if (_targetCol >= 0)
                {
                    GUILayout.Label($"Edit ({(_colZh >= 0 ? "SimplifiedChinese" : "Text")}):");
                    string cur = GetCell(row, _targetCol);
                    string edited = GUILayout.TextArea(cur ?? "", GUILayout.MinHeight(46));
                    if (edited != cur)
                        SetCell(row, _targetCol, edited);
                }

                GUILayout.EndVertical();
            }

            GUILayout.EndScrollView();
            DrawPager();

            GUILayout.EndVertical();
        }

        private void DrawHeader()
        {
            var header = _rows[0];
            GUILayout.BeginHorizontal(GUI.skin.box);
            GUILayout.Label("Columns:", GUILayout.Width(70));
            GUILayout.Label($"Name={IdxToName(_colName, header)} | Text={IdxToName(_colText, header)} | Zh={IdxToName(_colZh, header)} | Target={IdxToName(_targetCol, header)}");
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }

        private string IdxToName(int idx, List<string> header)
        {
            if (idx < 0 || header == null || idx >= header.Count) return "-";
            var s = header[idx]; return string.IsNullOrEmpty(s) ? $"[{idx}]" : s;
        }

        private void DrawPager()
        {
            if (_rows == null || _rows.Count <= 1) return;
            int totalData = _rows.Count - 1;
            int totalPages = Mathf.Max(1, (totalData + PageSize - 1) / PageSize);
            _page = Mathf.Clamp(_page, 0, totalPages - 1);

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("<<", GUILayout.Width(40))) _page = 0;
            if (GUILayout.Button("<", GUILayout.Width(40))) _page = Mathf.Max(0, _page - 1);
            GUILayout.Label($"Page {_page + 1}/{totalPages}  (rows={totalData})", GUILayout.Width(220));
            if (GUILayout.Button(">", GUILayout.Width(40))) _page = Mathf.Min(totalPages - 1, _page + 1);
            if (GUILayout.Button(">>", GUILayout.Width(40))) _page = totalPages - 1;
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }

        // ---------- 数据加载/保存 ----------

        private void LoadRowsFromObject(UnityEngine.Object obj, string bookNameForOverride)
        {
            _rows = null; _colName = _colText = _colZh = _targetCol = -1; _page = 0;
            if (obj == null) return;

            string json = JsonUtility.ToJson(obj);
            if (string.IsNullOrEmpty(json)) return;

            List<ArraySpan> spans;
            _rows = ExtractAllStringsArrays(json, out spans);
            if (_rows == null || _rows.Count == 0) return;

            var header = _rows[0];
            _colName = IndexOf(header, "CharacterName");
            _colText = IndexOf(header, "Text");
            _colZh = IndexOf(header, "SimplifiedChinese");
            _targetCol = (_colZh >= 0) ? _colZh : _colText;

            // 读取现有 override 作为初始值（若存在）
            if (!string.IsNullOrEmpty(bookNameForOverride))
            {
                var ov = ScriptOverrideStore.LoadFor(bookNameForOverride);
                if (ov != null && ov.LineToText.Count > 0 && _targetCol >= 0)
                {
                    foreach (var kv in ov.LineToText)
                    {
                        int rowIdx = kv.Key;       // 数据行：从 1 起
                        int real = 0 + rowIdx;     // header 在 0
                        if (real > 0 && real < _rows.Count)
                            SetCell(_rows[real], _targetCol, kv.Value);
                    }
                }
            }
        }

        private void SaveOverrideTsv(string bookName)
        {
            if (_rows == null || _rows.Count <= 1 || _targetCol < 0) return;

            var dir = Path.Combine(Paths.PluginPath, "KAMITSUBAKIMod", "script");
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            var path = Path.Combine(dir, bookName + ".override.tsv");

            var sb = new StringBuilder();

            // 头：尽量输出 Name/Text/SimplifiedChinese 三列（存在的才写）
            var header = _rows[0];
            var cols = new List<string>();
            if (_colName >= 0) cols.Add("CharacterName");
            if (_colText >= 0) cols.Add("Text");
            if (_colZh >= 0) cols.Add("SimplifiedChinese");
            if (cols.Count == 0) cols.AddRange(header);
            sb.AppendLine(string.Join("\t", cols.ToArray()));

            // 行：目标列非空就写；空代表不覆盖
            for (int i = 1; i < _rows.Count; i++)
            {
                var r = _rows[i];
                var val = GetCell(r, _targetCol);
                if (string.IsNullOrEmpty(val)) { sb.AppendLine(""); continue; }

                string name = _colName >= 0 && _colName < r.Count ? (r[_colName] ?? "") : "";
                string text = _colText >= 0 && _colText < r.Count ? (r[_colText] ?? "") : "";
                string zh = _colZh >= 0 && _colZh < r.Count ? (r[_colZh] ?? "") : "";

                string esc(string s) => (s ?? "").Replace("\\", "\\\\").Replace("\r", "").Replace("\n", "\\n");

                var outRow = new List<string>();
                if (_colName >= 0) outRow.Add(esc(name));
                if (_colText >= 0) outRow.Add(esc(text));
                if (_colZh >= 0) outRow.Add(esc(zh));

                if (outRow.Count == 0) outRow = new List<string>(r);
                sb.AppendLine(string.Join("\t", outRow.ToArray()));
            }

            File.WriteAllText(path, sb.ToString(), new UTF8Encoding(false));
            Debug.Log($"[Editor] Saved override: {path}");
        }

        // ---------- 轻量 JSON 解析（提取所有 "strings":[ ... ] 为二维数组） ----------
        struct ArraySpan { public int start; public int end; }

        private static List<List<string>> ExtractAllStringsArrays(string json, out List<ArraySpan> spans)
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

        private static int IndexOf(List<string> header, string name)
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

        private static string GetCell(List<string> row, int idx)
        {
            if (row == null || idx < 0 || idx >= row.Count) return "";
            return row[idx] ?? "";
        }
        private static void SetCell(List<string> row, int idx, string val)
        {
            if (row == null || idx < 0) return;
            while (row.Count <= idx) row.Add("");
            row[idx] = val ?? "";
        }
    }
}
