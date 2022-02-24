using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CommandLine;
using NLog;
using Kifa.Api.Files;
using Kifa.IO;

namespace Kifa.Tools.FileUtil.Commands; 

[Verb("upload", HelpText = "Upload file to a cloud location.")]
class UploadCommand : KifaCommand {
    static readonly Logger logger = LogManager.GetCurrentClassLogger();

    [Value(0, Required = true, HelpText = "Target file(s) to upload.")]
    public IEnumerable<string> FileNames { get; set; }

    [Option('d', "delete-source",
        HelpText = "Remove source if upload is successful. Won't remove valid cloud version.")]
    public bool DeleteSource { get; set; } = false;

    [Option('q', "quick", HelpText = "Finish quickly by not verifying validity of destination.")]
    public bool QuickMode { get; set; } = false;

    [Option('s', "service",
        HelpText = "Type of service to upload to. Default is google. Allowed values: [google, baidu, mega, swiss]")]
    public CloudServiceType ServiceType { get; set; } = CloudServiceType.Google;

    [Option('f', "format", HelpText = "Format used to upload file. Default is v1. Allowed values: [v1, v2]")]
    public CloudFormatType FormatType { get; set; } = CloudFormatType.V1;

    [Option('c', "use-cache", HelpText = "Use cache to help upload.")]
    public bool UseCache { get; set; } = false;

    [Option('l', "download-local", HelpText = "Download the file to local.")]
    public bool DownloadLocal { get; set; } = false;

    public override int Execute() {
        var (multi, files) = KifaFile.ExpandFiles(FileNames);
        if (multi) {
            foreach (var file in files) {
                Console.WriteLine(file);
            }

            var downloadText = DownloadLocal ? " and download to local" : "";
            var removalText = DeleteSource ? " and remove source afterwards" : "";
            Console.Write($"Confirm uploading the {files.Count} files above{downloadText}{removalText}?");
            Console.ReadLine();
        }

        var results = files.Select(f => (f.ToString(),
                new KifaFile(f.ToString()).Upload(ServiceType, FormatType, deleteSource: DeleteSource,
                    useCache: UseCache, downloadLocal: DownloadLocal, skipVerify: QuickMode, skipRegistered: true)))
            .ToList();
        return results.Select(r => r.Item2).Concat(results.Where(r => r.Item2 == -1).Select(r =>
            new KifaFile(r.Item1).Upload(ServiceType, FormatType, deleteSource: DeleteSource, useCache: UseCache,
                downloadLocal: DownloadLocal, skipVerify: QuickMode, skipRegistered: false))).Max();
    }
}