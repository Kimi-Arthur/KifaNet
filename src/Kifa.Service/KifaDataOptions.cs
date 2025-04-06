using System.Collections.Generic;

namespace Kifa.Service;

public class KifaDataOptions {
    // Only these fields should be returned. Empty means all fields.
    // Do add `Id` if that's the only fields needed.
    public List<string> Fields { get; set; } = [];

    // Only these fields should retrieve Link<> target values.
    public List<string> LinkedFields { get; set; } = [];

    public List<string> GetUrlParameters() {
        var parameters = new List<string>();
        if (Fields.Count > 0) {
            parameters.Add($"fields=[{Fields.JoinBy(",")}]");
        }

        if (LinkedFields.Count > 0) {
            parameters.Add($"linked_fields=[{LinkedFields.JoinBy(",")}]");
        }

        return parameters;
    }
}
