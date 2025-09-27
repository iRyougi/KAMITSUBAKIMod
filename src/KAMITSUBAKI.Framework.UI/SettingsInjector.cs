using System;
using UnityEngine;
using UnityEngine.UI;
using BepInEx.Logging;

namespace KAMITSUBAKI.Framework.UI
{
    public static class SettingsInjector
    { 
        public static void Inject(Transform root, ManualLogSource log = null)
        { 
            if (root == null) return; 
            if (root.Find("__KMFW_SettingsInjected") != null) return; 
            
            var marker = new GameObject("__KMFW_SettingsInjected"); 
            marker.transform.SetParent(root, false); 
            
            foreach(var w in SettingsRegistry.GetSorted())
            { 
                try 
                { 
                    var groupGO = new GameObject($"KMFW_Group_{w.Group}"); 
                    groupGO.transform.SetParent(root, false); 
                    var layout = groupGO.AddComponent<VerticalLayoutGroup>(); 
                    layout.spacing = 4; 
                    w.Build(groupGO.transform); 
                    log?.LogInfo($"[Settings] built widget Group={w.Group} Type={w.GetType().Name}"); 
                } 
                catch(Exception ex)
                { 
                    log?.LogWarning("[Settings] widget build failed: " + ex.Message);
                } 
            } 
        } 
    }
}