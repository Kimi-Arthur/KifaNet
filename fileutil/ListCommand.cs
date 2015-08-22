using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommandLine;
using Pimix.IO;

namespace fileutil
{
    [Verb("ls", HelpText = "List files and folders in the FOLDER.")]
    class ListCommand : FileUtilCommand
    {
        [Value(0, MetaName = "FOLDER", MetaValue = "STRING", Required = true, HelpText = "Folder to be listed.")]
        public string FolderUri { get; set; }

        public override int Execute()
        {
            Uri uri;
            if (Uri.TryCreate(FolderUri, UriKind.Absolute, out uri) && uri.Scheme.StartsWith("pimix"))
            {
                var info = FileInformation.GetFolderView(uri.LocalPath);
                var schemes = uri.Scheme.Split('+').ToList();
            }
            throw new NotImplementedException();
        }
    }
}
