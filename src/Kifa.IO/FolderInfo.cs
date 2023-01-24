using System.Collections.Generic;

namespace Kifa.IO;

public class FolderInfo {
    public required string Folder { get; set; }

    // Spec to stat.
    // Spec can be either storage client like "swiss", "google", or specific locations like
    // "local:server".
    public Dictionary<string, FileStat> Stats { get; set; } = new();
}

public class FileStat {
    public long TotalSize { get; set; }
    public long FileCount { get; set; }

    public void AddFile(long size) {
        TotalSize += size;
        FileCount++;
    }
}
