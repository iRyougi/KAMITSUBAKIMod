using System;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using KAMITSUBAKIMod.Text;

namespace KAMITSUBAKIMod.Patches
{
    [HarmonyPatch]
    public static class Patch_AssetBundle_Load_Any
    {
        static MethodBase TargetMethod()
        {
            return AccessTools.Method(typeof(AssetBundle), "LoadAsset_Internal",
                new Type[] { typeof(string), typeof(Type) });
        }

        static void Postfix(string name, Type type, ref UnityEngine.Object __result)
        {
            try
            {
                if (__result == null) return;

                var realType = __result.GetType().FullName ?? "UnknownType";

                // 调试期：了解资源加载情况
                BepInEx.Logging.Logger.CreateLogSource("KAMITSUBAKIMod")
                    .LogInfo($"[AB] Loaded: {name} -> {realType}");

                if (!name.EndsWith(".book", StringComparison.OrdinalIgnoreCase) &&
                    !(__result.name != null && __result.name.EndsWith(".book", StringComparison.OrdinalIgnoreCase)))
                    return;

                // 若是 TextAsset：可直接替换返回值（比 RES 钩子更稳）
                if (__result is TextAsset ta)
                {
                    string mod = TextBookMap.ApplySimple(ta.text);
                    if (mod != ta.text)
                    {
                        __result = new TextAsset(mod) { name = ta.name };
                        BepInEx.Logging.Logger.CreateLogSource("KAMITSUBAKIMod")
                            .LogInfo($"[AB] Replaced TextAsset .book: {ta.name}");
                    }
                    return;
                }

                // 非 TextAsset：通过 JsonUtility 覆盖字段
                string json = JsonUtility.ToJson(__result);
                if (!string.IsNullOrEmpty(json))
                {
                    string mod = TextBookMap.ApplySimple(json);
                    if (mod != json)
                    {
                        JsonUtility.FromJsonOverwrite(mod, __result);
                        BepInEx.Logging.Logger.CreateLogSource("KAMITSUBAKIMod")
                            .LogInfo($"[AB] Rewrote .book via JsonUtility: {__result.name} ({__result.GetType().Name})");
                    }
                }
            }
            catch (Exception e)
            {
                BepInEx.Logging.Logger.CreateLogSource("KAMITSUBAKIMod")
                    .LogWarning("AB book rewrite failed: " + e.Message);
            }
        }
    }
}
