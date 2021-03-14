using System.Collections.Generic;
using System.Text.RegularExpressions;
using Kifa.Service;
using Newtonsoft.Json;

namespace Kifa.Languages.German.Goethe {
    public class GoetheGermanWord : DataModel {
        public const string ModelId = "goethe/words";

        static readonly Regex RootWordPattern = new(@"^(das |der |die |\(.*\) |sich )?(.+?)(-$| \(.*\)| sein)?$");

        public string Word {
            get => Id;
            set => Id = value;
        }

        public string Level { get; set; }
        public string Form { get; set; }
        public string Meaning { get; set; }

        // A synonym text like: (CH) = (D, A) Hausmeister
        public string Synonym { get; set; }

        // Only Word, Level, Form are included.
        public GoetheGermanWord Feminine { get; set; }

        public GoetheGermanWord Abbreviation { get; set; }

        public List<string> Examples { get; set; }
        
        [JsonIgnore]
        public string RootWord => RootWordPattern.Match(Word).Groups[2].Value;
    }

    public interface GoetheGermanWordServiceClient : KifaServiceClient<GoetheGermanWord> {
    }

    public class GoetheGermanWordRestServiceClient : KifaServiceRestClient<GoetheGermanWord>,
        GoetheGermanWordServiceClient {
    }
}
