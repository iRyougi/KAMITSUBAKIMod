using System.IO;
using System.Collections.Generic;
using BepInEx.Logging;
using KAMITSUBAKI.Framework.Manifests;
using KAMITSUBAKI.Framework.Services;
using UnityEngine;  // JsonUtility
using Newtonsoft.Json; // 更健壮的 JSON 解析

namespace KAMITSUBAKI.Framework.Loader
{
    internal class ModsLoader
    {
        readonly ManualLogSource _log;
        readonly IAssetService _assets;

        internal class LoadedMod
        {
            public ModManifest Manifest;
            public string RootDir;
        }

        readonly List<LoadedMod> _loaded = new List<LoadedMod>();
        readonly Dictionary<string, LoadedMod> _byId = new Dictionary<string, LoadedMod>();
        public IReadOnlyList<LoadedMod> Loaded => _loaded;

        public ModsLoader(ManualLogSource log, IAssetService assets) { _log = log; _assets = assets; }

        public void LoadAllMods(string root)
        {
            _loaded.Clear();
            _byId.Clear();

            if (!Directory.Exists(root)) Directory.CreateDirectory(root);
            _log.LogInfo($"[Mods] scan root={root}");

            foreach (var dir in Directory.GetDirectories(root))
            {
                _log.LogDebug("[Mods] scan dir=" + dir);
                var manifestPath = Path.Combine(dir, "mod.json");
                if (!File.Exists(manifestPath)) { _log.LogDebug("[Mods] skip (no mod.json): " + dir); continue; }

                string json = null;
                ModManifest mf = null;
                try
                {
                    json = File.ReadAllText(manifestPath);
                    // 先尝试 Unity JsonUtility
                    mf = JsonUtility.FromJson<ModManifest>(json);
                    // 如果 mounts 仍为空再用 Newtonsoft.Json（更宽容，例如允许额外空白 / 转义）
                    if (mf == null || mf.mounts == null)
                    {
                        mf = JsonConvert.DeserializeObject<ModManifest>(json);
                    }
                }
                catch (System.Exception ex)
                {
                    _log.LogWarning($"[Mods] manifest parse error: {manifestPath} ({ex.Message})");
                    continue;
                }
                if (mf == null)
                {
                    _log.LogWarning("[Mods] manifest null after parse: " + manifestPath);
                    continue;
                }

                if (string.IsNullOrWhiteSpace(mf.id))
                {
                    mf.id = Path.GetFileName(dir).Replace(' ', '.');
                    _log.LogInfo($"[Mods] assign id '{mf.id}' from folder {dir}");
                }
                if (string.IsNullOrWhiteSpace(mf.name)) mf.name = mf.id;
                if (mf.priority == 0) mf.priority = 100;

                if (_byId.TryGetValue(mf.id, out var exist))
                {
                    if (mf.priority > exist.Manifest.priority)
                    {
                        _log.LogWarning($"[Mods] duplicate id {mf.id}, keep higher priority ({mf.priority} > {exist.Manifest.priority})");
                        _loaded.Remove(exist);
                    }
                    else
                    {
                        _log.LogWarning($"[Mods] duplicate id {mf.id}, skipping (priority {mf.priority} <= {exist.Manifest.priority})");
                        continue;
                    }
                }

                var lm = new LoadedMod { Manifest = mf, RootDir = dir };
                _loaded.Add(lm);
                _byId[mf.id] = lm;

                if (!string.IsNullOrEmpty(json))
                {
                    var slice = json.Length > 120 ? json.Substring(0, 120) + "..." : json;
                    _log.LogDebug($"[Mods] json head ({mf.id}): {slice}");
                }
            }

            _loaded.Sort((a, b) =>
            {
                int c = b.Manifest.priority.CompareTo(a.Manifest.priority);
                return c != 0 ? c : string.CompareOrdinal(a.Manifest.id, b.Manifest.id);
            });

            foreach (var lm in _loaded)
            {
                var mf = lm.Manifest;
                int mountsAdded = 0;
                bool hadDeclared = (mf.mounts != null && mf.mounts.Length > 0);

                if (hadDeclared)
                {
                    foreach (var m in mf.mounts)
                    {
                        if (m == null) continue;
                        var from = Path.Combine(lm.RootDir, m.from ?? "");
                        var to = (m.to ?? "").Replace("\\", "/").Trim('/');
                        _assets.AddMount(mf.id, from, to, mf.priority);
                        mountsAdded++;
                    }
                }
                else
                {
                    // fallback: 常规目录
                    var autoScripts = Path.Combine(lm.RootDir, "scripts");
                    if (Directory.Exists(autoScripts))
                    {
                        _assets.AddMount(mf.id, autoScripts, "scripts", mf.priority);
                        _log.LogInfo($"[Mods] fallback mount scripts for {mf.id}");
                        mountsAdded++;
                    }
                    var autoAssets = Path.Combine(lm.RootDir, "assets");
                    if (Directory.Exists(autoAssets))
                    {
                        _assets.AddMount(mf.id, autoAssets, "", mf.priority);
                        _log.LogInfo($"[Mods] fallback mount assets for {mf.id}");
                        mountsAdded++;
                    }
                }

                _log.LogInfo($"[Mods] mounted {mf.name} ({mf.id}) prio={mf.priority} mountsAdded={mountsAdded} declared={(hadDeclared ? (mf.mounts?.Length.ToString() ?? "0") : "fallback")}");
                if (mountsAdded == 0)
                    _log.LogWarning($"[Mods] no mounts added for {mf.id}");
                if (!string.IsNullOrEmpty(mf.entry))
                    _log.LogInfo($"[Mods] entry hint (not executed yet): {mf.entry}");
            }

            _log.LogInfo($"[Mods] total loaded: {_loaded.Count}");
        }
    }
}
