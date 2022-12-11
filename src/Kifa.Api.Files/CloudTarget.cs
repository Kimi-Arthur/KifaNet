using System;
using Kifa.IO.FileFormats;

namespace Kifa.Api.Files;

public class CloudTarget {
    public required CloudServiceType ServiceType { get; set; }
    public required KifaFileFormat FormatType { get; set; }

    // Parses targetSpec like google.v1, swiss.v2 etc.
    public static CloudTarget Parse(string targetSpec) {
        var segments = targetSpec.Split(".");

        return new CloudTarget {
            ServiceType = Enum.Parse<CloudServiceType>(segments[0], true),
            FormatType = segments[1] switch {
                "v1" => KifaFileV1Format.Instance,
                "v2" => KifaFileV2Format.Instance,
                _ => throw new Exception($"Format {segments[1]} is invalid or unsupported.")
            }
        };
    }

    public override string ToString()
        => $"{ServiceType.ToString().ToLowerInvariant()}.{FormatType}";
}
