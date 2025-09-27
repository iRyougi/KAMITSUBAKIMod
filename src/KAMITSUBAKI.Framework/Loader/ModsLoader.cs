using System.IO;
using BepInEx.Logging;
using KAMITSUBAKI.Framework.Manifests;
using KAMITSUBAKI.Framework.Services;
using UnityEngine;  // JsonUtility

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
                var manifestPath = Path.Combine(dir, "mod.json");
                if (!File.Exists(manifestPath)) continue;

                ModManifest mf = null;
                try
                {
                    var json = File.ReadAllText(manifestPath);
                    mf = JsonUtility.FromJson<ModManifest>(json);
                }
                catch
                {
                    _log.LogWarning($"[Mods] manifest error: {manifestPath}");
                    continue;
                }
                if (mf == null) continue;

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
