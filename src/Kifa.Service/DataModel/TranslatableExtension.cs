namespace Kifa.Service {
    public static class TranslatableExtension {
        public static TDataModel GetTranslated<TDataModel>(this TDataModel data, string language)
            where TDataModel : DataModel<TDataModel> =>
            language == data.Translations._ ? data : data.Merge(data.Translations.Data[language]);
    }
}
