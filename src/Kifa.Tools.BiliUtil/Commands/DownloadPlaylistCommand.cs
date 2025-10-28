using System.Linq;
using CommandLine;
using Kifa.Bilibili;
using Kifa.Jobs;
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
            var video = BilibiliVideo.Client.Get(videoId);
            if (video?.Pages == null) {
                Logger.Error($"Cannot find video ({videoId}). Skipping.");
                continue;
            }

            Logger.Trace($"To download video {video}");

            foreach (var page in video.Pages) {
                ExecuteItem($"{video.Id}p{page.Id} {video.Title} {page.Title}",
                    () => Download(video, page.Id,
                        alternativeFolder: $"{AlternateFolder ?? playlist.Title}.p{PlaylistId}"));
            }
        }

        return LogSummary();
    }
}
