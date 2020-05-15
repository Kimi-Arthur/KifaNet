using System;
using System.Collections.Generic;
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

        const string TranslationDivider = "–";
        const string EtymologyPrefix = "Etymology ";

        static readonly HashSet<string> SkippedSections = new HashSet<string> {
            "Further reading",
            "Alternative forms",
            "Etymology",
            "Pronunciation",
            "Declension",
            "See also",
            "References",
            "Hyponyms",
            "Synonyms"
        };

        public Word GetWord(string wordId) {
            var word = new Word {Id = wordId};
            var client = new WikiClient();
            var site = new WikiSite(client, "https://en.wiktionary.org/w/api.php");
            site.Initialization.Wait();
            var page = new WikiPage(site, wordId);
            page.RefreshAsync(PageQueryOptions.FetchContent).Wait();
            Meaning meaning = null;
            Example example = null;
            var parser = new WikitextParser();
            var content = parser.Parse(page.Content);
            var wordType = WordType.Unknown;
            var inGerman = false;
            var targetLevel = 3;
            foreach (var child in content.Lines) {
                if (child is Heading heading) {
                    if (heading.Level == 2) {
                        if (heading.GetTitle() == "German") {
                            inGerman = true;
                        } else if (inGerman) {
                            break;
                        }
                    } else if (inGerman) {
                        if (heading.Level == 3 && heading.GetTitle().StartsWith(EtymologyPrefix)) {
                            targetLevel = 4;
                        }

                        if (heading.Level == targetLevel) {
                            var title = heading.GetTitle();
                            if (!SkippedSections.Contains(title)) {
                                wordType = ParseWordType(title);
                                if (wordType == WordType.Unknown) {
                                    logger.Warn($"Unknown header when expecting word type: {title}.");
                                }
                            }
                        }
                    } else {
                        wordType = WordType.Unknown;
                    }
                } else if (inGerman && wordType != WordType.Unknown) {
                    if (child is ListItem listItem) {
                        var prefix = listItem.Prefix;
                        var listContent = GetLineWithoutNotes(listItem);
                        switch (prefix) {
                            case "#":
                                if (meaning != null) {
                                    word.Meanings.Add(meaning);
                                }

                                if (listContent == "") {
                                    meaning = null;
                                    continue;
                                }

                                meaning = new Meaning {
                                    Type = wordType,
                                    Translation = listContent,
                                    TranslationWithNotes = GetLineWithNotes(listItem)
                                };
                                break;
                            case "#:":
                                if (meaning == null) {
                                    logger.Warn("Meaning is null unexpectedly,");
                                    meaning = new Meaning();
                                }

                                if (example != null) {
                                    logger.Warn(
                                        $"Example value is unexpectedly not null: {example.Text}, {example.Translation}.");
                                }

                                if (listContent.Contains(TranslationDivider)) {
                                    var segments = listContent.Split(TranslationDivider);
                                    meaning.Examples.Add(new Example {
                                        Text = segments[0].Trim(), Translation = segments[1].Trim()
                                    });
                                } else {
                                    example = new Example {Text = listContent.Trim()};

                                    var nodes = listItem.Inlines;
                                    foreach (var node in nodes) {
                                        if (node is Template template && template.Name.ToPlainText() == "ux") {
                                            meaning.Examples.Add(new Example {
                                                Text = template.Arguments[2].Value.ToPlainText(),
                                                Translation =
                                                    (template.Arguments[3] ?? template.Arguments["t"] ??
                                                        template.Arguments["translation"]).Value.ToPlainText()
                                            });

                                            example = null;
                                        }
                                    }
                                }

                                break;
                            case "#::" when example == null:
                                logger.Warn($"Encountered translation line without example line: {listItem}");
                                break;
                            case "#::":
                                example.Translation = listContent.Trim();
                                meaning.Examples.Add(example);
                                example = null;
                                break;
                        }
                    }
                }
            }

            if (meaning != null) {
                word.Meanings.Add(meaning);
            }

            return word;
        }

        static string GetLineWithoutNotes(Node line) {
            return string.Join("",
                    line.EnumChildren().Select(c =>
                        c is Template template && template.Name.ToPlainText() == "l" ? GetText(c) : c.ToPlainText()))
                .Trim();
        }

        static string GetLineWithNotes(Node line) {
            return string.Join("", line.EnumChildren().Select(GetText)).Trim();
        }

        static string GetText(Node node) {
            if (node is Template template) {
                var templateName = template.Name.ToPlainText();
                switch (templateName) {
                    case "l":
                        return template.Arguments.Last().Value.ToPlainText();
                    case "lb":
                        return $"({string.Join(", ", template.Arguments.Skip(1).Select(a => a.Value.ToPlainText()))})";
                    case "gloss":
                    case "q":
                        return $"({string.Join(", ", template.Arguments.Select(a => a.Value.ToPlainText()))})";
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
                "Particle" => WordType.Particle,
                "Interjection" => WordType.Interjection,
                "Proper noun" => WordType.Special,
                _ => WordType.Unknown
            };
    }
}
