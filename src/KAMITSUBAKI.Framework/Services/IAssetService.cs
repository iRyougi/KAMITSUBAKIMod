using System;
using UnityEngine;

namespace KAMITSUBAKI.Framework.Services
{
    public interface IAssetService
    {
        void AddMount(string modId, string fromDir, string toVirtualRoot, int priority);
        bool TryGetOverrideFile(string virtualPath, out string fullPath);
        bool TryLoadFromVFS(string virtualPath, Type type, out UnityEngine.Object obj); // 同步简版
    }
}
