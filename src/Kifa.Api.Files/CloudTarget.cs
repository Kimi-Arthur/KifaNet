using System;

namespace Kifa.Api.Files;

public class CloudTarget {
    public CloudServiceType ServiceType { get; set; }
    public CloudFormatType FormatType { get; set; }

    // Parses targetSpec like google.v1, swiss.v2 etc.
    public static CloudTarget Parse(string targetSpec) {
        var segments = targetSpec.Split(".");
        return new CloudTarget {
            ServiceType = Enum.Parse<CloudServiceType>(segments[0], true),
            FormatType = Enum.Parse<CloudFormatType>(segments[1], true)
        };
    }

    public override string ToString()
        => $"{ServiceType.ToString().ToLowerInvariant()}.{FormatType.ToString().ToLowerInvariant()}";
}
