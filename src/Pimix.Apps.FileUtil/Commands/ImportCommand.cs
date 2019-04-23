using System;
using System.Collections.Generic;
using CommandLine;
using Pimix.Infos;
using Pimix.IO;
using Pimix.Service;

namespace Pimix.Apps.FileUtil.Commands {
    [Verb("import", HelpText = "Import files from /Downloads folder with resource id.")]
    class ImportCommand : PimixFileCommand {
        List<(Season season, Episode episode)> episodes;
        TvShow tvShow;

        public override bool ById => true;
        public override bool Recursive => true;

        [Option('s', "source-id", HelpText = "ID for the source, like tv_shows/Westworld/1")]
        public string SourceId { get; set; }

        public override int Execute() {
            var segments = SourceId.Split('/');
            switch (segments[0]) {
                case "tv_shows":
                    tvShow = PimixService.Get<TvShow>(segments[1]);
                    episodes = new List<(Season season, Episode episode)>();
                    foreach (var season in tvShow.Seasons) {
                        foreach (var episode in season.Episodes) {
                            episodes.Add((season, episode));
                        }
                    }

                    break;
                default:
                    Console.WriteLine($"Cannot find resource type {segments[0]}");
                    return 1;
            }

            return base.Execute();
        }

        protected override int ExecuteOne(string file) {
            var suffix = file.Substring(file.LastIndexOf('.'));
            var ((season, episode), index) = SelectOne(episodes, e => $"{file} => {tvShow.Format(e.season, e.episode)}{suffix}",
                "mapping", (null, null));
            if (index >= 0) {
                PimixService.Link<FileInformation>(file, tvShow.Format(season, episode) + suffix);
                episodes.RemoveAt(index);
            }

            return 0;
        }
    }
}
