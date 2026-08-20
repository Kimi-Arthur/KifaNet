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
        }

        return LogSummary();
    }

    KifaActionResult DownloadVideo(BilibiliPlaylist playlist, string videoId) {
        var video = BilibiliVideo.Client.Get(videoId);
        if (video?.Pages == null) {
            return KifaActionResult.Error($"Cannot find video ({videoId}).");
        }

        foreach (var page in video.Pages) {
            Download(video, page.Id,
                alternativeFolder: $"{AlternateFolder ?? playlist.Title}.p{PlaylistId}",
                includeUploaderInFileTitle: true);
        }

        return KifaActionResult.Success();
    }
}
