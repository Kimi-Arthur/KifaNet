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
    class GenerateSubtitleCommand : SubUtilCommand {
        static readonly Logger logger = LogManager.GetCurrentClassLogger();

        [Value(0, Required = true, HelpText = "Target file to generate subtitle for.")]
        public string FileUri { get; set; }

        public override int Execute() {
            var source = new PimixFile(FileUri);

            var files = source.List(true).ToList();
            if (files.Count > 0) {
                foreach (var file in files) {
                    Console.WriteLine(file);
                }

                Console.Write($"Confirm generating comments for the {files.Count} files above?");
                Console.ReadLine();

                return files.Max(f => GenerateComments(new PimixFile(f.ToString())));
            }

            if (source.Exists()) {
                return GenerateComments(source);
            }

            logger.Error("Source {0} doesn't exist or folder contains no files.", source);
            return 1;
        }

        int GenerateComments(PimixFile target) {
            var document = new AssDocument();

            document.Sections.Add(new AssScriptInfoSection {
                Title = target.BaseName
            });

            document.Sections.Add(new AssStylesSection {
                Styles = AssStyle.Styles
            });

            var srts = GetSrtSubtitles(target.Parent,
                target.BaseName.Normalize(NormalizationForm.FormD));
            var chats = GetBilibiliChats(target.Parent,
                target.BaseName.Normalize(NormalizationForm.FormD));

            var events = new AssEventsSection();

            events.Events.AddRange(srts.Values.First());

            PositionNormalComments(chats.Values.First()
                .Where(c => c.Style == AssStyle.NormalCommentStyle)
                .OrderBy(c => c.Start).ToList());

            PositionTopComments(chats.Values.First()
                .Where(c => c.Style == AssStyle.TopCommentStyle)
                .OrderBy(c => c.Start).ToList());

            PositionBottomComments(chats.Values.First()
                .Where(c => c.Style == AssStyle.BottomCommentStyle)
                .OrderBy(c => c.Start).ToList());

            events.Events.AddRange(chats.Values.First());

            document.Sections.Add(events);

            var assFile =
                target.Parent.GetFile(
                    $"{target.BaseName}.{srts.Keys.First()}.{chats.Keys.First()}.ass");

            assFile.Delete();

            using (var stream = new MemoryStream()) {
                using (var sw = new StreamWriter(stream, Encoding.UTF8)) {
                    sw.Write(document);
                    sw.Flush();

                    assFile.Write(stream);
                }
            }


            return 0;
        }

        static void PositionNormalComments(List<AssDialogue> comments) {
            var screenWidth = 1920;

            var sizes = comments
                .Select(x => x.Text.TextElements.Sum(e => e.Content.Length) * 50F).ToList();

            var speeds = sizes.Zip(comments,
                    (s, c) => (screenWidth + s) / (c.End - c.Start).TotalSeconds)
                .ToList();

            AddFunction(
                comments,
                (a, b) =>
                    Math.Max(
                        sizes[a] / speeds[a] - (comments[b].Start - comments[a].Start).TotalSeconds,
                        (comments[a].End - comments[b].Start).TotalSeconds -
                        screenWidth / speeds[b]),
                (c, row) => new AssMoveFunction {
                    Start = new PointF(screenWidth + sizes[c] / 2, row * 50),
                    End = new PointF(-sizes[c] / 2, row * 50)
                });
        }

        static void PositionTopComments(List<AssDialogue> comments) {
            AddFunction(
                comments,
                (a, b) => (comments[a].End - comments[b].Start).Seconds,
                (c, row) => new AssPositionFunction {
                    Position = new PointF(960, row * 50)
                });
        }

        static void PositionBottomComments(List<AssDialogue> comments) {
            AddFunction(
                comments,
                (a, b) => (comments[a].End - comments[b].Start).Seconds,
                (c, row) => new AssPositionFunction {
                    Position = new PointF(960, 1080 - 200 - row * 50)
                });
        }

        static void AddFunction(List<AssDialogue> comments, Func<int, int, double> getOverlap,
            Func<int, int, AssTextFunction> getFunction) {
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

                    comments[i].Text.TextElements.First().Function = getFunction(i, r);
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

                    comments[i].Text.TextElements.First().Function = getFunction(i, minRow);
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

        static Dictionary<string, List<AssDialogue>> GetSrtSubtitles(PimixFile parent,
            string baseName) {
            var result = new Dictionary<string, List<AssDialogue>>();
            foreach (var file in parent.List(ignoreFiles: false, pattern: $"{baseName}.??.srt")) {
                using (var sr = new StreamReader(file.OpenRead())) {
                    result[file.BaseName.Substring(baseName.Length + 1)] =
                        SrtDocument.Parse(sr.ReadToEnd()).Lines.Select(x => x.ToAss()).ToList();
                }
            }

            return result;
        }

        static Dictionary<string, List<AssDialogue>>
            GetBilibiliChats(PimixFile parent, string baseName) {
            var result = new Dictionary<string, List<AssDialogue>>();
            foreach (var file in parent.List(ignoreFiles: false, pattern: $"{baseName}.*.xml")) {
                var chat = new BilibiliChat();
                chat.Load(file.OpenRead());
                result[file.BaseName.Substring(baseName.Length + 1)] = chat.Comments
                    .Select(x => x.GenerateAssDialogue()).ToList();
            }

            return result;
        }
    }
}
