using System;
using System.Collections.Generic;
using System.Linq;
using CommandLine;
using Kifa.Api.Files;
using Kifa.Infos;
using Kifa.Jobs;
using NLog;

namespace Kifa.Tools.SubUtil.Commands;

[Verb("import", HelpText = "Import files from /Sources folder with resource id.")]
class ImportCommand : KifaCommand {
    static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    [Value(0, Required = true, HelpText = "Target file(s) to import.")]
    public IEnumerable<string> FileNames { get; set; }

    [Option('s', "source-id", HelpText = "ID for the source, like tv_shows/Westworld/1.",
        Required = true)]
    public string SourceId { get; set; }

    [Option('i', "id",
        HelpText =
            "Id to be added before suffix. This can be language code and/or group name, like en, zh-华盟 etc.",
        Required = true)]
    public string ReleaseId { get; set; }

    public override int Execute(KifaTask? task = null) {
        var segments = SourceId.Split('/', options: StringSplitOptions.RemoveEmptyEntries);
        var episodes = (TvShow.GetItems(segments) ?? Anime.GetItems(segments)).Checked()
            .Select(e => new MatchableItem(e)).ToList();

        // Assumed all FileNames are from SubtitlesCell.
        var files = KifaFile.FindExistingFiles(FileNames);

        foreach (var file in files) {
            var suffix = file.Extension.Checked().ToLower();

            // TODO: Remove this when we can print better message in SelectOne.
            Console.WriteLine($"Select a location to import {file} to:");
            var validEpisodes = episodes.Where(e => !e.Matched).ToList();
            try {
                var selected = SelectOne(validEpisodes, e => e.Item.Path, "mapping",
                    startingIndex: 1, supportsSpecial: true, reverse: true);
                if (selected == null) {
                    Logger.Warn($"Ignored {file}.");
                    continue;
                }

                var (choice, _, special) = selected.Value;
                if (special) {
                    var newName = Confirm($"Confirm linking {file} to (without suffix):",
                        choice.Item.Path);
                    var newFile = new KifaFile($"{file.Host}{newName}.{ReleaseId}.{suffix}");

                    file.Copy(newFile, true);
                    if (Confirm($"Remove info item {choice.Item.Path}?")) {
                        selected.Value.Choice.Matched = true;
                    }
                } else {
                    var newFile =
                        new KifaFile($"{file.Host}{choice.Item.Path}.{ReleaseId}.{suffix}");
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

class MatchableItem(ItemInfo item) {
    public ItemInfo Item { get; set; } = item;
    public bool Matched { get; set; }
}
