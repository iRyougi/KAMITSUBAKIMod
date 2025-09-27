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
            var wait = new WaitForSeconds(2f); // ÿ 2 ��ɨһ��
            while (true)
            {
                TryScan();
                yield return wait;
            }
        }

        private void TryScan()
        {
            // ɨ�����Ѽ��ض��󣨰��������ʲ���
            var objs = Resources.FindObjectsOfTypeAll<Object>();
            foreach (var o in objs)
            {
                if (o == null) continue;
                // ֻ�������� .book ��β��
                var nm = o.name;
                if (string.IsNullOrEmpty(nm) || !nm.EndsWith(".book", System.StringComparison.OrdinalIgnoreCase))
                    continue;

                // �����ظ�����
                int id = o.GetInstanceID();
                if (_done.Contains(id)) continue;

                // TextAsset ������������������ AB �������ţ�
                if (o is TextAsset ta)
                {
                    string mod = TextBookMap.ApplySimple(ta.text);
                    if (mod != ta.text)
                    {
                        // TextAsset ���ݲ���ԭλ�ģ�ֻ����־��ʾ��AB ���ӿ��滻����ֵ��
                        Debug.Log($"[BOOK-LIVE] Seen TextAsset .book: {nm} (cannot overwrite in place)");
                    }
                    _done.Add(id);
                    continue;
                }

                // ���� JSON ���ǣ������� MonoBehaviour / ScriptableObject �ʲ���
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