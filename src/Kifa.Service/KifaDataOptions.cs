using System.Collections.Generic;
using System.Linq;

namespace Kifa.Service;

public class KifaDataOptions {
    // Only these fields should be returned. Empty means all fields.
    // Do add `Id` if that's the only fields needed.
    public List<string> Fields { get; set; } = [];

    // Only these fields should retrieve Link<> target values.
    public List<string> LinkedFields { get; set; } = [];

    public IEnumerable<string> GetUrlParameters()
        => [
            ..Fields.Select(field => $"fields={field}"),
            ..LinkedFields.Select(field => $"linked_fields={field}")
        ];
}
