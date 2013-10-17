using System;
using System.Globalization;
using System.Windows.Media.Imaging;
using Microsoft.CSS.Core;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;

namespace MadsKristensen.EditorExtensions
{
    internal class MissingStandardDirectiveSmartTagAction : CssSmartTagActionBase
    {
        private readonly ITrackingSpan _span;
        private readonly AtDirective _directive;
        private readonly string _standardName;
        private ITextView _view;

        public MissingStandardDirectiveSmartTagAction(ITrackingSpan span, AtDirective directive, string standardName, ITextView view)
        {
            _span = span;
            _directive = directive;
            _standardName = standardName;
            _view = view;

            if (Icon == null)
            {
                Icon = BitmapFrame.Create(new Uri("pack://application:,,,/WebEssentials2012;component/Resources/warning.png", UriKind.RelativeOrAbsolute));
            }
        }

        public override string DisplayText
        {
            get { return string.Format(CultureInfo.InvariantCulture, Resources.StandardSmartTagActionName, _standardName); }
        }

        public override void Invoke()
        {
            EditorExtensionsPackage.DTE.UndoContext.Open(DisplayText);
            string text = _directive.Text.Replace("@" + _directive.Keyword.Text, _standardName);
            _span.TextBuffer.Insert(_directive.AfterEnd, Environment.NewLine + Environment.NewLine + text);
            EditorExtensionsPackage.DTE.ExecuteCommand("Edit.FormatSelection");
            EditorExtensionsPackage.DTE.UndoContext.Close();
        }
    }
}
