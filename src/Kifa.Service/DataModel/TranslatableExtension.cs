using System.Collections.Generic;

namespace Kifa.Service {
    public static class TranslatableExtension {
        public static TDataModel GetTranslated<TDataModel>(this TDataModel data, string language)
            where TDataModel : DataModel<TDataModel>, new() {
            data = data.Merge(
                (data.Translations ?? new Dictionary<string, TDataModel>()).GetValueOrDefault(language,
                    new TDataModel()));
            data.Translations = null;
            return data;
        }
    }
}
