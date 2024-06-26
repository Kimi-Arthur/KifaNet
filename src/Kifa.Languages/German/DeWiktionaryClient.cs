using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Web;
using HtmlAgilityPack;

namespace Kifa.Languages.German;

public class DeWiktionaryClient {
    static HttpClient wiktionaryClient = GetHttpClient();

    static HttpClient GetHttpClient() {
        var client = new HttpClient();
        client.DefaultRequestHeaders.UserAgent.ParseAdd(
            "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/87.0.4280.88 Safari/537.36");
        return client;
    }

    static readonly Dictionary<string, VerbFormType> FormMapping = new() {
        ["Imperative"] = VerbFormType.Imperative,
        ["Präsens"] = VerbFormType.IndicativePresent,
        ["Indikativ und Konjunktiv"] = VerbFormType.IndicativePresent,
        ["Präteritum"] = VerbFormType.IndicativePreterite,
        ["Perfekt"] = VerbFormType.IndicativePerfect
    };

    static readonly Dictionary<string, Person> PersonMapping = new() {
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

    static readonly Dictionary<Person, string> PersonPrefixes = new() {
        [Person.Ich] = "ich",
        [Person.Du] = "du",
        [Person.Er] = "<small>er/sie/es</small>",
        [Person.Wir] = "wir",
        [Person.Ihr] = "ihr",
        [Person.Sie] = "sie"
    };

    public GermanWord GetWord(string wordId) {
        var doc = new HtmlDocument();
        doc.LoadHtml(wiktionaryClient.GetStringAsync($"https://de.wiktionary.org/wiki/{wordId}")
            .Result);
        var pageContentNodes = doc.DocumentNode
            .SelectSingleNode(".//div[@class='mw-parser-output']").ChildNodes;
        var inDeutsch = false;
        var inSection = false;
        var inAudio = false;
        var word = new GermanWord {
            Id = wordId
        };
        foreach (var node in pageContentNodes) {
            if (inDeutsch) {
                if (node.Name == "h2") {
                    break;
                }

                if (node.Name == "h3") {
                    inSection = true;
                    // Word type info here.
                    var wordTypeNode = node.SelectSingleNode(".//span[@class='mw-headline']");
                    if (wordTypeNode != null) {
                        word.Type = ParseWordType(wordTypeNode.Id);
                        switch (word.Type) {
                            case WordType.Verb:
                                if (word.VerbForms == null) {
                                    FillVerbForms(word);
                                }

                                break;

                            case WordType.Noun:
                                word.Gender ??= wordTypeNode.Id.Split(",")[^1] switch {
                                    "_m" => Gender.Masculine,
                                    "_f" => Gender.Feminine,
                                    "_n" => Gender.Neuter,
                                    _ => null // Should be plural.
                                };

                                break;
                        }
                    }
                }

                if (inSection) {
                    if (node.Name == "p") {
                        if (node.InnerText.Trim() == "Aussprache:") {
                            inAudio = true;
                        } else {
                            inAudio = false;
                        }
                    }

                    if (word.AdjectiveForms == null && node.Name == "table" &&
                        node.HasClass("wikitable") &&
                        word.Type is WordType.Adjective or WordType.Adverb) {
                        word.AdjectiveForms = new AdjectiveForms {
                            [AdjectiveFormType.Positiv] =
                                node.SelectSingleNode(".//tr[2]/td[1]").InnerText.Trim()
                        };

                        var komparativ = node.SelectNodes(".//tr[2]/td[2]/a[1]");
                        word.AdjectiveForms[AdjectiveFormType.Komparativ] = komparativ?.Count > 0
                            ? komparativ[0].InnerTextTrimmed()
                            : null;

                        var superlativ = node.SelectNodes(".//tr[2]/td[3]/a[1]");
                        word.AdjectiveForms[AdjectiveFormType.Superlativ] = superlativ?.Count > 0
                            ? superlativ[0].InnerTextTrimmed()
                            : null;
                    }

                    if (word.NounForms == null && node.Name == "table" &&
                        node.HasClass("wikitable") && word.Type == WordType.Noun) {
                        var extraHeaderCount = node.SelectNodes(".//tr[1]/td")?.Count > 0 ? 7 : 0;

                        var selector = new Func<int, int, string?>((row, column) => {
                            var form = node
                                .SelectSingleNode(
                                    $".//tr[{row + extraHeaderCount + 1}]/td[{column}]").InnerText
                                .Split("\n").First().Split(" ").Last();
                            return form == "—" ? null : form;
                        });

                        var headers = node.SelectNodes($".//tr[{extraHeaderCount + 1}]/th")
                            .Select(node => node.InnerText.Trim().Split(" ")[0]).ToList();
                        var singularColumn = headers.IndexOf("Singular");
                        var pluralColumn = headers.IndexOf("Plural");

                        word.NounForms = new NounForms {
                            [Case.Nominative] = new() {
                                [Number.Singular] = selector(1, singularColumn),
                                [Number.Plural] = selector(1, pluralColumn)
                            },
                            [Case.Genitive] = new() {
                                [Number.Singular] = selector(2, singularColumn),
                                [Number.Plural] = selector(2, pluralColumn)
                            },
                            [Case.Dative] = new() {
                                [Number.Singular] = selector(3, singularColumn),
                                [Number.Plural] = selector(3, pluralColumn)
                            },
                            [Case.Accusative] = new() {
                                [Number.Singular] = selector(4, singularColumn),
                                [Number.Plural] = selector(4, pluralColumn)
                            }
                        };

                        foreach (var nounForm in word.NounForms.Values) {
                            foreach (var number in nounForm.Where(e => e.Value == null)
                                         .Select(e => e.Key).ToList()) {
                                nounForm.Remove(number);
                            }
                        }
                    }

                    if (word.Pronunciation == null) {
                        var ipaNode = node.SelectSingleNode("(.//span[@class='ipa'])[1]");
                        if (ipaNode != null) {
                            word.Pronunciation = ipaNode.InnerText;
                        }
                    }

                    if (inAudio) {
                        var audioNodes = node.SelectNodes($"(.//a[@class='internal'])");
                        if (audioNodes != null) {
                            word.PronunciationAudioLinks ??=
                                new Dictionary<Source, HashSet<string>>();
                            word.PronunciationAudioLinks[Source.Wiktionary] =
                                word.PronunciationAudioLinks.GetValueOrDefault(Source.Wiktionary,
                                    new HashSet<string>());
                            word.PronunciationAudioLinks[Source.Wiktionary].UnionWith(audioNodes
                                .Select(audioNode
                                    => HttpUtility.UrlDecode(
                                        $"https:{audioNode.Attributes["href"].Value}"))
                                .ToHashSet());
                        }
                    }
                }
            } else if (node.Name == "h2" &&
                       node.SelectSingleNode(
                           $"./span[@id='{wordId.NormalizeWikiTitle()}_(Deutsch)']") != null) {
                inDeutsch = true;
            }
        }

        return word;
    }

    void FillVerbForms(GermanWord word) {
        // TODO(improve): use some state machine lib.
        var doc = new HtmlDocument();
        doc.LoadHtml(wiktionaryClient
            .GetStringAsync($"https://de.wiktionary.org/wiki/Flexion:{word.Id}").Result);
        var rows = doc.DocumentNode.SelectNodes(".//tr|.//h2").SkipWhile(node
                => node.Name != "h2" || !(node.InnerText.StartsWith($"{word.Id} (Konjugation)") &&
                                          node.InnerText.EndsWith(" (Deutsch)"))).Skip(1)
            .TakeWhile(node => node.Name != "h2").ToList();

        VerbFormType? state = null;
        word.VerbForms ??= new VerbForms();
        foreach (var row in rows) {
            if (row.SelectNodes("./td|./th")?.Count == 1) {
                state = null;
            }

            var form = row.InnerTextTrimmed();
            if (FormMapping.ContainsKey(form)) {
                state = FormMapping[form];
                word.VerbForms[state.Value] = new Dictionary<Person, string>();
            } else if (state != null) {
                var cells = row.SelectNodes("./td")?.SkipWhile(c => c.InnerTextTrimmed() == "")
                    .ToList();
                if (cells?.Count > 1) {
                    var person = cells[0].InnerTextTrimmed();
                    if (PersonMapping.ContainsKey(person)) {
                        var p = PersonMapping[cells[0].InnerTextTrimmed()];
                        word.VerbForms[state.Value][p] = Normalize(
                            (cells[1].SelectSingleNode("p") ?? cells[1]).InnerHtmlTrimmed()
                            .Split("<br>")[0], state.Value, p);
                    }
                }
            }
        }
    }

    static string Normalize(string s, VerbFormType v, Person p) {
        var value = (s.StartsWith(PersonPrefixes[p]) ? s[(PersonPrefixes[p].Length + 1)..] : s)
            .Replace("\u00A0", " ").Trim(' ', ',');
        return v == VerbFormType.Imperative && !value.EndsWith("!") ? value + "!" : value;
    }

    static WordType ParseWordType(string id)
        => id.Split(",_").Select(type => type switch {
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
        }).Where(type => type != WordType.Unknown).FirstOrDefault(WordType.Unknown);
}
