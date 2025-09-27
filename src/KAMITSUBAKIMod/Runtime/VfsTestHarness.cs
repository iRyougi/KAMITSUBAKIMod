using UnityEngine;
using KAMITSUBAKI.Framework;

namespace KAMITSUBAKIMod.Runtime
{
    public class VfsTestHarness : MonoBehaviour
    {
        void Start()
        {
            DontDestroyOnLoad(gameObject);

            Debug.Log("[TEST] ---- Mounts ----");
            foreach (var m in FrameworkPlugin.ListMounts())
                Debug.Log($"[TEST] mount mod={m.ModId} to='{m.To}' from='{m.From}' prio={m.Priority}");

            // 1) ����һ����֪ override �ļ�
            if (FrameworkPlugin.TryGetOverride("scripts/story0003.book.override.tsv", out var full))
                Debug.Log("[TEST] resolve OK -> " + full);
            else
                Debug.Log("[TEST] resolve MISS");

            // 2) ���Լ���һ�����ڵ�ͼƬ���������� assets/ui/test_icon.png��
            var tex = FrameworkPlugin.LoadOrNull<Texture2D>("ui/test_icon.png");
            Debug.Log("[TEST] load texture result=" + (tex ? tex.name + $" ({tex.width}x{tex.height})" : "null"));

            // 3) ������ԣ���һ�μ���Ӧ�ô��� [VFS] hit + load���ڶ������л��棨���ٴ�ӡ load��
            var tex2 = FrameworkPlugin.LoadOrNull<Texture2D>("ui/test_icon.png");
            Debug.Log("[TEST] load again (should be cached)=" + (tex2 ? tex2.name : "null"));

            // 4) �Ƴ�����������ٴμ���
            FrameworkPlugin.RemoveVfsCache("ui/test_icon.png");
            var tex3 = FrameworkPlugin.LoadOrNull<Texture2D>("ui/test_icon.png");
            Debug.Log("[TEST] after RemoveVfsCache reload=" + (tex3 ? tex3.name : "null"));

            // 5) ���ȫ������
            FrameworkPlugin.ClearVfsCache();

            // 6) �ٴμ�����֤���������ļ�
            var tex4 = FrameworkPlugin.LoadOrNull<Texture2D>("ui/test_icon.png");
            Debug.Log("[TEST] after ClearVfsCache reload=" + (tex4 ? tex4.name : "null"));
        }
    }
}