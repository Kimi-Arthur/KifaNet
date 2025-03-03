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

    [Value(0, Required = true, HelpText = "Target file(s) to upload.")]
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

    List<string>? ignoreLocations;

    IEnumerable<string> IgnoreLocations
        => ignoreLocations ??= IgnoreAlreadyThere == null
            ? []
            : IgnoreAlreadyThere.Split("|").ToList();

    public override int Execute(KifaTask? task = null) {
        var files = KifaFile.FindPotentialFiles(FileNames, ignoreFiles: !IncludeAll);
        var selected = SelectMany(files,
            choiceToString: file => $"{file} ({file.FileInfo?.Size.ToSizeString()})",
            choiceName: "files");

        foreach (var file in selected) {
            ExecuteItem(file.ToString(), () => GetFile(file));
        }

        return LogSummary();
    }

    KifaActionResult GetFile(KifaFile file) {
        try {
            file.Add();
            return new KifaActionResult {
                Status = KifaActionStatus.OK,
                Message = "Already got!"
            };
        } catch (FileNotFoundException) {
            // File expected to be not found.
        } catch (FileCorruptedException ex) {
            return new KifaActionResult {
                Status = KifaActionStatus.Error,
                Message = "Target exists, but doesn't match."
            };
        }

        file.Unregister();

        var info = file.FileInfo;

        if (info == null || info.Locations.Count == 0) {
            return new KifaActionResult {
                Status = KifaActionStatus.Error,
                Message = "No instance exists."
            };
        }

        foreach (var (location, verifyTime) in info.Locations) {
            if (verifyTime != null) {
                var linkSource = new KifaFile(location);

                if (linkSource.IsLocal && linkSource.IsCompatible(file) && linkSource.Exists()) {
                    linkSource.Copy(file);
                    file.Register(true);
                    return new KifaActionResult {
                        Status = KifaActionStatus.OK,
                        Message = $"Successfully got file through hard linking to {linkSource}."
                    };
                }
            }
        }

        if (LightweightOnly) {
            return new KifaActionResult {
                Status = KifaActionStatus.OK,
                Message = "Not getting file, which requires downloading."
            };
        }

        var foundInstanceInIgnoredLocations = info.Locations.FirstOrDefault(l
            => l.Value != null && IgnoreLocations.Any(u => l.Key.StartsWith(u))).Key;

        if (foundInstanceInIgnoredLocations != null) {
            return new KifaActionResult {
                Status = KifaActionStatus.Warning,
                Message = $"File already exists in {foundInstanceInIgnoredLocations}."
            };
        }

        var source = new KifaFile(fileInfo: info,
            allowedClients: AllowedClients == null ? null : [..AllowedClients.Split(",")]);
        source.Copy(file);

        try {
            Logger.Debug($"Verifying destination {file}...");
            file.Add();
            source.Register(true);
            return new KifaActionResult {
                Status = KifaActionStatus.OK,
                Message = $"Successfully got file from {source}!"
            };
        } catch (IOException ex) {
            return new KifaActionResult {
                Status = KifaActionStatus.Error,
                Message = $"Failed to get destination: {ex}"
            };
        }
    }
}
