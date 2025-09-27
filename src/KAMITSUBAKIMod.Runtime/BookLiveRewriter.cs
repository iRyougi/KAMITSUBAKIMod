using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using KAMITSUBAKIMod.Text;

namespace KAMITSUBAKIMod.Runtime
{
    public class BookLiveRewriter : MonoBehaviour
    {
        private readonly HashSet<int> _done = new HashSet<int>();

        private void Start()
        {
            DontDestroyOnLoad(gameObject);
            StartCoroutine(ScanLoop());
        }

        private IEnumerator ScanLoop()
        {
            var wait = new WaitForSeconds(2f); // 每 2 秒扫一次
            while (true)
            {
                TryScan();
                yield return wait;
            }
        }

        private void TryScan()
        {
            // 扫所有已加载对象（包含隐藏资产）
            var objs = Resources.FindObjectsOfTypeAll<Object>();
            foreach (var o in objs)
            {
                if (o == null) continue;
                // 只看名字以 .book 结尾的
                var nm = o.name;
                if (string.IsNullOrEmpty(nm) || !nm.EndsWith(".book", System.StringComparison.OrdinalIgnoreCase))
                    continue;

                // 避免重复处理
                int id = o.GetInstanceID();
                if (_done.Contains(id)) continue;

                // TextAsset 单独处理（不过我们有 AB 补丁兜着）
                if (o is TextAsset ta)
                {
                    string mod = TextBookMap.ApplySimple(ta.text);
                    if (mod != ta.text)
                    {
                        // TextAsset 内容不可原位改，只做日志提示（AB 钩子可替换返回值）
                        Debug.Log($"[BOOK-LIVE] Seen TextAsset .book: {nm} (cannot overwrite in place)");
                    }
                    _done.Add(id);
                    continue;
                }

                // 尝试 JSON 覆盖（适用于 MonoBehaviour / ScriptableObject 资产）
                try
                {
                    string json = JsonUtility.ToJson(o);
                    if (!string.IsNullOrEmpty(json))
                    {
                        string mod = TextBookMap.ApplySimple(json);
                        if (mod != json)
                        {
                            JsonUtility.FromJsonOverwrite(mod, o);
                            Debug.Log($"[BOOK-LIVE] Patched .book via JsonUtility: {nm} ({o.GetType().Name})");
                        }
                    }
                }
                catch (System.SystemException e)
                {
                    Debug.LogWarning($"[BOOK-LIVE] Json patch failed for {nm}: {e.Message}");
                }

                _done.Add(id);
            }
        }
    }
}