using System.Collections.Generic;
using System.Linq;

namespace Kifa {
    public class Language : JsonSerializable {
        public static readonly Language German = new() {
            Name = "German",
            NativeName = "Deutsch",
            Code = "de"
        };

        public static readonly Language English = new() {
            Name = "English",
            NativeName = "English",
            Code = "en"
        };

        public static readonly Language Spanish = new() {
            Name = "Spanish",
            NativeName = "Español",
            Code = "es"
        };

        public static readonly Language French = new() {
            Name = "French",
            NativeName = "français",
            Code = "fr"
        };

        public static readonly Language Italian = new() {
            Name = "Italian",
            NativeName = "Italiano",
            Code = "it"
        };

        public static readonly Language Japanese = new() {
            Name = "Japanese",
            NativeName = "日本語",
            Code = "ja"
        };

        public static readonly Language Korean = new() {
            Name = "Korean",
            NativeName = "한국어",
            Code = "ko"
        };

        public static readonly Language Hungarian = new() {
            Name = "Hungarian",
            NativeName = "magyar",
            Code = "hu"
        };

        public static readonly Language Portuguese = new() {
            Name = "Portuguese",
            NativeName = "Português",
            Code = "pt"
        };

        public static readonly Language Russian = new() {
            Name = "Russian",
            NativeName = "русский",
            Code = "ru"
        };

        public static readonly Language Turkish = new() {
            Name = "Turkish",
            NativeName = "Türkçe",
            Code = "tr"
        };

        public static readonly Language Chinese = new() {
            Name = "Chinese",
            NativeName = "中文",
            Code = "zh"
        };

        public static readonly Language TraditionalChinese = new() {
            Name = "Traditional Chinese",
            NativeName = "繁体中文",
            Code = "zht"
        };

        public static readonly Language Unknown = new() {
            Name = "Unknown",
            NativeName = "Unknown",
            Code = ""
        };

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
            (r.Name, r),
            (r.NativeName, r)
        }).Distinct().ToDictionary(tuple => tuple.key, tuple => tuple.value);

        public string Name { get; set; } = "";
        public string NativeName { get; set; } = "";
        public string Code { get; set; } = "";

        public string ToJson() => Code;

        public void FromJson(string data) {
            var lang = All[data];
            Name = lang.Name;
            NativeName = lang.NativeName;
            Code = lang.Code;
        }

        public static implicit operator Language(string data) => All[data];

        public override int GetHashCode() => Code.GetHashCode();

        public override bool Equals(object? obj) =>
            obj != null && GetType() == obj.GetType() && Code == ((Language) obj).Code;

        public override string ToString() => Name;
    }
}
