﻿using EnvDTE;
using Microsoft.VisualStudio.Text;
using System;
using System.Globalization;
using System.IO;
using System.Text;

namespace MadsKristensen.EditorExtensions
{
    public class LessMargin : MarginBase
    {
        public const string MarginName = "LessMargin";

        public LessMargin(string contentType, string source, bool showMargin, ITextDocument document)
            : base(source, MarginName, contentType, showMargin, document)
        { }

        protected override bool CanCompileFileOnSave(string fullPath)
        {
            if (!WESettings.GetBoolean(WESettings.Keys.GenerateCssFileFromLess))
                return false;

            if (MadsKristensen.EditorExtensions.WEIgnore.TestWEIgnore(fullPath, "compiler", "less"))
            {
                Logger.Log(String.Format(CultureInfo.CurrentCulture, "LESS: The file {0} is ignored by .weignore. Skipping..", Path.GetFileName(fullPath)));
                return false;
            }
            else
                return true;
        }

        protected override void StartCompiler(string source)
        {
            string fileName = GetCompiledFileName(Document.FilePath, ".css", UseCompiledFolder);// Document.FilePath.Replace(".less", ".css");

            if (_isFirstRun && File.Exists(fileName))
            {
                OnCompilationDone(File.ReadAllText(fileName), Document.FilePath);
            }
            else
            {
                Logger.Log("LESS: Compiling " + Path.GetFileName(Document.FilePath));

                System.Threading.Tasks.Task.Run(() =>
                {
                    LessCompiler compiler = new LessCompiler(Completed);
                    compiler.Compile(Document.FilePath);
                });
            }
        }

        private void Completed(CompilerResult result)
        {
            if (result.IsSuccess)
            {
                OnCompilationDone(result.Result, result.FileName);
            }
            else
            {
                result.Error.Message = "LESS: " + result.Error.Message;

                CreateTask(result.Error);

                base.OnCompilationDone("ERROR:", Document.FilePath);
            }
        }

        public override void MinifyFile(string fileName, string source)
        {            
            if (WESettings.GetBoolean(WESettings.Keys.LessMinify) && CanMinify(fileName))
            {
                bool useBom    = WESettings.GetBoolean(WESettings.Keys.UseBom);
                string content = MinifyFileMenu.MinifyString(".css", source);
                string minFile = GetCompiledFileName(fileName, ".min.css", UseCompiledFolder);// fileName.Replace(".less", ".min.css");
                bool fileExist = File.Exists(minFile);

                ProjectHelpers.CheckOutFileFromSourceControl(minFile);
                using (StreamWriter writer = new StreamWriter(minFile, false, new UTF8Encoding(useBom)))
                {
                    writer.Write(content);
                }

                if (!fileExist)
                    AddFileToProject(Document.FilePath, minFile);
            }
        }

        private static bool CanMinify(string fileName)
        {
            if (Path.GetFileName(fileName).StartsWith("_"))
                return false;

            if (MadsKristensen.EditorExtensions.WEIgnore.TestWEIgnore(fileName, "compiler", "less"))
            {
                Logger.Log(String.Format(CultureInfo.CurrentCulture, "LESS: The file {0} is ignored by .weignore. Skipping..", Path.GetFileName(fileName)));
                return false;
            }

            return true;
        }

        public override bool UseCompiledFolder
        {
            get { return WESettings.GetBoolean(WESettings.Keys.LessCompileToFolder); }
        }

        public override bool IsSaveFileEnabled
        {
            get { return WESettings.GetBoolean(WESettings.Keys.GenerateCssFileFromLess) && CanMinify(Document.FilePath); }
        }

        protected override bool CanWriteToDisk(string source)
        {
            return true;
        }
    }
}
