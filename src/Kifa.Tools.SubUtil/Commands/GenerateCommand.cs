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
        if (selected.Status != KifaActionStatus.OK) {
            ExecuteItem("files to generate merged subtitle for", () => selected);
            return LogSummary();
        }

        foreach (var file in selected.Value) {
            ExecuteItem(file.ToString(), () => GenerateSubtitle(file));
        }

        return LogSummary();
    }

    KifaActionResult GenerateSubtitle(KifaFile file) {
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
        if (selectedSubtitles.Status == KifaActionStatus.OK) {
            events.Events.AddRange(selectedSubtitles.Value.SelectMany(sub => sub.Dialogs));
            styles.AddRange(selectedSubtitles.Value.SelectMany(sub => sub.Styles));
        }

        var bilibiliChats = SelectMany(GetBilibiliChats(file),
            chat => $"{chat.Id} ({chat.Comments.Count} chats)", "Bilibili chats to include",
            selectionKey: "bili_chats");
        var biliChatList = bilibiliChats.Status == KifaActionStatus.OK ? bilibiliChats.Value : [];
        var comments = biliChatList.SelectMany(chat => chat.Comments).ToList();
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
        var qqChatList = qqChats.Status == KifaActionStatus.OK ? qqChats.Value : [];
        if (qqChatList.Count > 0) {
            PositionNormalComments(qqChatList[0].Comments.OrderBy(c => c.Start).ToList());
            events.Events.AddRange(qqChatList[0].Comments);
        }

        document.Sections.Add(events);

        var subList = selectedSubtitles.Status == KifaActionStatus.OK ? selectedSubtitles.Value : [];

        if (subList.Count == 0 && biliChatList.Count == 0 && qqChatList.Count == 0) {
            return new KifaActionResult {
                Status = KifaActionStatus.Skipped,
                Message = $"No subtitles or danmaku selected for {file}."
            };
        }

        var subtitleIds = subList.Select(sub => sub.Id).ToList();

        var danmakuGroups = new List<string>();
        foreach (var chat in biliChatList) {
            var parts = chat.Id.Split('-');
            var group = parts.Length > 1 ? parts[1] : "bilibili";
            if (!danmakuGroups.Contains(group)) {
                danmakuGroups.Add(group);
            }
        }

        foreach (var chat in qqChatList) {
            var group = "qq";
            if (!danmakuGroups.Contains(group)) {
                danmakuGroups.Add(group);
            }
        }

        var danmakuTag = danmakuGroups.Count > 0 ? $"<{string.Join("+", danmakuGroups)}>" : null;
        var langTag = subtitleIds.Count > 0 ? string.Join("+", subtitleIds) : null;
        var filenameTag = string.Join(".", new[] { danmakuTag, langTag }.Where(t => t != null));

        var finalFile = file.GetSubtitleFile($"{filenameTag}.ass");

        if (finalFile.Exists() && !Force) {
            return new KifaActionResult {
                Status = KifaActionStatus.Skipped,
                Message =
                    $"No subtitle generated for {file} as {finalFile} exists. Overwrite with '-f'."
            };
        }

        scriptInfo.OriginalScript = string.Join(", ", subtitleIds.Concat(biliChatList.Select(c => c.Id)));

        finalFile.Delete();
        finalFile.Write(document.ToString());

        return new KifaActionResult {
            Status = KifaActionStatus.OK,
            Message =
                $"Created {finalFile} with {subList.Count} subtitles, {biliChatList.Count} Bilibili chats."
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
        => rawFile.GetSubtitleFiles("*.srt").Select(file => {
            using var sr = new StreamReader(file.OpenRead());
            return (file.BaseName[(rawFile.BaseName.Length + 1)..],
                SrtDocument.Parse(sr.ReadToEnd()).Lines.Select(x => x.ToAss()).ToList(),
                new List<AssStyle>());
        }).ToList();

    static List<(string Id, List<AssDialogue> Dialogs, List<AssStyle> Styles)>
        GetAssSubtitles(KifaFile rawFile)
        => rawFile.GetSubtitleFiles("*.ass")
            .Where(file => !file.BaseName.EndsWith(".default")).Select(file => {
                var document = AssDocument.Parse(file.OpenRead());
                return (file.BaseName[(rawFile.BaseName.Length + 1)..],
                    document.Sections.OfType<AssEventsSection>().First().Events
                        .OfType<AssDialogue>().ToList(),
                    document.Sections.OfType<AssStylesSection>().First().Styles);
            }).ToList();

    static List<(string Id, List<AssDialogue> Comments)> GetBilibiliChats(KifaFile rawFile) {
        var result = new List<(string Id, List<AssDialogue> Comments)>();
        foreach (var file in rawFile.GetSubtitleFiles("*.xml")) {
            var chat = new BilibiliChat();
            chat.Load(file.OpenRead());
            result.Add((file.BaseName.Split('.').Last(),
                chat.Comments.Select(x => x.GenerateAssDialogue()).ToList()));
        }

        return result;
    }

    static List<(string Id, List<AssDialogue> Comments)> GetTencentChats(KifaFile rawFile) {
        return rawFile.GetSubtitleFiles("*.json").Select(file => (
            file.BaseName.Split('.').Last(),
            JsonConvert.DeserializeObject<List<TencentDanmu>>(file.ReadAsString(),
                    KifaJsonSerializerSettings.Default)!.Select(x => x.GenerateAssDialogue())
                .ToList())).ToList();
    }
}
