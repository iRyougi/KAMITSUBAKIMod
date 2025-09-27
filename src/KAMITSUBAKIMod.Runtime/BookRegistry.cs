using System;
using System.Collections.Generic;
using UnityEngine;

namespace KAMITSUBAKIMod.Runtime
{
    // �����ѷ��ֵ� .book ��Դ���� GUI �б�ʹ��
    public static class BookRegistry
    {
        public class Entry
        {
            public string Name;                // ����story0001.book
            public UnityEngine.Object Object;  // ����AdvImportBook ʵ��
            public DateTime FirstSeen = DateTime.UtcNow;
            public bool OverrideApplied;       // �Ƿ���Ӧ�ù� override
        }

        private static readonly Dictionary<string, Entry> _byName =
            new Dictionary<string, Entry>(StringComparer.OrdinalIgnoreCase);

        public static void Register(string bookName, UnityEngine.Object obj)
        {
            if (string.IsNullOrEmpty(bookName) || obj == null) return; // ���˿���

            if (!_byName.TryGetValue(bookName, out var e))
            {
                e = new Entry { Name = bookName, Object = obj };
                _byName[bookName] = e;
                Debug.Log($"[Editor] +Book {bookName}");
            }
            else
            {
                // ����������ñ��ˣ���������Ӧ�� override
                if (!ReferenceEquals(e.Object, obj))
                    e.OverrideApplied = false;

                e.Object = obj;
            }

            // ע��/���º���������Ӧ�� override��ֻ��һ�Σ�
            if (!e.OverrideApplied)
            {
                BookOverrideRuntime.TryApplyOnRegister(bookName, obj);
                e.OverrideApplied = true;
            }

            // ����ͨ��Framework�ӿ�Ӧ��override�������ܿ��ã�
            try
            {
                var textService = KAMITSUBAKI.Framework.FrameworkPlugin.Texts;
                textService?.ApplyOverrideForBook(bookName, obj);
            }
            catch (System.Exception)
            {
                // Framework����δ���أ���Ĭ����
            }
        }

        public static List<Entry> List() => new List<Entry>(_byName.Values);

        public static Entry Get(string bookName)
        {
            if (string.IsNullOrEmpty(bookName)) return null;
            return _byName.TryGetValue(bookName, out var e) ? e : null;
        }
    }
}