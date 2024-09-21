using System;
using System.Collections.Generic;
using System.Linq;
using CommandLine;
using Kifa.Api.Files;
using Kifa.Infos;
using Kifa.Jobs;
using NLog;

namespace Kifa.Tools.SubUtil.Commands;

[Verb("import", HelpText = "Import files from /Subtitles/Sources folder with resource id.")]
class ImportCommand : KifaFileCommand {
    static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    List<ItemInfo> episodes;

    public override bool ById => false;
    protected override bool NaturalSorting => true;

    [Option('s', "source-id", HelpText = "ID for the source, like tv_shows/Westworld/1.",
        Required = true)]
    public string SourceId { get; set; }

    [Option('l', "language", HelpText = "Language code for the source, like en, ja, zh_en etc.",
        Required = true)]
    public string LanguageCode { get; set; }

    [Option('g', "group", HelpText = "Group name for the source, like 华盟字幕社, 人人影视.",
        Required = true)]
    public string ReleaseGroup { get; set; }

    public override int Execute(KifaTask? task = null) {
        var segments = SourceId.Split('/', options: StringSplitOptions.RemoveEmptyEntries);
        episodes = TvShow.GetItems(segments).Checked().ToList();

        return base.Execute();
    }

    protected override int ExecuteOneKifaFile(KifaFile file) {
        var suffix = file.Path[file.Path.LastIndexOf('.')..];
        var selected = SelectOne(episodes, e => $"{file} => {e.Path}{suffix}", "mapping");
        if (selected == null) {
            Logger.Warn($"File {file} skipped.");
            return 0;
        }

        var (choice, index, _) = selected.Value;

        file.Copy(
            new KifaFile($"{file.Host}/Subtitles{choice.Path}" +
                         $".{LanguageCode}-{ReleaseGroup}{suffix}"), true);
        episodes.RemoveAt(index);

        return 0;
    }
}
