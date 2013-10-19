using System;
using System.Windows.Media.Imaging;
using Microsoft.CSS.Core;
using Microsoft.VisualStudio.Text;

namespace MadsKristensen.EditorExtensions
{
    internal class SelectorHackSmartTagAction : CssSmartTagActionBase
    {
        private readonly ITrackingSpan _span;
        private readonly Selector _selector;
        private readonly string _hack;
        private readonly string _displayText;

        public SelectorHackSmartTagAction(ITrackingSpan span, Selector url, string hackPrefix, string displayText)
        {
            _span = span;
            _selector = url;
            _hack = hackPrefix;
            _displayText = displayText;
            
            if (Icon == null)
            {
                Icon = BitmapFrame.Create(new Uri("pack://application:,,,/WebEssentials2012;component/Resources/skull.png", UriKind.RelativeOrAbsolute));
            }
        }
        
        public override string DisplayText
        {
            get { return this._displayText; }
        }

        public override void Invoke()
        {
            EditorExtensionsPackage.DTE.UndoContext.Open(DisplayText);
            _span.TextBuffer.Replace(_span.GetSpan(_span.TextBuffer.CurrentSnapshot), _hack + _selector.Text);
            EditorExtensionsPackage.DTE.UndoContext.Close();
        }
    }
}
