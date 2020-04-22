using System.Linq;
using MwParserFromScratch;
using MwParserFromScratch.Nodes;
using NLog;
using WikiClientLibrary.Client;
using WikiClientLibrary.Pages;
using WikiClientLibrary.Sites;

namespace Pimix.Languages.German {
    public class EnWiktionaryClient {
        static readonly Logger logger = LogManager.GetCurrentClassLogger();

        public Word GetWord(string wordId) {
            var word = new Word {Id = wordId};
            var client = new WikiClient();
            var site = new WikiSite(client, "https://en.wiktionary.org/w/api.php");
            site.Initialization.Wait();
            var page = new WikiPage(site, wordId);
            page.RefreshAsync(PageQueryOptions.FetchContent).Wait();
            Meaning meaning = null;
            var parser = new WikitextParser();
            var content = parser.Parse(page.Content);
            var wordType = WordType.Unknown;
            var inGerman = false;
            foreach (var child in content.Lines) {
                if (child is Heading heading) {
                    if (heading.Level == 2) {
                        if (heading.GetTitle() == "German") {
                            inGerman = true;
                        } else if (inGerman) {
                            break;
                        }
                    } else if (inGerman)
                        if (heading.Level == 3) {
                            var title = heading.GetTitle();
                            if (title == "Alternative forms" || title == "Etymology" || title == "Pronunciation") {
                                // Do nothing for now.
                            } else {
                                wordType = ParseWordType(title);
                                if (wordType == WordType.Unknown) {
                                    logger.Warn($"Unknown header when expecting word type: {title}.");
                                }
                            }
                        } else {
                            wordType = WordType.Unknown;
                        }
                } else if (inGerman && wordType != WordType.Unknown) {
                    if (child is ListItem listItem) {
                        if (meaning != null) {
                            word.Meanings.Add(meaning);
                        }

                        meaning = new Meaning {
                            Type = wordType,
                            Translation = listItem.ToPlainText(),
                            TranslationWithNotes = string.Join("", listItem.EnumChildren().Select(GetText)).Trim()
                        };
                    }
                }
            }

            if (meaning != null) {
                word.Meanings.Add(meaning);
            }

            return word;
        }

        static string GetText(Node node) {
            if (node is Template template) {
                var templateName = template.Name.ToPlainText();
                switch (templateName) {
                    case "lb":
                        return $"({string.Join(", ", template.Arguments.Skip(1))})";
                    case "gloss":
                        return $"({string.Join(", ", template.Arguments)})";
                    default:
                        logger.Warn($"Unknown template: {template}");
                        break;
                }
            }

            return node.ToPlainText();
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
