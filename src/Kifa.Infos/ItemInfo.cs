using System.Collections.Generic;

namespace Kifa.Infos;

// Info for an item to be linked to a standard location.
public class ItemInfo {
    // The target file path without extension.
    public required string Path { get; set; }

    public int SeasonId { get; set; }

    public int EpisodeId { get; set; }

    public bool Matched { get; set; }
}

public class ItemsInfo {
    public Formattable Info { get; set; }
    public List<ItemInfo> Items { get; set; }
}
