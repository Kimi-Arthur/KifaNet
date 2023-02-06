using System.Collections.Generic;
using NLog;

namespace Kifa.IO;

public class FolderInfo {
    public required string Folder { get; set; }

    // Spec to stat.
    // Spec can be either storage client like "swiss", "google", or specific locations like
    // "local:server".
    public Dictionary<string, FileStat> Stats { get; set; } = new();
}

public class FileStat {
    static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    public Dictionary<string, long> Files { get; set; }
    public long TotalSize { get; set; }
    public long FileCount { get; set; }

    public void AddFile(string sha256, long size) {
        if (Files.TryGetValue(sha256, out var recordedSize)) {
            if (size != recordedSize) {
                Logger.Warn(
                    $"Files with same SHA256 ({sha256}) have different sizes ({recordedSize} != {size})");
            }

            return;
        }

        TotalSize += size;
        FileCount++;
    }
}
