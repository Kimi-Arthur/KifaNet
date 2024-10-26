using System.Collections.Generic;

namespace Kifa.Infos;

public interface SearchableModel {
    // Provide a list potential matches for the given file path.
    IEnumerable<string> Search(string path);
}
