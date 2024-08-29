using System;
using System.Collections.Generic;
using System.Linq;
using CommandLine;
using Kifa.Jobs;
using Kifa.Memrise;
using Kifa.Service;
using NLog;

namespace Kifa.Tools.MemriseUtil.Commands;

[Verb("clear", HelpText = "Clear word list for the given course.")]
public class ClearWordListCommand : KifaCommand {
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

    string? courseName;

    [Option('c', "course", Required = true, HelpText = "Course to add the word list to.")]
    public string CourseName {
        get => Late.Get(courseName);
        set => Late.Set(ref courseName, value);
    }

    #endregion

    public override int Execute(KifaTask? task = null) {
        var memriseCourseClient = MemriseCourse.Client;
        var course = memriseCourseClient.Get(CourseName);

        if (course == null) {
            Logger.Fatal($"Failed to find course ({CourseName}). Exiting.");
            return 1;
        }

        using var memriseClient = new MemriseClient {
            Course = course
        };

        foreach (var wordListId in WordListIds) {
            memriseClient.ClearWordList(wordListId);
        }

        var unusedWords = course.GetUnusedWords().ToList();
        if (unusedWords.Count > 0) {
            foreach (var w in unusedWords) {
                Console.WriteLine(w);
            }

            if (Confirm($"Found {unusedWords.Count} words not used by any level")) {
                Logger.LogResult(memriseClient.RemoveWords(unusedWords), "removing words");
            }
        }

        return 0;
    }
}
