using System;
using System.Linq;
using CommandLine;
using Pimix.IO;

namespace Pimix.Apps.FileUtil.Commands
{
    [Verb("rm", HelpText = "Remove the FILE. Can be either logic path like: /Software/... or real path like: local:desk/Software....")]
    class RemoveCommand : FileUtilCommand
    {
        [Value(0, MetaName = "FILE", MetaValue = "STRING", HelpText = "File to be removed.")]
        public string FileUri { get; set; }

        [Option('i', "id", HelpText = "ID for the uri.")]
        public string FileId { get; set; }

        [Option('l', "link", HelpText = "Remove link only.")]
        public bool RemoveLinkOnly { get; set; }

        public override int Execute()
        {
            if (String.IsNullOrEmpty(FileUri)) {
                var info = FileInformation.Get(FileId);

                // Remove logical file.
                if (!RemoveLinkOnly && info.Locations != null)
                {
                    // Remove real files.
                    foreach (var location in info.Locations)
                    {
                        var file = new PimixFile(location, FileId);
                        file.Delete();
                    }

                    info.Locations.Clear();
                    FileInformation.Patch(info);
                }

                // Logical removal.
                FileInformation.Delete(info.Id);
                return 0;
            }
            else
            {
                var file = new PimixFile(FileUri, FileId);
                if (file.FileInfo.Locations == null || !file.FileInfo.Locations.Contains(FileUri))
                {
                    file.Delete();
                    return 0;
                }

                // Remove specific location item.
                if (!RemoveLinkOnly)
                {
                    // Remove real files.
                    file.Delete();
                }

                FileInformation.RemoveLocation(file.Id, FileUri);

                return 0;
            }
        }
    }
}
