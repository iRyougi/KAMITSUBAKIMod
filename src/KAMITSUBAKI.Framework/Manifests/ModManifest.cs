namespace KAMITSUBAKI.Framework.Manifests
{
    public class ModManifest
    {
        public string id { get; set; }
        public string name { get; set; }
        public string version { get; set; }
        public string entry { get; set; }   // 可选：代码型 Mod
        public int priority { get; set; } = 100;
        public Mount[] mounts { get; set; } = new Mount[0];

        public class Mount { public string from { get; set; } public string to { get; set; } }
    }
}
