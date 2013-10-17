using System.ComponentModel.Design;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using System;
using System.Collections.Generic;

namespace MadsKristensen.EditorExtensions
{
    internal class DiffMenu
    {
        private DTE2 _dte;
        private OleMenuCommandService _mcs;

        public DiffMenu(DTE2 dte, OleMenuCommandService mcs)
        {
            _dte = dte;
            _mcs = mcs;
        }

        public void SetupCommands()
        {
            CommandID commandId = new CommandID(GuidList.guidDiffCmdSet, (int)PkgCmdIDList.cmdDiff);
            OleMenuCommand menuCommand = new OleMenuCommand((s, e) => Sort(), commandId);
            menuCommand.BeforeQueryStatus += menuCommand_BeforeQueryStatus;
            _mcs.AddCommand(menuCommand);
        }

        private List<string> files;

        void menuCommand_BeforeQueryStatus(object sender, EventArgs e)
        {
            OleMenuCommand menuCommand = sender as OleMenuCommand;
            files = new List<string>(ProjectHelpers.GetSelectedItemPaths());
            menuCommand.Enabled = files.Count == 2;
        }

        private void Sort()
        {
            if (files.Count == 2)
                EditorExtensionsPackage.DTE.ExecuteCommand("Tools.DiffFiles \"" + files[0] + "\" \"" + files[1] + "\"");
        }
    }
}