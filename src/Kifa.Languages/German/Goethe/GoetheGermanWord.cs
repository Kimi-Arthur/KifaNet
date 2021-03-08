using System.Collections.Generic;
using Kifa.Service;

namespace Kifa.Languages.German.Goethe {
    public class GoetheGermanWord : DataModel {
        public const string ModelId = "goethe/words";

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
    }

    public interface MemriseGermanWordServiceClient : KifaServiceClient<GoetheGermanWord> {
    }

    public class MemriseGermanWordRestServiceClient : KifaServiceRestClient<GoetheGermanWord>,
        MemriseGermanWordServiceClient {
    }
}
