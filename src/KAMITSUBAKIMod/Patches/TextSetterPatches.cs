using System;
using System.Reflection;
using HarmonyLib;

namespace KAMITSUBAKIMod.Patches
{
    // Patch UnityEngine.UI.Text.set_text(string)
    [HarmonyPatch]
    public static class Patch_UIText_SetText
    {
        static bool Prepare() => TextReplaceService.ApplyToUGUIText;

        static MethodBase TargetMethod() =>
            AccessTools.Property(typeof(UnityEngine.UI.Text), "text").GetSetMethod();

        static void Prefix(ref string value)
        {
            try { value = TextReplaceService.Apply(value); }
            catch (Exception e) { Plugin.Log?.LogWarning($"UIText replace failed: {e.Message}"); }
        }
    }

    // Patch TMPro.TMP_Text.set_text(string) —— 用字符串反射（避免必须引用 TMP 程序集）
    [HarmonyPatch]
    public static class Patch_TMPro_SetText
    {
        static bool Prepare() => TextReplaceService.ApplyToTMP;

        static MethodBase TargetMethod()
        {
            // 运行时解析 "TMPro.TMP_Text:set_text"
            var type = AccessTools.TypeByName("TMPro.TMP_Text");
            if (type == null) return null;
            var prop = AccessTools.Property(type, "text");
            return prop?.GetSetMethod();
        }

        static void Prefix(ref string value)
        {
            try { value = TextReplaceService.Apply(value); }
            catch (Exception e) { Plugin.Log?.LogWarning($"TMPro replace failed: {e.Message}"); }
        }
    }
}
