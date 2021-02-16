using System.Collections.Generic;
using System.Linq;

namespace Kifa.Infos {
    public class Language : JsonSerializable {
        public static readonly Language German = new Language {
            Name = "German",
            NativeName = "Deutsch",
            Code = "de"
        };

        public static readonly Language English = new Language {
            Name = "English",
            NativeName = "English",
            Code = "en"
        };

        public static readonly Language Spanish = new Language {
            Name = "Spanish",
            NativeName = "Español",
            Code = "es"
        };

        public static readonly Language French = new Language {
            Name = "French",
            NativeName = "français",
            Code = "fr"
        };

        public static readonly Language Italian = new Language {
            Name = "Italian",
            NativeName = "Italiano",
            Code = "it"
        };

        public static readonly Language Japanese = new Language {
            Name = "Japanese",
            NativeName = "日本語",
            Code = "ja"
        };

        public static readonly Language Korean = new Language {
            Name = "Korean",
            NativeName = "한국어",
            Code = "ko"
        };

        public static readonly Language Hungarian = new Language {
            Name = "Hungarian",
            NativeName = "magyar",
            Code = "hu"
        };

        public static readonly Language Portuguese = new Language {
            Name = "Portuguese",
            NativeName = "Português",
            Code = "pt"
        };

        public static readonly Language Russian = new Language {
            Name = "Russian",
            NativeName = "русский",
            Code = "ru"
        };

        public static readonly Language Turkish = new Language {
            Name = "Turkish",
            NativeName = "Türkçe",
            Code = "tr"
        };

        public static readonly Language Chinese = new Language {
            Name = "Chinese",
            NativeName = "中文",
            Code = "zh"
        };

        public static readonly Language Unknown = new Language {
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
            Unknown
        }.SelectMany(r => new List<(string key, Language value)> {
            (r.Code, r),
            (r.Name, r),
            (r.NativeName, r)
        }).Distinct().ToDictionary(tuple => tuple.key, tuple => tuple.value);

        public string Name { get; set; }
        public string NativeName { get; set; }
        public string Code { get; set; }

        public string ToJson() => Code;

        public void FromJson(string data) {
            var lang = All[data];
            Name = lang.Name;
            NativeName = lang.NativeName;
            Code = lang.Code;
        }

        public static implicit operator Language(string data) => All[data];

        public override int GetHashCode() => Code.GetHashCode();

        public override bool Equals(object obj) =>
            obj != null && GetType() == obj.GetType() && Code == ((Language) obj).Code;

        public override string ToString() => Name;
    }
}
