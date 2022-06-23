using System.Collections.Generic;
using System.Linq;
using CommandLine;
using Kifa.Api.Files;
using Kifa.Languages.German.Goethe;
using Kifa.Tools.DataUtil;

namespace Kifa.Tools.MemriseUtil.Commands;

[Verb("levels", HelpText = "Generate word lists.")]
public class GenerateWordListsCommand : KifaCommand {
    #region public late string WordsFile { get; set; }

    string? wordsFile;

    [Value(0, HelpText = "Words file containing raw words.")]
    public string WordsFile {
        get => Late.Get(wordsFile);
        set => Late.Set(ref wordsFile, value);
    }

    #endregion

    #region public late string ListsFile { get; set; }

    string? listsFile;

    [Value(1, HelpText = "Lists file containing lists of words.")]
    public string ListsFile {
        get => Late.Get(listsFile);
        set => Late.Set(ref listsFile, value);
    }

    #endregion

    public override int Execute() {
        var wordsChef = new DataChef<GoetheGermanWord, GoetheGermanWordRestServiceClient>();
        var words = wordsChef.Load(new KifaFile(WordsFile).ReadAsString());
        var wordsByLevels = words
            .Where(word => word.Level != null && !word.Examples[0].StartsWith("example"))
            .GroupBy(word => word.Level).ToDictionary(group => group.Key,
                group => group.Select(w => w.Id).ToList());

        var lists = new List<GoetheWordList> {
            new() {
                Id = "A1",
                Words = wordsByLevels["A1"]
            },
            new() {
                Id = "A2",
                Words = wordsByLevels["A2"]
            },
            new() {
                Id = "B1",
                Words = wordsByLevels["B1"]
            }
        };

        var targetFile = new KifaFile(ListsFile);
        targetFile.Delete();
        targetFile.Write(
            new DataChef<GoetheWordList, GoetheWordListRestServiceClient>().Save(lists, false));

        return 0;
    }
}
