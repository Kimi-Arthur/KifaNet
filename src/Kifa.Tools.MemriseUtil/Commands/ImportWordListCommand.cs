using System.Collections.Generic;
using CommandLine;
using Kifa.Languages.German.Goethe;
using Kifa.Memrise;
using NLog;

namespace Kifa.Tools.MemriseUtil.Commands;

[Verb("import", HelpText = "Import word list for the given course.")]
public class ImportWordListCommand : KifaCommand {
    static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    #region public late IEnumerable<string> WordListIds { get; set; }

    IEnumerable<string>? wordListIds;

    [Value(0, Min = 1, HelpText = "Word list IDs.")]
    public IEnumerable<string> WordListIds {
        get => Late.Get(wordListIds);
        set => Late.Set(ref wordListIds, value);
    }

    #endregion
    
    #region public late string Course { get; set; }

    string? course;

    [Option('c', "course", Required = true, HelpText = "Course to add the word list to.")]
    public string Course {
        get => Late.Get(course);
        set => Late.Set(ref course, value);
    }

    #endregion

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
