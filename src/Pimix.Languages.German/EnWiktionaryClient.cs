using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using HtmlAgilityPack;

namespace Pimix.Languages.German {
    public class EnWiktionaryClient {
        static HttpClient wiktionaryClient = new HttpClient();

        public Word GetWord(string wordId) {
            var doc = new HtmlDocument();
            doc.LoadHtml(wiktionaryClient.GetStringAsync($"https://en.wiktionary.org/wiki/{wordId}").Result);
            var pageContentNodes = doc.DocumentNode.SelectSingleNode(".//div[@class='mw-parser-output']").ChildNodes;
            var inDeutsch = false;
            var wordType = "";
            var inMeaning = false;
            var word = new Word();
            foreach (var node in pageContentNodes) {
                if (inDeutsch) {
                    if (node.Name == "h2") {
                        break;
                    }

                    if (new List<string>{"h3", "h4", "h5"}.Contains(node.Name)) {
                        var wordTypeNode = node.SelectSingleNode("./span[@class='mw-headline]");
                        if (wordTypeNode != null) {
                            wordType = wordTypeNode.InnerText;
                        }
                    }

                    if (node.Name == "p") {
                        var headwordNode = node.SelectSingleNode("./strong[@class='Latn headword']");
                        if (headwordNode != null) {
                            inMeaning = true;
                        }
                    }

                    if (inMeaning && node.Name == "ol") {
                        foreach (var meaningNode in node.SelectNodes("./li")) {
                            var innerText = "";
                            foreach (var childNode in meaningNode.ChildNodes) {
                                if (childNode.NodeType == HtmlNodeType.Element && childNode.Name == "dl") {
                                    
                                } else {
                                    
                                }
                            }
                        }
                    }
                } else if (node.Name == "h2" && node.SelectSingleNode($"./span[@id='German']") != null) {
                    inDeutsch = true;
                }
            }

            return word;
        }

        static WordType ParseWordType(string id) =>
            id.Split(",").First() switch {
                "Adjective" => WordType.Adjective,
                "Postposition" => WordType.Postposition,
                "Preposition" => WordType.Preposition,
                "Adverb" => WordType.Adverb,
                "Article" => WordType.Article,
                "Conjunction" => WordType.Conjunction,
                "Contraction" => WordType.Contraction,
                "Numeral" => WordType.Numeral,
                "Pronoun" => WordType.Pronoun,
                "Noun" => WordType.Noun,
                "Verb" => WordType.Verb,
                "Proper noun" => WordType.Special,
                _ => WordType.Unknown
            };
    }
}
