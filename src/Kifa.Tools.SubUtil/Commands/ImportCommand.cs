using System;
using System.Collections.Generic;
using CommandLine;
using Kifa.Api.Files;
using Kifa.Infos;

namespace Kifa.Tools.SubUtil.Commands;

[Verb("import", HelpText = "Import files from /Subtitles/Sources folder with resource id.")]
class ImportCommand : KifaFileCommand {
    List<(Season season, Episode episode)> episodes;
    Formattable series;

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

    public override int Execute() {
        var segments = SourceId.Split('/');
        var type = segments[0];
        var id = segments[1];
        var seasonId = 0;
        if (segments.Length > 2) {
            seasonId = int.Parse(segments[2]);
        }

        var episodeId = 0;
        if (segments.Length > 3) {
            episodeId = int.Parse(segments[3]);
        }

        switch (type) {
            case "tv_shows":
                var tvShow = TvShow.Client.Get(id);
                series = tvShow;
                episodes = new List<(Season season, Episode episode)>();
                foreach (var season in tvShow.Seasons) {
                    if (seasonId <= 0 || season.Id == seasonId) {
                        foreach (var episode in season.Episodes) {
                            if (episodeId <= 0 || episodeId == episode.Id) {
                                episodes.Add((season, episode));
                            }
                        }
                    }
                }

                break;
            case "animes":
                var anime = Anime.Client.Get(id);
                series = anime;
                episodes = new List<(Season season, Episode episode)>();
                foreach (var season in anime.Seasons) {
                    if (seasonId <= 0 || season.Id == seasonId) {
                        foreach (var episode in season.Episodes) {
                            if (episodeId <= 0 || episodeId == episode.Id) {
                                episodes.Add((season, episode));
                            }
                        }
                    }
                }

                break;
            default:
                Console.WriteLine($"Cannot find resource type {type}");
                return 1;
        }

        return base.Execute();
    }

    protected override int ExecuteOneKifaFile(KifaFile file) {
        var suffix = file.Path.Substring(file.Path.LastIndexOf('.'));
        var ((season, episode), index) = SelectOne(episodes,
            e => $"{file} => {series.Format(e.season, e.episode)}{suffix}", "mapping",
            (null, null));
        if (index >= 0) {
            file.Copy(
                new KifaFile($"{file.Host}/Subtitles{series.Format(season, episode)}" +
                             $".{LanguageCode}-{ReleaseGroup}{suffix}"), true);
            episodes.RemoveAt(index);
        }

        return 0;
    }
}
