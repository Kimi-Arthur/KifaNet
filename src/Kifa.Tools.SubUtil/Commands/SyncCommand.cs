using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using CommandLine;
using Kifa.Api.Files;
using Kifa.Jobs;
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

    [Option('c', "content", Required = false,
        HelpText = "Whether two subtitle files share part of the content.")]
    public bool ContentMatch { get; set; } = true;

    public override int Execute(KifaTask? task = null) {
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

        var matchedLines =
            new List<(List<SubtitleLine> TargetLines, List<SubtitleLine> SourceLines, List<string>
                MatchedWords)>();
        var referenceEnumerator = referenceLines.GetEnumerator();
        var hasValue = referenceEnumerator.MoveNext();
        foreach (var line in lines) {
            var matchedLine = (new List<SubtitleLine> {
                line
            }, new List<SubtitleLine>(), new List<string>());
            while (hasValue) {
                var matchedWords = IsContentMatch(line, referenceEnumerator.Current);
                matchedLine.Item3.AddRange(matchedWords);

                if (matchedWords.Count > 0) {
                    matchedLine.Item2.Add(referenceEnumerator.Current);
                }

                if (line.Content.Count == 0 || referenceEnumerator.Current.End > line.End) {
                    break;
                }

                hasValue = referenceEnumerator.MoveNext();
            }

            matchedLines.Add(matchedLine);
        }

        var combinedMatchedLines =
            new List<(List<SubtitleLine> TargetLines, List<SubtitleLine> SourceLines, List<string>
                MatchedWords)>();
        foreach (var line in matchedLines) {
            if (combinedMatchedLines.Count > 0 && combinedMatchedLines[^1].SourceLines.Count > 0 &&
                line.SourceLines.Count > 0 &&
                combinedMatchedLines[^1].SourceLines[^1] == line.SourceLines[0]) {
                combinedMatchedLines[^1].SourceLines.AddRange(line.SourceLines.Skip(1));
                combinedMatchedLines[^1].TargetLines.AddRange(line.TargetLines);
                continue;
            }

            combinedMatchedLines.Add(line);
        }

        foreach (var line in combinedMatchedLines) {
            if (line.SourceLines.Count == 0 || line.TargetLines.Count == 0) {
                continue;
            }

            line.TargetLines[0].Original.Start = line.SourceLines[0].Start;
            line.TargetLines[^1].Original.End = line.SourceLines[^1].End;
        }

        file.Delete();
        file.Write(subtitle.ToString());

        return 0;
    }

    const double MinTimeThreshold = 0.2;
    const double Threshold = 0.5;

    bool IsMatch(SubtitleLine line, SubtitleLine reference) {
        var score =
            (Math.Min(line.End.TotalMilliseconds, reference.End.TotalMilliseconds) -
             Math.Max(line.Start.TotalMilliseconds, reference.Start.TotalMilliseconds)) / Math.Min(
                line.End.TotalMilliseconds - line.Start.TotalMilliseconds,
                reference.End.TotalMilliseconds - reference.Start.TotalMilliseconds);

        return score switch {
            < MinTimeThreshold => false,
            _ => true
            // _ => ContentMatch ? IsContentMatch(line, reference) : score > Threshold
        };
    }

    static readonly Regex SplitterPattern = new(@"<[^>]*>| |\n|\\N|{[^}]*}|\W|(?=[^a-zA-Z0-9])");

    static List<string> IsContentMatch(SubtitleLine lineContent, SubtitleLine referenceContent) {
        var matchedWords = MatchWords(lineContent.Content, referenceContent.Content);
        if (matchedWords.Count >
            Math.Min(lineContent.Content.Count, referenceContent.Content.Count) * Threshold) {
            var words = lineContent.Content.Skip(matchedWords.EndLine - matchedWords.Count)
                .Take(matchedWords.Count).ToList();
            lineContent.Content = lineContent.Content.Skip(matchedWords.EndLine).ToList();
            referenceContent.Content =
                referenceContent.Content.Skip(matchedWords.EndReference).ToList();
            return words;
        }

        return new List<string>();
    }

    static (int Count, int EndLine, int EndReference) MatchWords(List<string> lineWords,
        List<string> referenceWords) {
        var n = lineWords.Count;
        var m = referenceWords.Count;
        var f = new int[n + 1, m + 1];
        var s = 0;
        var el = 0;
        var er = 0;
        for (var i = 0; i <= n; i++) {
            for (var j = 0; j <= m; j++) {
                if (i > 0 && j > 0 && lineWords[i - 1] == referenceWords[j - 1]) {
                    f[i, j] = f[i - 1, j - 1] + 1;
                    if (s < f[i, j]) {
                        s = Math.Max(s, f[i, j]);
                        el = i;
                        er = j;
                    }
                }
            }
        }

        return (s, el, er);
    }

    static List<SubtitleLine> GetAssLines(AssDocument assDocument) {
        return assDocument.Sections.First(section => section is AssEventsSection).AssLines
            .OfType<AssEvent>().Where(e => e.Style.Name.StartsWith("Subtitle")).Select(GetAssLine)
            .OrderBy(l => l.Start).ToList();
    }

    static readonly Regex TextPattern = new Regex(@"{\\fs40}(.*)");

    static SubtitleLine GetAssLine(AssEvent assEvent) {
        var text = assEvent.Text.ToString();
        var match = TextPattern.Match(text);
        return new SubtitleLine {
            Start = assEvent.Start,
            End = assEvent.End,
            Content = SplitterPattern.Split(match.Success ? match.Groups[1].Value : text)
                .Where(w => !string.IsNullOrEmpty(w)).Select(w => w.ToLower()).ToList(),
            Original = assEvent
        };
    }

    static List<SubtitleLine> GetSrtLines(SrtDocument srtDocument)
        => srtDocument.Lines.Select(GetSrtLine).OrderBy(l => l.Start).ToList();

    static SubtitleLine GetSrtLine(SrtLine srtLine) {
        return new SubtitleLine {
            Start = srtLine.StartTime,
            End = srtLine.EndTime,
            Content = SplitterPattern.Split(srtLine.Text.Content)
                .Where(w => !string.IsNullOrEmpty(w)).Select(w => w.ToLower()).ToList()
        };
    }
}

class SubtitleLine {
    public TimeSpan Start { get; set; }
    public TimeSpan End { get; set; }
    public List<string> Content { get; set; }
    public AssEvent Original { get; set; }
}
