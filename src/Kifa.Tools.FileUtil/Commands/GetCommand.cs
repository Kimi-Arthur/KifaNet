using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CommandLine;
using Kifa.Api.Files;
using Kifa.IO;
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

    [Option('u', "ignore-already-uploaded",
        HelpText = "Ignores files that are already uploaded to the given sources.")]
    public string? IgnoreAlreadyUploaded { get; set; }

    HashSet<string>? alreadyUploaded;

    HashSet<string> AlreadyUploaded
        => alreadyUploaded ??= IgnoreAlreadyUploaded == null
            ? new HashSet<string>()
            : new HashSet<string>(IgnoreAlreadyUploaded.Split(","));

    public override int Execute() {
        var files = KifaFile.FindPotentialFiles(FileNames, ignoreFiles: !IncludeAll);
        foreach (var file in files) {
            Console.WriteLine(file);
        }

        if (!Confirm($"Confirm getting the {files.Count} files above?")) {
            Logger.Info("Action canceled.");
            return -1;
        }

        foreach (var file in files) {
            ExecuteItem(file.Id, () => GetFile(file));
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

                var spec = location.Split("/")[0];
                if (AlreadyUploaded.Any(u => spec.StartsWith(u))) {
                    return new KifaActionResult {
                        Status = KifaActionStatus.Warning,
                        Message = $"File already exists in {spec} as {location}."
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

        var source = new KifaFile(fileInfo: info,
            allowedClients: AllowedClients == null
                ? null
                : new HashSet<string>(AllowedClients.Split(",")));
        source.Copy(file);

        try {
            Logger.Info($"Verifying destination {file}...");
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
