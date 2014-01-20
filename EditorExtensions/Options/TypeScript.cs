using Microsoft.VisualStudio.Shell;
using System.ComponentModel;

namespace MadsKristensen.EditorExtensions
{
    class TypeScriptOptions : DialogPage
    {
        public TypeScriptOptions()
        {
            Settings.Updated += delegate { LoadSettingsFromStorage(); };
        }

        public override void SaveSettingsToStorage()
        {
            Settings.SetValue(WESettings.Keys.ShowTypeScriptPreviewWindow, ShowTypeScriptPreviewWindow);
            
            Settings.Save();
        }

        public override void LoadSettingsFromStorage()
        {
            ShowTypeScriptPreviewWindow = WESettings.GetBoolean(WESettings.Keys.ShowTypeScriptPreviewWindow);
        }
        
        [LocDisplayName("Show preview window")]
        [Description("Show the preview window when editing a CoffeeScript file.")]
        [Category("CoffeeScript")]
        public bool ShowTypeScriptPreviewWindow { get; set; }
    }
}
