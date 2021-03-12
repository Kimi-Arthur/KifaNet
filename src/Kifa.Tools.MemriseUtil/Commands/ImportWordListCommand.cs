using System.Text.RegularExpressions;
using CommandLine;
using Kifa.Languages.German.Goethe;
using Kifa.Memrise;
using NLog;

namespace Kifa.Tools.MemriseUtil.Commands {
    [Verb("import", HelpText = "Import word list for the given course.")]
    public class ImportWordListCommand : KifaCommand {
        static readonly Logger logger = LogManager.GetCurrentClassLogger();
        private static Regex rootWordPattern = new(@"^(das |der |die |\(.*\) |sich)?(.+?)(-$| \(.*\)| sein)?$");
        static MemriseClient memriseClient;
        static GoetheGermanWordRestServiceClient goetheClient;

        [Value(0, Required = true, HelpText = "Word list ID.")]
        public string WordListId { get; set; }

        public override int Execute() {
            var wordList = new GoetheWordListRestServiceClient().Get(WordListId);
            memriseClient = new MemriseClient();
            goetheClient = new GoetheGermanWordRestServiceClient();

            foreach (var word in wordList.Words) {
                AddWord(word);
            }

            return 0;
        }

        static void AddWord(string word) {
            var rootWord = GetRootWord(word);
            logger.Info($"{word} => {rootWord}");
        }

        static string GetRootWord(string word) {
            return rootWordPattern.Match(word).Groups[2].Value;
        }
    }
}
