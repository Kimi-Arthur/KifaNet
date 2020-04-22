using System;
using System.Collections.Generic;
using NLog;

namespace Pimix.Languages.German {
    public class Noun : Word {
        static readonly Logger logger = LogManager.GetCurrentClassLogger();

        public new const string ModelId = "languages/german/nouns";

        public override WordType Type => WordType.Noun;

        public Gender Gender { get; set; }

        public NounForms NounForms { get; set; } = new NounForms();

        public override void Fill() {
            var wiki = new Noun();
            try {
                wiki = new DeWiktionaryClient().GetWord(Id) as Noun;
            } catch (Exception ex) {
                logger.Warn($"Failed to get wiki word for {Id}");
            }

            var pons = new Word();
            try {
                pons = new PonsClient().GetWord(Id);
            } catch (Exception ex) {
                logger.Warn($"Failed to get pons word for {Id}");
            }

            var duden = new DudenClient().GetWord(Id);

            FillWithData(wiki, pons, duden);

            Gender = wiki.Gender;
            NounForms = wiki.NounForms;
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
