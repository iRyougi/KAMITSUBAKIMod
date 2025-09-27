using BepInEx.Logging;
using HarmonyLib;

namespace KAMITSUBAKI.Framework.Core
{
    public interface IModContext
    {
        string ModId { get; }
        string ModRoot { get; }
        Harmony Harmony { get; }
        ManualLogSource Log { get; }

        string GetPath(params string[] parts);
    }
}