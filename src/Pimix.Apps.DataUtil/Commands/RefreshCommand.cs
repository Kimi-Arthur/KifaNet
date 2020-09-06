using System;
using System.Collections.Generic;
using System.Linq;
using CommandLine;
using NLog;
using Pimix.Infos;

namespace Pimix.Apps.DataUtil.Commands {
    [Verb("refresh", HelpText = "Refresh Data for an entity. Currently tv_shows and animes are supported.")]
    class RefreshCommand : PimixCommand {
        static readonly Logger logger = LogManager.GetCurrentClassLogger();

        [Value(0, Required = true, HelpText = "Entity to refresh.")]
        public string EntityId { get; set; }

        public override int Execute() {
            var segments = EntityId.Split('/');
            var type = segments[0];
            var id = segments[1];

            var episodes = new List<(Season season, Episode episode)>();

            switch (type) {
                case "tv_shows":
                    var oldTvEpisodes = TvShow.Client.Get(id).Seasons
                        .SelectMany(s => s.Episodes.Select(e => (s.Id, e.Id))).ToHashSet();
                    TvShow.Client.Refresh(id);
                    episodes.AddRange(TvShow.Client.Get(id).Seasons.SelectMany(s => s.Episodes.Select(e => (s, e)))
                        .Where(episode => !oldTvEpisodes.Contains((episode.s.Id, episode.e.Id))));
                    break;
                case "animes":
                    var oldAnimeEpisodes = Anime.Client.Get(id).Seasons
                        .SelectMany(s => s.Episodes.Select(e => (s.Id, e.Id))).ToHashSet();
                    Anime.Client.Refresh(id);
                    episodes.AddRange(Anime.Client.Get(id).Seasons
                        .SelectMany(s => s.Episodes.Select(e => (s: s as Season, e))).Where(episode =>
                            !oldAnimeEpisodes.Contains((episode.s.Id, episode.e.Id))));
                    break;
                default:
                    Console.WriteLine($"Cannot find resource type {type}");
                    return 1;
            }

            if (episodes.Count == 0) {
                logger.Info($"Successfully refreshed {EntityId}. No new episodes found.");
                return 0;
            }

            logger.Info($"Added {episodes.Count} new episodes to {EntityId}!");
            foreach (var (season, episode) in episodes) {
                logger.Info($"Season {season.Id} episode {episode.Id}: {episode.Title}");
            }

            return 0;
        }
    }
}
