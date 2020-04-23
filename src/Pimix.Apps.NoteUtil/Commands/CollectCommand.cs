using System.Collections.Generic;
using System.IO;
using System.Linq;
using CommandLine;
using NLog;
using Pimix.Api.Files;

namespace Pimix.Apps.NoteUtil.Commands {
    [Verb("collect", HelpText = "Collect all vocabulary into vocabulary files.")]
    public class CollectCommand : PimixCommand {
        static readonly Logger logger = LogManager.GetCurrentClassLogger();

        [Value(0, Required = true, HelpText = "Target file to collect vocabulary from.")]
        public string FileUri { get; set; }

        [Value(1, Required = true, HelpText = "Target file to collect vocabulary to.")]
        public string BookUri { get; set; }

        public override int Execute() {
            var source = new PimixFile(FileUri, simpleMode: true);
            var destination = new PimixFile(BookUri, simpleMode: true);
            var wordsSections = new Dictionary<string, WordsSection>();

            using var sr = new StreamReader(source.OpenRead());
            var state = ParsingState.New;
            var section = "";
            var lines = new List<string>();
            var line = sr.ReadLine();
            var columnNames = new Dictionary<string, int>();

            var startHeadingLevel = 2;
            while (line != null) {
                var heading = Heading.Get(line);
                switch (state) {
                    case ParsingState.New:
                        if (heading?.Level == startHeadingLevel && heading.Title == MarkdownHelpers.VocabularyTitle) {
                            state = ParsingState.Vocabulary;
                        }

                        lines.Add(line);
                        break;
                    case ParsingState.Vocabulary:
                        if (heading?.Level <= startHeadingLevel) {
                            break;
                        }

                        if (heading?.Level == startHeadingLevel + 1) {
                            section = heading.Title;

                            columnNames.Clear();
                        } else if (!line.Contains("|")) {
                            // Not in a table.
                            columnNames.Clear();
                            lines.Add(line);
                        } else if (columnNames.Count == 0) {
                            var definition = MarkdownHelpers.GetColumnsDefinition(line);
                            if (definition != null) {
                                if (wordsSections.ContainsKey(section) &&
                                    (wordsSections[section].Type != section ||
                                     !wordsSections[section].ColumnNames.SequenceEqual(definition))) {
                                    logger.Error("Different definitions.");
                                }

                                wordsSections[section] ??= new WordsSection();

                                for (int i = 0; i < definition.Length; i++) {
                                    columnNames[definition[i]] = i;
                                }
                            }
                        } else if (line.Contains("-|-")) {
                            // Table definition line.
                            lines.Add(line);
                        } else if (!columnNames.ContainsKey("Word")) {
                            // This table has no definition of Word.
                            lines.Add(line);
                        } else {
                            var parts = line.Trim('|').Split("|").Select(s => s.Trim()).ToList();
                            parts.AddRange(Enumerable.Repeat("", columnNames.Count - parts.Count));

                            lines.Add($"|{string.Join("|", parts)}|");
                        }

                        break;
                }

                line = sr.ReadLine();
            }

            destination.Delete();
            destination.Write(string.Join("\n", lines));
            return 0;
        }

        enum ParsingState {
            New,
            Vocabulary,
            Words
        }
    }
}
