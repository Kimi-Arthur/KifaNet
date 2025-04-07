using System;
using System.Collections.Generic;
using System.Linq;
using CommandLine;
using Kifa.Api.Files;
using Kifa.Jobs;
using NLog;

namespace Kifa.Tools.SubUtil.Commands;

[Verb("import", HelpText = "Import files from /Sources folder with resource id.")]
class ImportCommand : KifaCommand {
    static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    [Value(0, Required = true, HelpText = "Target subtitle file(s) to import.")]
    public IEnumerable<string> FileNames { get; set; }

    [Option('t', "targets", Separator = '|', HelpText = "Target media file(s) to import for.",
        Required = true)]
    public IEnumerable<string> Targets { get; set; }

    [Option('s', "release-id",
        HelpText =
            "Release Id to be added before suffix. This can be language code and/or group name, like en, zh-华盟 etc.",
        Required = true)]
    public string ReleaseId { get; set; }

    public override int Execute(KifaTask? task = null) {
        // Assumed all FileNames are from SubtitlesHost.
        var subtitleFiles = KifaFile.FindExistingFiles(FileNames);
        var targetFiles = KifaFile.FindExistingFiles(Targets)
            .Select(file => new MatchableItem(file.PathWithoutSuffix)).ToList();

        foreach (var file in subtitleFiles) {
            var suffix = file.Extension.Checked().ToLower();

            // TODO: Remove this when we can print better message in SelectOne.
            Console.WriteLine($"Select a location to import {file} to:");
            var validEpisodes = targetFiles.Where(e => !e.Matched).ToList();
            try {
                var selected = SelectOne(validEpisodes, e => e.Item, "mapping", startingIndex: 1,
                    supportsSpecial: true, reverse: true);
                if (selected == null) {
                    Logger.Warn($"Ignored {file}.");
                    continue;
                }

                var (choice, _, special) = selected.Value;
                if (special) {
                    var newName = Confirm($"Confirm linking {file} to (without suffix):",
                        choice.Item);
                    var newFile = new KifaFile($"{file.Host}{newName}.{ReleaseId}.{suffix}");

                    file.Copy(newFile, true);
                    if (Confirm($"Remove info item {choice.Item}?")) {
                        selected.Value.Choice.Matched = true;
                    }
                } else {
                    var newFile = new KifaFile($"{file.Host}{choice.Item}.{ReleaseId}.{suffix}");
                    file.Copy(newFile, true);
                    choice.Matched = true;
                }
            } catch (InvalidChoiceException ex) {
                Logger.Warn(ex, $"File {file} skipped.");
            }
        }

        return 0;
    }
}

class MatchableItem(string item) {
    public string Item { get; set; } = item;
    public bool Matched { get; set; }
}
