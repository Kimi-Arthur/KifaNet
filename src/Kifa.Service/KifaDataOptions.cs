using System.Collections.Generic;

namespace Kifa.Service;

public class KifaDataOptions {
    // Force refresh check now, bypassing RefreshInterval.
    public bool Refresh { get; set; }

    // Traversal strategy: full exhaustive fetch ignoring early-exit stops.
    public bool Deep { get; set; }

    // Disk formatting: rewrite JSON layout on disk.
    public bool Rewrite { get; set; }

    // Only these fields should be returned. Empty means all fields.
    // Do add `Id` if that's the only fields needed.
    public List<string> Fields { get; set; } = [];

    // Only these fields should retrieve Link<> target values.
    public List<string> LinkedFields { get; set; } = [];

    // Target language for sidecar translation overlay (e.g., "zh").
    public Language? Translated { get; set; }

    public KifaDataOptions Clone() => new() {
        Refresh = Refresh,
        Deep = Deep,
        Rewrite = Rewrite,
        Fields = [..Fields],
        LinkedFields = [..LinkedFields],
        Translated = Translated
    };

    public IEnumerable<(string Key, object? Value)> GetUrlParameters() {
        if (Refresh) {
            yield return ("refresh", "true");
        }

        if (Deep) {
            yield return ("deep", "true");
        }

        if (Rewrite) {
            yield return ("rewrite", "true");
        }

        if (Translated != null) {
            yield return ("translated", Translated.Code);
        }

        foreach (var field in Fields) {
            yield return ("fields", field);
        }

        foreach (var linkedField in LinkedFields) {
            yield return ("linked_fields", linkedField);
        }
    }
}
