namespace Kifa.Soccer;

public class Season {
    public int Year { get; set; }
    public bool SingleYear { get; set; } = false;

    public static Season Regular(int year)
        => new() {
            Year = year
        };

    public static Season SingleYearSeason(int year)
        => new() {
            Year = year,
            SingleYear = true
        };

    public override string ToString()
        => SingleYear ? $"Season {Year}" : $"Season {Year}-{(Year + 1) % 100}";
}
