using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CommandLine;
using NLog;
using Pimix.Api.Files;
using Pimix.Languages.German;
using VerbForms =
    System.Collections.Generic.Dictionary<Pimix.Languages.German.VerbFormType,
        System.Collections.Generic.Dictionary<Pimix.Languages.German.Person, string>>;

namespace Pimix.Apps.NoteUtil.Commands {
    [Verb("fill", HelpText = "Fill vocabulary tables with pronunciation, meaning and verb forms.")]
    public class FillCommand : PimixCommand {
        static readonly Logger logger = LogManager.GetCurrentClassLogger();

        const string VocabularyLine = "## Vocabulary";
        const string VerbsLine = "### Verbs";
        const string NounsLine = "### Nouns";
        const string SectionLine = "### ";

        [Value(0, Required = true, HelpText = "Target file to rename.")]
        public string FileUri { get; set; }

        public override int Execute() {
            var noteFile = new PimixFile(FileUri);
            using var sr = new StreamReader(noteFile.OpenRead());
            var state = ParsingState.New;
            var lines = new List<string>();
            var line = sr.ReadLine();
            var columnNames = new Dictionary<string, int>();
            while (line != null) {
                switch (state) {
                    case ParsingState.New:
                        if (line == VocabularyLine) {
                            state = ParsingState.Vocabulary;
                        }

                        lines.Add(line);
                        break;
                    case ParsingState.Vocabulary:
                    case ParsingState.Verbs:
                    case ParsingState.Nouns:
                        if (line.StartsWith(SectionLine)) {
                            state = line switch {
                                VerbsLine => ParsingState.Verbs,
                                NounsLine => ParsingState.Nouns,
                                _ => ParsingState.Vocabulary
                            };

                            columnNames.Clear();
                            lines.Add(line);
                        } else if (!line.Contains("|")) {
                            // Not in a table.
                            columnNames.Clear();
                            lines.Add(line);
                        } else if (columnNames.Count == 0) {
                            var definition = GetColumnsDefinition(line);
                            if (definition != null) {
                                for (int i = 0; i < definition.Length; i++) {
                                    columnNames[definition[i]] = i;
                                }
                            }

                            lines.Add(line);
                        } else if (line.Contains("--")) {
                            // Table definition line.
                            lines.Add(line);
                        } else if (!columnNames.ContainsKey("Word")) {
                            lines.Add(line);
                        } else {
                            var parts = line.Trim('|').Split("|").Select(s => s.Trim()).ToList();
                            parts.AddRange(Enumerable.Repeat("", columnNames.Count - parts.Count));
                            try {
                                switch (state) {
                                    case ParsingState.Verbs:
                                        FillVerbRow(parts, columnNames);
                                        break;
                                    case ParsingState.Nouns:
                                        FillNounRow(parts, columnNames);
                                        break;
                                    case ParsingState.Vocabulary:
                                        FillWordRow(parts, columnNames);
                                        break;
                                }
                            } catch (Exception ex) {
                                logger.Warn(ex, $"Fail to fill line: |{string.Join("|", parts)}|.");
                            }

                            lines.Add($"|{string.Join("|", parts)}|");
                        }

                        break;
                }

                line = sr.ReadLine();
            }

            noteFile.Delete();
            noteFile.Write(string.Join("\n", lines));
            return 0;
        }

        static string[] GetColumnsDefinition(string line) => line.Trim('|').Split("|").Select(x => x.Trim()).ToArray();

        static string GetWordId(List<string> parts, Dictionary<string, int> columnNames) =>
            parts[columnNames["Word"]].Replace("*", "").Split(" ").Last().Split("(").First();

        static Verb ParseVerbRow(List<string> parts, Dictionary<string, int> columnNames) {
            var verb = new Verb {
                Id = parts[columnNames["Word"]],
                Meaning = parts[columnNames["Meaning"]],
                VerbForms = new VerbForms {
                    [VerbFormType.IndicativePresent] = new Dictionary<Person, string> {
                        [Person.Ich] = parts[columnNames["ich"]],
                        [Person.Du] = parts[columnNames["du"]],
                        [Person.Er] = parts[columnNames["er/sie/es"]],
                        [Person.Wir] = parts[columnNames["wir"]],
                        [Person.Ihr] = parts[columnNames["ihr"]],
                        [Person.Sie] = parts[columnNames["sie/Sie"]],
                    }
                }
            };

            var pronunciationText = parts[columnNames["Pronunciation"]];
            if (pronunciationText.Length > 0) {
                var segments =
                    pronunciationText.Split(new[] {"[\\[", "\\]](", ")"}, StringSplitOptions.RemoveEmptyEntries);
                verb.Pronunciation = segments.First();
                var audioLink = segments.Last();
                if (audioLink.StartsWith("https://cdn.duden.de/")) {
                    verb.PronunciationAudioLinkDuden = audioLink;
                } else if (audioLink.StartsWith("https://sounds.pons.com/")) {
                    verb.PronunciationAudioLinkPons = audioLink;
                } else if (audioLink.StartsWith("https://upload.wikimedia.org/")) {
                    verb.PronunciationAudioLinkWiktionary = audioLink;
                }
            }

            return verb;
        }

        static void FillVerbRow(List<string> parts, Dictionary<string, int> columnNames) {
            var verb = new Verb {Id = GetWordId(parts, columnNames)};
            logger.Info($"Processing verb: {verb.Id}");

            verb.Fill();

            foreach (var (columnName, index) in columnNames.Where(column => parts[column.Value].Length == 0)) {
                parts[index] = columnName switch {
                    "Indicative Present" =>
                    $"<pre>ich       {verb.VerbForms[VerbFormType.IndicativePresent][Person.Ich]}<br>"
                    + $"du        {verb.VerbForms[VerbFormType.IndicativePresent][Person.Du]}<br>"
                    + $"er/sie/es {verb.VerbForms[VerbFormType.IndicativePresent][Person.Er]}<br>"
                    + $"wir       {verb.VerbForms[VerbFormType.IndicativePresent][Person.Wir]}<br>"
                    + $"ihr       {verb.VerbForms[VerbFormType.IndicativePresent][Person.Ihr]}<br>"
                    + $"sie/Sie   {verb.VerbForms[VerbFormType.IndicativePresent][Person.Sie]}</pre>",
                    "Pronunciation" => $"[[{verb.Pronunciation}]]({verb.PronunciationAudioLink})",
                    "Meaning" => verb.Meaning,
                    _ => parts[index]
                };
            }
        }

        static void FillNounRow(List<string> parts, Dictionary<string, int> columnNames) {
            var noun = new Noun {Id = GetWordId(parts, columnNames)};
            logger.Info($"Processing noun: {noun.Id}");

            noun.Fill();

            foreach (var (columnName, index) in columnNames.Where(column => parts[column.Value].Length == 0)) {
                parts[index] = columnName switch {
                    "Plural" => noun.GetNounFormWithArticle(Case.Nominative, Number.Plural),
                    "Pronunciation" => $"[[{noun.Pronunciation}]]({noun.PronunciationAudioLink})",
                    "Meaning" => noun.Meaning,
                    _ => parts[index]
                };
            }
        }

        static void FillWordRow(List<string> parts, Dictionary<string, int> columnNames) {
            var word = new Word {Id = GetWordId(parts, columnNames)};
            logger.Info($"Processing word: {word.Id}");

            word.Fill();

            foreach (var (columnName, index) in columnNames.Where(column => parts[column.Value].Length == 0)) {
                parts[index] = columnName switch {
                    "Pronunciation" => $"[[{word.Pronunciation}]]({word.PronunciationAudioLink})",
                    "Meaning" => word.Meaning,
                    _ => parts[index]
                };
            }
        }
    }

    enum ParsingState {
        New,
        Vocabulary,
        Verbs,
        Nouns
    }
}
