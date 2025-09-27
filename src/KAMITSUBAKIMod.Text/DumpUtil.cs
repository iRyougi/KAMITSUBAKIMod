using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using BepInEx;

namespace KAMITSUBAKIMod.Text
{
    public static class DumpUtil
    {
        private static readonly HashSet<string> _hashset = new HashSet<string>();
        public static string DumpDir => Path.Combine(Paths.PluginPath, "KAMITSUBAKIMod", "dump");

        static DumpUtil()
        {
            if (!Directory.Exists(DumpDir)) Directory.CreateDirectory(DumpDir);
        }

        public static void LogLine(string channel, string text)
        {
            if (string.IsNullOrEmpty(text)) return;
            var key = channel + ":" + Sha1(text);
            if (_hashset.Contains(key)) return;
            _hashset.Add(key);

            var file = Path.Combine(DumpDir, $"{channel}.txt");
            File.AppendAllText(file, text.Replace("\r\n", "\n") + "\n", new UTF8Encoding(false));
        }

        public static void LogPair(string channel, string k, string v)
        {
            var s = $"{k}\t{v}";
            LogLine(channel, s);
        }

        public static void SaveJson(string fileNameSafe, string json)
        {
            if (string.IsNullOrEmpty(json)) return;
            var file = Path.Combine(DumpDir, MakeSafe(fileNameSafe) + ".json");
            File.WriteAllText(file, json, new UTF8Encoding(false));
        }

        private static string Sha1(string s)
        {
            using (var sha1 = SHA1.Create())
            {
                var b = sha1.ComputeHash(Encoding.UTF8.GetBytes(s));
                var sb = new StringBuilder(b.Length * 2);
                foreach (var x in b) sb.Append(x.ToString("x2"));
                return sb.ToString();
            }
        }

        public static string MakeSafe(string name)
        {
            foreach (var c in Path.GetInvalidFileNameChars())
                name = name.Replace(c, '_');
            return name;
        }
    }
}