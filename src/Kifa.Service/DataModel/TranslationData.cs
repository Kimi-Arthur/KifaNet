using System.Collections.Generic;

namespace Kifa.Service {
    public class TranslationData<T> : DataModel where T : DataModel {
        public Dictionary<string, T> Data { get; set; }

        public string _ { get; set; }
    }
}
