namespace Kifa.Media;

public class StreamComparisonResult {
    public int Index { get; set; }
    public string StreamType { get; set; } = "";
    public string Codec { get; set; } = "";
    public string? Details { get; set; }
    public bool IsAttachedPic { get; set; }

    public bool IsMatch { get; set; }
    public bool IsBitstreamMatch { get; set; }
    public bool IsDecodedMatch { get; set; }

    public string? File1BitstreamHash { get; set; }
    public string? File2BitstreamHash { get; set; }
    public string? File1DecodedHash { get; set; }
    public string? File2DecodedHash { get; set; }
}
