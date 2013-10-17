using System.Security.Cryptography;
using System.Linq;
using System.ComponentModel.Design;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using System.Globalization;
using System.Text;

namespace MadsKristensen.EditorExtensions
{
    internal class TransformMenu
    {
        private readonly DTE2 _dte;
        private readonly OleMenuCommandService _mcs;
        private delegate string Replacement(string original);

        public TransformMenu(DTE2 dte, OleMenuCommandService mcs)
        {
            _dte = dte;
            _mcs = mcs;
        }

        public void SetupCommands()
        {
            SetupCommand(PkgCmdIDList.upperCaseTransform, x => x.ToUpperInvariant());
            SetupCommand(PkgCmdIDList.lowerCaseTransform, x => x.ToLowerInvariant());
            SetupCommand(PkgCmdIDList.titleCaseTransform, x => CultureInfo.InvariantCulture.TextInfo.ToTitleCase(x));
            SetupCommand(PkgCmdIDList.reverseTransform, x => new string(x.Reverse().ToArray()));
            SetupCommand(PkgCmdIDList.normalizeTransform, RemoveDiacritics);
            SetupCommand(PkgCmdIDList.md5Transform, x => Hash(x, new MD5CryptoServiceProvider()));
            SetupCommand(PkgCmdIDList.sha1Transform, x => Hash(x, new SHA1CryptoServiceProvider()));
            SetupCommand(PkgCmdIDList.sha256Transform, x => Hash(x, new SHA256CryptoServiceProvider()));
            SetupCommand(PkgCmdIDList.sha384Transform, x => Hash(x, new SHA384CryptoServiceProvider()));
            SetupCommand(PkgCmdIDList.sha512Transform, x => Hash(x, new SHA512CryptoServiceProvider()));
        }

        public static string RemoveDiacritics(string s)
        {
            string stFormD = s.Normalize(NormalizationForm.FormD);
            StringBuilder sb = new StringBuilder();

            for (int ich = 0; ich < stFormD.Length; ich++)
            {
                UnicodeCategory uc = CharUnicodeInfo.GetUnicodeCategory(stFormD[ich]);
                if (uc != UnicodeCategory.NonSpacingMark)
                {
                    sb.Append(stFormD[ich]);
                }
            }

            return (sb.ToString().Normalize(NormalizationForm.FormC));
        }

        private static string Hash(string original, HashAlgorithm algorithm)
        {
            byte[] hash = algorithm.ComputeHash(Encoding.UTF8.GetBytes(original));
            StringBuilder sb = new StringBuilder();
            
            foreach (byte b in hash)
            {
                sb.Append(b.ToString("x2").ToLowerInvariant());
            }

            return sb.ToString();
        }

        private void SetupCommand(uint command, Replacement callback)
        {
            CommandID commandId = new CommandID(GuidList.guidEditorExtensionsCmdSet, (int)command);
            OleMenuCommand menuCommand = new OleMenuCommand((s, e) => Replace(callback), commandId);

            menuCommand.BeforeQueryStatus += (s, e) =>
            {
                string selection = GetTextDocument().Selection.Text;
                menuCommand.Enabled = selection.Length > 0 && callback(selection) != selection;
            };

            _mcs.AddCommand(menuCommand);
        }

        private TextDocument GetTextDocument()
        {
            return _dte.ActiveDocument.Object("TextDocument") as TextDocument;
        }

        private void Replace(Replacement callback)
        {
            TextDocument document = GetTextDocument();
            string replacement = callback(document.Selection.Text);

            _dte.UndoContext.Open(callback.Method.Name);
            document.Selection.Insert(replacement, 0);
            _dte.UndoContext.Close();
        }
    }
}