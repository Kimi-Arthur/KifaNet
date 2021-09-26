using System.Text.RegularExpressions;
using Kifa.Service;

namespace Kifa.Soccer {
    public class SoccerShow : DataModel<SoccerShow> {
        public Program Program { get; set; }
        public Competition Competition { get; set; }
        public Season Season { get; set; }
        public Round Round { get; set; }
        public Date AirDate { get; set; }

        // GaLaTaMaN HD Football - https://galatamanhdfb.blogspot.com
        static readonly Regex GalatamanPattern =
            new Regex(@"/(?<date>\d+)-(?<program>\w+)-M(?<round>\d+)-(?<competition>\w+)-F-1080\.\w+$");

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
}
