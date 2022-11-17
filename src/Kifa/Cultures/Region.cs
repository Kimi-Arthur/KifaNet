namespace Kifa;

public partial class Region : JsonSerializable {
    public required string Name { get; init; }
    public required string Code { get; init; }

    public string ToJson() => Name;

    Region() {
    }

    public Region(string id) {
        var region = All[id];
        Name = region.Name;
        Code = region.Code;
    }

    public static implicit operator Region(string data) => All[data];

    public override int GetHashCode() => Code.GetHashCode();

    public override bool Equals(object? obj)
        => obj != null && GetType() == obj.GetType() && Code == ((Region) obj).Code;

    public override string ToString() => Name;
}
