using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using HtmlAgilityPack;

namespace Pimix.Languages.German {
    public class DeWiktionaryClient {
        static HttpClient wiktionaryClient = new HttpClient();

        static readonly Dictionary<string, VerbFormType> FormMapping = new Dictionary<string, VerbFormType> {
            ["Imperative"] = VerbFormType.Imperative,
            ["Präsens"] = VerbFormType.IndicativePresent,
            ["Indikativ und Konjunktiv"] = VerbFormType.IndicativePresent,
            ["Präteritum"] = VerbFormType.IndicativePreterite,
            ["Perfekt"] = VerbFormType.IndicativePerfect
        };

        static readonly Dictionary<string, Person> PersonMapping = new Dictionary<string, Person> {
            ["1. Person Singular"] = Person.Ich,
            ["Sg. 1. Pers."] = Person.Ich,
            ["2. Person Singular"] = Person.Du,
            ["Sg. 2. Pers."] = Person.Du,
            ["3. Person Singular"] = Person.Er,
            ["Sg. 3. Pers."] = Person.Er,
            ["1. Person Plural"] = Person.Wir,
            ["Pl. 1. Pers."] = Person.Wir,
            ["2. Person Plural"] = Person.Ihr,
            ["Pl. 2. Pers."] = Person.Ihr,
            ["3. Person Plural"] = Person.Sie,
            ["Pl. 3. Pers."] = Person.Sie,
            ["Höflichkeitsform"] = Person.Sie
        };

        static readonly Dictionary<Person, string> PersonPrefixes = new Dictionary<Person, string> {
            [Person.Ich] = "ich",
            [Person.Du] = "du",
            [Person.Er] = "<small>er/sie/es</small>",
            [Person.Wir] = "wir",
            [Person.Ihr] = "ihr",
            [Person.Sie] = "sie"
        };

        public Word GetWord(string wordId) {
            var doc = new HtmlDocument();
            doc.LoadHtml(wiktionaryClient.GetStringAsync($"https://de.wiktionary.org/wiki/{wordId}").Result);
            var pageContentNodes = doc.DocumentNode.SelectSingleNode(".//div[@class='mw-parser-output']").ChildNodes;
            var inDeutsch = false;
            var inSection = false;
            var hasPronunciation = false;
            var wordType = WordType.Unknown;
            var word = new Word {Id = wordId};
            foreach (var node in pageContentNodes) {
                if (inDeutsch) {
                    if (node.Name == "h2") {
                        break;
                    }

                    if (node.Name == "h3") {
                        if (inSection) {
                            break;
                        }

                        inSection = true;
                        // Word type info here.
                        var wordTypeNode = node.SelectSingleNode(".//span[@class='mw-headline']");
                        if (wordTypeNode != null) {
                            wordType = ParseWordType(wordTypeNode.Id);
                            switch (wordType) {
                                case WordType.Verb:
                                    if (word.VerbForms.Count == 0) {
                                        FillVerbForms(word);
                                    }

                                    break;

                                case WordType.Noun:
                                    word.Gender = wordTypeNode.Id.Split(",").Last() switch {
                                        "_m" => Gender.Masculine,
                                        "_f" => Gender.Feminine,
                                        "_n" => Gender.Neuter,
                                        _ => Gender.Error // Should not happen.
                                    };
                                    break;
                            }
                        }
                    }

                    if (inSection) {
                        if (node.Name == "table" && node.HasClass("wikitable") && wordType == WordType.Noun) {
                            var selector = new Func<int, int, string>((row, column) => {
                                var form = node.SelectSingleNode($".//tr[{row + 1}]/td[{column}]").InnerText.Split("\n")
                                    .First().Split(" ").Last();
                                return form == "—" ? null : form;
                            });

                            word.NounForms[Case.Nominative] = new Dictionary<Number, string> {
                                [Number.Singular] = selector(1, 1), [Number.Plural] = selector(1, 2)
                            };

                            word.NounForms[Case.Genitive] = new Dictionary<Number, string> {
                                [Number.Singular] = selector(2, 1), [Number.Plural] = selector(2, 2)
                            };

                            word.NounForms[Case.Dative] = new Dictionary<Number, string> {
                                [Number.Singular] = selector(3, 1), [Number.Plural] = selector(3, 2)
                            };

                            word.NounForms[Case.Accusative] = new Dictionary<Number, string> {
                                [Number.Singular] = selector(4, 1), [Number.Plural] = selector(4, 2)
                            };

                            foreach (var nounForm in word.NounForms.Values) {
                                foreach (var number in nounForm.Where(e => e.Value == null).Select(e => e.Key)
                                    .ToList()) {
                                    nounForm.Remove(number);
                                }
                            }
                        }

                        if (!hasPronunciation) {
                            var ipaNode = node.SelectSingleNode("(.//span[@class='ipa'])[1]");
                            if (ipaNode != null) {
                                hasPronunciation = true;
                                word.Pronunciation = ipaNode.InnerText;
                            }
                        }

                        var audioNode = node.SelectSingleNode($"(.//a[@class='internal'])[1]");
                        if (audioNode != null) {
                            word.PronunciationAudioLinks[Source.Wiktionary] =
                                $"https:{audioNode.Attributes["href"].Value}";
                        }
                    }
                } else if (node.Name == "h2" && node.SelectSingleNode($"./span[@id='{wordId}_(Deutsch)']") != null) {
                    inDeutsch = true;
                }
            }

            return word;
        }

        void FillVerbForms(Word word) {
            // TODO(improve): use some state machine lib.
            var doc = new HtmlDocument();
            doc.LoadHtml(wiktionaryClient.GetStringAsync($"https://de.wiktionary.org/wiki/Flexion:{word.Id}").Result);
            var rows = doc.DocumentNode.SelectNodes(".//tr|.//h2")
                .SkipWhile(node => node.Name != "h2" || !(node.InnerText.StartsWith($"{word.Id} (Konjugation)") &&
                                                          node.InnerText.EndsWith(" (Deutsch)"))).Skip(1)
                .TakeWhile(node => node.Name != "h2").ToList();

            VerbFormType? state = null;
            foreach (var row in rows) {
                if (row.SelectNodes("./td|./th")?.Count == 1) {
                    state = null;
                }

                var form = row.InnerTextTrimmed();
                if (FormMapping.ContainsKey(form)) {
                    state = FormMapping[form];
                    word.VerbForms[state.Value] = new Dictionary<Person, string>();
                } else if (state != null) {
                    var cells = row.SelectNodes("./td")?.SkipWhile(c => c.InnerTextTrimmed() == "").ToList();
                    if (cells?.Count > 1) {
                        var person = cells[0].InnerTextTrimmed();
                        if (PersonMapping.ContainsKey(person)) {
                            var p = PersonMapping[cells[0].InnerTextTrimmed()];
                            word.VerbForms[state.Value][p] = Normalize(
                                (cells[1].SelectSingleNode("p") ?? cells[1]).InnerHtmlTrimmed().Split("<br>")[0],
                                state.Value, p);
                        }
                    }
                }
            }
        }

        static string Normalize(string s, VerbFormType v, Person p) {
            var value = (s.StartsWith(PersonPrefixes[p]) ? s.Substring(PersonPrefixes[p].Length + 1) : s)
                .Trim(' ', ',');
            return v == VerbFormType.Imperative && !value.EndsWith("!") ? value + "!" : value;
        }

        static WordType ParseWordType(string id) =>
            id.Split(",").First() switch {
                "Adjektiv" => WordType.Adjective,
                "Postposition" => WordType.Postposition,
                "Präposition" => WordType.Preposition,
                "Adverb" => WordType.Adverb,
                "Modalpartikel" => WordType.Adverb,
                "Artikel" => WordType.Article,
                "Konjunktion" => WordType.Conjunction,
                "Kontraktion" => WordType.Contraction,
                "Numerale" => WordType.Numeral,
                "Pronomen" => WordType.Pronoun,
                "Personalpronomen" => WordType.Pronoun,
                "Substantiv" => WordType.Noun,
                "Verb" => WordType.Verb,
                _ => WordType.Unknown
            };
    }
}
