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
            ["Präteritum"] = VerbFormType.IndicativePreterite,
            ["Perfekt"] = VerbFormType.IndicativePerfect
        };

        static readonly Dictionary<string, Person> PersonMapping = new Dictionary<string, Person> {
            ["1. Person Singular"] = Person.Ich,
            ["2. Person Singular"] = Person.Du,
            ["3. Person Singular"] = Person.Er,
            ["1. Person Plural"] = Person.Wir,
            ["2. Person Plural"] = Person.Ihr,
            ["3. Person Plural"] = Person.Sie,
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
                            var noun = word as Noun;
                            var selector = new Func<int, int, string>((row, column) => {
                                var form = node.SelectSingleNode($".//tr[{row + 1}]/td[{column}]").InnerText.Split("\n")
                                    .First().Split(" ").Last();
                                return form == "—" ? null : form;
                            });

                            noun.NounForms[Case.Nominative] = new Dictionary<Number, string> {
                                [Number.Singular] = selector(1, 1), [Number.Plural] = selector(1, 2)
                            };

                            noun.NounForms[Case.Genitive] = new Dictionary<Number, string> {
                                [Number.Singular] = selector(2, 1), [Number.Plural] = selector(2, 2)
                            };

                            noun.NounForms[Case.Dative] = new Dictionary<Number, string> {
                                [Number.Singular] = selector(3, 1), [Number.Plural] = selector(3, 2)
                            };

                            noun.NounForms[Case.Accusative] = new Dictionary<Number, string> {
                                [Number.Singular] = selector(4, 1), [Number.Plural] = selector(4, 2)
                            };
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
                            word.PronunciationAudioLinkWiktionary = $"https:{audioNode.Attributes["href"].Value}";
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
            var rows = doc.DocumentNode.SelectNodes(".//tr");
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
                    var cells = row.SelectNodes("./td");
                    if (cells?.Count > 1) {
                        var person = cells[0].InnerTextTrimmed();
                        if (PersonMapping.ContainsKey(person)) {
                            var p = PersonMapping[cells[0].InnerTextTrimmed()];
                            word.VerbForms[state.Value][p] = Normalize(
                                (cells[1].SelectSingleNode("p") ?? cells[1]).InnerHtmlTrimmed().Split("<br>")[0], p);
                        }
                    }
                }
            }
        }

        static string Normalize(string s, Person p) =>
            (s.StartsWith(PersonPrefixes[p]) ? s.Substring(PersonPrefixes[p].Length + 1) : s).Trim(',');

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
