using CommandLine;
using Kifa.Jobs;
using Kifa.Service;
using Kifa.YouTube;
using NLog;

namespace Kifa.Tools.YoutubeUtil.Commands;

[Verb("list", HelpText = "Download all high quality YouTube videos in a playlist.")]
public class DownloadPlaylistCommand : DownloadCommand {
    static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    [Value(0, Required = true, HelpText = "Playlist ID or URL.")]
    public string PlaylistId { get; set; } = "";

    [Option('f', "folder",
        HelpText =
            "Alternate folder to use. Playlist Id will be appended as {folder}.p{id}.youtube")]
    public string? AlternateFolder { get; set; }

    public override int Execute(KifaTask? task = null) {
        var playlist = YouTubePlaylist.Client.Get(PlaylistId);
        if (playlist == null) {
            Logger.Fatal($"Cannot find playlist ({PlaylistId}). Exiting.");
            return 1;
        }

        foreach (var videoId in playlist.Videos) {
            var video = YouTubeVideo.Client.Get(videoId);
            if (video == null) {
                ExecuteItem(videoId,
                    () => KifaActionResult.Error($"Cannot find video ({videoId})."));
                continue;
            }

            ExecuteItem($"{video.Id} {video.Title}",
                () => Download(video,
                    alternativeFolder: $"{AlternateFolder ?? playlist.Title}.p{PlaylistId}"));
        }

        return LogSummary();
    }
}
