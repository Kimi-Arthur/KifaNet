using System;
using System.Collections.Generic;
using System.Linq;
using CommandLine;
using Kifa.Api.Files;
using Kifa.Jobs;
using Kifa.Service;
using NLog;

namespace Kifa.Tools.FileUtil.Commands;

[Verb("get", HelpText = "Get files.")]
public class GetCommand : KifaCommand {
    static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    [Value(0, Required = true, HelpText = "Target file(s) to get.")]
    public IEnumerable<string> FileNames { get; set; }

    [Option('c', "allowed-clients", SetName = "allowed-clients",
        HelpText = "Only get files from the given sources.")]
    public string? AllowedClients { get; set; }

    [Option('C', "no-copying", SetName = "no-copying",
        HelpText = "Only get files that can be hard-linked locally (no byte copying or downloading).")]
    public bool NoCopying { get; set; } = false;

    [Option('D', "no-downloading", SetName = "no-downloading",
        HelpText = "Only get files from local sources (no internet downloading).")]
    public bool NoDownloading { get; set; } = false;

    [Option('a', "include-all", HelpText = "Include all files already registered.")]
    public bool IncludeAll { get; set; } = false;

    [Option('i', "ignore",
        HelpText =
            "Ignores files that are already located in the given locations. Locations are given as prefixes and separated by '|'.")]
    public string? IgnoreAlreadyThere { get; set; }

    [Option('S', "show-size", HelpText = "Show size for each file and total size (can be slow).")]
    public bool ShowSize { get; set; } = false;

    List<string>? ignoreLocations;

    IEnumerable<string> IgnoreLocations
        => ignoreLocations ??= IgnoreAlreadyThere == null
            ? []
            : IgnoreAlreadyThere.Split("|").ToList();

    public override int Execute(KifaTask? task = null) {
        var files = KifaFile.FindPotentialFiles(FileNames, ignoreFiles: !IncludeAll);
        var selected = SelectMany(files, file => ShowSize ? $"{file} ({file.FileInfo?.Size.ToSizeString()})" : file.ToString(),
            new Func<List<KifaFile>, string>(choices
                => $"files{(ShowSize ? $" ({choices.Sum(c => c.FileInfo?.Size ?? 0).ToSizeString()})" : "")} to get"));

        if (selected.Status != KifaActionStatus.OK) {
            ExecuteItem("files to get", () => selected);
            return LogSummary();
        }

        foreach (var file in selected.Value) {
            ExecuteItem(file.ToString(), () => GetFile(file));
        }

        return LogSummary();
    }

    HashSet<string>? GetAllowedClients() {
        if (NoCopying) {
            return [];
        }

        if (NoDownloading) {
            return ["local"];
        }

        return AllowedClients == null ? null : [.. AllowedClients.Split(",")];
    }

    public KifaActionResult GetFile(KifaFile file)
        => file.GetFile(GetAllowedClients(), ignoreLocations: IgnoreLocations);
}
