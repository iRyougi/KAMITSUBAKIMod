using System;
using System.Collections.Generic;
using UnityEngine;

namespace KAMITSUBAKI.Framework.Core
{
    public interface IAssetService
    {
        void AddMount(string modId, string fromDir, string toVirtualRoot, int priority);
        bool TryGetOverrideFile(string virtualPath, out string fullPath);
        bool TryLoadFromVFS(string virtualPath, Type type, out UnityEngine.Object obj); // 同步简版

        // 新增：缓存管理 / 调试支持
        void ClearCache();
        bool RemoveCache(string virtualPath); // 按虚拟路径移除（忽略类型部分）
        IEnumerable<MountInfo> EnumerateMounts();

        public struct MountInfo
        {
            public string ModId;
            public string From;
            public string To;
            public int Priority;
        }
    }
}