using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Kifa.IO.StorageClients;

public class ShardedStorageClient : StorageClient {
    public long ShardSize { get; set; }
    public required List<StorageClient> Clients { get; set; }

    public override string Type => "sharded";
    public override string Id => "";

    public override string ToString() {
        var specs = new List<string>();
        var lastId = "";
        var lastIdCount = 0;
        foreach (var id in Clients.Select(c => c.Id)) {
            if (lastId != id) {
                if (lastIdCount > 0) {
                    specs.Add(lastId + (lastIdCount > 1 ? $"*{lastIdCount}" : ""));
                }

                lastId = id;
                lastIdCount = 0;
            }

            lastIdCount++;
        }

        if (lastIdCount > 0) {
            specs.Add(lastId + (lastIdCount > 1 ? $"*{lastIdCount}" : ""));
        }

        return $"{Clients.First().Type}:{string.Join("+", specs)}";
    }

    public override long Length(string path) {
        var lengths = GetShards(path).Select(shard => shard.client.Length(shard.path)).ToList();
        return lengths.Any(l => l == 0) ? 0 : lengths.Sum();
    }

    public override void Delete(string path) {
        foreach (var (client, p, _) in GetShards(path)) {
            client.Delete(p);
        }
    }

    public override void Touch(string path) {
        throw new NotImplementedException();
    }

    public override Stream OpenRead(string path)
        => new MultiReadStream(GetShards(path).Select(shard => shard.client.OpenRead(shard.path))
            .ToList());

    public override void Write(string path, Stream stream) {
        var length = stream.Length;
        foreach (var (client, p, index) in GetShards(path)) {
            client.Write(p, new PatchedStream(stream) {
                IgnoreBefore = ShardSize * index,
                IgnoreAfter = Math.Max(length - ShardSize * index - ShardSize, 0)
            });
        }
    }

    IEnumerable<(StorageClient client, string path, int index)> GetShards(string path)
        => Clients.Select((c, i) => (c, $"{path}.{i}", i));
}
