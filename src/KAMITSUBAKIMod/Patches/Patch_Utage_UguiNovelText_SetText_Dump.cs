using System;
using System.Reflection;
using HarmonyLib;
using KAMITSUBAKIMod.Text;

namespace KAMITSUBAKIMod.Patches
{
    [HarmonyPatch]
    public static class Patch_Utage_UguiNovelText_SetText_Dump
    {
        static MethodBase TargetMethod()
        {
            var t = AccessTools.TypeByName("Utage.UguiNovelText");
            if (t == null) return null;
            return AccessTools.Method(t, "SetText", new Type[] { typeof(string) });
        }

        static void Prefix(object __instance, ref string text)
        {
            try
            {
                if (!string.IsNullOrEmpty(text))
                {
                    // 纯文本流水：每行一条
                    DumpUtil.LogLine("novel_text", text);
                }
            }
            catch (Exception e)
            {
                BepInEx.Logging.Logger.CreateLogSource("KAMITSUBAKIMod")
                    .LogWarning("[Dump] UguiNovelText.SetText failed: " + e.Message);
            }
        }
    }
}
