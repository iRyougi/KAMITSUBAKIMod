using System;
using HarmonyLib;
using UnityEngine;
using KAMITSUBAKI.Framework.Core;

namespace KAMITSUBAKI.Framework.Services
{
    public static class HarmonyPatches_AssetLoad
    {
        static IAssetService _assets;

        public static void Apply(Harmony harmony, IAssetService assets)
        {
            _assets = assets;
            harmony.Patch(
                original: AccessTools.Method(typeof(AssetBundle), nameof(AssetBundle.LoadAsset), new[] { typeof(string), typeof(Type) }),
                prefix: new HarmonyMethod(typeof(HarmonyPatches_AssetLoad), nameof(Prefix_AB_LoadAsset))
            );

            var m = AccessTools.Method(typeof(Resources), nameof(Resources.Load), new[] { typeof(string), typeof(Type) });
            if (m != null)
                harmony.Patch(m, prefix: new HarmonyMethod(typeof(HarmonyPatches_AssetLoad), nameof(Prefix_Res_Load)));
        }

        static bool Prefix_AB_LoadAsset(string name, Type type, ref UnityEngine.Object __result)
        {
            if (_assets != null && _assets.TryLoadFromVFS(name, type, out var obj)) { __result = obj; return false; }
            return true;
        }

        static bool Prefix_Res_Load(string path, Type systemTypeInstance, ref UnityEngine.Object __result)
        {
            if (_assets != null && _assets.TryLoadFromVFS(path, systemTypeInstance, out var obj)) { __result = obj; return false; }
            return true;
        }
    }
}