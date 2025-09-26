using System;
using System.Collections.Generic;
using UnityEngine;

namespace KAMITSUBAKIMod.Runtime
{
    // 保存已发现的 .book 资源，供 GUI 列表使用
    public static class BookRegistry
    {
        public class Entry
        {
            public string Name;                // 例：story0001.book
            public UnityEngine.Object Object;  // 例：AdvImportBook 实例
            public DateTime FirstSeen = DateTime.UtcNow;
        }

        private static readonly Dictionary<string, Entry> _byName =
            new Dictionary<string, Entry>(StringComparer.OrdinalIgnoreCase);

        public static void Register(string bookName, UnityEngine.Object obj)
        {
            if (string.IsNullOrEmpty(bookName) || obj == null) return; // 过滤空项
            Entry e;
            if (!_byName.TryGetValue(bookName, out e))
            {
                e = new Entry { Name = bookName, Object = obj };
                _byName[bookName] = e;
                Debug.Log($"[Editor] +Book {bookName}");
            }
            else
            {
                e.Object = obj;
                // Debug.Log($"[Editor] ~Book refresh {bookName}");
            }
        }


        public static List<Entry> List() => new List<Entry>(_byName.Values);

        public static Entry Get(string bookName)
        {
            if (string.IsNullOrEmpty(bookName)) return null;
            Entry e; return _byName.TryGetValue(bookName, out e) ? e : null;
        }
    }
}
