using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using Microsoft.CSS.Core;
using Microsoft.CSS.Editor;
using Microsoft.VisualStudio.Utilities;

namespace MadsKristensen.EditorExtensions
{
    [Export(typeof(ICssCompletionContextProvider))]
    [Name("IdCompletionContextProvider")]
    internal class IdCompletionContextProvider : ICssCompletionContextProvider
    {
        public IEnumerable<Type> ItemTypes
        {
            get
            {
                return new[] { typeof(IdSelector), };
            }
        }

        public CssCompletionContext GetCompletionContext(ParseItem item, int position)
        {
            return new CssCompletionContext((CssCompletionContextType)603, item.Start, item.Length, null);
        }
    }
}
