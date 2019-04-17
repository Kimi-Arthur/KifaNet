using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using CommandLine;
using NLog;
using Pimix.Api.Files;
using Pimix.Bilibili;
using Pimix.Subtitle.Ass;
using Pimix.Subtitle.Srt;

namespace Pimix.Apps.SubUtil.Commands {
    [Verb("generate", HelpText = "Generate subtitle.")]
    class GenerateCommand : PimixFileCommand {
        static readonly Logger logger = LogManager.GetCurrentClassLogger();

        const string SubtitlesPrefix = "/Subtitles";

        [Option('f', "force", HelpText = "Forcing generating the subtitle.")]
        public bool Force { get; set; }

        protected override Func<List<PimixFile>, string> InstanceConfirmText
            => files => $"Confirm generating comments for the {files.Count} files above?";

        List<int> selectedSubtitleIndexes;
        List<int> selectedBilibiliChatIndexes;

        protected override int ExecuteOneInstance(PimixFile file) {
            var actualFile = file.Parent.GetFile($"{file.BaseName}.ass");
            var assFile = actualFile.GetFilePrefixed(SubtitlesPrefix);

            if (!assFile.Exists() || Force) {
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

                var rawSubtitles = GetSrtSubtitles(file.Parent.GetFilePrefixed(SubtitlesPrefix),
                    file.BaseName);
                rawSubtitles.AddRange(
                    GetAssSubtitles(file.Parent.GetFilePrefixed(SubtitlesPrefix),
                        file.BaseName));
                var subtitles = SelectSubtitles(rawSubtitles);
                events.Events.AddRange(subtitles.dialogs);

                // TODO: Do duplication check.
                styles.AddRange(subtitles.styles);

                var chats = GetBilibiliChats(file.Parent.GetFilePrefixed(SubtitlesPrefix),
                    file.BaseName);
                var comments = SelectBilibiliChats(chats);
                PositionNormalComments(comments.dialogs
                    .Where(c => c.Style == AssStyle.NormalCommentStyle)
                    .OrderBy(c => c.Start).ToList());
                PositionTopComments(comments.dialogs
                    .Where(c => c.Style == AssStyle.TopCommentStyle)
                    .OrderBy(c => c.Start).ToList());
                PositionBottomComments(comments.dialogs
                    .Where(c => c.Style == AssStyle.BottomCommentStyle)
                    .OrderBy(c => c.Start).ToList());
                events.Events.AddRange(comments.dialogs);

                document.Sections.Add(events);

                var subtitleIds = new List<string>();

                if (subtitles.dialogs.Count > 0) {
                    subtitleIds.AddRange(subtitles.ids);
                }

                if (comments.dialogs.Count > 0) {
                    subtitleIds.AddRange(comments.ids);
                }

                scriptInfo.OriginalScript = string.Join(", ", subtitleIds);

                assFile.Delete();
                assFile.Write(document.ToString());
            }

            actualFile.Delete();
            assFile.Copy(actualFile);

            return 0;
        }

        (List<string> ids, List<AssDialogue> dialogs) SelectBilibiliChats(
            List<(string id, List<AssDialogue> content)> chats) {
            for (int i = 0; i < chats.Count; i++) {
                Console.WriteLine($"[{i}] {chats[i].id}: {chats[i].content.Count} comments.");
            }

            List<int> chosenIndexes;
            if (selectedBilibiliChatIndexes == null) {
                Console.Write("Choose Bilibili chats: ");
                var chosen = Console.ReadLine() ?? "";
                chosenIndexes = chosen.Trim('a')
                    .Split(",", StringSplitOptions.RemoveEmptyEntries)
                    .Select(int.Parse)
                    .ToList();

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
            for (int i = 0; i < rawSubtitles.Count; i++) {
                Console.WriteLine(
                    $"[{i}] {rawSubtitles[i].id}: {rawSubtitles[i].content.Count} lines.");
            }

            List<int> chosenIndexes;
            if (selectedSubtitleIndexes == null) {
                Console.Write("Choose subtitles: ");
                var chosen = Console.ReadLine() ?? "";
                chosenIndexes = chosen.Trim('a')
                    .Split(",", StringSplitOptions.RemoveEmptyEntries)
                    .Select(int.Parse)
                    .ToList();

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

            var sizes = comments
                .Select(x => x.Text.TextElements.Where(e => e is AssDialogueRawTextElement)
                                 .Sum(e => ((AssDialogueRawTextElement) e).Content.Length) * 50F)
                .ToList();

            var speeds = sizes.Zip(comments,
                    (s, c) => (screenWidth + s) / (c.End - c.Start).TotalSeconds)
                .ToList();

            AddFunction(comments,
                (a, b) =>
                    Math.Max(
                        sizes[a] / speeds[a] - (comments[b].Start - comments[a].Start).TotalSeconds,
                        (comments[a].End - comments[b].Start).TotalSeconds -
                        screenWidth / speeds[b]),
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
            AddFunction(comments,
                (a, b) => (comments[a].End - comments[b].Start).Seconds,
                (c, row) => new AssDialogueControlTextElement {
                    Elements = new List<AssControlElement> {
                        new PositionFunction {
                            Position = new PointF(960, row * 50)
                        }
                    }
                });
        }

        static void PositionBottomComments(List<AssDialogue> comments) {
            AddFunction(comments,
                (a, b) => (comments[a].End - comments[b].Start).Seconds,
                (c, row) => new AssDialogueControlTextElement {
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
            var maxRows = 14;
            for (int i = 0; i < maxRows; i++) {
                rows.Add(-1);
            }

            var totalMoved = 0;
            var totalMovement = 0.0;
            var totalBigMove = 0;
            for (var i = 0; i < comments.Count; i++) {
                var movement = 1000.0;
                int minRow = -1;
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
                        logger.Warn("Comment {} moved by {}.", comments[i].Text, movement);
                    }

                    comments[i].Start += TimeSpan.FromSeconds(movement);
                    comments[i].End += TimeSpan.FromSeconds(movement);

                    comments[i].Text.TextElements.Insert(0, getFunction(i, minRow));
                    rows[minRow] = i;

                    totalMoved++;
                    totalMovement += movement;
                }
            }

            logger.Info("{} comments moved, by {} in total.", totalMoved, totalMovement);
            if (totalBigMove > 0) {
                logger.Warn("{} comments are moved by more than 10 seconds!", totalBigMove);
            }
        }

        static List<(string id, List<AssDialogue> content, List<AssStyle> styles)> GetSrtSubtitles(
            PimixFile parent,
            string baseName)
            => parent.List(ignoreFiles: false, pattern: $"{baseName}.*.srt").Select(file => {
                using (var sr = new StreamReader(file.OpenRead())) {
                    return (file.BaseName.Substring(baseName.Length + 1),
                        SrtDocument.Parse(sr.ReadToEnd()).Lines.Select(x => x.ToAss()).ToList(),
                        new List<AssStyle>());
                }
            }).ToList();

        static List<(string id, List<AssDialogue> content, List<AssStyle> styles)> GetAssSubtitles(
            PimixFile parent,
            string baseName)
            => parent.List(ignoreFiles: false, pattern: $"{baseName}.*.ass").Select(file => {
                var document = AssDocument.Parse(file.OpenRead());
                return (file.BaseName.Substring(baseName.Length + 1),
                    document.Sections.OfType<AssEventsSection>().First().Events
                        .OfType<AssDialogue>().ToList(),
                    document.Sections.OfType<AssStylesSection>().First().Styles);
            }).ToList();

        static List<(string id, List<AssDialogue> content)>
            GetBilibiliChats(PimixFile parent, string baseName) {
            var result = new List<(string id, List<AssDialogue> content)>();
            foreach (var file in parent.List(ignoreFiles: false, pattern: $"{baseName}*.xml")) {
                var chat = new BilibiliChat();
                chat.Load(file.OpenRead());
                result.Add((file.BaseName.Split('.').Last(),
                    chat.Comments.Select(x => x.GenerateAssDialogue()).ToList()));
            }

            return result;
        }
    }
}
