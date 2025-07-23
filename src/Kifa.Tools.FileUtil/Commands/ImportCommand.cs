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

    // Folder to models with search/match function. For example, "TV Shows" => "tv_shows".
    public static Dictionary<string, string> Categories = new();

    [Value(0, Required = true, HelpText = "Target file(s) to import.")]
    public IEnumerable<string> FileNames { get; set; }

    [Option('i', "id", HelpText = "Treat input files as logical ids.")]
    public bool ById { get; set; } = false;

    [Option('t', "type",
        HelpText = "Type for the source, in case it cannot be inferred from the path")]
    public string? Type { get; set; }

    [Option('s', "source-id", HelpText = "ID for the source, like Westworld/1")]
    public string? SourceId { get; set; }

    public override int Execute(KifaTask? task = null) {
        var files = (ById ? GetFromFileIds() : GetFromLocalFiles()).ToList();
        if (files.Count == 0) {
            Logger.Error("No files found. Action canceled.");
            return 1;
        }

        foreach (var file in files) {
            Console.WriteLine(file);
        }

        if (!Confirm($"Confirm processing the {files.Count} files above?")) {
            Logger.Error("Action canceled.");
            return 1;
        }

        // All source files are assumed to be in a path like
        //     /Downloads/<Type>/<Title>/<Subpaths>
        // For example,
        //     /Downloads/TV Shows/DARK.S01.2160p.NF.WEBRip.DDP5.1.x264-NTb[rartv]/DARK.S01E01.Secrets.2160p.NF.WEBRip.DDP5.1.x264-NTb.mkv
        //     /Downloads/Gaming/黑桐谷歌-合集·【黑神话:悟空】.43536-3659146.bilibili/4K超清【黑神话:悟空】全妖降伏攻略解说 11 第三回 夜生白露-小雷音寺.av113073067262848p1.c25765217836.120-hevc.mp4
        //     /Downloads/Movies/Death Note Trilogy/1. Death Note (2006).mkv

        var pathSegments = files[0].Split("/", options: StringSplitOptions.RemoveEmptyEntries);
        Type ??= pathSegments[1];
        var prefix = $"/Downloads/{Type}/";
        foreach (var file in files.Skip(1)) {
            if (!file.StartsWith(prefix)) {
                Logger.Error($"File {file} don't share a common prefix {prefix}. Exit.");
                return 1;
            }
        }

        SourceId ??= InferSourceId(pathSegments[2]);
        List<(Season Season, Episode Episode, bool Matched)> episodes;
        Formattable series;
        switch (Type) {
            case "Gaming": {
                // Example:
                // Gaming/黑桐谷歌/漫威蜘蛛侠2 -> /Gaming/黑桐谷歌/漫威蜘蛛侠2/漫威蜘蛛侠2 EP01 表面张力.mp4
                series = new Gaming {
                    Id = SourceId
                };
                episodes = Enumerable.Range(1, 100).Select(i => (new Season {
                    Id = 1
                }, new Episode {
                    Id = i
                }, false)).ToList();
                break;
            }
            case "TV Shows": {
                var segments = SourceId.Split('/');
                var id = segments.Length > 0 ? segments[0] : "";
                var seasonId = segments.Length > 1 ? int.Parse(segments[1]) : 0;
                var episodeId = segments.Length > 2 ? int.Parse(segments[2]) : 0;
                var tvShow = TvShow.Client.Get(id);
                series = tvShow;
                episodes = tvShow.Seasons.Where(season => seasonId <= 0 || season.Id == seasonId)
                    .SelectMany(season => season.Episodes,
                        (season, episode) => (season, episode, false)).Where(item
                        => episodeId <= 0 || episodeId == item.episode.Id).ToList();
                break;
            }
            case "Anime": {
                var segments = SourceId.Split('/');
                var id = segments.Length > 0 ? segments[0] : "";
                var seasonId = segments.Length > 1 ? int.Parse(segments[1]) : 0;
                var episodeId = segments.Length > 2 ? int.Parse(segments[2]) : 0;
                var anime = Anime.Client.Get(id);
                series = anime;
                episodes = anime.Seasons.Where(season => seasonId <= 0 || season.Id == seasonId)
                    .SelectMany(season => season.Episodes,
                        (season, episode) => (season, episode, false)).Where(item
                        => episodeId <= 0 || episodeId == item.episode.Id).ToList();
                break;
            }
            case "Soccer":
                foreach (var file in files) {
                    var ext = file[(file.LastIndexOf(".") + 1)..];
                    var targetFileName = $"{SoccerShow.FromFileName(file)}.{ext}";
                    targetFileName = Confirm($"Confirm importing {file} as:", targetFileName);
                    FileInformation.Client.Link(file, targetFileName);
                    Logger.Info($"Successfully linked {file} to {targetFileName}");
                }

                return 0;
            default:
                Console.WriteLine($"{Type} is not known. Will treat it like base folder");
                var lastSlash = SourceId.LastIndexOf("/");
                var folder = $"/{Type}/{SourceId[..lastSlash]}";
                var baseName = SourceId[(lastSlash + 1)..];
                string? fileVersion = null;

                var dotIndex = baseName.IndexOf('.');
                if (dotIndex >= 0) {
                    fileVersion = baseName[(dotIndex + 1)..];
                    baseName = baseName[..dotIndex];
                }

                if (files.Count == 1) {
                    var file = files.Single();
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
                foreach (var file in files) {
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

        var fileMatches = files.Select(f => (File: f, Matched: false)).ToList();

        for (int i = 0; i < fileMatches.Count; i++) {
            var info = FileInformation.Client.Get(fileMatches[i].File);
            var existingMatch = info.GetAllLinks().Select(l => (Link: l, Episode: series.Parse(l)))
                .FirstOrDefault(e => e.Episode != null, ("", null));

            if (existingMatch.Episode.HasValue) {
                var match = existingMatch.Episode.Value;
                Logger.Info($"{fileMatches[i].File} already matched to {existingMatch.Link}");
                MarkMatched(episodes, match.Season, match.Episode);
                fileMatches[i] = (fileMatches[i].File, true);
            }
        }

        foreach (var (file, matched) in fileMatches) {
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

    IEnumerable<string> GetFromFileIds()
        => FileNames.SelectMany(f => FileInformation.Client.ListFolder(f, true));

    IEnumerable<string> GetFromLocalFiles()
        => FilterRegisteredFiles(KifaFile.FindExistingFiles(FileNames)).Select(f => f.Id);

    string InferSourceId(string title) {
        return title;
    }

    IEnumerable<KifaFile> FilterRegisteredFiles(List<KifaFile> files) {
        var notRegisteredFiles = files.Where(f => !f.Registered).ToList();
        if (notRegisteredFiles.Count == 0) {
            return files;
        }

        var toRegister = SelectMany(notRegisteredFiles,
            choiceToString: file => $"{file} ({file.FileInfo?.Size.ToSizeString()})",
            choicesName: "files to add to the system");
        toRegister.ForEach(f => ExecuteItem($"register {f}", () => f.Add()));
        return files.Where(f => f.Registered);
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
