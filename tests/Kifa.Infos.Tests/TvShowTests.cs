using System.Collections.Generic;
using Newtonsoft.Json;
using Pimix;
using Kifa.Infos;
using Kifa.Service;
using Xunit;

namespace Kifa.Infos.Tests {
    public class TvShowTests {
        public TvShowTests() {
            KifaServiceRestClient.ServerAddress = "http://www.jingbian.tk/api";
            Client = new KifaServiceRestClient<TvShow>();
        }

        public KifaServiceClient<TvShow> Client { get; set; }

        [Fact]
        public void FormatMultiSeason() {
            var show = new TvShow {
                Title = "Westworld",
                AirDate = Date.Parse("2016-10-02"),
                Region = Region.UnitedStates,
                PatternId = "multi_season",
                Seasons = new List<Season> {
                    new Season {
                        Id = 1,
                        AirDate = Date.Parse("2016-10-02"),
                        SeasonIdWidth = 1,
                        Episodes = new List<Episode> {
                            new Episode {
                                Id = 4
                            }
                        }
                    },
                    new Season {
                        Id = 2,
                        Title = "The Door",
                        SeasonIdWidth = 4,
                        EpisodeIdWidth = 1,
                        AirDate = Date.Parse("2018-04-22"),
                        Episodes = new List<Episode> {
                            new Episode {
                                SeasonIdWidth = 3,
                                Id = 1,
                                Title = "Journey Into Night"
                            }
                        }
                    }
                }
            };

            Assert.Equal(
                "/TV Shows/United States/Westworld (2016)/Season 2 The Door (2018)/Westworld S002E1 Journey Into Night",
                show.Format(show.Seasons[1], show.Seasons[1].Episodes[0]));
            Assert.Equal("/TV Shows/United States/Westworld (2016)/Season 1 (2016)/Westworld S1E04",
                show.Format(show.Seasons[0], show.Seasons[0].Episodes[0]));
        }

        [Fact]
        public void FormatSingleSeason() {
            var show = new TvShow {
                Title = "信長協奏曲",
                AirDate = Date.Parse("2014-10-13"),
                Region = Region.Japan,
                PatternId = "single_season",
                Seasons = new List<Season> {
                    new Season {
                        Id = 1,
                        Episodes = new List<Episode> {
                            new Episode {
                                Id = 1,
                                Title = "高校生"
                            }
                        }
                    }
                }
            };

            Assert.Equal("/TV Shows/Japan/信長協奏曲 (2014)/信長協奏曲 EP01 高校生",
                show.Format(show.Seasons[0], show.Seasons[0].Episodes[0]));
        }

        [Fact]
        public void Get() {
            var show = Client.Get("信長協奏曲");
            var s = JsonConvert.SerializeObject(show, Defaults.JsonSerializerSettings);
            Assert.Equal("信長協奏曲", show.Id);
            Assert.Equal(Region.Japan, show.Region);
            Assert.Equal(Language.Japanese, show.Language);
        }
    }
}
