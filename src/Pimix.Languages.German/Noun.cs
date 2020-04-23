using NLog;

namespace Pimix.Languages.German {
    public class Noun : Word {
        static readonly Logger logger = LogManager.GetCurrentClassLogger();

        public new const string ModelId = "languages/german/nouns";

        public override void Fill() {
            var words = GetWords();
            FillWithData(words);
            Gender = words.wiki.Gender;
            NounForms = words.wiki.NounForms;
        }

        public string GetNounFormWithArticle(Case formCase, Number formNumber) =>
            NounForms[formCase][formNumber] != null
                ? $"{GetArticle(Gender, formCase, formNumber)} {NounForms[formCase][formNumber]}"
                : "-";

        public static string GetArticle(Gender gender, Case formCase, Number formNumber) =>
            formCase switch {
                Case.Nominative => formNumber switch {
                    Number.Singular => gender switch {
                        Gender.Masculine => "der",
                        Gender.Feminine => "die",
                        Gender.Neuter => "das",
                        _ => null
                    },
                    Number.Plural => "die",
                    _ => null
                },
                Case.Genitive => formNumber switch {
                    Number.Singular => gender switch {
                        Gender.Masculine => "des",
                        Gender.Feminine => "der",
                        Gender.Neuter => "des",
                        _ => null
                    },
                    Number.Plural => "der",
                    _ => null
                },
                Case.Dative => formNumber switch {
                    Number.Singular => gender switch {
                        Gender.Masculine => "dem",
                        Gender.Feminine => "der",
                        Gender.Neuter => "dem",
                        _ => null
                    },
                    Number.Plural => "den",
                    _ => null
                },
                Case.Accusative => formNumber switch {
                    Number.Singular => gender switch {
                        Gender.Masculine => "den",
                        Gender.Feminine => "die",
                        Gender.Neuter => "das",
                        _ => null
                    },
                    Number.Plural => "die",
                    _ => null
                },
                _ => null
            };
    }
}
