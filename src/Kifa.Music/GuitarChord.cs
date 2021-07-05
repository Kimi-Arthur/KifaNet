using System.Collections.Generic;
using Kifa.Service;

namespace Kifa.Music {
    public class GuitarChord : DataModel {
        public const string ModelId = "guitar/chords";

        static KifaServiceClient<GuitarChord> client;

        public static KifaServiceClient<GuitarChord> Client => client ??= new KifaServiceRestClient<GuitarChord>();

        /// Name of the chord. Can be like, `C`, `Cmaj7`, `Em` etc.
        public string Name { get; set; }

        /// Arrangements by each finger.
        public List<FingerArrangement> Arrangements { get; set; }
    }

    /// Finger arrangement of one finger on one string.
    public class FingerArrangement {
        /// 指, finger to use on the string, open -> 0, thumb -> 5, index -> 1, etc.
        public int Finger { get; set; }

        /// 弦, which strings this finger is on, 1 - 6, from higher to lower pitch string.
        /// Can contain multiple elements for `barre chord`, ordered.
        public List<int> Strings { get; set; }

        /// 品, Which fret this finger should be placed onto. For open, it should be 0.
        public int Fret { get; set; }
    }

    public interface GuitarChordServiceClient : KifaServiceClient<GuitarChord> {
    }

    public class GuitarChordRestServiceClient : KifaServiceRestClient<GuitarChord>, GuitarChordServiceClient {
    }
}
