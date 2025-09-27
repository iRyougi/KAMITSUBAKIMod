using System;
using System.Collections.Generic;
using UnityEngine;

using KAMITSUBAKI.Framework;

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
            public bool OverrideApplied;       // 是否已应用过 override
        }

        private static readonly Dictionary<string, Entry> _byName =
            new Dictionary<string, Entry>(StringComparer.OrdinalIgnoreCase);

        public static void Register(string bookName, UnityEngine.Object obj)
        {
            if (string.IsNullOrEmpty(bookName) || obj == null) return; // 过滤空项

            if (!_byName.TryGetValue(bookName, out var e))
            {
                e = new Entry { Name = bookName, Object = obj };
                _byName[bookName] = e;
                Debug.Log($"[Editor] +Book {bookName}");
            }
            else
            {
                // 如果对象引用变了，允许重新应用 override
                if (!ReferenceEquals(e.Object, obj))
                    e.OverrideApplied = false;

                e.Object = obj;
            }

            // 注册/更新后立即尝试应用 override（只做一次）
            if (!e.OverrideApplied)
            {
                BookOverrideRuntime.TryApplyOnRegister(bookName, obj);
                e.OverrideApplied = true;
            }

            // 通过接口访问（FrameworkPlugin.Texts 是 ITextService）
            FrameworkPlugin.Texts.ApplyOverrideForBook(bookName, obj);
        }

        public static List<Entry> List() => new List<Entry>(_byName.Values);

        public static Entry Get(string bookName)
        {
            if (string.IsNullOrEmpty(bookName)) return null;
            return _byName.TryGetValue(bookName, out var e) ? e : null;
        }
    }
}
