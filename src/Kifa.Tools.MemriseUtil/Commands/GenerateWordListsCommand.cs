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
    [Value(0, HelpText = "Words file containing raw words.")]
    public string WordsFile {
        get => Late.Get(field);
        set => Late.Set(ref field, value);
    }

    [Value(1, HelpText = "Lists file containing lists of words.")]
    public string ListsFile {
        get => Late.Get(field);
        set => Late.Set(ref field, value);
    }

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
