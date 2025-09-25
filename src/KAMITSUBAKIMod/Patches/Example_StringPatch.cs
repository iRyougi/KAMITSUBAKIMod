using System;
using System.Reflection;
using HarmonyLib;

namespace KAMITSUBAKIMod.Patches
{
    // Example: fill in your real target method name to activate this patch.
    // This template uses string-based reflection to avoid hard compile-time dependency on game types.
    [HarmonyPatch]
    public static class Example_StringPatch
    {
        // --- HOW TO USE ---
        // 1) Find the real method in ILSpy/dnSpy, e.g. Namespace.TypeName:MethodName
        // 2) Replace the string below.
        // 3) Optionally specify parameter types if overloaded (new Type[]{ typeof(int), typeof(string) })
        static MethodBase TargetMethod()
        {
            // RETURN NULL to disable the example until you've filled it (avoids startup errors)
            // return null;

            return AccessTools.Method("GameNamespace.Player:TakeDamage", new Type[]{ typeof(int) });
        }

        static void Prefix(ref int amount)
        {
            amount = Math.Max(1, amount / 2);
        }
    }
}
