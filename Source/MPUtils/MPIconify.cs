using KSP.UI.Screens;
using RUI.Icons.Selectable;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace MPUtils
{
    [KSPAddon(KSPAddon.Startup.EditorAny, true)]
    public class KerbalElectricFilter : BaseFilter
    {
        protected override string Manufacturer
        {
            get { return "Kerbal Electric"; }
            set { }
        }
        protected override string categoryTitle
        {
            get { return "Kerbal Electric"; }
            set { }
        }
    }

    [KSPAddon(KSPAddon.Startup.EditorAny, true)]
    public class FontFactoryFilter : BaseFilter
    {
        protected override string Manufacturer
        {
            get { return "Fengist's Font Factory - Zero Hour"; }
            set { }
        }
        protected override string categoryTitle
        {
            get { return "Fonts"; }
            set { }
        }
    }

    [KSPAddon(KSPAddon.Startup.EditorAny, true)]
    public class LTAFilter : BaseFilter
    {
        protected override string Manufacturer
        {
            get { return "Fengist's Balloon and Mattress Factory"; }// part manufacturer in cfgs and agents files 
            set { }
        }
        protected override string categoryTitle
        {
            get { return "Lighter than Air"; } // the category name 
            set { }
        }
    }

    [KSPAddon(KSPAddon.Startup.EditorAny, true)]
    public class MPFilter : BaseFilter
    {
        protected override string Manufacturer
        {
            get { return "Fengist's Shipyard and Gedunk Shoppe"; }// part manufacturer in cfgs and agents files 
            set { }
        }
        protected override string categoryTitle
        {
            get { return "Maritime Pack"; } // the category name 
            set { }
        }
    }

    [KSPAddon(KSPAddon.Startup.EditorAny, true)]
    public class SteampunkFilter : BaseFilter
    {
        protected override string Manufacturer
        {
            get { return "Fengist's Clock and Watch Factory"; }
            set { }
        }
        protected override string categoryTitle
        {
            get { return "1869"; }
            set { }
        }
    }

    public abstract class BaseFilter : MonoBehaviour
    {
        private readonly List<AvailablePart> parts = new List<AvailablePart>();
        internal string category = "Filter by function";
        internal bool filter = true;
        protected abstract string Manufacturer { get; set; }
        protected abstract string categoryTitle { get; set; }
        
        void Awake()
        {
            if (!MPConfig.MPLoadIcons)
            {
                return;
            }
            parts.Clear();
            var count = PartLoader.LoadedPartsList.Count;
            for (int i = 0; i < count; ++i)
            {
                var avPart = PartLoader.LoadedPartsList[i];
                if (!avPart.partPrefab) continue;
                if (avPart.manufacturer == Manufacturer)
                {
                    parts.Add(avPart);
                }
            }

            print(categoryTitle + "  Filter Count: " + parts.Count);
            if (parts.Count > 0)
                GameEvents.onGUIEditorToolbarReady.Add(SubCategories);
        }

        private bool EditorItemsFilter(AvailablePart avPart)
        {
            return parts.Contains(avPart);
        }

        private void SubCategories()
        {
            var icon = GenIcon(categoryTitle);
            var filter = PartCategorizer.Instance.filters.Find(f => f.button.categorydisplayName == "#autoLOC_453547");//change for 1.3.1
            PartCategorizer.AddCustomSubcategoryFilter(filter, categoryTitle, categoryTitle, icon, EditorItemsFilter);            
        }

        private Icon GenIcon(string iconName)
        {
            Texture2D normIcon = new Texture2D(32, 32, TextureFormat.RGBA32, false), selIcon = new Texture2D(32, 32, TextureFormat.RGBA32, false);
            var normIconFile = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), iconName + "_off.png"); // icon to be present in same folder as dll
            var selIconFile = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), iconName + "_on.png");// icon to be present in same folder as dll

            WWW www = new WWW(normIconFile);
            www.LoadImageIntoTexture(normIcon);
            normIcon.Apply();
            www = new WWW(normIconFile);
            www.LoadImageIntoTexture(selIcon);
            selIcon.Apply();
            www.Dispose();

            var icon = new Icon(iconName + "Icon", normIcon, selIcon);
            return icon;
        }
    }

}