using System.Collections.Generic;
using FluentAssertions;
using Kifa.Configs;
using Xunit;

namespace Kifa.Infos.Tests;

public class AnimeTests {
    public AnimeTests() {
        KifaConfigs.Init();
    }

    [Fact]
    public void FormatMultiSeason() {
        var show = new Anime {
            Title = "咲-Saki-",
            AirDate = Date.Parse("2009-04-06"),
            PatternId = "multi_season",
            Seasons = new List<Season> {
                new() {
                    Id = 1,
                    AirDate = Date.Parse("2009-04-06"),
                    SeasonIdWidth = 1,
                    Episodes = new List<Episode> {
                        new() {
                            Id = 4
                        }
                    }
                },
                new() {
                    Id = 3,
                    Title = "全国編",
                    SeasonIdWidth = 4,
                    EpisodeIdWidth = 1,
                    AirDate = Date.Parse("2014-01-06"),
                    Episodes = new List<Episode> {
                        new() {
                            SeasonIdWidth = 3,
                            Id = 12,
                            Title = "真実"
                        }
                    }
                }
            }
        };

        Assert.Equal("/Anime/咲-Saki- (2009)/Season 3 全国編 (2014)/咲-Saki- S003E12 真実",
            show.Format(show.Seasons[1], show.Seasons[1].Episodes[0]));
        Assert.Equal("/Anime/咲-Saki- (2009)/Season 1 (2009)/咲-Saki- S1E04",
            show.Format(show.Seasons[0], show.Seasons[0].Episodes[0]));
    }

    [Fact]
    public void FormatSingleSeason() {
        var show = new Anime {
            Title = "咲-Saki-",
            AirDate = Date.Parse("2009-04-06"),
            PatternId = "single_season",
            Seasons = new List<Season> {
                new() {
                    Id = 1,
                    Episodes = new List<Episode> {
                        new() {
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
    public void FillTest() {
        var show = new Anime {
            Id = "咲-Saki-",
            TmdbId = "29884"
        };
        show.Fill();

        // The seasons are not organized correctly:
        // https://www.themoviedb.org/tv/69038-saki-episode-of-side-a/discuss/63054c4e2e2b2c007a6de3fc
        show.Seasons.Should().HaveCount(2);

        Assert.Equal("咲-Saki-", show.Id);
        Assert.Equal("全国編", show.Seasons[1].Title);
    }
}
