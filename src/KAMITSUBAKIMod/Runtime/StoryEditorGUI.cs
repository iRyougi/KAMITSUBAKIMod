using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using BepInEx;
using UnityEngine;
using KAMITSUBAKIMod.Text;

namespace KAMITSUBAKIMod.Runtime
{
    // 让本脚本的 Update 比游戏里大多数脚本更早执行，优先清空输入
    [DefaultExecutionOrder(-32000)]
    public class StoryEditorGUI : MonoBehaviour
    {
        // —— 显示状态 & 窗口属性 ——
        private bool _show;
        private Rect _win = new Rect(30, 30, 980, 560);
        private float _alpha = 0.9f;
        private bool _resizing = false;

        // 可调大小/字体
        private int _w = 980, _h = 560;
        private int _fontSize = 14;
        private int _lastAppliedFontSize = -1;

        // —— 输入屏蔽相关 ——
        private bool _blockGameInput = true;   // 软屏蔽（清空输入轴）
        private bool _pauseWhileOpen = false;  // 硬屏蔽（暂停时间）
        private CursorLockMode _prevLock;
        private bool _prevCursorVisible;
        private float _prevTimeScale = 1f;

        // —— 列表/筛选/滚动 ——
        private string _filter = "";
        private Vector2 _leftScroll;
        private Vector2 _rightScroll;
        private bool _showEmptyRows = false;

        // —— 当前书 & 数据 ——
        private string _selected = null;
        private List<List<string>> _rows = null;  // rows[0] = header
        private int _colName = -1, _colText = -1, _colZh = -1, _targetCol = -1;

        // —— 分页 ——
        private int _page = 0;
        private const int PageSize = 20;

        // —— 样式 ——（只能在 OnGUI 里初始化/修改）
        private GUIStyle _labelWrap;

        private void Start()
        {
            DontDestroyOnLoad(gameObject);
            _labelWrap = null; // OnGUI 里再创建
            _w = (int)_win.width; _h = (int)_win.height;
        }

        private void Update()
        {
            // 先处理热键，再决定是否清空输入
            if (UnityEngine.Input.GetKeyDown(KeyCode.F1))
                ToggleShow();

            // Ctrl+S 快捷保存
            if (_show && (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)) && Input.GetKeyDown(KeyCode.S))
            {
                if (!string.IsNullOrEmpty(_selected) && _rows != null && _rows.Count > 1 && _targetCol >= 0)
                    SaveOverrideTsv(_selected);
            }

            if (_show && _blockGameInput)
                UnityEngine.Input.ResetInputAxes(); // 软屏蔽：尽早清空输入
        }


        private void LateUpdate()
        {
            // 双保险：在 LateUpdate 也清空一次，尽可能覆盖不同脚本顺序
            if (_show && _blockGameInput)
                UnityEngine.Input.ResetInputAxes();
        }

        private void ToggleShow()
        {
            _show = !_show;
            if (_show)
            {
                _prevLock = Cursor.lockState;
                _prevCursorVisible = Cursor.visible;
                _prevTimeScale = Time.timeScale;

                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;

                if (_pauseWhileOpen) Time.timeScale = 0f;
            }
            else
            {
                Cursor.lockState = _prevLock;
                Cursor.visible = _prevCursorVisible;

                Time.timeScale = _prevTimeScale;
            }
        }

        private void OnGUI()
        {
            if (!_show) return;

            // 顶层吞掉窗口外的鼠标事件（点击/拖拽/滚轮），避免传到游戏
            var e = Event.current;
            if (_blockGameInput && e != null)
            {
                if ((e.isMouse || e.type == EventType.ScrollWheel) &&
                    !_win.Contains(e.mousePosition))
                {
                    e.Use(); // 吞掉窗口外鼠标事件
                }
            }

            // 透明度 & 字体大小
            var oldColor = GUI.color;
            GUI.color = new Color(1f, 1f, 1f, _alpha);
            ApplyFontSizeIfNeeded(); // 只能在 OnGUI 里改 GUI.skin

            // 窗口
            _win.width = _w; _win.height = _h;
            _win = GUI.Window(0xCAFE123, _win, DoWindow, "Story Editor (F1 toggle)");

            GUI.color = oldColor;

            // 防止窗口跑出屏幕
            _win.x = Mathf.Clamp(_win.x, 0, Screen.width - 80);
            _win.y = Mathf.Clamp(_win.y, 0, Screen.height - 60);
        }

        private void ApplyFontSizeIfNeeded()
        {
            if (_labelWrap == null || _lastAppliedFontSize != _fontSize)
            {
                var skin = GUI.skin;
                skin.label.fontSize = _fontSize;
                skin.button.fontSize = _fontSize;
                skin.window.fontSize = _fontSize + 2;
                skin.textField.fontSize = _fontSize;
                skin.textArea.fontSize = _fontSize;
                skin.toggle.fontSize = _fontSize;

                _labelWrap = new GUIStyle(skin.label) { wordWrap = true, fontSize = _fontSize };
                _lastAppliedFontSize = _fontSize;
            }
        }

        private void DoWindow(int id)
        {
            // 顶部控制条
            GUILayout.BeginHorizontal();
            GUILayout.Label("Opacity", GUILayout.Width(70));
            _alpha = GUILayout.HorizontalSlider(_alpha, 0.3f, 1f, GUILayout.Width(140));

            GUILayout.Space(8);
            _blockGameInput = GUILayout.Toggle(_blockGameInput, "Block input", GUILayout.Width(120));
            var pauseNew = GUILayout.Toggle(_pauseWhileOpen, "Pause game", GUILayout.Width(120));
            if (pauseNew != _pauseWhileOpen)
            {
                _pauseWhileOpen = pauseNew;
                if (_show)
                    Time.timeScale = _pauseWhileOpen ? 0f : _prevTimeScale;
            }

            GUILayout.Space(8);
            _showEmptyRows = GUILayout.Toggle(_showEmptyRows, "Show empty rows", GUILayout.Width(150));

            GUILayout.FlexibleSpace();

            // 仅当有选中的书且已加载行时可保存
            GUI.enabled = !string.IsNullOrEmpty(_selected) && _rows != null && _rows.Count > 1 && _targetCol >= 0;
            if (GUILayout.Button("Save Override", GUILayout.Width(120)))
            {
                SaveOverrideTsv(_selected);
            }
            if (GUILayout.Button("Open Folder", GUILayout.Width(110)))
            {
                OpenOverrideFolder();
            }
            GUI.enabled = true;

            // 字体大小
            GUILayout.Space(2);
            GUILayout.Label($"Font {_fontSize}", GUILayout.Width(80));
            _fontSize = Mathf.RoundToInt(GUILayout.HorizontalSlider(_fontSize, 10, 24, GUILayout.Width(150)));


            GUILayout.Space(8);
            if (GUILayout.Button("Close", GUILayout.Width(70))) _show = false;
            GUILayout.EndHorizontal();

            GUILayout.Space(6);

            GUILayout.BeginHorizontal();
            DrawLeftPanel();
            DrawRightPanel();
            GUILayout.EndHorizontal();

            // 窗口拖动
            GUI.DragWindow(new Rect(0, 0, _win.width, 24));

            // 右下角拉伸
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
                _w = Mathf.Clamp(_w + Mathf.RoundToInt(e.delta.x), 700, Screen.width - (int)_win.x - 20);
                _h = Mathf.Clamp(_h + Mathf.RoundToInt(e.delta.y), 420, Screen.height - (int)_win.y - 20);
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
            GUILayout.Label("Filter:", GUILayout.Width(50));
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
                    var all = Resources.FindObjectsOfTypeAll<UnityEngine.Object>();
                    foreach (var o in all)
                    {
                        if (o == null) continue;
                        var nm = o.name;
                        if (!string.IsNullOrEmpty(nm) && nm.EndsWith(".book", StringComparison.OrdinalIgnoreCase))
                        {
                            BookRegistry.Register(nm, o);
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

                // —— 空行过滤（Name / Text / SimplifiedChinese 全空） ——
                if (!_showEmptyRows && IsEmptyRow(row)) continue;

                GUILayout.BeginVertical(GUI.skin.box);

                // 行号 + 角色名
                GUILayout.BeginHorizontal();
                GUILayout.Label($"#{i}", GUILayout.Width(60));
                if (_colName >= 0 && _colName < row.Count)
                    GUILayout.Label(row[_colName] ?? "", _labelWrap);
                GUILayout.EndHorizontal();

                // JP + CN 展示
                string jp = (_colText >= 0 && _colText < row.Count) ? (row[_colText] ?? "") : "";
                string cn = (_colZh >= 0 && _colZh < row.Count) ? (row[_colZh] ?? "") : "";

                if (!string.IsNullOrEmpty(jp))
                {
                    GUILayout.Label("JP(Text):");
                    GUILayout.Label(jp.Replace("\n", "\\n"), _labelWrap);
                }
                if (!string.IsNullOrEmpty(cn))
                {
                    GUILayout.Label("CN(SimplifiedChinese):");
                    GUILayout.Label(cn.Replace("\n", "\\n"), _labelWrap);
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
            GUILayout.Label("Columns:", GUILayout.Width(80));
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
            GUILayout.Label($"Page {_page + 1}/{totalPages}  (rows={totalData})", GUILayout.Width(240));
            if (GUILayout.Button(">", GUILayout.Width(40))) _page = Mathf.Min(totalPages - 1, _page + 1);
            if (GUILayout.Button(">>", GUILayout.Width(40))) _page = totalPages - 1;
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }

        // 打开覆盖文件夹
        private void OpenOverrideFolder()
        {
            var dir = Path.Combine(Paths.PluginPath, "KAMITSUBAKIMod", "script");
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            // Windows 下用 explorer 打开
            try { System.Diagnostics.Process.Start(dir); }
            catch { Debug.Log($"[Editor] Folder: {dir}"); }
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

            // 应用已有 override
            if (!string.IsNullOrEmpty(bookNameForOverride))
            {
                var ov = ScriptOverrideStore.LoadFor(bookNameForOverride);
                if (ov != null && ov.LineToText.Count > 0 && _targetCol >= 0)
                {
                    foreach (var kv in ov.LineToText)
                    {
                        int rowIdx = kv.Key;       // 数据行从 1 起
                        int real = rowIdx;       // header 在 0
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

            // 头
            var header = _rows[0];
            var cols = new List<string>();
            if (_colName >= 0) cols.Add("CharacterName");
            if (_colText >= 0) cols.Add("Text");
            if (_colZh >= 0) cols.Add("SimplifiedChinese");
            if (cols.Count == 0) cols.AddRange(header);
            sb.AppendLine(string.Join("\t", cols.ToArray()));

            // 行
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

        // ---------- 提取 JSON 中所有 "strings":[ ... ] ----------
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

        private bool IsEmptyRow(List<string> r)
        {
            string name = GetCell(r, _colName);
            string jp = GetCell(r, _colText);
            string cn = GetCell(r, _colZh);
            return string.IsNullOrEmpty(name) && string.IsNullOrEmpty(jp) && string.IsNullOrEmpty(cn);
        }
    }
}
