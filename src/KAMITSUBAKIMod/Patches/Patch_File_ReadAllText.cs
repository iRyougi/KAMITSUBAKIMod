using System;
using System.IO;
using HarmonyLib;
using KAMITSUBAKIMod.Text;

namespace KAMITSUBAKIMod.Patches
{
    [HarmonyPatch(typeof(File), nameof(File.ReadAllText), new Type[] { typeof(string) })]
    public static class Patch_File_ReadAllText
    {
        static void Postfix(string path, ref string __result)
        {
            try
            {
                if (!path.EndsWith(".book", StringComparison.OrdinalIgnoreCase)) return;
                __result = TextBookMap.ApplySimple(__result);
                // 或 BookJsonRewriter.RewriteChineseColumn(__result)
            }
            catch (Exception e)
            {
                BepInEx.Logging.Logger.CreateLogSource("KAMITSUBAKIMod")
                    .LogWarning($"ReadAllText book rewrite failed: {e.Message}");
            }
        }
    }
}
