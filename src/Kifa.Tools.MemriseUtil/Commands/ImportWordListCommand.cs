using System;
using System.Collections.Generic;
using System.Linq;
using CommandLine;
using Kifa.Languages.German.Goethe;
using Kifa.Memrise;
using Kifa.Service;
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

    #region public late string CourseName { get; set; }

    string? course;

    [Option('c', "course", Required = true, HelpText = "Course to add the word list to.")]
    public string CourseName {
        get => Late.Get(course);
        set => Late.Set(ref course, value);
    }

    #endregion

    [Option('f', "fill-empty", Default = false,
        HelpText = "Whether to force fill empty fields or not. Useful to fix column order.")]
    public bool FillEmpty { get; set; } = false;

    public override int Execute() {
        var memriseCourseClient = MemriseCourse.Client;
        var course = memriseCourseClient.Get(CourseName);

        if (course == null) {
            Logger.Fatal($"Failed to find course ({CourseName}). Exiting.");
            return 1;
        }

        using var memriseClient = new MemriseClient {
            Course = course,
            FillEmpty = FillEmpty
        };

        foreach (var wordListId in WordListIds) {
            var wordList = GoetheWordList.Client.Get(wordListId);
            memriseClient.AddWordList(wordList);
        }

        var unusedWords = course.GetUnusedWords().ToList();
        if (unusedWords.Count > 0) {
            foreach (var w in unusedWords) {
                Console.WriteLine(w);
            }

            if (Confirm($"Found {unusedWords.Count} words not used by any level")) {
                Logger.LogResult(memriseClient.RemoveWords(unusedWords), "removing words");
                course = MemriseCourse.Client.Get(course.Id, true).Checked();
                Logger.Info($"After refreshing, {course.GetUnusedWords().Count()} are found.");
            }
        }

        return 0;
    }
}
