using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using HtmlAgilityPack;

namespace Pimix.Languages.German {
    public class DeWiktionaryClient {
        static HttpClient wiktionaryClient = new HttpClient();

        public Word GetWord(string wordId) {
            var doc = new HtmlDocument();
            doc.LoadHtml(wiktionaryClient.GetStringAsync($"https://de.wiktionary.org/wiki/{wordId}").Result);
            var pageContentNodes = doc.DocumentNode.SelectSingleNode(".//div[@class='mw-parser-output']").ChildNodes;
            var inDeutsch = false;
            var inSection = false;
            var hasPronunciation = false;
            var wordType = WordType.Unknown;
            var word = new Word();
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
                            word = wordType switch {
                                WordType.Verb => new Verb(),
                                WordType.Noun => new Noun {
                                    Gender = wordTypeNode.Id.Split(",").Last() switch {
                                        "_m" => Gender.Masculine,
                                        "_f" => Gender.Feminine,
                                        "_n" => Gender.Neuter,
                                        _ => Gender.Error // Should not happen.
                                    }
                                },
                                _ => word
                            };
                        }
                    }

                    if (inSection) {
                        if (node.Name == "table" && node.HasClass("wikitable") && wordType == WordType.Noun) {
                            var noun = word as Noun;
                            var selector = new Func<int, int, string>((row, column) => {
                                var form = node.SelectSingleNode($".//tr[{row + 1}]/td[{column}]").InnerText.Split("\n")
                                    .First()
                                    .Split(" ").Last();
                                return form == "—" ? null : form;
                            });

                            noun.NounForms[Case.Nominative] = new Dictionary<Number, string> {
                                [Number.Singular] = selector(1, 1),
                                [Number.Plural] = selector(1, 2)
                            };

                            noun.NounForms[Case.Genitive] = new Dictionary<Number, string> {
                                [Number.Singular] = selector(2, 1),
                                [Number.Plural] = selector(2, 2)
                            };

                            noun.NounForms[Case.Dative] = new Dictionary<Number, string> {
                                [Number.Singular] = selector(3, 1),
                                [Number.Plural] = selector(3, 2)
                            };

                            noun.NounForms[Case.Accusative] = new Dictionary<Number, string> {
                                [Number.Singular] = selector(4, 1),
                                [Number.Plural] = selector(4, 2)
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
