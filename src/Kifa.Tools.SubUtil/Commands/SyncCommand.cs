using System;
using System.Collections.Generic;
using System.Linq;
using CommandLine;
using Kifa.Api.Files;
using Kifa.Subtitle.Ass;
using Kifa.Subtitle.Srt;
using NLog;

namespace Kifa.Tools.SubUtil.Commands;

[Verb("sync", HelpText = "Sync subtitle to be align with another subtitle.")]
public class SyncCommand : KifaCommand {
    static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    [Value(0, Required = true, HelpText = "File to sync time.")]
    public string FileName { get; set; }

    [Option('s', "source", Required = true,
        HelpText = "Reference source subtitle to sync time from.")]
    public string Source { get; set; }

    public override int Execute() {
        var file = new KifaFile(FileName);
        if (file.Extension != "ass") {
            Logger.Fatal("Only ass files are supported.");
            return 1;
        }

        var sourceFile = new KifaFile(Source);
        if (sourceFile.Extension != "ass" && sourceFile.Extension != "srt") {
            Logger.Fatal("Reference source must be ass or srt file.");
            return 1;
        }

        var subtitle = AssDocument.Parse(file.OpenRead());
        var referenceLines = sourceFile.Extension == "ass"
            ? GetAssLines(AssDocument.Parse(sourceFile.ReadAsString()))
            : GetSrtLines(SrtDocument.Parse(sourceFile.ReadAsString()));

        var lines = GetAssLines(subtitle);

        foreach (var line in lines) {
            Console.WriteLine($"{line.Start}, {line.End}, {line.Content}");
        }

        foreach (var line in referenceLines) {
            Console.WriteLine($"{line.Start}, {line.End}, {line.Content}");
        }

        return 0;
    }

    static List<SubtitleLine> GetAssLines(AssDocument assDocument) {
        return assDocument.Sections.First(section => section is AssEventsSection).AssLines
            .OfType<AssEvent>().Select(GetAssLine).ToList();
    }

    static SubtitleLine GetAssLine(AssEvent assEvent) {
        return new SubtitleLine {
            Start = assEvent.Start,
            End = assEvent.End,
            Content = assEvent.Text.ToString()
        };
    }

    static List<SubtitleLine> GetSrtLines(SrtDocument srtDocument)
        => srtDocument.Lines.Select(GetSrtLine).ToList();

    static SubtitleLine GetSrtLine(SrtLine srtLine) {
        return new SubtitleLine {
            Start = srtLine.StartTime,
            End = srtLine.EndTime,
            Content = srtLine.Text.Content
        };
    }
}

class SubtitleLine {
    public TimeSpan Start { get; set; }
    public TimeSpan End { get; set; }
    public string Content { get; set; }
}
