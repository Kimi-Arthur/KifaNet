using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using CommandLine;
using Kifa.Api.Files;
using Kifa.Bilibili;
using Kifa.Subtitle.Ass;
using Kifa.Subtitle.Srt;
using Kifa.Tencent;
using Newtonsoft.Json;
using NLog;

namespace Kifa.Tools.SubUtil.Commands;

[Verb("generate", HelpText = "Generate subtitle.")]
class GenerateCommand : KifaFileCommand {
    static readonly Logger Logger = LogManager.GetCurrentClassLogger();
    List<int> selectedBilibiliChatIndexes;

    List<int> selectedSubtitleIndexes;

    [Option('f', "force", HelpText = "Forcing generating the subtitle.")]
    public bool Force { get; set; }

    protected override Func<List<KifaFile>, string> KifaFileConfirmText
        => files => $"Confirm generating comments for the {files.Count} files above?";

    protected override int ExecuteOneKifaFile(KifaFile file) {
        var finalFile = file.GetSubtitleFile("default.ass");

        if (!finalFile.Exists() || Force) {
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
            var subtitles = SelectSubtitles(rawSubtitles);
            events.Events.AddRange(subtitles.dialogs);

            // TODO: Do duplication check.
            styles.AddRange(subtitles.styles);

            var chats = GetBilibiliChats(file);
            var comments = SelectBilibiliChats(chats);
            PositionNormalComments(comments.dialogs
                .Where(c => c.Style == AssStyle.NormalCommentStyle).OrderBy(c => c.Start).ToList());
            PositionTopComments(comments.dialogs.Where(c => c.Style == AssStyle.TopCommentStyle)
                .OrderBy(c => c.Start).ToList());
            PositionBottomComments(comments.dialogs
                .Where(c => c.Style == AssStyle.BottomCommentStyle).OrderBy(c => c.Start).ToList());
            events.Events.AddRange(comments.dialogs);

            var qqChats = GetTencentChats(file);
            if (qqChats.Count > 0) {
                PositionNormalComments(qqChats[0].content.OrderBy(c => c.Start).ToList());
                events.Events.AddRange(qqChats[0].content);
            }

            document.Sections.Add(events);

            var subtitleIds = new List<string>();

            if (subtitles.dialogs.Count > 0) {
                subtitleIds.AddRange(subtitles.ids);
            }

            if (comments.dialogs.Count > 0) {
                subtitleIds.AddRange(comments.ids);
            }

            scriptInfo.OriginalScript = string.Join(", ", subtitleIds);

            finalFile.Delete();
            finalFile.Write(document.ToString());
        }

        return 0;
    }

    (List<string> ids, List<AssDialogue> dialogs) SelectBilibiliChats(
        List<(string id, List<AssDialogue> content)> chats) {
        for (var i = 0; i < chats.Count; i++) {
            Console.WriteLine($"[{i}] {chats[i].id}: {chats[i].content.Count} comments.");
        }

        List<int> chosenIndexes;
        if (selectedBilibiliChatIndexes == null) {
            Console.Write("Choose Bilibili chats: ");
            var chosen = Console.ReadLine() ?? "";
            chosenIndexes = chosen.Trim('a').Split(",", StringSplitOptions.RemoveEmptyEntries)
                .Select(int.Parse).ToList();

            if (chosen.EndsWith('a')) {
                selectedBilibiliChatIndexes = chosenIndexes;
            }
        } else {
            chosenIndexes = selectedBilibiliChatIndexes;
        }

        var ids = new List<string>();
        var dialogs = new List<AssDialogue>();
        foreach (var index in chosenIndexes) {
            ids.Add(chats[index].id);
            dialogs.AddRange(chats[index].content);
        }

        return (ids, dialogs);
    }

    (List<string> ids, List<AssDialogue> dialogs, List<AssStyle> styles) SelectSubtitles(
        List<(string id, List<AssDialogue> content, List<AssStyle> styles)> rawSubtitles) {
        for (var i = 0; i < rawSubtitles.Count; i++) {
            Console.WriteLine(
                $"[{i}] {rawSubtitles[i].id}: {rawSubtitles[i].content.Count} lines.");
        }

        List<int> chosenIndexes;
        if (selectedSubtitleIndexes == null) {
            Console.Write("Choose subtitles: ");
            var chosen = Console.ReadLine() ?? "";
            chosenIndexes = chosen.Trim('a').Split(",", StringSplitOptions.RemoveEmptyEntries)
                .Select(int.Parse).ToList();

            if (chosen.EndsWith('a')) {
                selectedSubtitleIndexes = chosenIndexes;
            }
        } else {
            chosenIndexes = selectedSubtitleIndexes;
        }

        var ids = new List<string>();
        var dialogs = new List<AssDialogue>();
        var styles = new List<AssStyle>();
        foreach (var index in chosenIndexes) {
            ids.Add(rawSubtitles[index].id);
            dialogs.AddRange(rawSubtitles[index].content);
            styles.AddRange(rawSubtitles[index].styles);
        }

        return (ids, dialogs, styles);
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

    static List<(string id, List<AssDialogue> content, List<AssStyle> styles)>
        GetSrtSubtitles(KifaFile rawFile)
        => rawFile.GetSubtitleFile().Parent
            .List(ignoreFiles: false, pattern: $"{rawFile.BaseName}.*.srt").Select(file => {
                using var sr = new StreamReader(file.OpenRead());
                return (file.BaseName[(rawFile.BaseName.Length + 1)..],
                    SrtDocument.Parse(sr.ReadToEnd()).Lines.Select(x => x.ToAss()).ToList(),
                    new List<AssStyle>());
            }).ToList();

    static List<(string id, List<AssDialogue> content, List<AssStyle> styles)>
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

    static List<(string id, List<AssDialogue> content)> GetBilibiliChats(KifaFile rawFile) {
        var result = new List<(string id, List<AssDialogue> content)>();
        foreach (var file in rawFile.GetSubtitleFile().Parent
                     .List(ignoreFiles: false, pattern: $"{rawFile.BaseName}.*.xml")) {
            var chat = new BilibiliChat();
            chat.Load(file.OpenRead());
            result.Add((file.BaseName.Split('.').Last(),
                chat.Comments.Select(x => x.GenerateAssDialogue()).ToList()));
        }

        return result;
    }

    static List<(string id, List<AssDialogue> content)> GetTencentChats(KifaFile rawFile) {
        return rawFile.GetSubtitleFile().Parent
            .List(ignoreFiles: false, pattern: $"{rawFile.BaseName}.*.json").Select(file => (
                file.BaseName.Split('.').Last(),
                JsonConvert.DeserializeObject<List<TencentDanmu>>(file.ReadAsString(),
                        KifaJsonSerializerSettings.Default)!.Select(x => x.GenerateAssDialogue())
                    .ToList())).ToList();
    }
}
