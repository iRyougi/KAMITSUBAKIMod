using UnityEngine;
using UnityEngine.UI;
using BepInEx.Logging;

namespace KAMITSUBAKI.Framework.UI.Widgets
{
    public class DisplayModeWidget : ISettingsWidget
    {
        public string Group => "Display"; 
        public int Order => 0;
        
        private ManualLogSource _log;
        
        public DisplayModeWidget(ManualLogSource log = null)
        {
            _log = log;
        }

        public void Build(Transform parent)
        {
            // Row container
            var row = new GameObject("DisplayModeRow"); 
            row.transform.SetParent(parent, false);
            var h = row.AddComponent<HorizontalLayoutGroup>(); 
            h.spacing = 8; 
            h.childForceExpandHeight = false; 
            h.childForceExpandWidth = false;
            
            // Find a template text (any existing Text in scene) for font
            Font font = Resources.FindObjectsOfTypeAll<Text>().Length > 0 ? 
                Resources.FindObjectsOfTypeAll<Text>()[0].font : 
                Font.CreateDynamicFontFromOSFont("Arial", 14);
            
            // Label
            var labelGO = new GameObject("Label"); 
            labelGO.transform.SetParent(row.transform, false); 
            var text = labelGO.AddComponent<Text>(); 
            text.font = font; 
            text.text = "ÏÔÊ¾Ä£Ê½"; 
            text.color = Color.white; 
            text.alignment = TextAnchor.MiddleLeft; 
            var lc = labelGO.AddComponent<LayoutElement>(); 
            lc.preferredWidth = 160;
            
            // Dropdown
            var ddGO = new GameObject("ModeDropdown"); 
            ddGO.transform.SetParent(row.transform, false);
            var image = ddGO.AddComponent<Image>(); 
            image.color = new Color(0, 0, 0, 0.35f);
            var dd = ddGO.AddComponent<Dropdown>(); 
            dd.targetGraphic = image; 
            dd.captionText = CreateInnerText(ddGO.transform, font, ""); 
            var templateRT = CreateTemplate(ddGO.transform, font); 
            dd.template = templateRT; 
            dd.itemText = templateRT.GetComponentInChildren<Text>();
            
            dd.options.Add(new Dropdown.OptionData("Fullscreen"));
            dd.options.Add(new Dropdown.OptionData("Borderless"));
            dd.options.Add(new Dropdown.OptionData("Windowed"));
            dd.value = CurrentIndex(); 
            dd.RefreshShownValue();
            dd.onValueChanged.AddListener(i => Apply(i));
        }
        
        int CurrentIndex()
        { 
            var m = Screen.fullScreenMode; 
            if (m == FullScreenMode.ExclusiveFullScreen) return 0; 
            if (m == FullScreenMode.FullScreenWindow) return 1; 
            return 2; 
        }
        
        void Apply(int i)
        { 
            switch(i)
            { 
                case 0: 
                    Screen.SetResolution(Display.main.systemWidth, Display.main.systemHeight, FullScreenMode.ExclusiveFullScreen); 
                    break; 
                case 1: 
                    Screen.SetResolution(Display.main.systemWidth, Display.main.systemHeight, FullScreenMode.FullScreenWindow); 
                    break; 
                default: 
                    Screen.SetResolution(Mathf.RoundToInt(Display.main.systemWidth * 0.8f), 
                                       Mathf.RoundToInt(Display.main.systemHeight * 0.8f), 
                                       FullScreenMode.Windowed); 
                    break;
            } 
            _log?.LogInfo("[Settings] Display mode -> " + i); 
        }
        
        Text CreateInnerText(Transform parent, Font f, string t)
        { 
            var go = new GameObject("Label"); 
            go.transform.SetParent(parent, false); 
            var tx = go.AddComponent<Text>(); 
            tx.font = f; 
            tx.text = t; 
            tx.color = Color.white; 
            tx.alignment = TextAnchor.MiddleLeft; 
            var le = go.AddComponent<LayoutElement>(); 
            le.minHeight = 24; 
            return tx; 
        }
        
        RectTransform CreateTemplate(Transform parent, Font f)
        {
            var tplGO = new GameObject("Template", typeof(RectTransform), typeof(Image), typeof(ScrollRect));
            tplGO.transform.SetParent(parent, false); 
            tplGO.SetActive(false);
            var scroll = tplGO.GetComponent<ScrollRect>();
            var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(Mask), typeof(Image)); 
            viewport.transform.SetParent(tplGO.transform, false); 
            viewport.GetComponent<Image>().color = new Color(0, 0, 0, 0.01f); 
            viewport.GetComponent<Mask>().showMaskGraphic = false; 
            scroll.viewport = viewport.GetComponent<RectTransform>();
            var content = new GameObject("Content", typeof(RectTransform)); 
            content.transform.SetParent(viewport.transform, false); 
            scroll.content = content.GetComponent<RectTransform>();
            
            // item
            var item = new GameObject("Item", typeof(RectTransform), typeof(Toggle)); 
            item.transform.SetParent(content.transform, false);
            var itemBg = new GameObject("Item Background", typeof(RectTransform), typeof(Image)); 
            itemBg.transform.SetParent(item.transform, false);
            var itemCheck = new GameObject("Item Checkmark", typeof(RectTransform), typeof(Image)); 
            itemCheck.transform.SetParent(item.transform, false);
            var itemLabel = new GameObject("Item Label", typeof(RectTransform), typeof(Text)); 
            itemLabel.transform.SetParent(item.transform, false);
            var txt = itemLabel.GetComponent<Text>(); 
            txt.font = f; 
            txt.text = "Option"; 
            txt.color = Color.white; 
            txt.alignment = TextAnchor.MiddleLeft;
            return tplGO.GetComponent<RectTransform>();
        }
    }
}