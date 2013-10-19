using Microsoft.Less.Core;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.Utilities;
using System.ComponentModel.Composition;

namespace MadsKristensen.EditorExtensions
{
    [Export(typeof(IVsTextViewCreationListener))]
    [ContentType(LessContentTypeDefinition.LessContentType)]
    [TextViewRole(PredefinedTextViewRoles.Document)]
    class LessViewCreationListener : IVsTextViewCreationListener
    {
        [Import, System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal IVsEditorAdaptersFactoryService EditorAdaptersFactoryService { get; set; }

        public void VsTextViewCreated(IVsTextView textViewAdapter)
        {
            var textView = EditorAdaptersFactoryService.GetWpfTextView(textViewAdapter);

            textView.Properties.GetOrCreateSingletonProperty(() => new LessExtractVariableCommandTarget(textViewAdapter, textView));
            textView.Properties.GetOrCreateSingletonProperty(() => new LessExtractMixinCommandTarget(textViewAdapter, textView));
        }
    }
}
