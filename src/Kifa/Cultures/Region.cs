using System.Collections.Generic;

namespace Kifa;

public partial class Region : JsonSerializable {
    public required string Name { get; init; }
    public required string Code { get; init; }

    public string ToJson() => Name;

    public static implicit operator Region(string id) => All.GetValueOrDefault(id, Unknown);

    public override int GetHashCode() => Code.GetHashCode();

    public override bool Equals(object? obj)
        => obj != null && GetType() == obj.GetType() && Code == ((Region) obj).Code;

    public override string ToString() => Name;
}
