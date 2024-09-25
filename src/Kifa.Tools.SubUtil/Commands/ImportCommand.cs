using System;
using System.Collections.Generic;
using System.Linq;
using CommandLine;
using Kifa.Api.Files;
using Kifa.Infos;
using Kifa.IO;
using Kifa.Jobs;
using NLog;

namespace Kifa.Tools.SubUtil.Commands;

[Verb("import", HelpText = "Import files from /Subtitles/Sources folder with resource id.")]
class ImportCommand : KifaCommand {
    static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    [Value(0, Required = true, HelpText = "Target file(s) to import.")]
    public IEnumerable<string> FileNames { get; set; }

    [Option('s', "source-id", HelpText = "ID for the source, like tv_shows/Westworld/1.",
        Required = true)]
    public string SourceId { get; set; }

    [Option('i', "id",
        HelpText =
            "Id to be added before suffix. This can be language code and/or group name, like en, zh-华盟 etc.", Required = true)]
    public string ReleaseId { get; set; }

    public override int Execute(KifaTask? task = null) {
        var segments = SourceId.Split('/', options: StringSplitOptions.RemoveEmptyEntries);
        var episodes = (TvShow.GetItems(segments) ?? Anime.GetItems(segments)).Checked()
            .Select(e => (Episode: e, Matched: false)).ToList();

        var files = KifaFile.FindExistingFiles(FileNames);

        foreach (var file in files) {
            var id = file.Id;
            var suffix = id[id.LastIndexOf('.')..].ToLower();

            var validEpisodes = episodes.Where(e => !e.Matched).ToList();
            try {
                var selected = SelectOne(validEpisodes, e => e.Episode.Path, "mapping",
                    startingIndex: 1, supportsSpecial: true, reverse: true);
                if (selected == null) {
                    Logger.Warn($"Ignored {file}.");
                    continue;
                }

                var (choice, _, special) = selected.Value;
                if (special) {
                    var newName = Confirm($"Confirm linking {file} to:", choice.Episode.Path);
                    file.Copy(new KifaFile($"{file.Host}/Subtitles{newName}.{ReleaseId}{suffix}"),
                        true);
                    if (Confirm($"Remove info item {choice.Episode.Path}?")) {
                        choice.Matched = true;
                    }
                } else {
                    file.Copy(
                        new KifaFile(
                            $"{file.Host}/Subtitles{choice.Episode.Path}.{ReleaseId}{suffix}"),
                        true);
                    choice.Matched = true;
                }
            } catch (InvalidChoiceException ex) {
                Logger.Warn(ex, $"File {file} skipped.");
            }
        }

        return 0;
    }
}
