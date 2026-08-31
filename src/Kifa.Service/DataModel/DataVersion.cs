using System;
using System.Globalization;

namespace Kifa.Service;

public class DataVersion : JsonSerializable, IComparable<DataVersion>, IComparable<DateTimeOffset>,
    IEquatable<DataVersion>, IEquatable<DateTimeOffset> {
    public DateTimeOffset Value { get; set; }

    public DataVersion() {
    }

    public DataVersion(DateTimeOffset value) {
        Value = value.ToUniversalTime();
    }

    public DataVersion(DateTime value) {
        Value = new DateTimeOffset(value.ToUniversalTime(), TimeSpan.Zero);
    }

    public DataVersion(int year, int month, int day, int hour = 0, int minute = 0, int second = 0,
        int millisecond = 0, int microsecond = 0) {
        Value = new DateTimeOffset(year, month, day, hour, minute, second, millisecond, microsecond,
            TimeSpan.Zero);
    }

    const string Format = "yyyyMMddHHmmssffffff";

    public string ToJson() => Value.UtcDateTime.ToString(Format);

    static readonly string[] Formats = [
        "yyyyMMddHHmmssffffff",
        "yyyyMMddHHmmssfff",
        "yyyyMMddHHmmss",
        "yyyyMMddHHmm",
        "yyyyMMdd",
        "yyyy-MM-ddTHH:mm:ss.ffffffzzz",
        "yyyy-MM-ddTHH:mm:ss.ffffff",
        "yyyy-MM-ddTHH:mm:sszzz",
        "yyyy-MM-ddTHH:mm:ss",
        "yyyy-MM-dd HH:mm:ss.ffffff",
        "yyyy-MM-dd HH:mm:ss",
        "yyyy-MM-dd"
    ];

    public static DataVersion? Parse(string? data) {
        if (data == null || data.Length == 0) {
            return null;
        }

        if (long.TryParse(data, out var num)) {
            // Legacy integer version (< 20000101): treat as null to trigger refresh.
            if (num < 20000101) {
                return null;
            }
        }

        if (DateTimeOffset.TryParseExact(data, Formats, CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var dto)) {
            return new DataVersion(dto);
        }

        if (DateTimeOffset.TryParse(data, CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var fallback)) {
            return new DataVersion(fallback);
        }

        return null;
    }

    public static implicit operator DataVersion?(string? data) => Parse(data);

    public static implicit operator DataVersion?(DateTimeOffset? dto)
        => dto == null ? null : new DataVersion(dto.Value);

    public static implicit operator DataVersion(DateTimeOffset dto) => new(dto);

    public static implicit operator DataVersion?(DateTime? dt)
        => dt == null ? null : new DataVersion(dt.Value);

    public static implicit operator DataVersion(DateTime dt) => new(dt);

    public static implicit operator DateTimeOffset?(DataVersion? version) => version?.Value;

    public static implicit operator DateTimeOffset(DataVersion version) => version.Value;

    public static bool operator <(DataVersion? left, DataVersion? right)
        => (left?.Value ?? DateTimeOffset.MinValue) < (right?.Value ?? DateTimeOffset.MinValue);

    public static bool operator >(DataVersion? left, DataVersion? right)
        => (left?.Value ?? DateTimeOffset.MinValue) > (right?.Value ?? DateTimeOffset.MinValue);

    public static bool operator <=(DataVersion? left, DataVersion? right)
        => (left?.Value ?? DateTimeOffset.MinValue) <= (right?.Value ?? DateTimeOffset.MinValue);

    public static bool operator >=(DataVersion? left, DataVersion? right)
        => (left?.Value ?? DateTimeOffset.MinValue) >= (right?.Value ?? DateTimeOffset.MinValue);

    public static bool operator ==(DataVersion? left, DataVersion? right) {
        if (ReferenceEquals(left, right)) {
            return true;
        }

        if (left is null || right is null) {
            return false;
        }

        return left.Value == right.Value;
    }

    public static bool operator !=(DataVersion? left, DataVersion? right) => !(left == right);

    public static DataVersion operator +(DataVersion version, TimeSpan timeSpan)
        => new(version.Value + timeSpan);

    public static DataVersion? operator +(DataVersion? version, TimeSpan? timeSpan) {
        if (version == null || timeSpan == null) {
            return null;
        }

        return new DataVersion(version.Value + timeSpan.Value);
    }

    public int CompareTo(DataVersion? other) => Value.CompareTo(other?.Value ?? DateTimeOffset.MinValue);

    public int CompareTo(DateTimeOffset other) => Value.CompareTo(other);

    public bool Equals(DataVersion? other) => other != null && Value == other.Value;

    public bool Equals(DateTimeOffset other) => Value == other;

    public override bool Equals(object? obj) {
        if (obj is DataVersion other) {
            return Equals(other);
        }

        if (obj is DateTimeOffset dto) {
            return Equals(dto);
        }

        return false;
    }

    public override int GetHashCode() => Value.GetHashCode();

    public override string ToString() => ToJson();
}
