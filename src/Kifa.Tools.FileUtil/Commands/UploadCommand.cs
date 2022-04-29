using System;
using System.Collections.Generic;
using System.Linq;
using CommandLine;
using Kifa.Api.Files;
using NLog;

namespace Kifa.Tools.FileUtil.Commands;

[Verb("upload", HelpText = "Upload file to a cloud location.")]
class UploadCommand : KifaCommand {
    static readonly Logger logger = LogManager.GetCurrentClassLogger();

    public static List<string> DefaultTargets { get; set; }

    [Value(0, Required = true, HelpText = "Target file(s) to upload.")]
    public IEnumerable<string> FileNames { get; set; }

    [Option('d', "delete-source",
        HelpText = "Remove source if upload is successful. Won't remove valid cloud version.")]
    public bool DeleteSource { get; set; } = false;

    [Option('q', "quick", HelpText = "Finish quickly by not verifying validity of destination.")]
    public bool QuickMode { get; set; } = false;

    [Option('t', "targets",
        HelpText =
            "Targets to upload to, in the format of 'google.v1', 'swiss.v2' or combined 'google.v1,swiss.v2' etc.")]
    public string Targets { get; set; } = "";

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
            Console.Write(
                $"Confirm uploading the {files.Count} files above{downloadText}{removalText}?");
            Console.ReadLine();
        }

        var targetsFromFlag = Targets.Split(",").ToList();

        var targets = (targetsFromFlag.Count == 0 ? DefaultTargets : targetsFromFlag)
            .Select(ParseDestination).ToList();

        var results = files.SelectMany(f => targets.Select(d => (f.ToString(), d,
            new KifaFile(f.ToString()).Upload(d.serviceType, d.formatType, DeleteSource, UseCache,
                DownloadLocal, QuickMode, true)))).ToList();
        return results.Select(r => r.Item3).Concat(results.Where(r => r.Item3 != 0).Select(r
            => new KifaFile(r.Item1).Upload(r.Item2.serviceType, r.Item2.formatType, DeleteSource,
                UseCache, DownloadLocal, QuickMode, false))).Max();
    }

    (CloudServiceType serviceType, CloudFormatType formatType) ParseDestination(string s) {
        var segments = s.Split(".");
        return (Enum.Parse<CloudServiceType>(segments[0], true),
            Enum.Parse<CloudFormatType>(segments[1], true));
    }
}
