using System;
using System.Collections.Generic;

namespace KAMITSUBAKI.Framework.UI
{
    public static class SettingsRegistry
    { 
        static readonly List<ISettingsWidget> _widgets = new List<ISettingsWidget>(); 
        
        public static void Register(ISettingsWidget w)
        { 
            if (w != null) _widgets.Add(w);
        } 
        
        internal static List<ISettingsWidget> GetSorted()
        { 
            _widgets.Sort((a,b) => {
                int g = string.CompareOrdinal(a.Group, b.Group); 
                return g != 0 ? g : a.Order.CompareTo(b.Order);
            }); 
            return _widgets; 
        } 
    }
}