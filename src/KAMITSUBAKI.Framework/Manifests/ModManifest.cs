namespace KAMITSUBAKI.Framework.Manifests
{
    [System.Serializable]
    public class ModManifest
    {
        public string id;
        public string name;
        public string version;
        public string entry;     // 可为 null
        public int priority = 100;
        public Mount[] mounts;

        [System.Serializable]
        public class Mount
        {
            public string from;
            public string to;
        }
    }
}
