using System;
using Kifa.Service;
using Newtonsoft.Json;
using YamlDotNet.Serialization;

namespace Kifa.IO;

public class FileIdInfo : DataModel, WithModelId<FileIdInfo> {
    public static string ModelId => "file_ids";

    public static KifaServiceClient<FileIdInfo> Client { get; set; } =
        new KifaServiceRestClient<FileIdInfo>();

    // Query is only needed from inode to SHA256.

    // id = local/kch/238353647342895845

    // File => <file_id> => file_ids/<file_id> => KifaFile location

    string? id;

    public override string Id {
        get => id ??= $"{HostId}/{InternalFildId}";
        set => id = value;
    }

    [JsonIgnore]
    [YamlIgnore]
    public string? HostId { get; set; }

    [JsonIgnore]
    [YamlIgnore]
    public string? InternalFildId { get; set; }

    // Security measures to make sure the file is not changed.
    public long Size { get; set; }
    public DateTime? LastModified { get; set; }

    public string? Sha256 { get; set; }
}
