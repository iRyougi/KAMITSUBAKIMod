using UnityEngine;

namespace KAMITSUBAKI.Framework.UI
{
    public interface ISettingsWidget 
    { 
        string Group { get; } 
        int Order { get; } 
        void Build(Transform parent); 
    }
}