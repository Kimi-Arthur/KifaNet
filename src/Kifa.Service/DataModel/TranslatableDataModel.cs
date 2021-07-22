using System.Collections.Generic;

namespace Kifa.Service {
    public abstract class TranslatableDataModel<T> : DataModel where T : DataModel {
        public Dictionary<string, T> Translations { get; set; }

        public string DefaultLanguage { get; set; }
    }
}
