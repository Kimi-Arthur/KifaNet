using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CommandLine;
using Pimix.Api.Files;
using Pimix.Languages.German;
using VerbForms =
    System.Collections.Generic.Dictionary<Pimix.Languages.German.VerbFormType,
        System.Collections.Generic.Dictionary<Pimix.Languages.German.Person, string>>;

namespace Pimix.Apps.NoteUtil.Commands {
    [Verb("fill", HelpText = "Fill vocabulary tables with pronunciation, meaning and verb forms.")]
    public class FillCommand : PimixCommand {
        const string VocabularyLine = "## Vocabulary";
        const string VerbsLine = "### Verbs";
        const string SectionLine = "### ";

        [Value(0, Required = true, HelpText = "Target file to rename.")]
        public string FileUri { get; set; }

        public override int Execute() {
            using var sr = new StreamReader(new PimixFile(FileUri).OpenRead());
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
                        if (line == VerbsLine) {
                            state = ParsingState.Verbs;
                            columnNames.Clear();
                        }

                        lines.Add(line);
                        break;
                    case ParsingState.Verbs:
                        if (line.StartsWith(SectionLine)) {
                            state = ParsingState.Vocabulary;
                            lines.Add(line);
                        } else {
                            if (columnNames.Count == 0) {
                                var definition = GetColumnsDefinition(line);
                                if (definition != null) {
                                    for (int i = 0; i < definition.Length; i++) {
                                        columnNames[definition[i]] = i;
                                    }
                                }

                                lines.Add(line);
                            } else {
                                if (!line.Contains("|") || line.StartsWith("-")) {
                                    lines.Add(line);
                                } else {
                                    var parts = line.Split("|").ToList();
                                    parts.AddRange(Enumerable.Repeat<string>("", columnNames.Count - parts.Count));
                                    var verb = new Verb {Id = GetWordId(parts, columnNames)};
                                    verb.Fill();
                                    FillVerbRow(verb, parts, columnNames);
                                    lines.Add(string.Join("|", parts));
                                }
                            }
                        }

                        break;
                }

                line = sr.ReadLine();
            }

            new PimixFile(FileUri).Write(string.Join("\n", lines));
            return 0;
        }

        static string[] GetColumnsDefinition(string line) => line.Contains("|") ? line.Split("|") : null;

        static string GetWordId(List<string> parts, Dictionary<string, int> columnNames) => parts[columnNames["Word"]];

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

        static void FillVerbRow(Verb verb, List<string> parts, Dictionary<string, int> columnNames) {
            foreach (var (columnName, index) in columnNames.Where(column => parts[column.Value].Length == 0)) {
                parts[index] = columnName switch {
                    "Indicative Present" =>
                    $"<pre>ich       {verb.VerbForms[VerbFormType.IndicativePresent][Person.Ich]}<br>"
                    + $"du        {verb.VerbForms[VerbFormType.IndicativePresent][Person.Du]}<br>"
                    + $"er/sie/es {verb.VerbForms[VerbFormType.IndicativePresent][Person.Du]}<br>"
                    + $"wir       {verb.VerbForms[VerbFormType.IndicativePresent][Person.Du]}<br>"
                    + $"ihr       {verb.VerbForms[VerbFormType.IndicativePresent][Person.Du]}<br>"
                    + $"sie/Sie   {verb.VerbForms[VerbFormType.IndicativePresent][Person.Du]}</pre>",
                    "Pronunciation" => $"[\\[{verb.Pronunciation}\\]]({verb.PronunciationAudioLink})",
                    "Meaning" => verb.Meaning,
                    _ => parts[index]
                };
            }
        }
    }

    enum ParsingState {
        New,
        Vocabulary,
        Verbs
    }
}
