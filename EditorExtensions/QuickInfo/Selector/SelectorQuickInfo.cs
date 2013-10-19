using Microsoft.CSS.Core;
using Microsoft.CSS.Editor;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using System;
using System.Collections.Generic;
using System.Text;

namespace MadsKristensen.EditorExtensions
{
    internal class SelectorQuickInfo : IQuickInfoSource
    {
        private SelectorQuickInfoSourceProvider _provider;
        private readonly ITextBuffer _buffer;
        private CssTree _tree;
        private bool _isDisposed;

        public SelectorQuickInfo(SelectorQuickInfoSourceProvider provider, ITextBuffer subjectBuffer)
        {
            _provider = provider;
            _buffer = subjectBuffer;
        }

        public void AugmentQuickInfoSession(IQuickInfoSession session, IList<object> qiContent, out ITrackingSpan applicableToSpan)
        {
            applicableToSpan = null;

            if (!EnsureTreeInitialized() || session == null || qiContent == null)
                return;

            // Map the trigger point down to our buffer.
            SnapshotPoint? point = session.GetTriggerPoint(_buffer.CurrentSnapshot);
            if (!point.HasValue)
                return;

            ParseItem item = _tree.StyleSheet.ItemBeforePosition(point.Value.Position);
            if (item == null || !item.IsValid)
                return;

            Selector sel = item.FindType<Selector>();
            if (sel == null)
                return;

            applicableToSpan = _buffer.CurrentSnapshot.CreateTrackingSpan(item.Start, item.Length, SpanTrackingMode.EdgeNegative);

            string content = GenerateContent(sel);
            qiContent.Add(content);
        }

        private static string GenerateContent(Selector sel)
        {
            SelectorSpecificity specificity = new SelectorSpecificity(sel);

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("Selector specificity:\t\t" + specificity);

            return sb.ToString().Trim();
        }

        /// <summary>
        /// This must be delayed so that the TextViewConnectionListener
        /// has a chance to initialize the WebEditor host.
        /// </summary>
        public bool EnsureTreeInitialized()
        {
            if (_tree == null)
            {
                try
                {
                    CssEditorDocument document = CssEditorDocument.FromTextBuffer(_buffer);
                    _tree = document.Tree;
                }
                catch (Exception)
                {
                }
            }

            return _tree != null;
        }


        public void Dispose()
        {
            if (!_isDisposed)
            {
                GC.SuppressFinalize(this);
                _isDisposed = true;
            }
        }
    }
}
