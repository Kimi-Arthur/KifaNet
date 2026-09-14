using System.Linq;
using CommandLine;
using Kifa.Bilibili;
using Kifa.Jobs;
using Kifa.Service;
using NLog;

namespace Kifa.Tools.BiliUtil.Commands;

[Verb("list", HelpText = "Download all high quality Bilibili videos in a playlist.")]
public class DownloadPlaylistCommand : DownloadCommand {
    static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    [Value(0, Required = true, HelpText = "Playlist ID")]
    public string PlaylistId { get; set; }

    [Option('f', "folder",
        HelpText =
            "Alternate folder to use. Playlist Id will be appended as {folder}.p{id}.bilibili")]
    public string? AlternateFolder { get; set; }

    public override int Execute(KifaTask? task = null) {
        var playlist = BilibiliPlaylist.Client.Get(PlaylistId);
        if (playlist == null) {
            Logger.Fatal($"Cannot find playlist ({PlaylistId}). Exiting.");
            return 1;
        }

        foreach (var videoId in playlist.Videos.Reverse<string>()) {
            ExecuteItem(videoId, () => DownloadVideo(playlist, videoId));
            if (BreakOnExisting && LastItemAlreadyExists) {
                Logger.Info($"Stopping early: video ({videoId}) already exists.");
                break;
            }
        }

        return LogSummary();
    }

    KifaActionResult DownloadVideo(BilibiliPlaylist playlist, string videoId) {
        var video = BilibiliVideo.Client.Get(videoId);
        if (video?.Pages == null) {
            LastItemAlreadyExists = false;
            return KifaActionResult.Error($"Cannot find video ({videoId}).");
        }

        var allPagesExisted = true;
        var results = new KifaBatchActionResult();
        foreach (var page in video.Pages) {
            results.Add($"{video.Id}p{page.Id} {video.Title} {page.Title}",
                KifaActionResult.FromAction(() => {
                    var result = Download(video, page.Id,
                        alternativeFolder: $"{AlternateFolder ?? playlist.Title}.p{PlaylistId}",
                        includeUploaderInFileTitle: true);
                    if (!LastItemAlreadyExists) {
                        allPagesExisted = false;
                    }

                    return result;
                }));
        }

        LastItemAlreadyExists = allPagesExisted && video.Pages.Count > 0;
        return results;
    }
}
