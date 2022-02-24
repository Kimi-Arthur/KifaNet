namespace Kifa.Soccer;

public class Competition {
    public string Name { get; set; }
    public string ShortName { get; set; }

    public static Competition PremierLeague = new() {
        Name = "Premier League",
        ShortName = "EPL"
    };

    public static Competition Bundesliga = new() {
        Name = "Bundesliga",
        ShortName = "BL"
    };

    public static Competition UefaChampionsLeague = new() {
        Name = "UEFA Champions League",
        ShortName = "UCL"
    };

    public override string ToString() => Name;
}
