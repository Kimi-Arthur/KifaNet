using System.Collections.Generic;
using System.Linq;
using Kifa.Service;

namespace Kifa.Languages.Kindle;

public class KindleBook : DataModel, WithModelId<KindleBook> {
    public static string ModelId => "kindle/books";

    public static KifaServiceClient<KindleBook> Client { get; set; } =
        new KifaServiceRestClient<KindleBook>();

    // Id will be <AUTHOR>/<TITLE>. <TITLE?> can contain '/'.

    public string? Author => Id?.Split('/')[0];

    public string? Title => Id?.Split('/')[1..].JoinBy('/');

    public string? Language { get; set; }

    // Normalized book key with ':' mapped to '_'.
    // Examples:
    //   - CR!RH6FXHDSTX2JZ4F1433CP1GZFQ2E:5F77425A -> RH6FXHDSTX2JZ4F1433CP1GZFQ2E_5F77425A
    //   - CR!54RAATQMNX5WH62V53K9PGBPJJ4S -> 54RAATQMNX5WH62V53K9PGBPJJ4S
    public HashSet<string> HashKeys { get; set; } = [];

    public override SortedSet<string> GetVirtualItems()
        => new(HashKeys.Select(key => VirtualItemPrefix + key));
}
