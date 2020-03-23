using System.Collections.Generic;
using System.IO;
using CommandLine;
using NLog;
using Pimix.Api.Files;
using VerbForms =
    System.Collections.Generic.Dictionary<Pimix.Languages.German.VerbFormType,
        System.Collections.Generic.Dictionary<Pimix.Languages.German.Person, string>>;

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
            var destination = new PimixFile(BookUri, simpleMode: true);;
            var wordsSections = new Dictionary<string, WordsSection>();

            using var sr = new StreamReader(source.OpenRead());
            var state = ParsingState.New;
            var section = "";
            var lines = new List<string>();
            var line = sr.ReadLine();
            var columnNames = new Dictionary<string, int>();

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
