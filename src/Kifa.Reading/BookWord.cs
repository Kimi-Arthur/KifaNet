using Kifa.Service;

namespace Kifa.Reading; 

public class BookWord : DataModel, WithModelId<BookWord> {
    public static string ModelId => "reading/words";

    public string? Book { get; set; }
    public string? Word { get; set; }
    public string? Context { get; set; }
    public string? Meaning { get; set; }
    
    // Other fields to fill a Memrise word.
}
