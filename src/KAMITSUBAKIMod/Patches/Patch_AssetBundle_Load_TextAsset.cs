using System;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using KAMITSUBAKIMod.Text;

namespace KAMITSUBAKIMod.Patches
{
    [HarmonyPatch]
    public static class Patch_AssetBundle_Load_TextAsset
    {
        static MethodBase TargetMethod() =>
            AccessTools.Method(typeof(AssetBundle), "LoadAsset_Internal",
                new Type[] { typeof(string), typeof(Type) });

        static void Postfix(string name, Type type, ref UnityEngine.Object __result)
        {
            try
            {
                if (type != typeof(TextAsset)) return;
                if (__result is not TextAsset ta) return;
                if (!name.EndsWith(".book", StringComparison.OrdinalIgnoreCase) &&
                    !ta.name.EndsWith(".book", StringComparison.OrdinalIgnoreCase))
                    return;

                var mod = TextBookMap.ApplySimple(ta.text);
                // 或 BookJsonRewriter.RewriteChineseColumn(ta.text)
                if (mod != ta.text)
                    __result = new TextAsset(mod) { name = ta.name };
            }
            catch (Exception e)
            {
                BepInEx.Logging.Logger.CreateLogSource("KAMITSUBAKIMod")
                    .LogWarning($"AssetBundle book rewrite failed: {e.Message}");
            }
        }
    }
}
