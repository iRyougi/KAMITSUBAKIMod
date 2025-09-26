using System.IO;
using System.Linq;
using System.Text.Json;
using BepInEx.Logging;
using KAMITSUBAKI.Framework.Manifests;
using KAMITSUBAKI.Framework.Services;

namespace KAMITSUBAKI.Framework.Loader
{
    internal class ModsLoader
    {
        readonly ManualLogSource _log;
        readonly IAssetService _assets;
        public ModsLoader(ManualLogSource log, IAssetService assets) { _log = log; _assets = assets; }

        public void LoadAllMods(string root)
        {
            if (!Directory.Exists(root)) Directory.CreateDirectory(root);
            foreach (var dir in Directory.GetDirectories(root))
            {
                var manifest = Path.Combine(dir, "mod.json");
                if (!File.Exists(manifest)) continue;

                ModManifest mf = null;
                try { mf = JsonSerializer.Deserialize<ModManifest>(File.ReadAllText(manifest)); }
                catch { _log.LogWarning($"[Mods] manifest error: {manifest}"); continue; }

                if (mf.mounts != null)
                {
                    foreach (var m in mf.mounts)
                    {
                        var from = Path.Combine(dir, m.from ?? "");
                        var to = (m.to ?? "").Replace("\\", "/").Trim('/');
                        _assets.AddMount(mf.id, from, to, mf.priority);
                    }
                }
                _log.LogInfo($"[Mods] mounted {mf.name} ({mf.id})");
            }
        }
    }
}
