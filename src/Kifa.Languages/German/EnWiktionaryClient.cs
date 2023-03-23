using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using MwParserFromScratch;
using MwParserFromScratch.Nodes;
using NLog;
using WikiClientLibrary.Client;
using WikiClientLibrary.Pages;
using WikiClientLibrary.Sites;

namespace Kifa.Languages.German;

public class EnWiktionaryClient {
    static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    const string TranslationDivider = "–";

    static readonly HashSet<string> NextLevelPrefixes = new() {
        "Etymology",
        "Pronunciation"
    };

    static readonly HashSet<string> SkippedSections = new() {
        "Further reading",
        "Alternative forms",
        "Etymology",
        "Pronunciation",
        "Declension",
        "See also",
        "References",
        "Hyponyms",
        "Synonyms",
        "External links"
    };

    public GermanWord GetWord(string wordId) {
        var word = new GermanWord {
            Id = wordId
        };
        var client = new WikiClient() {
            ClientUserAgent =
                "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/87.0.4280.88 Safari/537.36"
        };
        var site = new WikiSite(client, "https://en.wiktionary.org/w/api.php");
        site.Initialization.Wait();
        var page = new WikiPage(site, wordId);
        page.RefreshAsync(PageQueryOptions.FetchContent).Wait();
        word.Meanings = new List<Meaning>();
        Meaning? meaning = null;
        TextWithTranslation? example = null;
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
                    if (heading.Level == 3) {
                        var title = heading.GetTitle();
                        if (title.Contains(" ") &&
                            NextLevelPrefixes.Contains(title.Split(" ").First())) {
                            targetLevel = 4;
                        }
                    }

                    if (heading.Level == targetLevel) {
                        var title = heading.GetTitle();
                        if (!SkippedSections.Contains(title)) {
                            wordType = ParseWordType(title);
                            if (wordType == WordType.Unknown) {
                                Logger.Warn($"Unknown header when expecting word type: {title}.");
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
                        case "##":
                            if (meaning != null) {
                                word.Meanings.Add(meaning);
                            }

                            var lineWithNotes = GetLineWithNotes(listItem);

                            if (listContent == "" && lineWithNotes == "") {
                                meaning = null;
                                continue;
                            }

                            meaning = new WikiMeaning {
                                Type = wordType,
                                RawTranslation = listContent,
                                TranslationWithNotes = lineWithNotes,
                                Examples = new List<TextWithTranslation>()
                            };
                            break;
                        case "#:":
                            if (meaning == null) {
                                Logger.Warn("Meaning is null unexpectedly,");
                                meaning = new Meaning {
                                    Examples = new List<TextWithTranslation>()
                                };
                            }

                            if (example != null) {
                                Logger.Warn(
                                    $"Example value is unexpectedly not null: {example.Text}, {example.Translation}.");
                            }

                            if (listContent.Contains(TranslationDivider)) {
                                var segments = listContent.Split(TranslationDivider);
                                meaning.Examples.Add(new TextWithTranslation {
                                    Text = segments[0].Trim(),
                                    Translation = segments[1].Trim()
                                });
                            } else {
                                example = new TextWithTranslation {
                                    Text = listContent.Trim()
                                };

                                var nodes = listItem.Inlines;
                                foreach (var node in nodes) {
                                    if (node is Template template &&
                                        (template.Name.ToPlainText() == "ux" ||
                                         template.Name.ToPlainText() == "uxi")) {
                                        meaning.Examples.Add(new TextWithTranslation {
                                            Text = template.Arguments[2].Value.ToPlainText(),
                                            Translation =
                                                (template.Arguments[3] ?? template.Arguments["t"] ??
                                                    template.Arguments["translation"]).Value
                                                .ToPlainText()
                                        });

                                        example = null;
                                    }
                                }
                            }

                            break;
                        case "#::" when example == null:
                            Logger.Warn(
                                $"Encountered translation line without example line: {listItem}");
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
        return Normalize(string.Join("",
            line.EnumChildren().Select(c
                => c is Template template && template.Name.ToPlainText() == "l"
                    ? GetText(c)
                    : c.ToPlainText())));
    }

    static string GetLineWithNotes(Node line)
        => Normalize(string.Join("", line.EnumChildren().Select(GetText)));

    static readonly Regex SpacesPattern = new(" +");
    static string Normalize(string text) => SpacesPattern.Replace(text.Trim(), " ");

    static string GetText(Node node) {
        if (node is Template template) {
            var templateName = template.Name.ToPlainText();
            switch (templateName) {
                case "l":
                    return template.Arguments.Last().Value.ToPlainText();
                case "lb":
                    return
                        $"({string.Join(", ", template.Arguments.Skip(1).Select(a => a.Value.ToPlainText()))})";
                case "gloss":
                case "q":
                case "non-gloss definition":
                case "n-g":
                case "ngd":
                    return
                        $"({string.Join(", ", template.Arguments.Select(a => a.Value.ToPlainText()))})";
                default:
                    return "";
            }
        }

        return node.ToPlainText();
    }

    static WordType ParseWordType(string id)
        => id.Split(",").First() switch {
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
            "Proper noun" => WordType.ProperNoun,
            "Suffix" => WordType.Suffix,
            "Prefix" => WordType.Prefix,
            "Infix" => WordType.Infix,
            "Interfix" => WordType.Interfix,
            _ => WordType.Unknown
        };
}

public class WikiMeaning : Meaning {
    public override string Translation
        => string.IsNullOrEmpty(RawTranslation) ? TranslationWithNotes : RawTranslation;

    public string RawTranslation { get; set; } = "";
    public string TranslationWithNotes { get; set; } = "";
}
