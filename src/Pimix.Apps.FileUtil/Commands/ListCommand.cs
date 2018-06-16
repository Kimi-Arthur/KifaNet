using System;
using CommandLine;
using Pimix.Api.Files;
using Pimix.IO;

namespace Pimix.Apps.FileUtil.Commands {
    [Verb("ls", HelpText = "List files and folders in the FOLDER.")]
    class ListCommand : FileUtilCommand {
        [Value(0, MetaName = "FOLDER", MetaValue = "STRING", HelpText = "Folder to be listed.")]
        public string FolderUri { get; set; }

        [Option('i', "id", HelpText = "ID for the uri.")]
        public string FolderId { get; set; }

        [Option('r', "recursive", HelpText = "List files recursively.")]
        public bool Recursive { get; set; } = false;

        [Option('l', "long", HelpText = "Long list mode")]
        public bool LongListMode { get; set; } = false;

        public override int Execute() {
            int counter = 0;
            if (string.IsNullOrEmpty(FolderUri)) {
                foreach (var info in FileInformation.ListFolder(FolderId, Recursive)) {
                    Console.WriteLine(info);
                    counter++;
                }
            } else {
                foreach (var file in new PimixFile(FolderUri).List(Recursive)) {
                    Console.WriteLine(LongListMode ? $"{file}\t{file.FileInfo.Size}" : file.Path);
                    counter++;
                }
            }

            Console.WriteLine($"\nIn total, {counter} files in {FolderUri ?? FolderId}");
            return 0;
        }
    }
}
