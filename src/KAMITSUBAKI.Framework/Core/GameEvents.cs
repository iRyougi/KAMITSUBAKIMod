using System;

namespace KAMITSUBAKI.Framework.Core
{
    public static class GameEvents
    {
        // 在书本注册时触发（由你的 BookRegistry 调用）
        public static event Action<string, UnityEngine.Object> BookRegistered;

        internal static void RaiseBookRegistered(string name, UnityEngine.Object obj)
            => BookRegistered?.Invoke(name, obj);
    }
}
