using System.Collections.Generic;
using System.Linq;
using System.Web;
using CommandLine;
using Kifa.Api.Files;
using Kifa.Media.MpegDash;
using Kifa.Service;
using Kifa.SkyCh;
using NLog;

namespace Kifa.Tools.SkyChDownloader.Commands;

[Verb("live", HelpText = "Download program with id from Live TV page.")]
public class DownloadLiveCommand : KifaCommand {
    static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    [Value(0, Required = true, HelpText = "Live TV ID.")]
    public string LiveId { get; set; }

    public override int Execute() {
        var skyProgram = new KifaServiceRestClient<SkyProgram>().Get(LiveId);
        if (skyProgram.Id != LiveId) {
            skyProgram.Id = LiveId;
        }

        var title = HttpUtility.HtmlDecode(skyProgram.Title);
        if (skyProgram.Subtitle?.Length > 0) {
            title += " - " + HttpUtility.HtmlDecode(skyProgram.Subtitle);
        }

        var targetFile = CurrentFolder.GetFile($"{title}.{skyProgram.Id}.mp4");
        Logger.Info($"Name: {title}.{skyProgram.Id}.mp4");
        Logger.Info($"Cover: {skyProgram.ImageLink}");
        var videoLink = skyProgram.GetVideoLink();
        Logger.Info($"Link: {videoLink}");

        var mpegDash = new MpegDashFile(videoLink);
        var (videoStreamGetter, audioStreamGetters) = mpegDash.GetStreams();

        var selected = SelectMany(audioStreamGetters, _ => "audio");

        var parts = new List<KifaFile>();
        var videoFile = targetFile.GetTempFile("v.mp4");
        videoFile.Write(videoStreamGetter);
        parts.Add(videoFile);

        foreach (var (streamGetter, index) in selected.Select((x, i) => (x, i))) {
            var audioFile = targetFile.GetTempFile($"a{index}.m4a");
            audioFile.Write(streamGetter);
            parts.Add(audioFile);
        }

        return 0;
    }
}
