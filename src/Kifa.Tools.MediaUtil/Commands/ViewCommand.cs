using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CommandLine;
using FFMpegCore;
using Kifa.Api.Files;
using Kifa.Graphics;
using Kifa.Jobs;
using NLog;

namespace Kifa.Tools.MediaUtil.Commands;

[Verb("view", HelpText = "View files with its thumbnails etc.")]
public class ViewCommand : KifaCommand {
    static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    [Value(0, Required = true, HelpText = "Files to produce preview for.")]
    public IEnumerable<string> FileNames { get; set; }

    public static TimeSpan DefaultTimeFrame { get; set; } = TimeSpan.FromMinutes(1);

    [Option('t', "timeframe", HelpText = "Timeframe to use as thumbnail for videos.")]
    public string? Timeframe { get; set; } = null;

    public static string? DefaultWidth { get; set; } = "80%";

    [Option('w', "width", HelpText = "Display width of the view.")]
    public string? Width { get; set; } = DefaultWidth;

    public static string? DefaultHeight { get; set; } = "80%";

    [Option('h', "height", HelpText = "Display height of the view.")]
    public string? Height { get; set; } = DefaultHeight;

    public static HashSet<string> ImageExtensions { get; set; } = new() {
        "png",
        "jpg",
        "bmp",
        "pdf"
    };

    public static HashSet<string> VideoExtensions { get; set; } = new() {
        "mp4",
        "mkv",
        "webm",
        "mov",
        "ts",
        "flv"
    };

    public override int Execute(KifaTask? task = null) {
        var files = KifaFile.FindExistingFiles(FileNames, recursive: false);
        foreach (var file in files.Where(f => ImageExtensions.Contains(f.Extension.ToLower()))) {
            Console.WriteLine(file);
            Console.WriteLine(
                ITermImage.GetITermImageFromRawBytes(file.ReadAsBytes(), Width, Height));
        }

        Logger.Trace($"Window width: {Console.WindowWidth}");

        foreach (var file in files.Where(f => VideoExtensions.Contains(f.Extension.ToLower()))) {
            Console.WriteLine($"{file}\n");
            var tmp = new FileInfo(Path.Join(Path.GetTempPath(),
                Path.GetRandomFileName() + ".png"));
            if (GetScreenshot(file, tmp)) {
                Console.WriteLine(new string(' ', Console.WindowWidth / 10) +
                                  ITermImage.GetITermImageFromRawBytes(tmp.OpenRead().ToByteArray(),
                                      Width, Height));
                tmp.Delete();
            }
        }

        return 0;
    }

    bool GetScreenshot(KifaFile file, FileInfo output) {
        var info = FFProbe.Analyse(file.GetLocalPath());

        if (Timeframe == null) {
            var cover = info.VideoStreams.FirstOrDefault(v
                => v.Disposition?.GetValueOrDefault("attached_pic", false) ?? false);
            if (cover != null) {
                return Executor.Run("ffmpeg",
                        $"-i \"{file.GetLocalPath()}\" -map 0:{cover.Index} -c copy  {output.FullName}")
                    .ExitCode == 0;
            }
        }

        var timePoint = Timeframe?.ParseTimeSpanString() ?? DefaultTimeFrame;
        if (timePoint > info.Duration) {
            timePoint = DefaultTimeFrame;
            if (timePoint > info.Duration) {
                timePoint = TimeSpan.Zero;
            }
        }

        return Executor.Run("ffmpeg",
                $"-ss {timePoint} -i \"{file.GetLocalPath()}\" -frames:v 1 {output.FullName}")
            .ExitCode == 0;
    }
}
