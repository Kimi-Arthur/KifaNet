using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using CommandLine;
using Kifa.Api.Files;
using Kifa.Jobs;
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

    [Option('k', "keep", HelpText = "Keep temp files.")]
    public bool KeepTempFiles { get; set; } = false;

    public override int Execute(KifaTask? task = null) {
        var skyProgram = new KifaServiceRestClient<SkyLiveProgram>().Get(LiveId);
        if (skyProgram == null) {
            Logger.Fatal($"Cannot find Sky program with id {liveId}.");
            return 1;
        }

        Title ??= InferTitle(skyProgram) ?? "";

        var date = skyProgram.AirDateTime.ToString("yyyyMMdd");

        var targetFile = CurrentFolder.GetFile($"{date[2..6]}/{date}_{Title}.{skyProgram.Id}.mp4");
        if (targetFile.Exists() || targetFile.ExistsSomewhere()) {
            Logger.Info($"File {targetFile} already downloaded.");
            return 0;
        }

        KifaFile? coverFile = null;
        if (!skyProgram.ImageLink?.EndsWith("svg") ?? false) {
            var coverLink = new KifaFile(skyProgram.ImageLink);
            coverFile = targetFile.GetIgnoredFile($"c.{coverLink.Extension}");
            coverLink.Copy(coverFile);
        }

        var videoLink = skyProgram.GetVideoLink();
        Logger.Info($"Link: {videoLink}");

        if (videoLink == null) {
            Logger.Fatal($"Cannot get video link for {liveId}.");
            return 1;
        }

        var mpegDash = new MpegDashFile(videoLink);
        var (videoStreamGetter, audioStreamGetters) = mpegDash.GetStreams();

        var selected = SelectMany(audioStreamGetters, choiceToString: _ => "audio");

        var parts = new List<KifaFile>();
        var videoFile = targetFile.GetIgnoredFile("v.mp4");
        parts.Add(videoFile);

        Parallel.Invoke(() => videoFile.Write(videoStreamGetter), () => {
            foreach (var (streamGetter, index) in selected.Select((x, i) => (x, i))) {
                var audioFile = targetFile.GetIgnoredFile($"a{index}.m4a");
                audioFile.Write(streamGetter);
                parts.Add(audioFile);
            }
        });

        MergeParts(parts, coverFile, targetFile);

        if (KeepTempFiles) {
            Logger.Info("Temp files are kept.");
        } else {
            foreach (var part in parts) {
                part.Delete();
            }

            Logger.Info("Removed temp files.");
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

    static string? InferTitle(SkyLiveProgram skyLiveProgram) {
        var title = HttpUtility.HtmlDecode(skyLiveProgram.Title);
        if (skyLiveProgram.Subtitle?.Length > 0) {
            title += " - " + HttpUtility.HtmlDecode(skyLiveProgram.Subtitle);
        }

        return title;
    }
}
