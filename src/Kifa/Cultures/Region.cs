using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Kifa;

public partial class Region : JsonSerializable<Region> {
    public required string Name { get; init; }
    public required string Code { get; init; }

    public string ToJson() => Name;

    [return: NotNullIfNotNull(nameof(id))]
    public static implicit operator Region?(string? id)
        => id == null ? null : All.GetValueOrDefault(id, Unknown);

    public override int GetHashCode() => Code.GetHashCode();

    public override bool Equals(object? obj)
        => obj != null && GetType() == obj.GetType() && Code == ((Region) obj).Code;

    public override string ToString() => Name;
}
