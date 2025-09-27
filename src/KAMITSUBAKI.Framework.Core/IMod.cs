namespace KAMITSUBAKI.Framework.Core
{
    public interface IMod
    {
        void Initialize(IModContext ctx);
        void Start();
        void Unload();
    }
}