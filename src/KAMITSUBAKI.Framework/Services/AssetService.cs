using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using BepInEx.Logging;

namespace KAMITSUBAKI.Framework.Services
{
    internal class AssetService : IAssetService
    {
        class Mount { public string ModId; public string From; public string To; public int Priority; }
        readonly List<Mount> _mounts = new();
        readonly Dictionary<string, UnityEngine.Object> _cache = new();
        readonly ManualLogSource _log;
        readonly string _workspace; // 框架工作区：BepInEx/plugins/KAMITSUBAKIMod/assets
        public AssetService(ManualLogSource log)
        {
            _log = log;
            _workspace = Path.Combine(Paths.PluginPath, "KAMITSUBAKIMod", "assets");
            if (!Directory.Exists(_workspace)) Directory.CreateDirectory(_workspace);

            // 工作区挂载（优先级 150）
            AddMount("workspace", _workspace, "", 150);
        }

        public void AddMount(string modId, string fromDir, string toVirtualRoot, int priority)
        {
            _mounts.Add(new Mount
            {
                ModId = modId,
                From = Path.GetFullPath(fromDir ?? "."),
                To = toVirtualRoot?.Replace("\\", "/") ?? "",
                Priority = priority
            });
            _mounts.Sort((a, b) => b.Priority.CompareTo(a.Priority));
        }

        public bool TryGetOverrideFile(string virtualPath, out string fullPath)
        {
            virtualPath = (virtualPath ?? "").Replace("\\", "/");
            foreach (var m in _mounts)
            {
                string rel = string.IsNullOrEmpty(m.To) ? virtualPath
                    : virtualPath.StartsWith(m.To + "/", StringComparison.OrdinalIgnoreCase)
                        ? virtualPath.Substring(m.To.Length + 1) : null;
                if (rel == null) continue;

                var candidate = Path.Combine(m.From, rel);
                if (File.Exists(candidate)) { fullPath = candidate; return true; }
            }
            fullPath = null; return false;
        }

        public bool TryLoadFromVFS(string virtualPath, Type type, out UnityEngine.Object obj)
        {
            obj = null;
            if (string.IsNullOrEmpty(virtualPath)) return false;
            if (_cache.TryGetValue(CacheKey(virtualPath, type), out obj)) return true;

            if (TryGetOverrideFile(virtualPath, out var file))
            {
                obj = LoadFileAsUnityObject(file, type);
                if (obj != null)
                {
                    _cache[CacheKey(virtualPath, type)] = obj;
                    _log.LogInfo($"[VFS] hit {virtualPath} -> {file}");
                    return true;
                }
            }
            return false;
        }

        static string CacheKey(string p, Type t) => $"{t?.Name}:{p}";

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
                // 建议用异步 UWR 版本；这里先返回 null，后续补 Async 解析器（OGG/WAV）
                return null;
            }
            return null;
        }
    }
}
