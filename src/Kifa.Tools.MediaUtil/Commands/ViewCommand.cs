using CommandLine;
using Kifa.Api.Files;
using Kifa.ITerm;

namespace Kifa.Tools.MediaUtil.Commands;

[Verb("view", HelpText = "View files with its thumbnails etc.")]
public class ViewCommand : KifaCommand {
    [Value(0, Required = true, HelpText = "Files to produce preview for.")]
    public IEnumerable<string> FileNames { get; set; }

    [Option('t', "timeframe", HelpText = "Timeframe to use as thumbnail for videos.")]
    public string? Timeframe { get; set; }

    [Option('w', "width", HelpText = "Display width of the view.")]
    public int Width { get; set; } = 0;

    [Option('h', "height", HelpText = "Display height of the view.")]
    public int Height { get; set; } = 0;

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

        return 0;
    }
}
