using System.Net.Http;
using CommandLine;
using Kifa.Api.Files;
using Kifa.Bilibili;
using Kifa.IO;
using NLog;

namespace Kifa.Tools.BiliUtil.Commands;

[Verb("manga", HelpText = "Download manga.")]
public class DownloadMangaCommand : KifaCommand {
    #region public late string MangaId { get; set; }

    string? mangaId;

    [Value(0, Required = true, HelpText = "Manga ID. Should start with 'mc'.")]
    public string MangaId {
        get => Late.Get(mangaId);
        set => Late.Set(ref mangaId, value);
    }

    #endregion

    static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    static readonly HttpClient NoAuthClient = new();

    public override int Execute() {
        var manga = BilibiliManga.Client.Get(mangaId);
        foreach (var (name, link) in manga.GetDownloadLinksForEpisode(manga.Episodes[0])) {
            var targetFile = new KifaFile(name);
            if (targetFile.Exists() || targetFile.ExistsSomewhere()) {
                Logger.Debug($"{targetFile} already exists.");
                continue;
            }

            targetFile.Write(NoAuthClient.GetStreamAsync(link).Result);
        }

        return 0;
    }
}
