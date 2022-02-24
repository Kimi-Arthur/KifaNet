using System.Collections.Generic;
using CommandLine;
using Kifa.Languages.German.Goethe;
using Kifa.Memrise;
using NLog;

namespace Kifa.Tools.MemriseUtil.Commands; 

[Verb("import", HelpText = "Import word list for the given course.")]
public class ImportWordListCommand : KifaCommand {
    static readonly Logger logger = LogManager.GetCurrentClassLogger();

    [Value(0, Min = 1, HelpText = "Word list IDs.")]
    public IEnumerable<string> WordListIds { get; set; }

    [Option('c', "course", HelpText = "Course to add the word list to.")]
    public string Course { get; set; } = "test-course";

    public override int Execute() {
        var memriseCourseClient = new MemriseCourseRestServiceClient();
        var course = memriseCourseClient.Get(Course);

        using var memriseClient = new MemriseClient {
            Course = course
        };

        foreach (var wordListId in WordListIds) {
            var wordList = new GoetheWordListRestServiceClient().Get(wordListId);
            memriseClient.AddWordList(wordList);
        }

        return 0;
    }
}