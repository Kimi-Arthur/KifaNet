namespace Kifa.Media;

public class MetadataFieldDifference {
    public string Category { get; set; } = "";
    public string Name { get; set; } = "";
    public string? File1Value { get; set; }
    public string? File2Value { get; set; }

    public string FullName => $"[{Category}] {Name}";
}
