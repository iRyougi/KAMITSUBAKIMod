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
        internal static Harmony Harmony;
        internal static ModsLoader Loader;
        internal static AssetService Assets;
        internal static TextService Texts;

        private void Awake()
        {
            Log = Logger;
            Harmony = new Harmony("kamitsubaki.framework.harmony");

            // 初始化服务
            Assets = new AssetService(Logger);
            Texts = new TextService(Logger);

            // 扫描 Mods 并挂载（形成 VFS 挂载点）
            Loader = new ModsLoader(Logger, Assets);
            Loader.LoadAllMods(System.IO.Path.Combine(Paths.PluginPath, "Mods"));

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
