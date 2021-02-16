using System.Collections.Generic;
using Kifa.Service;

namespace Kifa.Memrise {
    public class MemriseGermanWord : DataModel {
        public const string ModelId = "memrise/german";

        public string Word {
            get => Id;
            set => Id = value;
        }

        public string Level { get; set; }
        public string Form { get; set; }
        public string Meaning { get; set; }
        public List<string> Examples { get; set; }

        // Only Word, Level, Form are included.
        public MemriseGermanWord Feminine { get; set; }

        // A synonym text like: (CH) = (D, A) Hausmeister
        public string Synonym { get; set; }
    }

    public interface MemriseGermanWordServiceClient : KifaServiceClient<MemriseGermanWord> {
    }

    public class MemriseGermanWordRestServiceClient : KifaServiceRestClient<MemriseGermanWord>,
        MemriseGermanWordServiceClient {
    }
}
