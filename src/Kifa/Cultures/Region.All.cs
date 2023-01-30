using System.Collections.Generic;
using System.Linq;

namespace Kifa;

public partial class Region {
    public static readonly Region Canada = new() {
        Name = "Canada",
        Code = "CA"
    };

    public static readonly Region China = new() {
        Name = "China",
        Code = "CN"
    };

    public static readonly Region HongKong = new() {
        Name = "Hong Kong",
        Code = "HK"
    };

    public static readonly Region Taiwan = new() {
        Name = "Taiwan",
        Code = "TW"
    };

    public static readonly Region Germany = new() {
        Name = "Germany",
        Code = "DE"
    };

    public static readonly Region UnitedKingdom = new() {
        Name = "United Kingdom",
        Code = "GB"
    };

    public static readonly Region Italy = new() {
        Name = "Italy",
        Code = "IT"
    };

    public static readonly Region Japan = new() {
        Name = "Japan",
        Code = "JP"
    };

    public static readonly Region Poland = new() {
        Name = "Poland",
        Code = "PL"
    };

    public static readonly Region UnitedStates = new() {
        Name = "United States",
        Code = "US"
    };

    public static readonly Region Unknown = new() {
        Name = "Unknown",
        Code = ""
    };

    public static readonly Dictionary<string, Region> All = new List<Region> {
        Canada,
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
}
