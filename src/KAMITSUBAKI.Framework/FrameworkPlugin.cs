using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using KAMITSUBAKI.Framework.Loader;
using KAMITSUBAKI.Framework.Services;

namespace KAMITSUBAKI.Framework
{
    [BepInPlugin("kamitsubaki.framework", "Kamitsubaki Mod Framework", "0.1.0")]
    public class FrameworkPlugin : BaseUnityPlugin
    {
        internal static ManualLogSource Log;
        internal static ModsLoader Loader;

        public static Harmony Harmony { get; private set; }
        public static IAssetService Assets { get; private set; }  // 改为接口，避免可访问性冲突
        public static ITextService Texts { get; private set; }    // 改为接口，避免可访问性冲突

        private void Awake()
        {
            Log = Logger;
            Harmony = new Harmony("kamitsubaki.framework.harmony");

            // 初始化服务（内部实现类仍为 internal）
            Assets = new AssetService(Logger);
            Texts = new TextService(Logger);

            // 扫描 Mods 并挂载（形成 VFS 挂载点）
            Loader = new ModsLoader(Logger, Assets);
            Loader.LoadAllMods(System.IO.Path.Combine(Paths.PluginPath, "Mods"));

            // 调试：直接测试一个常用 override 路径（可帮助判断 VFS 挂载是否生效）
            if (Assets.TryGetOverrideFile("scripts/story0003.book.override.tsv", out var hit))
                Log.LogInfo("[DEBUG] pre-check override story0003 hit -> " + hit);
            else
                Log.LogInfo("[DEBUG] pre-check override story0003 MISS");

            // 打补丁：重定向资源加载
            Hooks.HarmonyPatches_AssetLoad.Apply(Harmony, Assets);

            Log.LogInfo("[Framework] ready");
        }

        // 原来：Harmony.UnpatchAll(Harmony.Id);
        private void OnDestroy()
        {
            try { Harmony?.UnpatchSelf(); } catch { }
        }
    }
}
