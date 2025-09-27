#if DEBUG
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using BepInEx.Logging;

namespace KAMITSUBAKI.Framework.UI.Runtime
{
    public class SettingsUIScanner : MonoBehaviour
    {
        readonly HashSet<Type> _loggedTypes = new HashSet<Type>();
        float _timer;
        const float Interval = 1.5f; // 扫描频率
        
        private ManualLogSource _log;
        private Func<bool> _scanEnabledFunc;
        private Func<bool> _aggressiveScanFunc;

        void Awake()
        {
            DontDestroyOnLoad(gameObject);
            _log?.LogInfo("[ScanSettings2] runtime enumeration scanner active");
        }
        
        public void Initialize(ManualLogSource log, Func<bool> scanEnabled, Func<bool> aggressiveScan)
        {
            _log = log;
            _scanEnabledFunc = scanEnabled;
            _aggressiveScanFunc = aggressiveScan;
        }

        void Update()
        {
            if (_scanEnabledFunc?.Invoke() != true && _aggressiveScanFunc?.Invoke() != true) return;
            _timer += Time.unscaledDeltaTime;
            if (_timer < Interval) return;
            _timer = 0f;
            TryScan();
        }

        void TryScan()
        {
            var all = Resources.FindObjectsOfTypeAll<MonoBehaviour>();
            foreach (var mb in all)
            {
                if (mb == null) continue;
                var t = mb.GetType();
                if (_loggedTypes.Contains(t)) continue;

                bool match = _aggressiveScanFunc?.Invoke() == true;
                var name = t.Name;
                if (!match)
                {
                    if (name.IndexOf("Setting", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        name.IndexOf("Option", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        name.IndexOf("Config", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        name.IndexOf("设", StringComparison.OrdinalIgnoreCase) >= 0)
                        match = true;
                }

                if (!match)
                {
                    // 结构检查：是否有 UI 控件字段 / 子节点等行为
                    try
                    {
                        var flds = t.GetFields(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
                        if (flds.Any(f => IsUIType(f.FieldType))) match = true;
                    }
                    catch { }
                }

                if (!match) continue;
                _loggedTypes.Add(t);
                var path = GetHierarchyPath(mb.gameObject);
                try
                {
                    _log?.LogInfo($"[ScanSettings2] Type={t.FullName} obj={mb.gameObject.name} path={path}");
                    // 列出直接子节点
                    var tr = mb.transform;
                    int childCount = tr.childCount;
                    for (int i = 0; i < childCount; i++)
                    {
                        var c = tr.GetChild(i);
                        _log?.LogInfo($"[ScanSettings2]   child[{i}] {c.name}");
                    }
                }
                catch { }
            }
        }

        static bool IsUIType(Type ft)
        {
            if (ft == null) return false;
            var n = ft.FullName ?? ft.Name;
            return n.Contains("UnityEngine.UI.Toggle") || n.Contains("UnityEngine.UI.Slider") ||
                   n.Contains("UnityEngine.UI.Dropdown") || n.Contains("UnityEngine.UI.Button") ||
                   n.Contains("TMPro.TMP_Dropdown") || n.Contains("TMPro.TMP_Text") ||
                   n.Contains("UnityEngine.UI.InputField") || n.Contains("TMPro.TMP_InputField");
        }

        static string GetHierarchyPath(GameObject go)
        {
            if (go == null) return "";
            var stack = new Stack<string>();
            var t = go.transform;
            while (t != null)
            {
                stack.Push(t.name);
                t = t.parent;
            }
            return string.Join("/", stack);
        }
    }
}
#endif