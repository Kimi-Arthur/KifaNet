using System.Text.RegularExpressions;
using Pimix;
using Pimix.Service;

namespace Kifa.Soccer {
    public class SoccerShow : DataModel {
        public Program Program { get; set; }
        public Competition Competition { get; set; }
        public Season Season { get; set; }
        public Round Round { get; set; }
        public Date AirDate { get; set; }

        // GaLaTaMaN HD Football - https://galatamanhdfb.blogspot.com
        static readonly Regex GalatamanPattern =
            new Regex(@"^(?<date>\d+)-(?<program>\w+)-M(?<round>\d+)-(?<competition>\w+)-F-1080\.\w+$");

        public static SoccerShow FromFileName(string fileName) => ParseGalataman(fileName);

        static SoccerShow ParseGalataman(string fileName) {
            var match = GalatamanPattern.Match(fileName);
            if (!match.Success) {
                return null;
            }

            var show = new SoccerShow {
                AirDate = Date.Parse(match.Groups["date"].Value),
                Round = Round.Regular(int.Parse(match.Groups["round"].Value)),
                Program = match.Groups["program"].Value switch {
                    "MTD" => Program.MatchOfTheDay,
                    "MTD2" => Program.MatchOfTheDay2,
                    _ => null
                },
                Competition = match.Groups["competition"].Value switch {
                    "EPL" => Competition.PremierLeague,
                    _ => null
                }
            };

            // TODO: Season should be based on competition and date.
            show.Season = Season.Regular(show.AirDate.Month > 6 ? show.AirDate.Year : show.AirDate.Year - 1);

            return show;
        }

        public override string ToString() =>
            $"/Soccer/{Program.CommonName}/{Season}/{AirDate} {Program.Name} {Competition.Name} {Round}";
    }

    public class Program {
        public string Name { get; set; }
        public string CommonName { get; set; }

        public static Program MatchOfTheDay = new Program {Name = "Match of the Day", CommonName = "Match of the Day"};

        public static Program MatchOfTheDay2 =
            new Program {Name = "Match of the Day 2", CommonName = "Match of the Day"};
    }

    public class Competition {
        public string Name { get; set; }
        public string ShortName { get; set; }

        public static Competition PremierLeague = new Competition {Name = "Premier League", ShortName = "EPL"};

        public static Competition Bundesliga = new Competition {Name = "Bundesliga", ShortName = "BL"};

        public static Competition UefaChampionsLeague =
            new Competition {Name = "UEFA Champions League", ShortName = "UCL"};

        public override string ToString() => Name;
    }

    public class Season {
        public int Year { get; set; }
        public bool SingleYear { get; set; } = false;

        public static Season Regular(int year) => new Season {Year = year};

        public static Season SingleYearSeason(int year) => new Season {Year = year, SingleYear = true};

        public override string ToString() => SingleYear ? $"Season {Year}" : $"Season {Year}-{(Year + 1) % 100}";
    }

    public class Round {
        public string Name { get; set; }

        public static Round Regular(int round) => new Round {Name = $"Round {round}"};

        public static Round Group(int round) => new Round {Name = $"Group Stage Round {round}"};

        public static Round RoundOf(string roundName, int? leg) => new Round {Name = $"{roundName}{GetLeg(leg)}"};

        public static Round RoundOf(int totalTeams, int? leg) => RoundOf($"Round of {totalTeams}", leg);

        public static Round Playoff(int? leg) => RoundOf("Play-off Round", leg);

        public static Round QuarterFinal(int? leg) => RoundOf("Quarter-final", leg);

        public static Round SemiFinal(int? leg) => RoundOf("Semi-final", leg);

        public static Round Final(int? leg) => RoundOf("Final", leg);

        static string GetLeg(int? leg) => leg.HasValue ? $" {(leg == 1 ? "1st" : "2nd")} Leg" : "";

        public override string ToString() => Name;
    }
}
