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

                // 1) 解析一个已知 override 文件
                if (KAMITSUBAKI.Framework.FrameworkPlugin.TryGetOverride("scripts/story0003.book.override.tsv", out var full))
                    Debug.Log("[TEST] resolve OK -> " + full);
                else
                    Debug.Log("[TEST] resolve MISS");

                // 2) 尝试加载一个存在的图片（假设放在 assets/ui/test_icon.png）
                var tex = KAMITSUBAKI.Framework.FrameworkPlugin.LoadOrNull<Texture2D>("ui/test_icon.png");
                Debug.Log("[TEST] load texture result=" + (tex ? tex.name + $" ({tex.width}x{tex.height})" : "null"));

                // 3) 缓存测试：第一次加载应该打印 [VFS] hit + load，第二次不打印（来自缓存）
                var tex2 = KAMITSUBAKI.Framework.FrameworkPlugin.LoadOrNull<Texture2D>("ui/test_icon.png");
                Debug.Log("[TEST] load again (should be cached)=" + (tex2 ? tex2.name : "null"));

                // 4) 移除缓存，再次加载
                KAMITSUBAKI.Framework.FrameworkPlugin.RemoveVfsCache("ui/test_icon.png");
                var tex3 = KAMITSUBAKI.Framework.FrameworkPlugin.LoadOrNull<Texture2D>("ui/test_icon.png");
                Debug.Log("[TEST] after RemoveVfsCache reload=" + (tex3 ? tex3.name : "null"));

                // 5) 清空全部缓存
                KAMITSUBAKI.Framework.FrameworkPlugin.ClearVfsCache();

                // 6) 再次加载验证缓存清空文件
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