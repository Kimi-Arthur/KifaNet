﻿using System;
using System.Collections.Generic;
using CommandLine;
using Pimix.Infos;
using Pimix.IO;
using Pimix.Service;

namespace Pimix.Apps.FileUtil.Commands {
    [Verb("import", HelpText = "Import files from /Downloads folder with resource id.")]
    class ImportCommand : PimixFileCommand {
        List<(Season season, Episode episode)> episodes;
        Formattable series;

        public override bool ById => true;
        protected override bool NaturalSorting => true;

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

            switch (type) {
                case "tv_shows":
                    var tvShow = PimixService.Get<TvShow>(id);
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
                    var anime = PimixService.Get<Anime>(id);
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

        protected override int ExecuteOne(string file) {
            var suffix = file.Substring(file.LastIndexOf('.'));
            var ((season, episode), index) = SelectOne(episodes,
                e => $"{file} => {series.Format(e.season, e.episode)}{suffix}",
                "mapping", (null, null));
            if (index >= 0) {
                PimixService.Link<FileInformation>(file, series.Format(season, episode) + suffix);
                episodes.RemoveAt(index);
            }

            return 0;
        }
    }
}