using System.Collections.Generic;
using System.Linq;

namespace Kifa;

public partial class Language {
    public static readonly Dictionary<string, Language> All = new List<Language> {
        German,
        English,
        Spanish,
        French,
        Italian,
        Japanese,
        Korean,
        Hungarian,
        Portuguese,
        Russian,
        Turkish,
        Chinese,
        TraditionalChinese,
        Unknown
    }.SelectMany(r => new List<(string key, Language value)> {
        (r.Code, r),
        (r.Code3, r),
        (r.Name, r),
        (r.NativeName, r)
    }).Distinct().ToDictionary(tuple => tuple.key, tuple => tuple.value);

    public static readonly Language German = new() {
        Name = "German",
        NativeName = "Deutsch",
        Code = "de",
        Code3 = "ger",
        Code3T = "deu"
    };

    public static readonly Language English = new() {
        Name = "English",
        NativeName = "English",
        Code = "en",
        Code3 = "eng",
        Code3T = "eng"
    };

    public static readonly Language Spanish = new() {
        Name = "Spanish",
        NativeName = "Español",
        Code = "es",
        Code3 = "spa",
        Code3T = "spa"
    };

    public static readonly Language French = new() {
        Name = "French",
        NativeName = "français",
        Code = "fr",
        Code3 = "fre",
        Code3T = "fra"
    };

    public static readonly Language Italian = new() {
        Name = "Italian",
        NativeName = "Italiano",
        Code = "it",
        Code3 = "ita",
        Code3T = "ita"
    };

    public static readonly Language Japanese = new() {
        Name = "Japanese",
        NativeName = "日本語",
        Code = "ja",
        Code3 = "jpn",
        Code3T = "jpn"
    };

    public static readonly Language Korean = new() {
        Name = "Korean",
        NativeName = "한국어",
        Code = "ko",
        Code3 = "kor",
        Code3T = "kor"
    };

    public static readonly Language Hungarian = new() {
        Name = "Hungarian",
        NativeName = "magyar",
        Code = "hu",
        Code3 = "hun",
        Code3T = "hun"
    };

    public static readonly Language Portuguese = new() {
        Name = "Portuguese",
        NativeName = "Português",
        Code = "pt",
        Code3 = "por",
        Code3T = "por"
    };

    public static readonly Language Russian = new() {
        Name = "Russian",
        NativeName = "русский",
        Code = "ru",
        Code3 = "rus",
        Code3T = "rus"
    };

    public static readonly Language Turkish = new() {
        Name = "Turkish",
        NativeName = "Türkçe",
        Code = "tr",
        Code3 = "tur",
        Code3T = "tur"
    };

    public static readonly Language Chinese = new() {
        Name = "Chinese",
        NativeName = "中文",
        Code = "zh",
        Code3 = "chi",
        Code3T = "zho"
    };

    public static readonly Language TraditionalChinese = new() {
        Name = "Traditional Chinese",
        NativeName = "繁体中文",
        Code = "zht",
        Code3 = "chi",
        Code3T = "zho"
    };

    public static readonly Language Unknown = new() {
        Name = "Unknown",
        NativeName = "Unknown",
        Code = "xx",
        Code3 = "und",
        Code3T = "und"
    };
}
