using System.Collections.Generic;

namespace Kifa;

public partial class Language : JsonSerializable {
    public required string Name { get; init; }
    public required string NativeName { get; init; }
    public required string Code { get; init; }
    public required string Code3 { get; init; }
    public required string Code3T { get; init; }

    public string ToJson() => Code;

    Language() {
    }

    public Language(string id) {
        var lang = All[id];
        Name = lang.Name;
        NativeName = lang.NativeName;
        Code = lang.Code;
        Code3 = lang.Code3;
        Code3T = lang.Code3T;
    }

    public static implicit operator Language(string data) => All.GetValueOrDefault(data, Unknown);

    public override int GetHashCode() => Code.GetHashCode();

    public override bool Equals(object? obj)
        => obj != null && GetType() == obj.GetType() && Code == ((Language) obj).Code;

    public override string ToString() => Name;
}
