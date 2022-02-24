namespace Kifa.Soccer; 

public class Program {
    public string Name { get; set; }
    public string CommonName { get; set; }

    public static Program MatchOfTheDay = new Program {Name = "Match of the Day", CommonName = "Match of the Day"};

    public static Program MatchOfTheDay2 =
        new Program {Name = "Match of the Day 2", CommonName = "Match of the Day"};
}