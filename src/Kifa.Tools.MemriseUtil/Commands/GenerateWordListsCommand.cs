using System.Collections.Generic;
using System.Linq;
using CommandLine;
using Kifa.Api.Files;
using Kifa.Jobs;
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

    public override int Execute(KifaTask? task = null) {
        var wordsChef = new DataChef<GoetheGermanWord>();
        var words = wordsChef.Load(new KifaFile(WordsFile).ReadAsString())
            .Where(word => word.Level != null).ToList();

        var lists = new List<GoetheWordList> {
            new() {
                Id = "A1",
                Words = words.Where(word => word.Level is "A1").Select(word => word.Id).ToList()
            },
            new() {
                Id = "A2",
                Words = words.Where(word => word.Level is "A1" or "A2").Select(word => word.Id)
                    .ToList()
            },
            new() {
                Id = "B1",
                Words = words.Where(word => word.Level is "A1" or "A2" or "B1")
                    .Select(word => word.Id).ToList()
            }
        };

        var targetFile = new KifaFile(ListsFile);
        targetFile.Delete();
        targetFile.Write(new DataChef<GoetheWordList>().Save(lists, false));

        return 0;
    }
}
