using System;
using System.Collections.Generic;
using System.Windows.Media.Imaging;
using Microsoft.CSS.Core;
using Microsoft.CSS.Editor;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.Web.Editor;

namespace MadsKristensen.EditorExtensions
{
    internal class VendorOrderSmartTagAction : CssSmartTagActionBase
    {
        private readonly ITrackingSpan _span;
        private readonly Declaration _lastVendor;
        private readonly Declaration _standard;
        private readonly ITextView _view;

        public VendorOrderSmartTagAction(ITrackingSpan span, Declaration lastVendor, Declaration standard, ITextView view)
        {
            _span = span;
            _lastVendor = lastVendor;
            _standard = standard;
            _view = view;

            if (Icon == null)
            {
                Icon = BitmapFrame.Create(new Uri("pack://application:,,,/WebEssentials2012;component/Resources/warning.png", UriKind.RelativeOrAbsolute));
            }
        }

        public override string DisplayText
        {
            get { return Resources.VendorOrderSmartTagActionName; }
        }

        public override void Invoke()
        {
            string separator = CssSettings.FormatterBlockBracePosition == BracePosition.Compact ? " " : Environment.NewLine;
            string insert = _lastVendor.Text + separator + _standard.Text;

            EditorExtensionsPackage.DTE.UndoContext.Open(DisplayText);
            _span.TextBuffer.Replace(new Span(_lastVendor.Start, _lastVendor.Length), insert);
            _span.TextBuffer.Delete(new Span(_standard.Start, _standard.Length));
            FormatItem(_lastVendor.Start);
            EditorExtensionsPackage.DTE.UndoContext.Close();
        }

        private void FormatItem(int start)
        {
            IEnumerable<Lazy<IEditorFormatter>> providers = ComponentLocator<IEditorFormatter>.ImportMany();
            foreach (var locator in providers)
            {
                Span span = new Span(start, 1);
                locator.Value.FormatRange(_view, _view.TextBuffer, span);
                break;
            }
        }
    }
}
