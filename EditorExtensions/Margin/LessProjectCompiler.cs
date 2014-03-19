using EnvDTE;
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MadsKristensen.EditorExtensions
{
    internal class LessProjectCompiler
    {
        public static void CompileProject(Project project)
        {
            if (project != null && !string.IsNullOrEmpty(project.FullName))
            {
                Task.Run(() => Compile(project));
            }
        }

        private static void Compile(Project project)
        {
            LessCompiler compiler = new LessCompiler(Completed);

            string dir = Path.GetDirectoryName(project.Properties.Item("FullPath").Value.ToString());
            var files = Directory.GetFiles(dir, "*.less", SearchOption.AllDirectories).Where(f => CanCompile(f));

            foreach (string file in files)
            {
                compiler.Compile(file);
            }
        }

        private static bool CanCompile(string fileName)
        {
            if (EditorExtensionsPackage.DTE.Solution.FindProjectItem(fileName) == null)
                return false;

            if (Path.GetFileName(fileName).StartsWith("_"))
                return false;

            return true;
        }

        private static void Completed(CompilerResult result)
        {
            if (result.IsSuccess)
            {
                string cssFileName = MarginBase.GetCompiledFileName(result.FileName, ".css", WESettings.GetBoolean(WESettings.Keys.LessCompileToFolder));// result.FileName.Replace(".less", ".css");

                bool cssFileExists = File.Exists(cssFileName);
                string old = string.Empty;
                if (cssFileExists)
                {
                    old = File.ReadAllText(cssFileName);
                }

                if (!cssFileExists || old != result.Result)
                {
                    if (cssFileExists)
                    {                            
                        ProjectHelpers.CheckOutFileFromSourceControl(cssFileName);
                    }

                    try
                    {
                        bool useBom = WESettings.GetBoolean(WESettings.Keys.UseBom);
                        using (StreamWriter writer = new StreamWriter(cssFileName, false, new UTF8Encoding(useBom)))
                        {
                            writer.Write(result.Result);
                        }

                        if (!cssFileExists)
                        {
                            MarginBase.AddFileToProject(result.FileName, cssFileName);
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.Log(ex);
                    }
                }

                MinifyFile(result.FileName, result.Result);
            }
            else if (result.Error != null && !string.IsNullOrEmpty(result.Error.Message))
            {
                Logger.Log(result.Error.Message);
            }
            else
            {
                Logger.Log("Error compiling LESS file: " + result.FileName);
            }
        }

        public static void MinifyFile(string lessFileName, string source)
        {
            if (WESettings.GetBoolean(WESettings.Keys.LessMinify))
            {
                string content = MinifyFileMenu.MinifyString(".css", source);
                string minFile = MarginBase.GetCompiledFileName(lessFileName, ".min.css", WESettings.GetBoolean(WESettings.Keys.LessCompileToFolder)); //lessFileName.Replace(".less", ".min.css");
                string old = string.Empty;
                bool minFileExist = File.Exists(minFile);
                if (minFileExist)
                {
                    old = File.ReadAllText(minFile);                    
                }

                if (!minFileExist || old != content)
                {
                    bool useBom    = WESettings.GetBoolean(WESettings.Keys.UseBom);

                    if (minFileExist)
                    {
                        ProjectHelpers.CheckOutFileFromSourceControl(minFile);                        
                    }

                    using (StreamWriter writer = new StreamWriter(minFile, false, new UTF8Encoding(useBom)))
                    {
                        writer.Write(content);
                    }

                    if (!minFileExist)
                        MarginBase.AddFileToProject(lessFileName, minFile);
                }
            }
        }
    }
}