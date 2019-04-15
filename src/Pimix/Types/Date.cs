using System;

namespace Pimix {
    public struct Date : JsonSerializable, IComparable<Date> {
        DateTime date;

        public string ToJson() => date.ToString("yyyy-MM-dd");

        public void FromJson(string data) {
            date = DateTime.Parse(data);
        }

        public override string ToString() => ToJson();

        public int CompareTo(Date other) => date.Date.CompareTo(other.date.Date);
    }
}
