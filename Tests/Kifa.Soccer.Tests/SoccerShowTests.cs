using Xunit;

namespace Kifa.Soccer.Tests;

public class SoccerShowTests {
    [Fact]
    public void ToStringTest() {
        var s = new SoccerShow {
            Competition = Competition.PremierLeague,
            Program = Program.MatchOfTheDay,
            AirDate = Date.Parse("2020-09-12"),
            Round = Round.Regular(1),
            Season = Season.Regular(2020)
        };
        Assert.Equal(
            "/Soccer/Match of the Day/Season 2020-21/2020-09-12 Match of the Day Premier League Round 1",
            s.ToString());
    }

    [Fact]
    public void FromFileNameTest() {
        var s = SoccerShow.FromFileName("/Downloads/Soccer/2009/20200912-MTD-M1-EPL-F-1080.ts");
        Assert.Equal(
            "/Soccer/Match of the Day/Season 2020-21/2020-09-12 Match of the Day Premier League Round 1",
            s.ToString());
    }
}
