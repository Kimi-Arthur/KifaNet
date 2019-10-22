using System.Collections.Generic;
using System.Linq;

namespace Pimix.Infos {
    public class Language : JsonSerializable {
        public static readonly Language Chinese = new Language {
            Name = "Chinese",
            Code = "zh"
        };

        public static readonly Language English = new Language {
            Name = "English",
            Code = "en"
        };

        public static readonly Language Japanese = new Language {
            Name = "Japanese",
            Code = "ja"
        };

        public static readonly Dictionary<string, Language> All = new List<Language> {
            Chinese,
            English,
            Japanese
        }.SelectMany(r => new List<(string key, Language value)> {
            (r.Code, r),
            (r.Name, r)
        }).ToDictionary(tuple => tuple.key, tuple => tuple.value);

        public string Name { get; set; }
        public string Code { get; set; }

        public string ToJson() => Code;

        public void FromJson(string data) {
            var lang = All[data];
            Name = lang.Name;
            Code = lang.Code;
        }

        public static implicit operator Language(string data) => All[data];

        public override int GetHashCode() => Code.GetHashCode();

        public override bool Equals(object obj) =>
            obj != null && GetType() == obj.GetType() && Code == ((Language) obj).Code;

        public override string ToString() => Name;
    }
}
