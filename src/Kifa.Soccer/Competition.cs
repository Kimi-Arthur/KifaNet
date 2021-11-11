namespace Kifa.Soccer {
    public class Competition {
        public string Name { get; set; }
        public string ShortName { get; set; }

        public static Competition PremierLeague = new Competition {Name = "Premier League", ShortName = "EPL"};

        public static Competition Bundesliga = new Competition {Name = "Bundesliga", ShortName = "BL"};

        public static Competition UefaChampionsLeague =
            new Competition {Name = "UEFA Champions League", ShortName = "UCL"};

        public override string ToString() => Name;
    }
}
