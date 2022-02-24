namespace Kifa.Soccer; 

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