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
        var file = new KifaFile(FileName, simpleMode: true);
        if (file.Extension != "ass") {
            Logger.Fatal("Only ass files are supported.");
            return 1;
        }

        var sourceFile = new KifaFile(Source, simpleMode: true);
        if (sourceFile.Extension != "ass" && sourceFile.Extension != "srt") {
            Logger.Fatal("Reference source must be ass or srt file.");
            return 1;
        }

        var subtitle = AssDocument.Parse(file.OpenRead());
        var referenceLines = sourceFile.Extension == "ass"
            ? GetAssLines(AssDocument.Parse(sourceFile.ReadAsString()))
            : GetSrtLines(SrtDocument.Parse(sourceFile.ReadAsString()));

        var lines = GetAssLines(subtitle);

        var matchedLines =
            new List<(List<SubtitleLine> targetLines, List<SubtitleLine> sourceLines)>();
        var referenceEnumerator = referenceLines.GetEnumerator();
        var hasValue = referenceEnumerator.MoveNext();
        foreach (var line in lines) {
            while (hasValue && !IsMatch(line, referenceEnumerator.Current) &&
                   referenceEnumerator.Current.Start < line.End) {
                if (matchedLines.Count > 0 && matchedLines[^1].sourceLines.Count > 0 &&
                    matchedLines[^1].sourceLines[^1] != referenceEnumerator.Current) {
                    if (IsMatch(matchedLines[^1].targetLines[0], referenceEnumerator.Current)) {
                        matchedLines[^1].sourceLines.Add(referenceEnumerator.Current);
                    } else {
                        matchedLines.Add((new() {
                        }, new() {
                            referenceEnumerator.Current
                        }));
                    }
                }

                hasValue = referenceEnumerator.MoveNext();
            }

            if (hasValue && IsMatch(line, referenceEnumerator.Current)) {
                matchedLines.Add((new() {
                    line
                }, new() {
                    referenceEnumerator.Current
                }));
            } else {
                matchedLines.Add((new() {
                    line
                }, new()));
            }
        }

        var combinedMatchedLines =
            new List<(List<SubtitleLine> targetLines, List<SubtitleLine> sourceLines)>();
        foreach (var line in matchedLines) {
            if (combinedMatchedLines.Count > 0 && combinedMatchedLines[^1].sourceLines.Count == 1 &&
                line.sourceLines.Count == 1 &&
                combinedMatchedLines[^1].sourceLines[0] == line.sourceLines[0]) {
                combinedMatchedLines[^1].targetLines.AddRange(line.targetLines);
                continue;
            }

            combinedMatchedLines.Add(line);
        }

        foreach (var matchedLine in combinedMatchedLines) {
            if (matchedLine.targetLines.Count == 0 && matchedLine.sourceLines.Count == 0) {
                continue;
            }

            Logger.Info(">>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>");

            foreach (var targetLine in matchedLine.targetLines) {
                Logger.Info(targetLine.Start);
                Logger.Info(targetLine.End);
                Logger.Info(targetLine.Content);
            }

            Logger.Info("=======================================");

            foreach (var sourceLine in matchedLine.sourceLines) {
                Logger.Info(sourceLine.Start);
                Logger.Info(sourceLine.End);
                Logger.Info(sourceLine.Content);
            }

            Logger.Info("<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<");
        }

        return 0;
    }

    const double TimeThreshold = 0.2;

    static bool IsMatch(SubtitleLine line, SubtitleLine reference)
        => (Math.Min(line.End.TotalMilliseconds, reference.End.TotalMilliseconds) -
            Math.Max(line.Start.TotalMilliseconds, reference.Start.TotalMilliseconds)) / Math.Min(
            line.End.TotalMilliseconds - line.Start.TotalMilliseconds,
            reference.End.TotalMilliseconds - reference.Start.TotalMilliseconds) > TimeThreshold;

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
