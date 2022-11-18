using System.Collections.Generic;

namespace Kifa;

public partial class Language : JsonSerializable {
    public required string Name { get; init; }
    public required string NativeName { get; init; }
    public required string Code { get; init; }
    public required string Code3 { get; init; }
    public required string Code3T { get; init; }

    public string ToJson() => Code;

    public static implicit operator Language(string id) => All.GetValueOrDefault(id, Unknown);

    public override int GetHashCode() => Code.GetHashCode();

    public override bool Equals(object? obj)
        => obj != null && GetType() == obj.GetType() && Code == ((Language) obj).Code;

    public override string ToString() => Name;
}
