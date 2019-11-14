using System;
using System.Collections.Generic;
using System.Linq;
using CommandLine;
using Pimix.Api.Files;
using Pimix.Infos;
using Pimix.IO;

namespace Pimix.Apps.FileUtil.Commands {
    [Verb("import", HelpText = "Import files from /Downloads folder with resource id.")]
    class ImportCommand : PimixCommand {
        [Value(0, Required = true, HelpText = "Target file(s) to import.")]
        public IEnumerable<string> FileNames { get; set; }

        [Option('i', "id", HelpText = "Treat input files as logical ids.")]
        public virtual bool ById { get; set; } = false;

        [Option('s', "source-id", HelpText = "ID for the source, like tv_shows/Westworld/1")]
        public string SourceId { get; set; }

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

            List<(Season season, Episode episode)> episodes;
            Formattable series;

            switch (type) {
                case "tv_shows":
                    var tvShow = TvShow.Client.Get(id);
                    series = tvShow;
                    episodes = tvShow.Seasons.Where(season => seasonId <= 0 || season.Id == seasonId)
                        .SelectMany(season => season.Episodes, (season, episode) => (season, episode))
                        .Where(item => episodeId <= 0 || episodeId == item.episode.Id)
                        .ToList();

                    break;
                case "animes":
                    var anime = Anime.Client.Get(id);
                    series = anime;
                    episodes = anime.Seasons.Where(season => seasonId <= 0 || season.Id == seasonId)
                        .SelectMany(season => season.Episodes, (season, episode) => ((Season) season, episode))
                        .Where(item => episodeId <= 0 || episodeId == item.episode.Id)
                        .ToList();

                    break;
                default:
                    Console.WriteLine($"Cannot find resource type {type}");
                    return 1;
            }

            foreach (var file in FileNames.SelectMany(path =>
                FileInformation.Client.ListFolder(ById ? path : new PimixFile(path).Id, true))) {
                var suffix = file.Substring(file.LastIndexOf('.'));
                var ((season, episode), index) = SelectOne(episodes,
                    e => $"{file} => {series.Format(e.season, e.episode)}{suffix}",
                    "mapping", (null, null));
                if (index >= 0) {
                    FileInformation.Client.Link(file, series.Format(season, episode) + suffix);
                    episodes.RemoveAt(index);
                }
            }

            return 0;
        }
    }
}
