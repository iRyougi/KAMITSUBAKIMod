using System;
using HarmonyLib;
using UnityEngine;
using KAMITSUBAKI.Framework.Services;

namespace KAMITSUBAKI.Framework.Hooks
{
    internal static class HarmonyPatches_AssetLoad
    {
        static IAssetService _assets;

        public static void Apply(Harmony harmony, IAssetService assets)
        {
            _assets = assets;
            harmony.Patch(
                original: AccessTools.Method(typeof(AssetBundle), nameof(AssetBundle.LoadAsset), new[] { typeof(string), typeof(Type) }),
                prefix: new HarmonyMethod(typeof(HarmonyPatches_AssetLoad), nameof(Prefix_AB_LoadAsset))
            );

            // 如项目使用 Resources.Load，可加这一组：
            var m = AccessTools.Method(typeof(Resources), nameof(Resources.Load), new[] { typeof(string), typeof(Type) });
            if (m != null)
                harmony.Patch(m, prefix: new HarmonyMethod(typeof(HarmonyPatches_AssetLoad), nameof(Prefix_Res_Load)));
        }

        static bool Prefix_AB_LoadAsset(string name, Type type, ref UnityEngine.Object __result)
        {
            // 约定：把 AB 内部名字当作“虚拟路径”，例如 Texture/Character/ch103_diff.png
            if (_assets != null && _assets.TryLoadFromVFS(name, type, out var obj))
            {
                __result = obj; return false; // 命中 → 阻止原始加载
            }
            return true;
        }

        static bool Prefix_Res_Load(string path, Type systemTypeInstance, ref UnityEngine.Object __result)
        {
            if (_assets != null && _assets.TryLoadFromVFS(path, systemTypeInstance, out var obj))
            {
                __result = obj; return false;
            }
            return true;
        }
    }
}
