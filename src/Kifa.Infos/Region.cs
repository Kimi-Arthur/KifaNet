using System.Collections.Generic;
using System.Linq;
using Pimix;

namespace Kifa.Infos {
    public class Region : JsonSerializable {
        public static readonly Region China = new Region {
            Name = "China",
            Code = "CN"
        };

        public static readonly Region Germany = new Region {
            Name = "Germany",
            Code = "DE"
        };

        public static readonly Region UnitedKingdom = new Region {
            Name = "United Kingdom",
            Code = "GB"
        };

        public static readonly Region Italy = new Region {
            Name = "Italy",
            Code = "IT"
        };

        public static readonly Region Japan = new Region {
            Name = "Japan",
            Code = "JP"
        };

        public static readonly Region Poland = new Region {
            Name = "Poland",
            Code = "PL"
        };

        public static readonly Region UnitedStates = new Region {
            Name = "United States",
            Code = "US"
        };

        public static readonly Region Unknown = new Region {
            Name = "Unknown",
            Code = ""
        };

        public static readonly Dictionary<string, Region> All = new List<Region> {
            China,
            Germany,
            UnitedKingdom,
            Italy,
            Japan,
            Poland,
            UnitedStates,
            Unknown
        }.SelectMany(r => new List<(string key, Region value)> {
            (r.Code, r),
            (r.Name, r)
        }).ToDictionary(tuple => tuple.key, tuple => tuple.value);

        public string Name { get; set; }
        public string Code { get; set; }

        public string ToJson() => Name;

        public void FromJson(string data) {
            var region = All[data];
            Name = region.Name;
            Code = region.Code;
        }

        public static implicit operator Region(string data) => All[data];

        public override int GetHashCode() => Code.GetHashCode();

        public override bool Equals(object obj) =>
            obj != null && GetType() == obj.GetType() && Code == ((Region) obj).Code;

        public override string ToString() => Name;
    }
}
