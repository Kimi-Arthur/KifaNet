using CommandLine;
using Kifa.Bilibili;
using QuickEPUB;

namespace Kifa.Tools.BookUtil.Commands;

[Verb("manga", HelpText = "Generate Epub book based on Manga chapters.")]
public class CreateMangaCommand : KifaCommand {
    #region public late string MangaId { get; set; }

    string? mangaId;

    [Value(0, Required = true, HelpText = "Manga id to generate Epub book for.")]
    public string MangaId {
        get => Late.Get(mangaId);
        set => Late.Set(ref mangaId, value);
    }

    #endregion

    public override int Execute() {
        var manga = BilibiliManga.Client.Get(MangaId);
        var episode = manga.Episodes[0];
        var epub = new Epub($"{manga.Title} {episode.Title}", manga.Authors[0]);
        foreach (var page in episode.Pages) {
            epub.AddResource(page.Id + ".jpg", EpubResourceType.JPEG, null);
        }

        return 0;
    }
}
