namespace Kifa.Tools.NoteUtil;

public class Heading {
    public int Level { get; set; }
    public string Title { get; set; }

    public static Heading Get(string markdownLine) {
        if (!markdownLine.StartsWith("#")) {
            return null;
        }

        var parts = markdownLine.Split(" ", 2);
        return new Heading {
            Level = parts[0].Length,
            Title = parts[1]
        };
    }
}
