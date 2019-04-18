using System;
using System.Collections.Generic;
using System.Linq;

namespace Pimix.Infos {
    public class Region : JsonSerializable, IComparable<Region> {
        public string Name { get; set; }
        public string Code { get; set; }

        public string ToJson() => Name;

        public int CompareTo(Region other) => Code.CompareTo(Code);

        public override string ToString() => Name;

        public void FromJson(string data) {
            var region = All[data];
            Name = region.Name;
            Code = region.Code;
        }

        public static readonly Region UnitedStates = new Region {
            Name = "United States",
            Code = "US"
        };

        public static readonly Region UnitedKingdom = new Region {
            Name = "United Kingdom",
            Code = "UK"
        };

        public static readonly Region Japan = new Region {
            Name = "Japan",
            Code = "JP"
        };

        public static readonly Region China = new Region {
            Name = "China",
            Code = "CN"
        };

        public static readonly Dictionary<string, Region> All = new List<Region> {
            UnitedStates,
            UnitedKingdom,
            Japan,
            China
        }.SelectMany(r => new List<(string key, Region value)> {
            (r.Code, r),
            (r.Name, r)
        }).ToDictionary(tuple => tuple.key, tuple => tuple.value);
    }
}
