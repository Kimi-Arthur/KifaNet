using System.Collections.Generic;
using Newtonsoft.Json;
using Pimix;
using Pimix.Infos;
using Pimix.Service;
using Xunit;

namespace PimixTest.Infos {
    public class AnimeTests {
        public PimixServiceClient Client { get; set; }

        public AnimeTests() {
            PimixServiceRestClient.PimixServerApiAddress = "http://www.pimix.tk/api";
            Client = new PimixServiceRestClient();
        }

        [Fact]
        public void Get() {
            var show = Client.Get<Anime>("咲-Saki-");
            var s = JsonConvert.SerializeObject(show, Defaults.JsonSerializerSettings);
            Assert.Equal("咲-Saki-", show.Id);
            Assert.Equal("全国編", show.Seasons[2].Title);
        }

        [Fact]
        public void FormatSingleSeason() {
            var show = new Anime {
                Title = "咲-Saki-",
                AirDate = Date.Parse("2009-04-06"),
                PatternId = "single_season",
                Seasons = new List<AnimeSeason> {
                    new AnimeSeason {
                        Id = 1,
                        Episodes = new List<Episode> {
                            new Episode {
                                Id = 1,
                                Title = "出会い"
                            }
                        }
                    }
                }
            };

            Assert.Equal("/Anime/咲-Saki- (2009)/咲-Saki- EP01 出会い",
                show.Format(show.Seasons[0], show.Seasons[0].Episodes[0]));
        }

        [Fact]
        public void FormatMultiSeason() {
            var show = new Anime {
                Title = "咲-Saki-",
                AirDate = Date.Parse("2009-04-06"),
                PatternId = "multi_season",
                Seasons = new List<AnimeSeason> {
                    new AnimeSeason {
                        Id = 1,
                        AirDate = Date.Parse("2009-04-06"),
                        SeasonIdWidth = 1,
                        Episodes = new List<Episode> {
                            new Episode {
                                Id = 4,
                            }
                        }
                    },
                    new AnimeSeason {
                        Id = 3,
                        Title = "全国編",
                        SeasonIdWidth = 4,
                        EpisodeIdWidth = 1,
                        AirDate = Date.Parse("2014-01-06"),
                        Episodes = new List<Episode> {
                            new Episode {
                                SeasonIdWidth = 3,
                                Id = 12,
                                Title = "真実"
                            }
                        }
                    }
                }
            };

            Assert.Equal(
                "/Anime/咲-Saki- (2009)/Season 3 全国編 (2014)/咲-Saki- S003E12 真実",
                show.Format(show.Seasons[1], show.Seasons[1].Episodes[0]));
            Assert.Equal("/Anime/咲-Saki- (2009)/Season 1 (2009)/咲-Saki- S1E04",
                show.Format(show.Seasons[0], show.Seasons[0].Episodes[0]));
        }
    }
}
