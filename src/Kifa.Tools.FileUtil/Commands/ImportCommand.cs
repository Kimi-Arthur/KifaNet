using System;
using System.Collections.Generic;
using System.Linq;
using CommandLine;
using Kifa.Soccer;
using NLog;
using Kifa.Api.Files;
using Kifa.Infos;
using Kifa.IO;
using Season = Kifa.Infos.Season;

namespace Kifa.Tools.FileUtil.Commands {
    [Verb("import", HelpText = "Import files from /Downloads folder with resource id.")]
    class ImportCommand : KifaCommand {
        static readonly Logger logger = LogManager.GetCurrentClassLogger();

        [Value(0, Required = true, HelpText = "Target file(s) to import.")]
        public IEnumerable<string> FileNames { get; set; }

        [Option('i', "id", HelpText = "Treat input files as logical ids.")]
        public virtual bool ById { get; set; } = false;

        [Option('s', "source-id", HelpText = "ID for the source, like tv_shows/Westworld/1")]
        public string SourceId { get; set; }

        [Option('r', "recursive", HelpText = "Whether to list folder recursively.")]
        public bool Recursive { get; set; }

        public override int Execute() {
            var segments = SourceId.Split('/');
            var type = segments[0];
            var id = segments.Length > 1 ? segments[1] : "";
            var seasonId = segments.Length > 2 ? int.Parse(segments[2]) : 0;
            var episodeId = segments.Length > 3 ? int.Parse(segments[3]) : 0;

            List<(Season season, Episode episode)> episodes;
            Formattable series;

            switch (type) {
                case "tv_shows":
                    var tvShow = TvShow.Client.Get(id);
                    series = tvShow;
                    episodes = tvShow.Seasons.Where(season => seasonId <= 0 || season.Id == seasonId)
                        .SelectMany(season => season.Episodes, (season, episode) => (season, episode))
                        .Where(item => episodeId <= 0 || episodeId == item.episode.Id).ToList();
                    break;
                case "animes":
                    var anime = Anime.Client.Get(id);
                    series = anime;
                    episodes = anime.Seasons.Where(season => seasonId <= 0 || season.Id == seasonId)
                        .SelectMany(season => season.Episodes, (season, episode) => ((Season) season, episode))
                        .Where(item => episodeId <= 0 || episodeId == item.episode.Id).ToList();
                    break;
                case "soccer":
                    foreach (var file in FileNames.SelectMany(path =>
                        FileInformation.Client.ListFolder(ById ? path : new KifaFile(path).Id, Recursive)
                            .DefaultIfEmpty(ById ? path : new KifaFile(path).Id))) {
                        var ext = file.Substring(file.LastIndexOf(".") + 1);
                        var targetFileName = $"{SoccerShow.FromFileName(file)}.{ext}";
                        targetFileName = Confirm($"Confirm importing {file} as ", targetFileName);
                        FileInformation.Client.Link(file, targetFileName);
                        logger.Info($"Successfully linked {file} to {targetFileName}");
                    }
                    return 0;
                default:
                    Console.WriteLine($"Cannot find resource type {type}");
                    return 1;
            }

            foreach (var file in FileNames.SelectMany(path =>
                FileInformation.Client.ListFolder(ById ? path : new KifaFile(path).Id, Recursive))) {
                var suffix = file.Substring(file.LastIndexOf('.'));
                var ((season, episode), index) = SelectOne(episodes,
                    e => $"{file} => {series.Format(e.season, e.episode)}{suffix}", "mapping", (null, null));
                if (index >= 0) {
                    FileInformation.Client.Link(file, series.Format(season, episode) + suffix);
                    episodes.RemoveAt(index);
                }
            }

            return 0;
        }
    }
}
