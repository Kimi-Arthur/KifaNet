using System.Collections.Generic;
using Kifa.Service;

namespace Kifa.Memrise;

public class MemriseLevel : DataModel {
    public const string ModelId = "memrise/levels";

    public List<Link<MemriseWord>> Words { get; set; }
}
