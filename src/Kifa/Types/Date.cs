using System;
using System.Globalization;

namespace Kifa;

public class Date : JsonSerializable, IComparable<Date> {
    DateTime date;

    public int Year => date.Year;
    public int Month => date.Month;
    public int Day => date.Day;

    public static readonly DateTimeOffset Zero = new(2018, 5, 7, 0, 0, 0, TimeSpan.Zero);

    public static Date Parse(string data)
        => new() {
            date = ParseDateTime(data)
        };

    public string ToJson() => date.ToString("yyyy-MM-dd");

    public void FromJson(string data) {
        date = ParseDateTime(data);
    }

    static DateTime ParseDateTime(string data) {
        if (string.IsNullOrEmpty(data)) {
            return DateTime.MinValue;
        }

        FormatException exception;
        try {
            return DateTime.Parse(data);
        } catch (FormatException ex) {
            exception = ex;
        }

        DateTime date;
        if (DateTime.TryParseExact(data, "yyyyMMdd", CultureInfo.InvariantCulture,
                DateTimeStyles.None, out date)) {
            return date;
        }

        throw exception;
    }

    public override string ToString() => ToJson();

    public int CompareTo(Date? other) => date.Date.CompareTo(other?.date.Date);
}
