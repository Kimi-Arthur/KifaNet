using System;
using Kifa.Service;

namespace Kifa.IO;

public class FileIdInfo : DataModel, WithModelId<FileIdInfo> {
    public static string ModelId => "file_ids";

    public static KifaServiceClient<FileIdInfo> Client { get; set; } =
        new KifaServiceRestClient<FileIdInfo>();

    // Query is only needed from inode to SHA256.

    // id = local/kch/238353647342895845

    // File => <file_id> => file_ids/<file_id> => KifaFile location

    // Security measures to make sure the file is not changed.
    public long Size { get; set; }
    public DateTime? LastModified { get; set; }

    public string? Sha256 { get; set; }
}
