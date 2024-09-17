using System.Collections.Generic;
using System.Linq;
using NLog;

namespace Kifa.IO;

public class FolderInfo {
    public required string Folder { get; set; }

    public FileStat Overall { get; set; } = new() {
        FileCount = 0,
        TotalSize = 0
    };

    // Spec to stat.
    // Spec can be either storage client like "swiss", "google", or specific locations like
    // "local:server".
    public Dictionary<string, FileStat> Stats { get; set; } = new();

    public List<long> GetMissingSizes(List<string> targets)
        => targets.Select(t => Overall.TotalSize - Stats.GetValueOrDefault(t, new FileStat {
            FileCount = 0,
            TotalSize = 0
        }).TotalSize).ToList();
}

public class FileStat {
    static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    Dictionary<string, long> Files { get; set; } = new();
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

        Files[sha256] = size;

        TotalSize += size;
        FileCount++;
    }
}
