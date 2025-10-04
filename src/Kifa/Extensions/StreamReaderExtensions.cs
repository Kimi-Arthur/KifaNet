using System.Collections.Generic;
using System.IO;

namespace Kifa;

public static class StreamReaderExtensions {
    public static IEnumerable<string> GetLines(this StreamReader reader) {
        string? line;
        while ((line = reader.ReadLine()) != null) {
            yield return line;
        }
    }
}
