using System.Collections.Generic;
using Kifa.Service;

namespace Kifa.Reading;

public class BookNote : DataModel, WithModelId<BookNote> {
    public static string ModelId => "reading/notes";

    public string? Text { get; set; }

    // Selection as number of characters.
    public int Start { get; set; }
    public int End { get; set; }

    public string? Comment { get; set; }

    // Id of the book.
    public string? Book { get; set; }

    // Location information like {"chapter": "1"}, and/or {"page": "21"}.
    public Dictionary<string, string> Locations { get; set; } = new();
}
