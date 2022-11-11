using System.Linq;
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

        if (manga == null) {
            Logger.Fatal($"Cannot find manga with id {mangaId}.");
            return 1;
        }

        DownloadEpisode(manga, manga.Episodes[1]);

        return 0;
    }

    void DownloadEpisode(BilibiliManga manga, BilibiliMangaEpisode episode) {
        var names = manga.GetNames(episode).ToList();
        var targetFiles = names.Select(name => (Desired: new KifaFile(name.desiredName),
            Canonical: new KifaFile(name.canonicalName))).ToList();

        if (targetFiles.All(f => f.Canonical.Exists() || f.Canonical.ExistsSomewhere())) {
            Logger.Debug("All images exist.");
        } else {
            var links = manga.GetLinks(episode);
            foreach (var (name, link) in targetFiles.Zip(links)) {
                var targetFile = name.Canonical;
                if (targetFile.Exists() || targetFile.ExistsSomewhere()) {
                    Logger.Debug($"{targetFile} already exists.");
                    continue;
                }

                targetFile.Write(NoAuthClient.GetStreamAsync(link).Result);
                Logger.Debug($"Downloaded {targetFile}.");
            }
        }

        foreach (var (desired, canonical) in targetFiles) {
            canonical.Add(false);
            if (desired.Exists()) {
                desired.Add(false);
            }

            if (canonical.FileInfo.GetAllLinks().Contains(desired.Id)) {
                continue;
            }

            if (desired.Exists()) {
                if (Confirm($"Confirm removing old version of {desired}?")) {
                    desired.Delete();
                    FileInformation.Client.RemoveLocation(desired.Id, desired.ToString());
                    foreach (var (location, _) in desired.FileInfo.Locations) {
                        var file = new KifaFile(location);
                        if (file.Id == desired.Id) {
                            file.Delete();
                            FileInformation.Client.RemoveLocation(desired.Id, file.ToString());
                        }
                    }

                    FileInformation.Client.Delete(desired.Id);
                }
            }

            canonical.Copy(desired);
            desired.Add(false);
            Logger.Debug($"Copied {canonical} to {desired}");
        }
    }
}
