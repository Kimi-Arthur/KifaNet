using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CommandLine;
using Kifa.Api.Files;
using Kifa.Jobs;
using Kifa.Media.MpegDash;
using Kifa.SkyCh;
using Kifa.SkyCh.Api;
using NLog;

namespace Kifa.Tools.SkyChDownloader.Commands;

[Verb("program", HelpText = "Download program with program id and event id.")]
public class DownloadProgramCommand : KifaCommand {
    static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    #region public late string ProgramId { get; set; }

    string? programId;

    [Value(0, Required = true, HelpText = "Program id like 'xxx/xxx'.")]
    public string ProgramId {
        get => Late.Get(programId);
        set => Late.Set(ref programId, value);
    }

    #endregion

    [Option('t', "title", Required = true, HelpText = "Descriptive file title.")]
    public string? Title { get; set; }

    [Option('c', "cover", Required = true, HelpText = "Add cover image.")]
    public string? Cover { get; set; }

    [Option('d', "date", Required = true, HelpText = "Date of program.")]
    public string? Date { get; set; }

    [Option('k', "keep", HelpText = "Keep temp files.")]
    public bool KeepTempFiles { get; set; } = false;

    public override int Execute(KifaTask? task = null) {
        var segments = ProgramId.Split("/");
        var programId = segments[0];
        var eventId = segments[1];

        var targetFile =
            CurrentFolder.GetFile($"{Date[2..6]}/{Date}_{Title}.{programId}-{eventId}.mp4");
        if (targetFile.Exists() || targetFile.ExistsSomewhere()) {
            Logger.Info($"File {targetFile} already downloaded.");
            return 0;
        }

        var response = SkyLiveProgram.SkyClient.Call(new ProgramPlayerRpc(programId, eventId));

        if (response == null) {
            throw new Exception("Failed to get player response.");
        }

        if (response.LicenseUrl != null) {
            throw new Exception("Video requires license verification.");
        }

        var videoLink = response.Url;

        Logger.Info($"Link: {videoLink}");

        if (videoLink == null) {
            Logger.Fatal($"Cannot get video link for {programId}/{eventId}.");
            return 1;
        }

        var mpegDash = new MpegDashFile(videoLink);
        var (videoStreamGetter, audioStreamGetters) = mpegDash.GetStreams();

        var selected = SelectMany(audioStreamGetters, _ => "audio", "audio tracks to include");

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

        MergeParts(parts, Cover == null ? null : new KifaFile(Cover), targetFile);

        if (KeepTempFiles) {
            Logger.Info("Temp files are kept.");
        } else {
            foreach (var part in parts) {
                part.Delete();
            }

            Logger.Info("Removed temp files.");
        }

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
}
