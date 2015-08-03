using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommandLine;
using Pimix.Cloud.BaiduCloud;
using Pimix.IO;

namespace fileutil
{
    [Verb("rm", HelpText = "Remove the FILE. Can be either logic path like: pimix:///Software/... or real path like: pimix://xxx@xxx/....")]
    class RemoveCommand : Command
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
                var schemes = uri.Scheme.Split('+').ToList();

                if (string.IsNullOrEmpty(uri.Host))
                {
                    // Remove logical file.
                    if (!RemoveLinkOnly)
                    {
                        // Remove real files.
                        foreach (var location in info.Locations.Values)
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
                    if (!info.Locations.Values.Contains(FileUri))
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
                    info.Locations.Remove($"{uri.Scheme}://{uri.Host}");
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

        bool RemoveRealFile(string fileUri)
        {
            Uri uri;
            if (Uri.TryCreate(fileUri, UriKind.Absolute, out uri) && uri.Scheme.StartsWith("pimix"))
            {
                var schemes = uri.Scheme.Split('+').ToList();

                // Concerning file source
                if (schemes.Contains("cloud"))
                {
                    switch (uri.Host)
                    {
                        case "pan.baidu.com":
                            {
                                BaiduCloudStorageClient.Config = BaiduCloudConfig.Get("baidu_cloud");
                                try
                                {
                                    new BaiduCloudStorageClient { AccountId = uri.UserInfo }.DeleteFile(uri.LocalPath);
                                    return true;
                                }
                                catch (Exception ex)
                                {
                                    Console.Error.WriteLine($"BaiduCloudStorageClient.DeleteFile failed for path {fileUri}");
                                    Console.Error.WriteLine(ex);
                                    return false;
                                }
                            }
                        default:
                            throw new ArgumentException(nameof(fileUri));
                    }
                }
                else
                {
                    try
                    {
                        File.Delete(GetPath(fileUri));
                        return true;
                    }
                    catch (Exception ex)
                    {
                        Console.Error.WriteLine($"File.Delete failed for path {GetPath(fileUri)}");
                        Console.Error.WriteLine(ex);
                        return false;
                    }
                }
            }
            else
            {
                try
                {
                    File.Delete(GetPath(fileUri));
                    return true;
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"File.Delete failed for path {GetPath(fileUri)}");
                    Console.Error.WriteLine(ex);
                    return false;
                }
            }
        }
    }
}
