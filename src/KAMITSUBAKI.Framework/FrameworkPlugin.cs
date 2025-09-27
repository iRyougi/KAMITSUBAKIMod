using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using KAMITSUBAKI.Framework.Core;
using KAMITSUBAKI.Framework.Services;
using KAMITSUBAKI.Framework.UI;
using KAMITSUBAKI.Framework.UI.Widgets;
using System;
using System.Collections.Generic;
using UnityEngine;
using BepInEx.Configuration;
#if DEBUG
using KAMITSUBAKI.Framework.UI.Runtime;
#endif
using UnityEngine.UI;

namespace KAMITSUBAKI.Framework
{
    [BepInPlugin("kamitsubaki.framework", "Kamitsubaki Mod Framework", "0.1.0")]
    public class FrameworkPlugin : BaseUnityPlugin
    {
        internal static ManualLogSource Log;
        internal static ModsLoader Loader;
        public static Harmony Harmony { get; private set; }
        public static IAssetService Assets { get; private set; }
        public static ITextService Texts { get; private set; }
        internal static ConfigEntry<bool> CfgVerboseVfsMiss;
        internal static ConfigEntry<bool> CfgScanSettingsUI;       
        internal static ConfigEntry<bool> CfgScanSettingsUIAggressive; 
        internal static bool ScanSettingsUIEnabled => CfgScanSettingsUI != null && CfgScanSettingsUI.Value;
        internal static bool ScanSettingsUIAggressive => CfgScanSettingsUIAggressive != null && CfgScanSettingsUIAggressive.Value;

        private void Awake()
        {
            Log = Logger;
            Harmony = new Harmony("kamitsubaki.framework.harmony");
            
            // Configuration
            CfgVerboseVfsMiss = Config.Bind("Debug", "VerboseVfsMiss", false, "输出每次 VFS miss 调试行 (默认关闭)");
            CfgScanSettingsUI = Config.Bind("Debug", "ScanSettingsUI", true, "扫描含 Setting/Option/Config/设 等关键词的 OnEnable (定位设置菜单)");
            CfgScanSettingsUIAggressive = Config.Bind("Debug", "ScanSettingsUIAggressive", false, "激进模式：记录所有可能的 UI 组件脚本 (仅定位时开启)");
            
            // Initialize services
            Assets = new AssetService(Logger);
            Texts = new TextService(Logger, Assets);
            AssetService.VerboseMisses = CfgVerboseVfsMiss.Value;
            CfgVerboseVfsMiss.SettingChanged += (_, __) => AssetService.VerboseMisses = CfgVerboseVfsMiss.Value;
            
            // Load mods
            Loader = new ModsLoader(Logger, Assets);
            Loader.LoadAllMods(System.IO.Path.Combine(Paths.PluginPath, "Mods"));
            
            // Test VFS
            if (Assets.TryGetOverrideFile("scripts/story0003.book.override.tsv", out var hit))
                Log.LogInfo("[DEBUG] pre-check override story0003 hit -> " + hit);
            else
                Log.LogInfo("[DEBUG] pre-check override story0003 MISS");
            
            // Apply Harmony patches
            HarmonyPatches_AssetLoad.Apply(Harmony, Assets);
            TryInstallSettingsPatch();
            
#if DEBUG
            var scanGo = new GameObject("KMFW_SettingsUIScanner");
            DontDestroyOnLoad(scanGo); 
            var scanner = scanGo.AddComponent<SettingsUIScanner>();
            scanner.Initialize(Log, () => ScanSettingsUIEnabled, () => ScanSettingsUIAggressive);
#endif
            Log.LogInfo("[Framework] ready");
        }

        void TryInstallSettingsPatch()
        {
            // 注册示例 Widget（仅一次）
            SettingsRegistry.Register(new DisplayModeWidget(Log));
            
            // 动态查找可能的设置窗口脚本（根据扫描结果：ConfigWindow）
            var t = AccessTools.TypeByName("ConfigWindow");
            if (t == null)
            {
                Log.LogInfo("[Settings] ConfigWindow type not found yet (will rely on future refresh)");
                return;
            }
            var m = AccessTools.Method(t, "OnEnable") ?? AccessTools.Method(t, "Start");
            if (m == null) { Log.LogWarning("[Settings] no OnEnable/Start on ConfigWindow"); return; }
            try
            {
                Harmony.Patch(m, postfix: new HarmonyMethod(typeof(FrameworkPlugin).GetMethod(nameof(Post_ConfigWindow_OnEnable), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)));
                Log.LogInfo("[Settings] Patched ConfigWindow." + m.Name + " for injection");
            }
            catch (Exception ex)
            { Log.LogWarning("[Settings] patch failed: " + ex.Message); }
        }

        static void Post_ConfigWindow_OnEnable(object __instance)
        {
            try
            {
                var comp = __instance as Component; if (comp == null) return;
                // 优先 Panel 子节点
                var root = comp.transform;
                var panel = root.Find("Panel") ?? root;
                SettingsInjector.Inject(panel, Log);
            }
            catch (Exception ex) { Log?.LogWarning("[Settings] inject failed: " + ex.Message); }
        }

        private void OnDestroy()
        { 
            try { Harmony?.UnpatchSelf(); } catch { } 
        }

        // Public API
        public static bool TryGetOverride(string virtualPath, out string fullPath)
        { 
            fullPath = null; 
            return Assets != null && Assets.TryGetOverrideFile(virtualPath, out fullPath); 
        }
        
        public static T LoadOrNull<T>(string virtualPath) where T : UnityEngine.Object
        { 
            if (Assets != null && Assets.TryLoadFromVFS(virtualPath, typeof(T), out var obj)) 
                return obj as T; 
            return null; 
        }
        
        public static void ClearVfsCache() => Assets?.ClearCache();
        
        public static bool RemoveVfsCache(string virtualPath) => Assets != null && Assets.RemoveCache(virtualPath);
        
        public static IEnumerable<IAssetService.MountInfo> ListMounts()
        { 
            if (Assets == null) yield break; 
            foreach (var m in Assets.EnumerateMounts()) yield return m; 
        }
    }
}
