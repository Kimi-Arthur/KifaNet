using System.Collections.Generic;
using Kifa.Service;

namespace Kifa.Memrise;

public class MemriseLevel : DataModel, WithModelId {
    public static string ModelId => "memrise/levels";

    public List<Link<MemriseWord>> Words { get; set; }
}
