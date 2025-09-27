using System;
using System.Collections.Generic;
using UnityEngine;

namespace KAMITSUBAKI.Framework.Core
{
    public interface IAssetService
    {
        void AddMount(string modId, string fromDir, string toVirtualRoot, int priority);
        bool TryGetOverrideFile(string virtualPath, out string fullPath);
        bool TryLoadFromVFS(string virtualPath, Type type, out UnityEngine.Object obj); // ͬ�����

        // ������������� / ����֧��
        void ClearCache();
        bool RemoveCache(string virtualPath); // ������·���Ƴ����������Ͳ��֣�
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