namespace Kifa.Service {
    public static class TranslatableExtension {
        public static TDataModel GetTranslated<TDataModel>(this TDataModel data, string language)
            where TDataModel : TranslatableDataModel<TDataModel> =>
            language == data.DefaultLanguage ? data : data.Merge(data.Translations[language]);
    }
}
