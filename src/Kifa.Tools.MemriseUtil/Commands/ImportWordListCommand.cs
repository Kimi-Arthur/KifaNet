using System.Text.RegularExpressions;
using CommandLine;
using Kifa.Languages.German.Goethe;
using Kifa.Memrise;
using NLog;

namespace Kifa.Tools.MemriseUtil.Commands {
    [Verb("import", HelpText = "Import word list for the given course.")]
    public class ImportWordListCommand : KifaCommand {
        static readonly Logger logger = LogManager.GetCurrentClassLogger();

        [Value(0, Required = true, HelpText = "Word list ID.")]
        public string WordListId { get; set; }

        [Option('c', "course", HelpText = "Course to add the word list to.")]
        public string Course { get; set; } = "test-course";

        public override int Execute() {
            var wordList = new GoetheWordListRestServiceClient().Get(WordListId);

            var memriseCourseClient = new MemriseCourseRestServiceClient();
            var course = memriseCourseClient.Get(Course);
            using var memriseClient = new MemriseClient {Course = course};
            memriseClient.AddWordList(wordList);

            return 0;
        }
    }
}
