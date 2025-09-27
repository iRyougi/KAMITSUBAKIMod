using UnityEngine;

namespace KAMITSUBAKIMod.Runtime
{
    public class VfsTestHarness : MonoBehaviour
    {
        void Start()
        {
            DontDestroyOnLoad(gameObject);

            Debug.Log("[TEST] ---- Mounts ----");
            try
            {
                foreach (var m in KAMITSUBAKI.Framework.FrameworkPlugin.ListMounts())
                    Debug.Log($"[TEST] mount mod={m.ModId} to='{m.To}' from='{m.From}' prio={m.Priority}");

                // 1) ����һ����֪ override �ļ�
                if (KAMITSUBAKI.Framework.FrameworkPlugin.TryGetOverride("scripts/story0003.book.override.tsv", out var full))
                    Debug.Log("[TEST] resolve OK -> " + full);
                else
                    Debug.Log("[TEST] resolve MISS");

                // 2) ���Լ���һ�����ڵ�ͼƬ��������� assets/ui/test_icon.png��
                var tex = KAMITSUBAKI.Framework.FrameworkPlugin.LoadOrNull<Texture2D>("ui/test_icon.png");
                Debug.Log("[TEST] load texture result=" + (tex ? tex.name + $" ({tex.width}x{tex.height})" : "null"));

                // 3) ������ԣ���һ�μ���Ӧ�ô�ӡ [VFS] hit + load���ڶ��β���ӡ�����Ի��棩
                var tex2 = KAMITSUBAKI.Framework.FrameworkPlugin.LoadOrNull<Texture2D>("ui/test_icon.png");
                Debug.Log("[TEST] load again (should be cached)=" + (tex2 ? tex2.name : "null"));

                // 4) �Ƴ����棬�ٴμ���
                KAMITSUBAKI.Framework.FrameworkPlugin.RemoveVfsCache("ui/test_icon.png");
                var tex3 = KAMITSUBAKI.Framework.FrameworkPlugin.LoadOrNull<Texture2D>("ui/test_icon.png");
                Debug.Log("[TEST] after RemoveVfsCache reload=" + (tex3 ? tex3.name : "null"));

                // 5) ���ȫ������
                KAMITSUBAKI.Framework.FrameworkPlugin.ClearVfsCache();

                // 6) �ٴμ�����֤��������ļ�
                var tex4 = KAMITSUBAKI.Framework.FrameworkPlugin.LoadOrNull<Texture2D>("ui/test_icon.png");
                Debug.Log("[TEST] after ClearVfsCache reload=" + (tex4 ? tex4.name : "null"));
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning("[TEST] Framework not available: " + ex.Message);
            }
        }
    }
}