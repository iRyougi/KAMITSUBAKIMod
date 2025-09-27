namespace KAMITSUBAKI.Framework.Core
{
    public interface IModContext
    {
        string ModId { get; }
        string ModRoot { get; }
        HarmonyLib.Harmony Harmony { get; }
        BepInEx.Logging.ManualLogSource Log { get; }

        // 暴露框架服务
        Services.IAssetService Assets { get; }
        Services.ITextService Texts { get; }

        string GetPath(params string[] parts);
    }
}