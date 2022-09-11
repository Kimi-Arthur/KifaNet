using CommandLine;
using FFMpegCore;
using FFMpegCore.Arguments;
using FFMpegCore.Builders.MetaData;
using Kifa.Api.Files;
using NLog;

namespace Kifa.Tools.MediaUtil.Commands;

[Verb("combine", HelpText = "Combine video files and add chapters for each.")]
public class CombineCommand : KifaCommand {
    static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    [Option('c', "cover", HelpText = "Cover file to add.")]
    public string? Cover { get; set; }

    [Option('t', "title", HelpText = "File title.")]
    public string? Title { get; set; }

    [Option('o', "output", HelpText = "Output file name.")]
    public string? OutputFile { get; set; }

    [Option('m', "add-chapters", HelpText = "Add Chapters.")]
    public bool AddChapters { get; set; } = false;

    [Value(0, Required = true, HelpText = "Files to combine.")]
    public IEnumerable<string> FileNames { get; set; }

    public override int Execute() {
        var files = FileNames.Select(file => new KifaFile(file)).ToList();

        OutputFile ??= $"{Title}.{files[0].Extension}";
        var target = new KifaFile(OutputFile);
        if (target.Exists()) {
            Logger.Info("Target file already exists. Skipped");
            return 1;
        }

        var arguments =
            FFMpegArguments.FromDemuxConcatInput(files.Select(file => $"{file.GetLocalPath()}"));
        if (Cover != null) {
            arguments.AddFileInput(new KifaFile(Cover).GetLocalPath());
        }

        arguments.AddMetaData(GetMetadata(files));

        var processor = arguments.OutputToFile(target.GetLocalPath(), addArguments: options => {
            options.WithArgument(new CustomArgument("-map 0 -c copy"));
            if (Cover != null) {
                options.WithCustomArgument("-map 1 -disposition:v:1 attached_pic");
            }
        });

        Logger.Info(processor.Arguments);
        Logger.Info(processor.ProcessSynchronously());

        return 0;
    }

    IReadOnlyMetaData GetMetadata(IEnumerable<KifaFile> files) {
        var metadata = new MetaData();

        if (Title != null) {
            metadata.Entries["title"] = Title;
        }

        var rawMediaInfos = files
            .Select(file => (Title: file.BaseName, Info: FFProbe.Analyse(file.GetLocalPath())))
            .ToList();

        if (rawMediaInfos.Count > 1 && AddChapters) {
            // Add chapters for more than one.
            var mediaInfos = new List<(string Title, TimeSpan Start, TimeSpan End)> {
                (rawMediaInfos[0].Title, TimeSpan.Zero, rawMediaInfos[0].Info.Duration)
            };

            foreach (var info in rawMediaInfos.Skip(1)) {
                var last = mediaInfos[^1];
                mediaInfos.Add((info.Title, last.End, last.End + info.Info.Duration));
            }

            metadata.Chapters.AddRange(mediaInfos.Select(info
                => new ChapterData(info.Title, info.Start, info.End)));
        }

        return metadata;
    }
}
