using Kifa.Service;

namespace Kifa.Reading;

public class Book : DataModel, WithModelId<Book> {
    public static string ModelId => "reading/books";

    public string? Title { get; set; }
    public string? Author { get; set; }
}
