using System.Collections.Generic;
using Kifa.Service;

namespace Kifa.Languages.Kindle;

public class KindleBook : DataModel, WithModelId<KindleBook> {
    public static string ModelId => "kindle/books";

    public static KifaServiceClient<KindleBook> Client { get; set; } =
        new KifaServiceRestClient<KindleBook>();

    // Id will be the name with proper deduplication.

    // Normalized book key with ':' mapped to '_'.
    public string? HashKey { get; set; }

    public override SortedSet<string> GetVirtualItems()
        => HashKey != null
            ? new SortedSet<string> {
                VirtualItemPrefix + HashKey
            }
            : new SortedSet<string>();
}
