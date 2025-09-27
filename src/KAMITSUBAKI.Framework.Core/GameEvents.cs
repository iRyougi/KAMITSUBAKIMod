using System;

namespace KAMITSUBAKI.Framework.Core
{
    public static class GameEvents
    {
        // ���鱾ע��ʱ����������� BookRegistry ���ã�
        public static event Action<string, UnityEngine.Object> BookRegistered;

        internal static void RaiseBookRegistered(string name, UnityEngine.Object obj)
            => BookRegistered?.Invoke(name, obj);
    }
}