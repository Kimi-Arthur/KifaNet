using System;
using System.Linq;
using CommandLine;
using Pimix.IO;

namespace Pimix.Apps.FileUtil.Commands
{
    [Verb("rm", HelpText = "Remove the FILE. Can be either logic path like: pimix:///Software/... or real path like: pimix://xxx@xxx/....")]
    class RemoveCommand : FileUtilCommand
    {
        [Value(0, MetaName = "FILE", MetaValue = "STRING", Required = true, HelpText = "File to be removed.")]
        public string FileUri { get; set; }

        [Option('l', "link", HelpText = "Remove link only.")]
        public bool RemoveLinkOnly { get; set; }

        public override int Execute()
        {
            Uri uri;
            if (Uri.TryCreate(FileUri, UriKind.Absolute, out uri) && uri.Scheme.StartsWith("pimix"))
            {
                var info = FileInformation.Get(uri.LocalPath);

                if (string.IsNullOrEmpty(uri.Host))
                {
                    // Remove logical file.
                    if (!RemoveLinkOnly && info.Locations != null)
                    {
                        // Remove real files.
                        foreach (var location in info.Locations)
                        {
                            if (!RemoveRealFile(location))
                            {
                                Console.Error.WriteLine($"Removal of real file for {location} failed.");
                                return 1;
                            }
                        }
                    }

                    // Logical removal.
                    FileInformation.Delete(info.Id);
                    return 0;
                }
                else
                {
                    if (!info.Locations.Contains(FileUri))
                    {
                        // Not found the entry. Then it's already done somehow.
                        Console.Error.WriteLine($"File {FileUri} already removed!");
                        return 0;
                    }

                    // Remove specific location item.
                    if (!RemoveLinkOnly)
                    {
                        // Remove real files.
                        if (!RemoveRealFile(FileUri))
                        {
                            Console.Error.WriteLine($"Removal of real file for {FileUri} failed.");
                            return 1;
                        }
                    }

                    // Logical removal.
                    info.Locations.Remove(FileUri);
                    FileInformation.Patch(info);
                    return 0;
                }
            }
            else
            {
                // Since it's local path, it won't have logical path.
                if (!RemoveLinkOnly)
                {
                    // Remove real files.
                    if (!RemoveRealFile(FileUri))
                    {
                        Console.Error.WriteLine($"Removal of real file for {FileUri} failed.");
                        return 1;
                    }
                }

                return 0;
            }
        }

        bool RemoveRealFile(string location)
        {
            try
            {
                new PimixFile(location).Delete();
                return true;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"StorageClient.DeleteFile failed for path {location}");
                Console.Error.WriteLine(ex);
                return false;
            }
        }
    }
}
