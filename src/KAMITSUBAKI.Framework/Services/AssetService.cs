using System;
using System.Collections.Generic;
using System.IO;
using BepInEx;
using BepInEx.Logging;
using UnityEngine;
using static UnityEngine.ImageConversion; // ← for Texture2D.LoadImage

namespace KAMITSUBAKI.Framework.Services
{
    internal class AssetService : IAssetService
    {
        class Mount { public string ModId; public string From; public string To; public int Priority; }
        readonly List<Mount> _mounts = new List<Mount>();
        readonly Dictionary<string, UnityEngine.Object> _cache = new Dictionary<string, UnityEngine.Object>();
        readonly ManualLogSource _log;
        readonly string _workspace;

        public AssetService(ManualLogSource log)
        {
            _log = log;
            _workspace = Path.Combine(Paths.PluginPath, "KAMITSUBAKIMod", "assets");
            if (!Directory.Exists(_workspace)) Directory.CreateDirectory(_workspace);
            AddMount("workspace", _workspace, "", 150);
        }

        public void AddMount(string modId, string fromDir, string toVirtualRoot, int priority)
        {
            var mount = new Mount
            {
                ModId = modId,
                From = Path.GetFullPath(fromDir ?? "."),
                To = (toVirtualRoot ?? "").Replace("\\", "/"),
                Priority = priority
            };
            _mounts.Add(mount);
            _mounts.Sort((a, b) => b.Priority.CompareTo(a.Priority));
            _log.LogInfo($"[VFS] mount add: {mount.ModId} from='{mount.From}' -> '{mount.To}' prio={mount.Priority} exists={(Directory.Exists(mount.From) ? 1:0)} total={_mounts.Count}");
        }

        public bool TryGetOverrideFile(string virtualPath, out string fullPath)
        {
            virtualPath = (virtualPath ?? "").Replace("\\", "/");
            for (int i = 0; i < _mounts.Count; i++)
            {
                var m = _mounts[i];
                string rel;
                if (string.IsNullOrEmpty(m.To)) rel = virtualPath;
                else if (virtualPath.StartsWith(m.To + "/", StringComparison.OrdinalIgnoreCase))
                    rel = virtualPath.Substring(m.To.Length + 1);
                else rel = null;

                if (rel == null) continue;

                var candidate = Path.Combine(m.From, rel);
                if (File.Exists(candidate))
                {
                    fullPath = candidate;
                    _log.LogInfo($"[VFS] hit {virtualPath} => {candidate} (mod={m.ModId})");
                    return true;
                }
                else
                {
                    _log.LogDebug($"[VFS] miss {virtualPath} tried {candidate} (mod={m.ModId})");
                }
            }
            fullPath = null; return false;
        }

        public bool TryLoadFromVFS(string virtualPath, Type type, out UnityEngine.Object obj)
        {
            obj = null;
            if (string.IsNullOrEmpty(virtualPath)) return false;

            string key = (type != null ? type.Name : "Any") + ":" + virtualPath;
            if (_cache.TryGetValue(key, out obj)) return true;

            string file;
            if (TryGetOverrideFile(virtualPath, out file))
            {
                obj = LoadFileAsUnityObject(file, type);
                if (obj != null)
                {
                    _cache[key] = obj;
                    _log.LogInfo("[VFS] load " + virtualPath + " -> " + file);
                    return true;
                }
            }
            return false;
        }

        static UnityEngine.Object LoadFileAsUnityObject(string path, Type type)
        {
            if (type == typeof(Texture2D) || type == typeof(Sprite))
            {
                var bytes = File.ReadAllBytes(path);
                var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                if (tex.LoadImage(bytes))
                {
                    if (type == typeof(Sprite))
                        return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
                    return tex;
                }
            }
            if (type == typeof(AudioClip))
            {
                return null;
            }
            return null;
        }
    }
}
