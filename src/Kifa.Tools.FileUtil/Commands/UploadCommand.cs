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
        var targetsFromFlag = Targets.Split(",").ToList();

        var targets = (targetsFromFlag.Count == 0 ? DefaultTargets : targetsFromFlag)
            .Select(CloudTarget.Parse).ToList();

        var (multi, files) = KifaFile.ExpandFiles(FileNames);
        if (multi) {
            foreach (var file in files) {
                Console.WriteLine(file);
            }

            var verifyText = QuickMode ? " without verification" : "";
            var downloadText = DownloadLocal ? " and download to local" : "";
            var removalText = DeleteSource ? " and remove source afterwards" : "";
            Console.Write(
                $"Confirm uploading the {files.Count} files above to {string.Join(", ", targets)}{verifyText}{downloadText}{removalText}?");
            Console.ReadLine();
        }

        var allResults = files.Select(f => (f.ToString(), targets,
            new KifaFile(f.ToString()).Upload(targets, DeleteSource, UseCache, DownloadLocal,
                QuickMode, true))).ToList();
        var resultsByFinal = allResults.GroupBy(results => results.Item3.All(result => result.result != null))
            .ToDictionary(result => result.Key, result => result.ToList());
        var finalResultsBySuccess = resultsByFinal
            .GetValueOrDefault(true,
                new List<(string file, List<CloudTarget> targets,
                    List<(CloudTarget target, string? destination, bool? result)> results)>())
            .Select(result => result.Item3)
            .Concat(resultsByFinal
                .GetValueOrDefault(false,
                    new List<(string file, List<CloudTarget> targets,
                        List<(CloudTarget target, string? destination, bool? result)> results)>())
                .Select(result => new KifaFile(result.Item1).Upload(result.Item2, DeleteSource,
                    UseCache, DownloadLocal, QuickMode)))
            .GroupBy(results => results.All(result => result.result == true))
            .ToDictionary(result => result.Key, result => result.ToList());

        if (finalResultsBySuccess.ContainsKey(true)) {
            logger.Info(
                $"Successfully uploaded {finalResultsBySuccess[true].Count} files to:");
            foreach (var finalResults in finalResultsBySuccess[true]) {
                foreach (var result in finalResults) {
                    logger.Info($"\t{result.destination}");
                }
            }
        }

        if (finalResultsBySuccess.ContainsKey(false)) {
            logger.Info($"Failed to upload {finalResultsBySuccess[false].Count} files:");
            foreach (var finalResults in finalResultsBySuccess[false]) {
                foreach (var result in finalResults) {
                    if (result.result == false) {
                        logger.Info($"\t{result.destination}");
                    }
                }
            }

            return 1;
        }

        if (QuickMode && finalResultsBySuccess.ContainsKey(true)) {
            Console.WriteLine("To verify the unverified files:");
            Console.Write("filex check -s");
            foreach (var finalResults in finalResultsBySuccess[true]) {
                foreach (var result in finalResults) {
                    Console.WriteLine("\\");
                    Console.Write($"  {result.destination}");
                }
            }
        }

        return 0;
    }

    static bool IsFinalResult(List<(CloudTarget target, string? destination, bool? result)> results)
        => results.All(result => result.result != null);
}
