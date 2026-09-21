using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CommandLine;
using Kifa.Api.Files;
using Kifa.IO;
using Kifa.Jobs;
using Kifa.Service;
using NLog;

namespace Kifa.Tools.FileUtil.Commands;

[Verb("get", HelpText = "Get files.")]
class GetCommand : KifaCommand {
    static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    [Value(0, Required = true, HelpText = "Target file(s) to get.")]
    public IEnumerable<string> FileNames { get; set; }

    [Option('l', "lightweight-only", HelpText = "Only get files that need no download.")]
    public bool LightweightOnly { get; set; } = false;

    [Option('a', "include-all", HelpText = "Include all files already registered.")]
    public bool IncludeAll { get; set; } = false;

    [Option('c', "allowed-clients", HelpText = "Only get files from the given sources.")]
    public string? AllowedClients { get; set; }

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

    KifaActionResult GetFile(KifaFile file)
        => file.GetFile(LightweightOnly,
            allowedClients: AllowedClients == null ? null : [..AllowedClients.Split(",")],
            ignoreLocations: IgnoreLocations);
}
