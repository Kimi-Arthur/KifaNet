using System;
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

    #region public late string LiveId { get; set; }

    string? liveId;

    [Value(0, Required = true, HelpText = "Live TV ID.")]
    public string LiveId {
        get => Late.Get(liveId);
        set => Late.Set(ref liveId, value);
    }

    #endregion

    [Option('t', "title", HelpText = "Descriptive file title.")]
    public string? Title { get; set; }

    public override int Execute() {
        var skyProgram = new KifaServiceRestClient<SkyProgram>().Get(LiveId);
        if (skyProgram == null) {
            Logger.Fatal($"Cannot find Sky program with id {liveId}.");
            return 1;
        }

        Title ??= InferTitle(skyProgram) ?? "";

        var date = skyProgram.AirDateTime.ToString("yyyyMMdd");

        var targetFile = KifaFile.GetLocal($"/Downloads/Soccer/{date[2..6]}/{date}_{Title}.{skyProgram.Id}.mp4");
        if (targetFile.Exists() || targetFile.ExistsSomewhere()) {
            Logger.Info($"File {targetFile} already downloaded.");
            return 0;
        }

        KifaFile? coverFile = null;
        if (skyProgram.ImageLink?.EndsWith("svg") != true) {
            var coverLink = new KifaFile(skyProgram.ImageLink);
            coverFile = targetFile.GetTempFile($"c.{coverLink.Extension}");
            coverLink.Copy(coverFile);
        }

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

        MergeParts(parts, coverFile, targetFile);

        foreach (var part in parts) {
            part.Delete();
        }
        
        // Cover file is left there by design as avidemux will not bring the cover along.
        
        return 0;
    }

    static void MergeParts(List<KifaFile> parts, KifaFile? cover, KifaFile target) {
        var arguments = cover == null
            ? $"{string.Join(" ", parts.Select((_, index) => $"-map {index}"))} -c copy"
            : $"-i \"{cover.GetLocalPath()}\" " +
              string.Join(" ", parts.Select((_, index) => $"-map {index}")) + " -c copy " +
              $"-map {parts.Count} -disposition:v:1 attached_pic";
        var result = Executor.Run("ffmpeg",
            string.Join(" ", parts.Select(f => $"-i \"{f.GetLocalPath()}\"")) +
            $" {arguments} \"{target.GetLocalPath()}\"");

        if (result.ExitCode != 0) {
            throw new Exception("Merging files failed.");
        }
    }

    static string? InferTitle(SkyProgram skyProgram) {
        var title = HttpUtility.HtmlDecode(skyProgram.Title);
        if (skyProgram.Subtitle?.Length > 0) {
            title += " - " + HttpUtility.HtmlDecode(skyProgram.Subtitle);
        }

        return title;
    }
}
