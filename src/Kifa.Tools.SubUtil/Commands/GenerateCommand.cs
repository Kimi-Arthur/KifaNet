using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using CommandLine;
using Kifa.Api.Files;
using Kifa.Bilibili;
using Kifa.Jobs;
using Kifa.Service;
using Kifa.Subtitle.Ass;
using Kifa.Subtitle.Srt;
using Kifa.Tencent;
using Newtonsoft.Json;
using NLog;

namespace Kifa.Tools.SubUtil.Commands;

[Verb("generate", HelpText = "Generate subtitle.")]
class GenerateCommand : KifaCommand {
    static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    [Value(0, Required = true, HelpText = "Target files to generate subtitle for.")]
    public IEnumerable<string> FileNames { get; set; }

    [Option('f', "force", HelpText = "Forcing generating the subtitle.")]
    public bool Force { get; set; }

    public override int Execute(KifaTask? task = null) {
        var selected = SelectMany(KifaFile.FindExistingFiles(FileNames), file => file.ToString(),
            "files to generate merged subtitle for");
        foreach (var file in selected) {
            ExecuteItem(file.ToString(), () => GenerateSubtitle(file));
        }

        return LogSummary();
    }

    KifaActionResult GenerateSubtitle(KifaFile file) {
        var finalFile = file.GetSubtitleFile("default.ass");

        if (finalFile.Exists() && !Force) {
            return new KifaActionResult {
                Status = KifaActionStatus.Skipped,
                Message =
                    $"No subtitle generated for {file} as {finalFile} exists. Overwrite with '-f'."
            };
        }

        var document = new AssDocument();

        var scriptInfo = new AssScriptInfoSection {
            Title = file.BaseName,
            PlayResX = AssScriptInfoSection.PreferredPlayResX,
            PlayResY = AssScriptInfoSection.PreferredPlayResY
        };
        document.Sections.Add(scriptInfo);

        var styles = AssStyle.Styles;
        document.Sections.Add(new AssStylesSection {
            Styles = styles
        });

        var events = new AssEventsSection();

        var rawSubtitles = GetSrtSubtitles(file);
        rawSubtitles.AddRange(GetAssSubtitles(file));
        var selectedSubtitles = SelectMany(rawSubtitles,
            subtitle => $"{subtitle.Id} ({subtitle.Dialogs.Count} lines)", "subtitles to include",
            selectionKey: "subtitles");
        events.Events.AddRange(selectedSubtitles.SelectMany(sub => sub.Dialogs));

        // TODO: Do duplication check.
        styles.AddRange(selectedSubtitles.SelectMany(sub => sub.Styles));

        var bilibiliChats = SelectMany(GetBilibiliChats(file),
            chat => $"{chat.Id} ({chat.Comments.Count} chats)", "Bilibili chats to include",
            selectionKey: "bili_chats");
        var comments = bilibiliChats.SelectMany(chat => chat.Comments).ToList();
        PositionNormalComments(comments.Where(c => c.Style == AssStyle.NormalCommentStyle)
            .OrderBy(c => c.Start).ToList());
        PositionTopComments(comments.Where(c => c.Style == AssStyle.TopCommentStyle)
            .OrderBy(c => c.Start).ToList());
        PositionBottomComments(comments.Where(c => c.Style == AssStyle.BottomCommentStyle)
            .OrderBy(c => c.Start).ToList());
        events.Events.AddRange(comments);

        var qqChats = SelectMany(GetTencentChats(file),
            chat => $"{chat.Id} ({chat.Comments.Count} chats)", "QQ chats to include",
            selectionKey: "qq_chats");
        if (qqChats.Count > 0) {
            PositionNormalComments(qqChats[0].Comments.OrderBy(c => c.Start).ToList());
            events.Events.AddRange(qqChats[0].Comments);
        }

        document.Sections.Add(events);

        var subtitleIds = new List<string>();

        if (selectedSubtitles.Count > 0) {
            subtitleIds.AddRange(selectedSubtitles.Select(sub => sub.Id));
        }

        if (bilibiliChats.Count > 0) {
            subtitleIds.AddRange(bilibiliChats.Select(chat => chat.Id));
        }

        scriptInfo.OriginalScript = string.Join(", ", subtitleIds);

        finalFile.Delete();
        finalFile.Write(document.ToString());

        return new KifaActionResult {
            Status = KifaActionStatus.OK,
            Message =
                $"Created {finalFile} with {selectedSubtitles.Count} subtitles, {bilibiliChats} Bilibili chats."
        };
    }

    static void PositionNormalComments(List<AssDialogue> comments) {
        var screenWidth = 1920;

        var sizes = comments.Select(x
            => x.Text.TextElements.Where(e => e is AssDialogueRawTextElement)
                .Sum(e => ((AssDialogueRawTextElement) e).Content.Length) * 50F).ToList();

        var speeds = sizes.Zip(comments,
            (s, c) => (screenWidth + s) / (c.End - c.Start).TotalSeconds).ToList();

        AddFunction(comments,
            (a, b) => Math.Max(
                sizes[a] / speeds[a] - (comments[b].Start - comments[a].Start).TotalSeconds,
                (comments[a].End - comments[b].Start).TotalSeconds - screenWidth / speeds[b]),
            (c, row) => new AssDialogueControlTextElement {
                Elements = new List<AssControlElement> {
                    new MoveFunction {
                        StartPosition = new PointF(screenWidth + sizes[c] / 2, row * 50),
                        EndPosition = new PointF(-sizes[c] / 2, row * 50)
                    }
                }
            });
    }

    static void PositionTopComments(List<AssDialogue> comments) {
        AddFunction(comments, (a, b) => (comments[a].End - comments[b].Start).Seconds, (c, row)
            => new AssDialogueControlTextElement {
                Elements = new List<AssControlElement> {
                    new PositionFunction {
                        Position = new PointF(960, row * 50)
                    }
                }
            });
    }

    static void PositionBottomComments(List<AssDialogue> comments) {
        AddFunction(comments, (a, b) => (comments[a].End - comments[b].Start).Seconds, (c, row)
            => new AssDialogueControlTextElement {
                Elements = new List<AssControlElement> {
                    new PositionFunction {
                        Position = new PointF(960, 1080 - 200 - row * 50)
                    }
                }
            });
    }

    static void AddFunction(List<AssDialogue> comments, Func<int, int, double> getOverlap,
        Func<int, int, AssDialogueTextElement> getFunction) {
        var rows = new List<int>();
        var maxRows = 30;
        for (var i = 0; i < maxRows; i++) {
            rows.Add(-1);
        }

        var totalMoved = 0;
        var totalMovement = 0.0;
        var totalBigMove = 0;
        for (var i = 0; i < comments.Count; i++) {
            var movement = 100000.0;
            var minRow = -1;
            for (var r = 0; r < maxRows; ++r) {
                if (rows[r] >= 0) {
                    var o = getOverlap(rows[r], i);
                    if (o > 0) {
                        if (o < movement) {
                            movement = Math.Min(movement, o);
                            minRow = r;
                        }

                        continue;
                    }
                }

                comments[i].Text.TextElements.Insert(0, getFunction(i, r));
                rows[r] = i;
                movement = -1;
                break;
            }

            if (movement > 0) {
                if (movement > 10) {
                    totalBigMove++;
                    Logger.Warn($"Comment {comments[i].Text} moved by {movement}.");
                }

                comments[i].Start += TimeSpan.FromSeconds(movement);
                comments[i].End += TimeSpan.FromSeconds(movement);

                comments[i].Text.TextElements.Insert(0, getFunction(i, minRow));
                rows[minRow] = i;

                totalMoved++;
                totalMovement += movement;
            }
        }

        Logger.Trace($"{totalMoved} comments moved, by {totalMovement} in total.");
        if (totalBigMove > 0) {
            Logger.Debug($"{totalBigMove} comments are moved by more than 10 seconds!");
        }
    }

    static List<(string Id, List<AssDialogue> Dialogs, List<AssStyle> Styles)>
        GetSrtSubtitles(KifaFile rawFile)
        => rawFile.GetSubtitleFile().Parent
            .List(ignoreFiles: false, pattern: $"{rawFile.BaseName}.*.srt").Select(file => {
                using var sr = new StreamReader(file.OpenRead());
                return (file.BaseName[(rawFile.BaseName.Length + 1)..],
                    SrtDocument.Parse(sr.ReadToEnd()).Lines.Select(x => x.ToAss()).ToList(),
                    new List<AssStyle>());
            }).ToList();

    static List<(string Id, List<AssDialogue> Dialogs, List<AssStyle> Styles)>
        GetAssSubtitles(KifaFile rawFile)
        => rawFile.GetSubtitleFile().Parent
            .List(ignoreFiles: false, pattern: $"{rawFile.BaseName}.*.ass")
            .Where(file => !file.BaseName.EndsWith(".default")).Select(file => {
                var document = AssDocument.Parse(file.OpenRead());
                return (file.BaseName[(rawFile.BaseName.Length + 1)..],
                    document.Sections.OfType<AssEventsSection>().First().Events
                        .OfType<AssDialogue>().ToList(),
                    document.Sections.OfType<AssStylesSection>().First().Styles);
            }).ToList();

    static List<(string Id, List<AssDialogue> Comments)> GetBilibiliChats(KifaFile rawFile) {
        var result = new List<(string Id, List<AssDialogue> Comments)>();
        foreach (var file in rawFile.GetSubtitleFile().Parent
                     .List(ignoreFiles: false, pattern: $"{rawFile.BaseName}.*.xml")) {
            var chat = new BilibiliChat();
            chat.Load(file.OpenRead());
            result.Add((file.BaseName.Split('.').Last(),
                chat.Comments.Select(x => x.GenerateAssDialogue()).ToList()));
        }

        return result;
    }

    static List<(string Id, List<AssDialogue> Comments)> GetTencentChats(KifaFile rawFile) {
        return rawFile.GetSubtitleFile().Parent
            .List(ignoreFiles: false, pattern: $"{rawFile.BaseName}.*.json").Select(file => (
                file.BaseName.Split('.').Last(),
                JsonConvert.DeserializeObject<List<TencentDanmu>>(file.ReadAsString(),
                        KifaJsonSerializerSettings.Default)!.Select(x => x.GenerateAssDialogue())
                    .ToList())).ToList();
    }
}
