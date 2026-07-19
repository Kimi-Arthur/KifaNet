using System;
using System.Collections.Generic;
using System.Linq;
using CommandLine;
using Kifa.Api.Files;
using Kifa.Jobs;
using Kifa.Service;
using NLog;

namespace Kifa.Tools.SubUtil.Commands;

[Verb("import", HelpText = "Import files from /Sources folder with resource id.")]
partial class ImportCommand : KifaCommand {
    static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    [Value(0, Required = true, HelpText = "Target subtitle file(s) to import.")]
    public IEnumerable<string> FileNames { get; set; }

    [Option('t', "targets", Separator = '|', HelpText = "Target media file(s) to import for.",
        Required = true)]
    public IEnumerable<string> Targets { get; set; }

    [Option('s', "release-id",
        HelpText =
            "Release Id to be added before suffix. This should contain a language code, like en, 华盟.zh etc.",
        Required = true)]
    public string ReleaseId { get; set; }

    public override int Execute(KifaTask? task = null) {
        var langPart = ReleaseId.Split('.').Last();
        if (langPart.Contains('-')) {
            Logger.Error(
                $"Language code '{langPart}' in ReleaseId '{ReleaseId}' contains '-'. Dual language codes (e.g. 'zh-en') are not allowed. Only the main language code (e.g. 'zh') should be used.");
            return 1;
        }

        // Assumed all FileNames are from SubtitlesHost.
        var subtitleFiles = KifaFile.FindExistingFiles(FileNames);
        var targetFiles = KifaFile.FindExistingFiles(Targets)
            .Select(file => new MatchableItem(file.PathWithoutSuffix)).ToList();

        foreach (var subtitleFile in subtitleFiles) {
            ExecuteItem(subtitleFile.ToString(), () => ImportSubtitle(subtitleFile, targetFiles));
        }

        return LogSummary();
    }

    KifaActionResult ImportSubtitle(KifaFile subtitleFile, List<MatchableItem> targetFiles) {
        var suffix = subtitleFile.Extension.Checked().ToLower();

        var validEpisodes = targetFiles.Where(e => !e.Matched).ToList();
        try {
            var selected = SelectOne(validEpisodes, e => e.Item, "target media file", startingIndex: 1, reverse: true);
            if (selected.Status != KifaActionStatus.OK) {
                return selected;
            }

            var choice = selected.Response!.Choice;
            var newFile = new KifaFile($"{subtitleFile.Host}{choice.Item}.{ReleaseId}.{suffix}");

            subtitleFile.Copy(newFile, true);
            if (suffix == "ass") {
                FixSubtitle(newFile, new KifaFile(choice.Item).Name, ReleaseId);
            }

            choice.Matched = true;

            return KifaActionResult.Success();
        } catch (InvalidChoiceException ex) {
            return new KifaActionResult {
                Status = KifaActionStatus.Error,
                Message = $"File {subtitleFile} skipped due to invalid choice: {ex.Message}"
            };
        }
    }
}

class MatchableItem(string item) {
    public string Item { get; set; } = item;
    public bool Matched { get; set; }
}
