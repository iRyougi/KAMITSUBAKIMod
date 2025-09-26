using System.Collections.Generic;
using System.IO;
using System.Text;
using BepInEx;

namespace KAMITSUBAKIMod.Text
{
    public static class TextBookMap
    {
        public static readonly Dictionary<string, string> Map = new Dictionary<string, string>();

        public static string MapFile
        {
            get
            {
                return Path.Combine(Paths.PluginPath, "KAMITSUBAKIMod", "text", "replace.csv");
            }
        }

        public static void Load()
        {
            Map.Clear();
            var dir = Path.GetDirectoryName(MapFile);
            if (string.IsNullOrEmpty(dir) == false && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            if (!File.Exists(MapFile))
            {
                File.WriteAllText(MapFile, "From,To\n开始,Start\n", new UTF8Encoding(false));
            }

            using (var sr = new StreamReader(MapFile, Encoding.UTF8, true))
            {
                string line = null;
                bool first = true;
                while ((line = sr.ReadLine()) != null)
                {
                    if (first)
                    {
                        first = false;
                        if (line.StartsWith("From")) continue;
                    }

                    int idx = line.IndexOf(',');
                    if (idx < 0) continue;

                    string from = line.Substring(0, idx);
                    string to = line.Substring(idx + 1);

                    if (!Map.ContainsKey(from))
                        Map[from] = to;
                }
            }
            BepInEx.Logging.Logger.CreateLogSource("KAMITSUBAKIMod")
                .LogInfo("Loaded " + Map.Count + " replace pair(s).");
        }

        public static string ApplySimple(string s)
        {
            if (string.IsNullOrEmpty(s) || Map.Count == 0) return s;
            foreach (var kv in Map)
                s = s.Replace(kv.Key, kv.Value);
            return s;
        }
    }
}
