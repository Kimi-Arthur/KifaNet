using System;
using System.Collections.Generic;
using Xunit;

namespace Kifa.SkyCh.Tests; 

public class SkyProgramTests {
    public static IEnumerable<object[]> Data =>
        new List<object[]> {
            new object[] {
                "1",
                new SkyProgram {
                    Id = "1",
                    Type = "Sports non-event",
                    Title = "Eurosport News",
                    Subtitle = "",
                    AirDateTime = DateTime.Parse("2018-11-05 23:10:00"),
                    Channel = "Eurosport HD",
                    Duration = TimeSpan.FromMinutes(5),
                    ImageLink = "https://ondemo.tmsimg.com/assets/p9555256_b_h2_aa.jpg",
                    Categories = new List<string> {
                        "News"
                    }
                }
            },
            new object[] {
                "100",
                new SkyProgram {
                    Id = "100",
                    Type = "Sports non-event",
                    Title = "Football Greats",
                    Subtitle = "Xavi Hernandez",
                    AirDateTime = DateTime.Parse("2018-11-01 20:20"),
                    Channel = "Sport Digital",
                    Duration = TimeSpan.FromMinutes(40),
                    ImageLink = "/Content/Img/_Sky/Pages/Epg/sport.svg",
                    Categories = new List<string> {
                        "Soccer"
                    }
                }
            },
            new object[] {
                "2693487",
                new SkyProgram {
                    Id = "2693487",
                    Type = "Sports event",
                    Title = "Football : UEFA Champions League - Matchday 2",
                    Subtitle = "Paris Saint-Germain / Manchester City",
                    AirDateTime = DateTime.Parse("2021-09-28 20:15"),
                    Channel = "Blue Sports 1 HD",
                    Duration = TimeSpan.FromMinutes(165),
                    ImageLink = "https://ondemo.tmsimg.com/assets/p20170832_tb2_h2_ae.jpg",
                    Categories = new List<string> {
                        "Soccer"
                    }
                }
            },
        };

    [Theory]
    [MemberData(nameof(Data))]
    public void EpgExtractionTest(string id, SkyProgram expectedProgram) {
        var program = new SkyProgram {
            Id = id
        };
        program.Fill();

        Assert.Equal(expectedProgram.ToString(), program.ToString());
    }
}