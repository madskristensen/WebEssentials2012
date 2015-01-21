using System;
using System.IO;
using Microsoft.VisualStudio.Text;

namespace MadsKristensen.EditorExtensions
{
    internal class TypeScriptMargin : MarginBase
    {
        private bool _isReady;
        private FileSystemWatcher _watcher;

        public TypeScriptMargin(string contentType, string source, bool showMargin, ITextDocument document)
            : base(source, "TypeScriptMargin", contentType, showMargin && !document.FilePath.EndsWith(".d.ts", StringComparison.OrdinalIgnoreCase), document)
        {
            SetupWatcher();
        }

        protected override bool CanCompileFileOnSave(string fullPath)
        {
            return true;
        }

        protected override void StartCompiler(string source)
        {
            if (_isFirstRun)
            {
                string file = Path.ChangeExtension(Document.FilePath, ".js");
                UpdateMargin(file);
            }
        }

        private void SetupWatcher()
        {
            string file = Path.ChangeExtension(Document.FilePath, ".js");

            _watcher = new FileSystemWatcher();
            _watcher.Path = Path.GetDirectoryName(file);
            _watcher.Filter = Path.GetFileName(file);
            _watcher.EnableRaisingEvents = true;
            _watcher.Created += FileTouched;
            _watcher.Changed += FileTouched;
        }

        private void FileTouched(object sender, FileSystemEventArgs e)
        {
            UpdateMargin(e.FullPath);
        }

        private void UpdateMargin(string jsFile)
        {
            if (!_isReady && !_isFirstRun)
            {
                // The check for _isReady is in place to deal with the issue where
                // FileSystemWatcher fires twice per file change.
                _isReady = true;
                return;
            }

            if (File.Exists(jsFile))
            {
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    OnCompilationDone(File.ReadAllText(jsFile), jsFile);
                    _isReady = false;
                }), System.Windows.Threading.DispatcherPriority.ApplicationIdle, null);
            }
            else
            {
                OnCompilationDone("// Not compiled to disk yet", jsFile);
                _isReady = false;
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && _watcher != null)
            {
                _watcher.Created -= FileTouched;
                _watcher.Changed -= FileTouched;
                _watcher.Dispose();
                _watcher = null;
            }

            base.Dispose(disposing);
        }

        public override void MinifyFile(string fileName, string source)
        {
            // Nothing to minify
        }

        public override bool IsSaveFileEnabled
        {
            get { return false; }
        }

        public override bool UseCompiledFolder
        {
            get { return false; }
        }

        protected override bool CanWriteToDisk(string source)
        {
            return false;
        }
    }
}