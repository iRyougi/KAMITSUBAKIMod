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

            if (!string.IsNullOrEmpty(name) && name.EndsWith(".book", StringComparison.OrdinalIgnoreCase))
            {
                // 已有：Dump JSON / 应用覆盖 …
                // 新增：注册给 GUI
                // 命中 .book 后 —— 无论是用 name 还是 obj.name，都注册到 GUI
                try
                {
                    string key = null;

                    // 优先用 AB 传入的 name
                    if (!string.IsNullOrEmpty(name) && name.EndsWith(".book", StringComparison.OrdinalIgnoreCase))
                        key = name;

                    // 兜底用对象自己的 name（很多 AdvImportBook 的 obj.name 就是 story0001.book）
                    if (string.IsNullOrEmpty(key))
                    {
                        var on = __result.name; // UnityEngine.Object.name
                        if (!string.IsNullOrEmpty(on) && on.EndsWith(".book", StringComparison.OrdinalIgnoreCase))
                            key = on;
                    }

                    if (!string.IsNullOrEmpty(key))
                    {
                        KAMITSUBAKIMod.Runtime.BookRegistry.Register(key, __result);
                        Debug.Log($"[Editor] Register book from AB: {key} ({__result.GetType().Name})");
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning("[Editor] Register in AB failed: " + ex.Message);
                }

            }
        }
    }
}
