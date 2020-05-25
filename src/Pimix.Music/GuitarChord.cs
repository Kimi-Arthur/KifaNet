using System.Collections.Generic;
using Pimix.Service;

namespace Pimix.Music {
    public class GuitarChord : DataModel {
        public const string ModelId = "guitar/chords";

        static PimixServiceClient<GuitarChord> client;

        public static PimixServiceClient<GuitarChord> Client => client ??= new PimixServiceRestClient<GuitarChord>();

        /// Name of the chord. Can be like, `C`, `Cmaj7`, `Em` etc.
        public string Name { get; set; }

        /// Arrangement of the fingers by each string.
        public List<FingerArrangement> Arrangement { get; set; }
    }

    /// Finger arrangement of one finger on one string.
    public class FingerArrangement {
        /// Finger to use on the string, open -> 0, thumb -> 5, index -> 1, etc.
        public int Finger { get; set; }

        /// Which strings this finger is on. Can contain multiple elements for `barre chord`.
        public List<int> Strings { get; set; }

        /// Which fret this finger should be placed onto.
        public int Fret { get; set; }
    }

    public interface GuitarChordServiceClient : PimixServiceClient<GuitarChord> {
    }

    public class GuitarChordRestServiceClient : PimixServiceRestClient<GuitarChord>, GuitarChordServiceClient {
    }
}
