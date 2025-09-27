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

            // 1) 解析一个已知 override 文件
            if (FrameworkPlugin.TryGetOverride("scripts/story0003.book.override.tsv", out var full))
                Debug.Log("[TEST] resolve OK -> " + full);
            else
                Debug.Log("[TEST] resolve MISS");

            // 2) 尝试加载一个存在的图片（如果你放了 assets/ui/test_icon.png）
            var tex = FrameworkPlugin.LoadOrNull<Texture2D>("ui/test_icon.png");
            Debug.Log("[TEST] load texture result=" + (tex ? tex.name + $" ({tex.width}x{tex.height})" : "null"));

            // 3) 缓存测试：第一次加载应该触发 [VFS] hit + load；第二次命中缓存（不再打印 load）
            var tex2 = FrameworkPlugin.LoadOrNull<Texture2D>("ui/test_icon.png");
            Debug.Log("[TEST] load again (should be cached)=" + (tex2 ? tex2.name : "null"));

            // 4) 移除单条缓存后再次加载
            FrameworkPlugin.RemoveVfsCache("ui/test_icon.png");
            var tex3 = FrameworkPlugin.LoadOrNull<Texture2D>("ui/test_icon.png");
            Debug.Log("[TEST] after RemoveVfsCache reload=" + (tex3 ? tex3.name : "null"));

            // 5) 清空全部缓存
            FrameworkPlugin.ClearVfsCache();

            // 6) 再次加载验证会重新走文件
            var tex4 = FrameworkPlugin.LoadOrNull<Texture2D>("ui/test_icon.png");
            Debug.Log("[TEST] after ClearVfsCache reload=" + (tex4 ? tex4.name : "null"));
        }
    }
}