using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Kifa.Configs;
using Xunit;

namespace Kifa.Infos.Tests;

public class TvShowTests {
    public TvShowTests() {
        KifaConfigs.Init();
    }

    [Fact]
    public void FormatMultiSeason() {
        var show = new TvShow {
            Title = "Westworld",
            AirDate = Date.Parse("2016-10-02"),
            Region = Region.UnitedStates,
            PatternId = "multi_season",
            Seasons = new List<Season> {
                new() {
                    Id = 1,
                    AirDate = Date.Parse("2016-10-02"),
                    SeasonIdWidth = 1,
                    Episodes = new List<Episode> {
                        new() {
                            Id = 4
                        }
                    }
                },
                new() {
                    Id = 2,
                    Title = "The Door",
                    SeasonIdWidth = 4,
                    EpisodeIdWidth = 1,
                    AirDate = Date.Parse("2018-04-22"),
                    Episodes = new List<Episode> {
                        new() {
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
                new() {
                    Id = 1,
                    Episodes = new List<Episode> {
                        new() {
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
    public void FillTest() {
        var show = new TvShow {
            Id = "信長協奏曲",
            TmdbId = "65200",
            Language = Language.Japanese
        };

        show.Fill();
        Assert.Equal("信長協奏曲", show.Id);
        Assert.Equal(Region.Japan, show.Region);
        Assert.Equal(Language.Japanese, show.Language);
    }

    [Fact]
    public void ParseTest() {
        var show = new TvShow {
            Id = "Mayday",
            Title = "Mayday",
            Region = Region.Canada,
            AirDate = Date.Parse("2003-09-03"),
            PatternId = "multi_season",
            Seasons = new List<Season> {
                new Season {
                    Id = 1,
                    Episodes = new List<Episode> {
                        new Episode {
                            Id = 1,
                            Title = "Racing the storm"
                        }
                    }
                }
            }
        };

        var parsed =
            show.Parse(
                "/TV Shows/Canada/Mayday (2003)/Season 1 (2003)/Mayday S01E01 Racing the storm.mp4");

        Assert.Equal(1, parsed.Value.Season.Id);
        Assert.Equal(1, parsed.Value.Episode.Id);
    }

    [Fact]
    public void GetItemsTest() {
        var items = TvShow.GetItems(["TV Shows", "Westworld", "2"]).Checked().ToList();
        items[0].Path.Should().Be(
            "/TV Shows/United States/Westworld (2016)/Season 2 The Door (2018)/Westworld S02E01 Journey Into Night");
    }
}
