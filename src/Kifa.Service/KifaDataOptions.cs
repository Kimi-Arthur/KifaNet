using System.Collections.Generic;
using System.Linq;

namespace Kifa.Service;

public class KifaDataOptions {
    // Only these fields should be returned. Empty means all fields.
    // Do add `Id` if that's the only fields needed.
    public List<string> Fields { get; set; } = [];

    // Only these fields should retrieve Link<> target values.
    public List<string> LinkedFields { get; set; } = [];

    public IEnumerable<(string Key, object? Value)> GetUrlParameters()
        => [
            ..Fields.Select(field => ("fields", (object?) field)),
            ..LinkedFields.Select(field => ("linked_fields", (object?) field))
        ];
}
