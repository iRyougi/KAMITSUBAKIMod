using System;
using HarmonyLib;
using UnityEngine;
using KAMITSUBAKIMod.Runtime;

namespace KAMITSUBAKIMod.Patches
{
    // 统一拦 AssetBundle.LoadAsset(string, Type)
    [HarmonyPatch(typeof(AssetBundle), nameof(AssetBundle.LoadAsset), new[] { typeof(string), typeof(Type) })]
    static class Patch_AssetBundle_Load_Any
    {
        // 1) 前缀：先走 VFS（Mods/工作区）命中则直接返回替代资源
        static bool Prefix(string name, Type type, ref UnityEngine.Object __result)
        {
            // 若框架未加载或出现 TypeLoadException，避免连续抛异常导致卡死
            try
            {
                var assets = KAMITSUBAKI.Framework.FrameworkPlugin.Assets;
                if (assets != null && assets.TryLoadFromVFS(name, type, out var obj))
                {
                    __result = obj;     // 命中替换，阻止原加载
                    return false;
                }
            }
            catch (TypeLoadException tle)
            {
                // 仅记录一次可加静态标志，这里简单输出
                Debug.LogWarning("[KAMITSUBAKIMod] Framework not loaded (TypeLoadException): " + tle.Message);
            }
            return true;             // 没命中 / 框架不可用 → 继续原逻辑
        }

        // 2) 后缀：如果是 .book，把对象注册给 BookRegistry（TextService 会在 Register 时应用覆盖）
        static void Postfix(string name, Type type, UnityEngine.Object __result)
        {
            if (__result == null) return;

            bool isBook =
                (!string.IsNullOrEmpty(name) && name.EndsWith(".book", StringComparison.OrdinalIgnoreCase)) ||
                (!string.IsNullOrEmpty(__result.name) && __result.name.EndsWith(".book", StringComparison.OrdinalIgnoreCase));

            if (isBook)
            {
                string key = !string.IsNullOrEmpty(name) ? name : __result.name;
                BookRegistry.Register(key, __result);
            }
        }
    }
}
