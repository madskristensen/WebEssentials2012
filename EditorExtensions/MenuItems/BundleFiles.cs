using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Design;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Threading;
using System.Xml;

namespace MadsKristensen.EditorExtensions
{
    [Export(typeof(IWpfTextViewCreationListener))]
    [ContentType("CSS")]
    [ContentType("JavaScript")]
    [ContentType("XML")]
    [TextViewRole(PredefinedTextViewRoles.Document)]
    internal class BundleFilesMenu : IWpfTextViewCreationListener
    {
        private static DTE2 _dte;
        private readonly OleMenuCommandService _mcs;
        public const string _ext = ".bundle";

        public BundleFilesMenu()
        {
            // Used by the IWpfTextViewCreationListener
            _dte = EditorExtensionsPackage.DTE;
        }

        public BundleFilesMenu(DTE2 dte, OleMenuCommandService mcs)
        {
            _dte = dte;
            _mcs = mcs;
        }

        public void TextViewCreated(IWpfTextView textView)
        {
            ITextDocument document;
            textView.TextDataModel.DocumentBuffer.Properties.TryGetProperty(typeof(ITextDocument), out document);

            if (document != null)
            {
                document.FileActionOccurred += document_FileActionOccurred;
            }
        }

        private void document_FileActionOccurred(object sender, TextDocumentFileActionEventArgs e)
        {
            if (e.FileActionType == FileActionTypes.ContentSavedToDisk)
            {
                string file = e.FilePath.EndsWith(_ext) ? null : e.FilePath;

                System.Threading.Tasks.Task.Run(() => UpdateBundles(file, file == null));
            }
        }

        public static void UpdateBundles(string changedFile, bool isBuild)
        {
            if (string.IsNullOrEmpty(changedFile))
            {
                foreach (Project project in EditorExtensionsPackage.DTE.Solution.Projects)
                {
                    UpdateBundle(project, isBuild);
                }
            }
            else
            {
                ProjectItem item = EditorExtensionsPackage.DTE.Solution.FindProjectItem(changedFile);
                UpdateBundle(item.ContainingProject, isBuild);
            }
        }

        /// <summary>
        /// Update all bundles in the provided project.
        /// </summary>
        /// <param name="project">The project to search in.</param>
        /// <param name="isBuild">Is this a build time event.</param>
        private static void UpdateBundle(Project project, bool isBuild)
        {
            if (project != null && 
                project.ProjectItems != null &&
                project.Properties != null)
            {
                Property full_path = project.Properties.Item("FullPath");
                if(full_path == null || full_path.Value == null)
                    return;

                var root_folder = full_path.Value.ToString();

                if(string.IsNullOrWhiteSpace(root_folder))
                    return;

                UpdateBundle(root_folder, project.ProjectItems, isBuild);
            }
        }

        /// <summary>
        /// Update all the bundles in the amond the provided project items.
        /// </summary>
        /// <param name="rootFolder">The root folder of the project.</param>
        /// <param name="projectItems">The projects items to search amongst.</param>
        /// <param name="isBuild">Is this a build time event.</param>
        private static void UpdateBundle(string rootFolder, ProjectItems projectItems, bool isBuild)
        {
            if(projectItems == null) return;

            foreach (ProjectItem project_item in projectItems)
            {
                UpdateBundle(rootFolder, project_item, isBuild);
            }
        }

        /// <summary>
        /// Update all the bundles in the provided project item.
        /// </summary>
        /// <remarks>The project hierarchy is recursive, so we need to do this recursively too.</remarks>
        /// <param name="rootFolder">The root folder of the project.</param>
        /// <param name="projectItems">The projects item to search amongst.</param>
        /// <param name="isBuild">Is this a build time event.</param>
        private static void UpdateBundle(string rootFolder, ProjectItem projectItem, bool isBuild)
        {
            for (short i = 0; i < projectItem.FileCount; i++)
            {
                var filename = projectItem.FileNames[i];
                UpdateBundle(rootFolder, filename, isBuild);
            }
            UpdateBundle(rootFolder, projectItem.ProjectItems, isBuild);
        }

        /// <summary>
        /// Update the following bundle file.
        /// </summary>
        /// <remarks>
        /// This method will update the bundle file only if the filename is actually a bundle file.
        /// Otherwise does nothing.
        /// It also skips the app_data folder.
        /// </remarks>
        /// <param name="rootFolder">The root folder of the project.</param>
        /// <param name="changedFile">The bundle file to update.</param>
        /// <param name="isBuild">Is this a build time event.</param>
        private static void UpdateBundle(string rootFolder, string changedFile, bool isBuild)
        {
            if (changedFile.IndexOf("\\app_data\\", StringComparison.OrdinalIgnoreCase) > -1)
                return;

            if(Path.GetExtension(changedFile) != _ext)
                return;

            XmlDocument doc = GetXmlDocument(changedFile);
            bool enabled = false;

            if (doc != null)
            {
                XmlNode bundleNode = doc.SelectSingleNode("//bundle");
                if (bundleNode == null)
                    return;

                XmlNodeList nodes = doc.SelectNodes("//file");
                foreach (XmlNode node in nodes)
                {
                    string relative = node.InnerText;
                    string absolute = ProjectHelpers.ToAbsoluteFilePath(relative, rootFolder).Replace("/", "\\").Replace("\\\\", "\\");

                    if (absolute.Equals(changedFile.Replace("\\\\", "\\"), StringComparison.OrdinalIgnoreCase))
                    {
                        enabled = true;
                        break;
                    }
                }

                if (isBuild && bundleNode.Attributes != null && bundleNode.Attributes["runOnBuild"] != null && bundleNode.Attributes["runOnBuild"].InnerText == "true")
                {
                    enabled = true;
                }

                if (enabled)
                {
                    WriteBundleFile(changedFile, doc);
                }
            }
        }

        private static XmlDocument GetXmlDocument(string filePath)
        {
            try
            {
                XmlDocument doc = new XmlDocument();
                doc.LoadXml(File.ReadAllText(filePath));
                return doc;
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
                return null;
            }
        }

        public void SetupCommands()
        {
            CommandID commandCss = new CommandID(GuidList.guidBundleCmdSet, (int)PkgCmdIDList.BundleCss);
            OleMenuCommand menuCommandCss = new OleMenuCommand((s, e) => CreateBundlefile(".css"), commandCss);
            menuCommandCss.BeforeQueryStatus += (s, e) => { BeforeQueryStatus(s, ".css"); };
            _mcs.AddCommand(menuCommandCss);

            CommandID commandJs = new CommandID(GuidList.guidBundleCmdSet, (int)PkgCmdIDList.BundleJs);
            OleMenuCommand menuCommandJs = new OleMenuCommand((s, e) => CreateBundlefile(".js"), commandJs);
            menuCommandJs.BeforeQueryStatus += (s, e) => { BeforeQueryStatus(s, ".js"); };
            _mcs.AddCommand(menuCommandJs);
        }

        private void BeforeQueryStatus(object sender, string extension)
        {
            OleMenuCommand menuCommand = sender as OleMenuCommand;

            menuCommand.Enabled = GetSelectedItems(extension).Count() > 1;
        }

        private IEnumerable<ProjectItem> GetSelectedItems(string extension)
        {
            return ProjectHelpers.GetSelectedItems().Where(p => Path.GetExtension(p.FileNames[1]) == extension);
        }

        private void CreateBundlefile(string extension)
        {
            StringBuilder sb = new StringBuilder();
            string firstFile = null;
            var items = GetSelectedItems(extension);

            foreach (ProjectItem item in items)
            {
                if (string.IsNullOrEmpty(firstFile))
                    firstFile = item.FileNames[1];

                string content = File.ReadAllText(item.FileNames[1]);
                sb.AppendLine(content);
            }

            if (firstFile != null)
            {
                string dir = Path.GetDirectoryName(firstFile);


                dir = GetProjectRelativeFolder(items.ElementAt(0));

                if (Directory.Exists(dir))
                {
                    string bundleFile = Microsoft.VisualBasic.Interaction.InputBox("Specify the name of the bundle", "Web Essentials", "bundle1");

                    if (!bundleFile.EndsWith(_ext, StringComparison.OrdinalIgnoreCase))
                        bundleFile += extension + _ext;

                    string bundlePath = Path.Combine(dir, bundleFile);

                    if (File.Exists(bundlePath))
                    {
                        MessageBox.Show("The file already exist", "Web Essentials", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                    else
                    {
                        Dispatcher.CurrentDispatcher.BeginInvoke(
                            new Action(() => WriteFile(bundlePath, items, extension, bundleFile.Replace(_ext, string.Empty))),
                        DispatcherPriority.ApplicationIdle, null);
                    }
                }
            }
        }

        private static string GetProjectRelativeFolder(ProjectItem item)
        {
            object parent = item.Collection.Parent;
            ProjectItem folder = parent as ProjectItem;
            Project project = parent as Project;

            if (folder != null)
            {
                return folder.FileNames[1];
            }
            else if (project != null)
            {
                return project.FullName;
            }

            return null;
        }

        private void WriteFile(string filePath, IEnumerable<ProjectItem> files, string extension, string output)
        {
            string projectRoot = ProjectHelpers.GetProjectFolder(files.ElementAt(0).FileNames[1]);
            StringBuilder sb = new StringBuilder();
            XmlWriterSettings settings = new XmlWriterSettings();
            settings.Indent = true;

            using (XmlWriter writer = XmlWriter.Create(sb, settings))
            {
                writer.WriteStartElement("bundle");
                writer.WriteAttributeString("minify", "true");
                writer.WriteAttributeString("runOnBuild", "true");
                writer.WriteAttributeString("output", output);
                writer.WriteComment("The order of the <file> elements determines the order of them when bundled.");

                foreach (ProjectItem item in files)
                {
                    string relative = item.IsLink() ? item.FileNames[1] : "/" + FileHelpers.RelativePath(projectRoot, item.FileNames[1]);
                    writer.WriteElementString("file", relative);
                }

                writer.WriteEndElement();
            }

            sb.Replace(Encoding.Unicode.WebName, Encoding.UTF8.WebName);

            ProjectHelpers.CheckOutFileFromSourceControl(filePath);
            File.WriteAllText(filePath, sb.ToString());
            ProjectHelpers.AddFileToActiveProject(filePath, "None");

            _dte.ItemOperations.OpenFile(filePath);

            XmlDocument doc = GetXmlDocument(filePath);

            if (doc != null)
            {
                Dispatcher.CurrentDispatcher.BeginInvoke(
                    new Action(() => WriteBundleFile(filePath, doc)), DispatcherPriority.ApplicationIdle, null);
            }
        }

        private static void WriteBundleFile(string filePath, XmlDocument doc)
        {
            XmlNode bundleNode = doc.SelectSingleNode("//bundle");

            if (bundleNode == null)
                return;

            XmlNode outputAttr = bundleNode.Attributes["output"];

            if (outputAttr != null && (outputAttr.InnerText.Contains("/") || outputAttr.InnerText.Contains("\\")))
            {
                MessageBox.Show("The 'output' attribute is for file names only - not paths", "Web Essentials");
                return;
            }

            Dictionary<string, string> files = new Dictionary<string, string>();
            string extension = Path.GetExtension(filePath.Replace(_ext, string.Empty));
            XmlNodeList nodes = doc.SelectNodes("//file");

            foreach (XmlNode node in nodes)
            {
                string absolute = ProjectHelpers.ToAbsoluteFilePath(node.InnerText, ProjectHelpers.GetProjectFolder(filePath)).Replace("\\\\", "\\");

                if (node.InnerText.Contains(":\\") || node.InnerText.StartsWith("\\\\"))
                {
                    absolute = node.InnerText;
                }

                if (File.Exists(absolute))
                {
                    if (!files.ContainsKey(absolute))
                        files.Add(absolute, node.InnerText);
                }
                else
                {
                    string error = string.Format("Bundle error: The file '{0}' doesn't exist", node.InnerText);
                    _dte.ItemOperations.OpenFile(filePath);
                    MessageBox.Show(error, "Web Essentials");
                    return;
                }
            }

            string bundlePath = outputAttr != null ? Path.Combine(Path.GetDirectoryName(filePath), outputAttr.InnerText) : filePath.Replace(_ext, string.Empty);
            StringBuilder sb = new StringBuilder();

            foreach (string file in files.Keys)
            {
                if (extension.Equals(".js", StringComparison.OrdinalIgnoreCase) && WESettings.GetBoolean(WESettings.Keys.GenerateJavaScriptSourceMaps))
                {
                    sb.AppendLine("///#source 1 1 " + files[file]);
                }

                sb.AppendLine(File.ReadAllText(file));
            }

            if (!File.Exists(bundlePath) || File.ReadAllText(bundlePath) != sb.ToString())
            {
                ProjectHelpers.CheckOutFileFromSourceControl(bundlePath);
                using (StreamWriter writer = new StreamWriter(bundlePath, false, new UTF8Encoding(true)))
                {
                    writer.Write(sb.ToString());
                    Logger.Log("Updating bundle: " + Path.GetFileName(bundlePath));
                }
                MarginBase.AddFileToProject(filePath, bundlePath);

                if (bundleNode.Attributes["minify"] != null || bundleNode.Attributes["minify"].InnerText == "true")
                {
                    WriteMinFile(filePath, bundlePath, sb.ToString(), extension);
                }
            }
        }

        private static void WriteMinFile(string filePath, string bundlePath, string content, string extension)
        {
            string minPath = bundlePath.Replace(Path.GetExtension(bundlePath), ".min" + Path.GetExtension(bundlePath));

            if (extension.Equals(".js", StringComparison.OrdinalIgnoreCase))
            {
                JavaScriptSaveListener.Minify(bundlePath, minPath, true);
                MarginBase.AddFileToProject(filePath, minPath);

                if (WESettings.GetBoolean(WESettings.Keys.GenerateJavaScriptSourceMaps))
                {
                    MarginBase.AddFileToProject(filePath, minPath + ".map");
                }

                MarginBase.AddFileToProject(filePath, minPath + ".gzip");
            }
            else if (extension.Equals(".css", StringComparison.OrdinalIgnoreCase))
            {
                string minContent = MinifyFileMenu.MinifyString(extension, content);

                ProjectHelpers.CheckOutFileFromSourceControl(minPath);

                using (StreamWriter writer = new StreamWriter(minPath, false, new UTF8Encoding(true)))
                {
                    writer.Write(minContent);
                }

                MarginBase.AddFileToProject(filePath, minPath);

                if (WESettings.GetBoolean(WESettings.Keys.CssEnableGzipping))
                    CssSaveListener.GzipFile(filePath, minPath, minContent);
            }
        }
    }
}