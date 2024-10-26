using System;
using System.Collections.Generic;
using System.Linq;
using CommandLine;
using Kifa.Api.Files;
using Kifa.Infos;
using Kifa.IO;
using Kifa.Jobs;
using Kifa.Soccer;
using NLog;
using Season = Kifa.Infos.Season;

namespace Kifa.Tools.FileUtil.Commands;

[Verb("import", HelpText = "Import files from /Downloads folder with resource id.")]
class ImportCommand : KifaCommand {
    static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    [Value(0, Required = true, HelpText = "Target file(s) to import.")]
    public IEnumerable<string> FileNames { get; set; }

    [Option('i', "id", HelpText = "Treat input files as logical ids.")]
    public bool ById { get; set; } = false;

    [Option('s', "source-id", HelpText = "ID for the source, like tv_shows/Westworld/1")]
    public string SourceId { get; set; }

    [Option('r', "recursive", HelpText = "Whether to list folder recursively.")]
    public bool Recursive { get; set; }

    public override int Execute(KifaTask? task = null) {
        var segments = SourceId.Trim('/').Split('/');
        var type = segments[0];

        List<(Season Season, Episode Episode, bool Matched)> episodes;
        Formattable series;

        switch (type) {
            case "Gaming": {
                // Example:
                // Gaming/黑桐谷歌/漫威蜘蛛侠2 -> /Gaming/黑桐谷歌/漫威蜘蛛侠2/漫威蜘蛛侠2 EP01 表面张力.mp4
                var id = segments[1];
                series = new Gaming {
                    Id = segments[1..].JoinBy("/")
                };
                episodes = Enumerable.Range(1, 100).Select(i => (new Season {
                    Id = 1
                }, new Episode {
                    Id = i
                }, false)).ToList();
                break;
            }
            case "TV Shows": {
                var id = segments.Length > 1 ? segments[1] : "";
                var seasonId = segments.Length > 2 ? int.Parse(segments[2]) : 0;
                var episodeId = segments.Length > 3 ? int.Parse(segments[3]) : 0;
                var tvShow = TvShow.Client.Get(id);
                series = tvShow;
                episodes = tvShow.Seasons.Where(season => seasonId <= 0 || season.Id == seasonId)
                    .SelectMany(season => season.Episodes,
                        (season, episode) => (season, episode, false)).Where(item
                        => episodeId <= 0 || episodeId == item.episode.Id).ToList();
                break;
            }
            case "Anime": {
                var id = segments.Length > 1 ? segments[1] : "";
                var seasonId = segments.Length > 2 ? int.Parse(segments[2]) : 0;
                var episodeId = segments.Length > 3 ? int.Parse(segments[3]) : 0;
                var anime = Anime.Client.Get(id);
                series = anime;
                episodes = anime.Seasons.Where(season => seasonId <= 0 || season.Id == seasonId)
                    .SelectMany(season => season.Episodes,
                        (season, episode) => (season, episode, false)).Where(item
                        => episodeId <= 0 || episodeId == item.episode.Id).ToList();
                break;
            }
            case "Soccer":
                foreach (var file in FileNames.SelectMany(path
                             => FileInformation.Client
                                 .ListFolder(ById ? path : new KifaFile(path).Id, Recursive)
                                 .DefaultIfEmpty(ById ? path : new KifaFile(path).Id))) {
                    var ext = file[(file.LastIndexOf(".") + 1)..];
                    var targetFileName = $"{SoccerShow.FromFileName(file)}.{ext}";
                    targetFileName = Confirm($"Confirm importing {file} as:", targetFileName);
                    FileInformation.Client.Link(file, targetFileName);
                    Logger.Info($"Successfully linked {file} to {targetFileName}");
                }

                return 0;
            default:
                SourceId = SourceId.TrimEnd('/');
                if (!SourceId.StartsWith('/')) {
                    SourceId = $"/{SourceId}";
                }

                Console.WriteLine(
                    $"{SourceId} doesn't have a known type. Will treat it like base folder");
                var lastSlash = SourceId.LastIndexOf("/");
                var folder = SourceId[..lastSlash];
                var baseName = SourceId[(lastSlash + 1)..];
                string? fileVersion = null;

                var dotIndex = baseName.IndexOf('.');
                if (dotIndex >= 0) {
                    fileVersion = baseName[(dotIndex + 1)..];
                    baseName = baseName[..dotIndex];
                }

                var sourceFiles = FileNames.SelectMany(path
                    => FileInformation.Client
                        .ListFolder(ById ? path : new KifaFile(path).Id, Recursive)
                        .DefaultIfEmpty(ById ? path : new KifaFile(path).Id)).ToList();
                if (sourceFiles.Count == 1) {
                    var file = sourceFiles.Single();
                    var ext = file[(file.LastIndexOf(".") + 1)..];
                    var targetFileName = fileVersion != null
                        ? $"{folder}/{baseName}/{baseName}.{fileVersion}.{ext}"
                        : $"{folder}/{baseName}/{baseName}.{ext}";
                    targetFileName = Confirm($"Confirm importing {file} as:", targetFileName);
                    FileInformation.Client.Link(file, targetFileName);
                    Logger.Info($"Successfully linked {file} to {targetFileName}");

                    return 0;
                }

                var counter = 'A';
                foreach (var file in sourceFiles) {
                    var ext = file[(file.LastIndexOf(".") + 1)..];
                    var targetFileName = fileVersion != null
                        ? $"{folder}/{baseName}/{baseName}-{counter}.{fileVersion}.{ext}"
                        : $"{folder}/{baseName}/{baseName}-{counter}.{ext}";
                    targetFileName = Confirm($"Confirm importing {file} as:", targetFileName);
                    FileInformation.Client.Link(file, targetFileName);
                    Logger.Info($"Successfully linked {file} to {targetFileName}");
                    counter++;
                }

                return 0;
        }

        var files = FileNames.SelectMany(path
                => FileInformation.Client.ListFolder(ById ? path : new KifaFile(path).Id,
                    Recursive))
            .Select(f => (File: f, Matched: false)).ToList();

        for (int i = 0; i < files.Count; i++) {
            var info = FileInformation.Client.Get(files[i].File);
            var existingMatch = info.GetAllLinks().Select(l => (Link: l, Episode: series.Parse(l)))
                .FirstOrDefault(e => e.Episode != null, ("", null));

            if (existingMatch.Episode.HasValue) {
                var match = existingMatch.Episode.Value;
                Logger.Info($"{files[i].File} already matched to {existingMatch.Link}");
                MarkMatched(episodes, match.Season, match.Episode);
                files[i] = (files[i].File, true);
            }
        }

        foreach (var (file, matched) in files) {
            if (matched) {
                continue;
            }

            var suffix = file[file.LastIndexOf('.')..];

            var validEpisodes = episodes.Where(e => !e.Matched).ToList();
            try {
                var selected = SelectOne(validEpisodes,
                    e => $"{file} => {series.Format(e.Season, e.Episode)}{suffix}", "mapping",
                    startingIndex: 1, supportsSpecial: true, reverse: true);
                if (selected == null) {
                    Logger.Warn($"Ignored {file}.");
                    continue;
                }

                var (choice, _, special) = selected.Value;
                if (special) {
                    var newName = Confirm($"Confirm linking {file} to:",
                        $"{series.Format(choice.Season, choice.Episode)}{suffix}");
                    FileInformation.Client.Link(file, newName);
                    if (Confirm(
                            $"Remove info item {series.Format(choice.Season, choice.Episode)}?")) {
                        MarkMatched(episodes, choice.Season, choice.Episode);
                    }
                } else {
                    FileInformation.Client.Link(file,
                        series.Format(choice.Season, choice.Episode) + suffix);
                    MarkMatched(episodes, choice.Season, choice.Episode);
                }
            } catch (InvalidChoiceException ex) {
                Logger.Warn(ex, $"File {file} skipped.");
            }
        }

        return 0;
    }

    static void MarkMatched(List<(Season Season, Episode Episode, bool Matched)> episodes,
        Season matchSeason, Episode matchEpisode) {
        for (var i = 0; i < episodes.Count; i++) {
            if (episodes[i].Season.Id == matchSeason.Id &&
                episodes[i].Episode.Id == matchEpisode.Id) {
                episodes[i] = (episodes[i].Season, episodes[i].Episode, true);
            }
        }
    }
}
