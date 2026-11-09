using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Kifa;

public partial class Language : JsonSerializable, IComparable<Language>, IEquatable<Language>,
    IParsable<Language> {
    public required string Name { get; init; }
    public required string NativeName { get; init; }
    public required string Code { get; init; }
    public required string Code3 { get; init; }
    public required string Code3T { get; init; }

    public string ToJson() => Code;

    public static implicit operator Language(string id) => All.GetValueOrDefault(id, Unknown);

    public static Language Parse(string s, IFormatProvider? provider = null) => s;

    public static bool TryParse([NotNullWhen(true)] string? s, IFormatProvider? provider,
        [MaybeNullWhen(false)] out Language result) {
        result = s == null ? null : (Language) s;
        return result != null;
    }

    public int CompareTo(Language? other) => string.CompareOrdinal(Code, other?.Code);

    public bool Equals(Language? other) => other != null && Code == other.Code;

    public override bool Equals(object? obj) => obj is Language other && Equals(other);

    public override int GetHashCode() => Code.GetHashCode();

    public static bool operator ==(Language? left, Language? right) => Equals(left, right);

    public static bool operator !=(Language? left, Language? right) => !Equals(left, right);

    public override string ToString() => Name;
}
