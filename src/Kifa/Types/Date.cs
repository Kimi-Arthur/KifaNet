using System;
using System.Globalization;

namespace Kifa;

public class Date : JsonSerializable, IComparable<Date> {
    DateTime DateTime { get; set; }

    public int Year => DateTime.Year;
    public int Month => DateTime.Month;
    public int Day => DateTime.Day;

    public static readonly DateTimeOffset Zero = new(2018, 5, 7, 0, 0, 0, TimeSpan.Zero);

    public static Date Parse(string data)
        => new() {
            DateTime = ParseDateTime(data)
        };

    public static Date Parse(string data, string format)
        => new() {
            DateTime = ParseDateTime(data, format)
        };


    public string ToJson() => DateTime.ToString("yyyy-MM-dd");

    public static implicit operator Date(string data) => Parse(data);

    public static implicit operator Date(DateTime dataTime)
        => new() {
            DateTime = dataTime
        };

    static DateTime ParseDateTime(string data, string? format = null) {
        if (string.IsNullOrEmpty(data)) {
            return DateTime.MinValue;
        }

        if (format != null) {
            if (DateTime.TryParseExact(data, format, CultureInfo.InvariantCulture,
                    DateTimeStyles.None, out var date)) {
                return date;
            }
        }

        return DateTime.Parse(data);
    }

    public override string ToString() => ToJson();

    public int CompareTo(Date? other) => DateTime.Date.CompareTo(other?.DateTime.Date);
}
