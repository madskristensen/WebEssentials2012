using CssSorter;
using Microsoft.CSS.Core;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using System;
using System.Windows.Media.Imaging;

namespace MadsKristensen.EditorExtensions
{
    internal class SortSmartTagAction : CssSmartTagActionBase
    {
        private readonly RuleSet _rule;
        private readonly ITrackingSpan _span;
        private readonly ITextView _view;

        public SortSmartTagAction(RuleSet rule, ITrackingSpan span, ITextView view)
        {
            _rule = rule;
            _span = span;
            _view = view;

            if (Icon == null)
            {
                Icon = BitmapFrame.Create(new Uri("pack://application:,,,/WebEssentials2012;component/Resources/sort.png", UriKind.RelativeOrAbsolute));
            }
        }

        public override string DisplayText
        {
            get { return "Sort Properties"; }
        }

        public override void Invoke()
        {
            Sorter sorter = new Sorter();

            if (_view.TextBuffer.ContentType.IsOfType("LESS"))
            {
                sorter.SortLess(_rule.Text);
            }
            else
            {
                sorter.SortStyleSheet(_rule.Text);
            }

            var position = _view.Caret.Position.BufferPosition.Position;

            EditorExtensionsPackage.DTE.UndoContext.Open(DisplayText);

            _view.Caret.MoveTo(new SnapshotPoint(_span.TextBuffer.CurrentSnapshot, position));
            EditorExtensionsPackage.ExecuteCommand("Edit.FormatSelection");

            EditorExtensionsPackage.DTE.UndoContext.Close();
        }
    }
}
