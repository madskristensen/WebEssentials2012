using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Editor.DragDrop;
using Microsoft.VisualStudio.Utilities;

namespace MadsKristensen.EditorExtensions
{
    [Export(typeof(IDropHandlerProvider))]
    [DropFormat("FileDrop")]
    [DropFormat("CF_VSSTGPROJECTITEMS")]
    [Name("FontDropHandler")]
    [ContentType("CSS")]
    [ContentType("LESS")]
    [Order(Before = "DefaultFileDropHandler")]
    internal class FontDropHandlerProvider : IDropHandlerProvider
    {
        public IDropHandler GetAssociatedDropHandler(IWpfTextView view)
        {
            return view.Properties.GetOrCreateSingletonProperty<FontDropHandler>(() => new FontDropHandler(view));
        }
    }

    internal class FontDropHandler : IDropHandler
    {
        readonly IWpfTextView _view;
        private readonly Dictionary<string, string> _formats = new Dictionary<string, string>() 
        {
            {".ttf", " format('truetype')"},
            {".woff", ""},
            {".eot", ""},
            {".otf", " format('opentype')"}
        };
        private string _draggedFilename;
        private string _fontName = string.Empty;
        const string _fontFace = "@font-face {{\n\tfont-family: {0};\n\tsrc: {1};\n}}";
        const string _fontUrls = "url('{0}'){1}";
        //ITextDocument document;

        public FontDropHandler(IWpfTextView view)
        {
            this._view = view;
        }

        public DragDropPointerEffects HandleDataDropped(DragDropInfo dragDropInfo)
        {
            if (File.Exists(_draggedFilename))
            {
                string fontFamily;
                _view.TextBuffer.Insert(dragDropInfo.VirtualBufferPosition.Position.Position, GetCodeFromFile(_draggedFilename, out fontFamily));

                return DragDropPointerEffects.Copy;
            }
            else if (_draggedFilename.StartsWith("http://localhost:"))
            {

                _view.TextBuffer.Insert(dragDropInfo.VirtualBufferPosition.Position.Position, GetCodeFromLocalhost(_draggedFilename));

                return DragDropPointerEffects.Copy;
            }
            else
            {
                return DragDropPointerEffects.None;
            }
        }

        public string GetCodeFromFile(string fileName, out string fontFamily)
        {
            var files = GetRelativeFiles(fileName);
            string[] sources = new string[files.Count()];

            for (int i = 0; i < files.Count(); i++)
            {
                string file = files.ElementAt(i);
                string extension = Path.GetExtension(file).ToLowerInvariant();
                string reference = FileHelpers.RelativePath(EditorExtensionsPackage.DTE.ActiveDocument.FullName, file);

                if (reference.StartsWith("http://localhost:"))
                {
                    int index = reference.IndexOf('/', 24);
                    if (index > -1)
                        reference = reference.Substring(index + 1).ToLowerInvariant();
                }

                sources[i] = string.Format(_fontUrls, reference, _formats[extension]);
            }
            
            string sourceUrls = string.Join(", ", sources);
            fontFamily = _fontName;
            return string.Format(_fontFace, _fontName, sourceUrls);
        }

        private string GetCodeFromLocalhost(string fileName)
        {
            int index = _draggedFilename.IndexOf('/', 24);
            if (index > -1)
                _draggedFilename = _draggedFilename.Substring(index).ToLowerInvariant();

            string extension = Path.GetExtension(_draggedFilename).ToLowerInvariant();
            string sourceUrl = string.Format(_fontUrls, _draggedFilename, _formats[extension]);

            return string.Format(_fontFace, "MyFontName", sourceUrl);
        }

        private IEnumerable<string> GetRelativeFiles(string fileName)
        {
            var fi = new FileInfo(fileName);
            _fontName = fi.Name.Replace(fi.Extension, string.Empty);
            foreach (var file in fi.Directory.GetFiles(_fontName + ".*"))
            {
                string extension = file.Extension.ToLowerInvariant();
                if (_formats.ContainsKey(extension))
                    yield return file.FullName;
            }
        }

        public void HandleDragCanceled()
        {
        }

        public DragDropPointerEffects HandleDragStarted(DragDropInfo dragDropInfo)
        {
            return DragDropPointerEffects.All;
        }

        public DragDropPointerEffects HandleDraggingOver(DragDropInfo dragDropInfo)
        {
            return DragDropPointerEffects.All;
        }

        public bool IsDropEnabled(DragDropInfo dragDropInfo)
        {
            _draggedFilename = GetImageFilename(dragDropInfo);

            if (!string.IsNullOrEmpty(_draggedFilename))
            {
                string fileExtension = Path.GetExtension(_draggedFilename).ToLowerInvariant();
                if (this._formats.ContainsKey(fileExtension))
                {
                    return true;
                }
            }

            return false;
        }

        public static string GetImageFilename(DragDropInfo info)
        {
            DataObject data = new DataObject(info.Data);

            if (info.Data.GetDataPresent("FileDrop"))
            {
                // The drag and drop operation came from the file system
                StringCollection files = data.GetFileDropList();

                if (files != null && files.Count == 1)
                {
                    return files[0];
                }
            }
            else if (info.Data.GetDataPresent("CF_VSSTGPROJECTITEMS"))
            {
                return data.GetText();
            }

            return null;
        }

    }
}
