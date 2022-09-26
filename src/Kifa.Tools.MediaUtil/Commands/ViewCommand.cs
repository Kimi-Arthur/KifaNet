using CommandLine;
using FFMpegCore;
using Kifa.Api.Files;
using Kifa.ITerm;

namespace Kifa.Tools.MediaUtil.Commands;

[Verb("view", HelpText = "View files with its thumbnails etc.")]
public class ViewCommand : KifaCommand {
    [Value(0, Required = true, HelpText = "Files to produce preview for.")]
    public IEnumerable<string> FileNames { get; set; }

    [Option('t', "timeframe", HelpText = "Timeframe to use as thumbnail for videos.")]
    public string Timeframe { get; set; } = "00:01:00";

    [Option('i', "ignore-cover", HelpText = "Ignore embedded cover for videos.")]
    public bool IgnoreCover { get; set; } = false;

    public static int DefaultWidth { get; set; } = 0;

    [Option('w', "width", HelpText = "Display width of the view.")]
    public int Width { get; set; } = DefaultWidth;

    public static int DefaultHeight { get; set; } = 0;

    [Option('h', "height", HelpText = "Display height of the view.")]
    public int Height { get; set; } = DefaultHeight;

    static readonly HashSet<string> ImageExtensions = new() {
        "png",
        "jpg",
        "bmp"
    };

    static readonly HashSet<string> VideoExtensions = new() {
        "mp4",
        "mkv",
        "webm",
        "mov",
        "ts"
    };

    public override int Execute() {
        var (_, files) = KifaFile.FindExistingFiles(FileNames, recursive: false);
        foreach (var file in files.Where(f => ImageExtensions.Contains(f.Extension))) {
            Console.WriteLine(file);
            Console.WriteLine(
                ITermImage.GetITermImageFromRawBytes(file.ReadAsBytes(), Width, Height));
        }

        foreach (var file in files.Where(f => VideoExtensions.Contains(f.Extension))) {
            Console.WriteLine(file);
            var tmp = new FileInfo(Path.Join(Path.GetTempPath(),
                Path.GetRandomFileName() + ".png"));
            if (GetScreenshot(file, tmp)) {
                Console.WriteLine(ITermImage.GetITermImageFromRawBytes(tmp.OpenRead().ToByteArray(),
                    Width, Height));
                tmp.Delete();
            }
        }

        return 0;
    }

    bool GetScreenshot(KifaFile file, FileInfo output) {
        if (!IgnoreCover) {
            var info = FFProbe.Analyse(file.GetLocalPath());
            var cover = info.VideoStreams.FirstOrDefault(v
                => v.Disposition.GetValueOrDefault("attached_pic", false));
            if (cover != null) {
                return Executor.Run("ffmpeg",
                        $"-i {file.GetLocalPath()} -map 0:{cover.Index} -c copy  {output.FullName}")
                    .ExitCode == 0;
            }
        }

        return Executor.Run("ffmpeg",
                   $"-ss {Timeframe} -i {file.GetLocalPath()} -frames:v 1 {output.FullName}")
               .ExitCode ==
               0;
    }
}
