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
    class GenerateCommand : SubUtilCommand {
        static readonly Logger logger = LogManager.GetCurrentClassLogger();

        [Value(0, Required = true, HelpText = "Target file to generate subtitle for.")]
        public string FileUri { get; set; }

        public override int Execute() {
            var target = new PimixFile(FileUri);

            var document = new AssDocument();

            document.Sections.Add(new AssScriptInfoSection {
                Title = target.BaseName
            });

            document.Sections.Add(new AssStylesSection {
                Styles = AssStyle.Styles
            });

            var srts = GetSrt(target.Parent, target.BaseName.Normalize(NormalizationForm.FormD));
            var chats = GetBilibiliChats(target.Parent,
                target.BaseName.Normalize(NormalizationForm.FormD));

            var events = new AssEventsSection();

            events.Events.AddRange(srts.Values.First().Lines.Select(x => x.ToAss()));

            var bilibiliComments = chats.Values.First().Comments
                .Select(x => x.GenerateAssDialogue()).ToList();
            AddMove(bilibiliComments);

            events.Events.AddRange(bilibiliComments);

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

        static void AddMove(List<AssDialogue> comments) {
            var screenWidth = 1920;

            var sizes = comments
                .Select(x => x.Text.ToString().Length * 50F).ToList();

            var speeds = sizes.Zip(comments,
                    (s, c) => (screenWidth + s) / (c.End - c.Start).TotalSeconds)
                .ToList();

            var indexes = Enumerable.Range(0, comments.Count)
                .Where(i => comments[i].Style == AssStyle.NormalCommentStyle)
                .OrderBy(i => comments[i].Start).ToList();

            var rows = new List<int>();
            var maxRows = 14;
            for (int i = 0; i < maxRows; i++) {
                rows.Add(-1);
            }

            var overlap = new Func<int, int, double>((a, b) =>
                Math.Max(
                    sizes[a] / speeds[a] - (comments[b].Start - comments[a].Start).TotalSeconds,
                    (comments[a].End - comments[b].Start).TotalSeconds - screenWidth / speeds[b])
            );

            var addMove = new Action<int, int>((c, row)
                => comments[c].Text.TextElements.First().Function = new AssMoveFunction {
                    Start = new PointF(screenWidth + sizes[c] / 2, row * 50),
                    End = new PointF(-sizes[c] / 2, row * 50)
                });

            var totalMoved = 0;
            var totalMovement = 0.0;

            foreach (var i in indexes) {
                var movement = 1000.0;
                int minRow = -1;
                for (var r = 0; r < maxRows; ++r) {
                    if (rows[r] >= 0) {
                        var o = overlap(rows[r], i);
                        if (o > 0) {
                            if (o < movement) {
                                movement = Math.Min(movement, o);
                                minRow = r;
                            }

                            continue;
                        }
                    }

                    addMove(i, r);
                    rows[r] = i;
                    movement = -1;
                    break;
                }

                if (movement > 0) {
                    comments[i].Start += TimeSpan.FromSeconds(movement);
                    comments[i].End += TimeSpan.FromSeconds(movement);

                    addMove(i, minRow);
                    rows[minRow] = i;

                    logger.Warn("Comment {} moved by {}.", comments[i].Text, movement);
                    totalMoved++;
                    totalMovement += movement;
                }
            }

            logger.Info("{} comments moved, by {}.", totalMoved, totalMovement);
        }

        static Dictionary<string, SrtDocument> GetSrt(PimixFile parent, string baseName) {
            var result = new Dictionary<string, SrtDocument>();
            foreach (var file in parent.List(ignoreFiles: false, pattern: $"{baseName}.??.srt")) {
                using (var sr = new StreamReader(file.OpenRead())) {
                    result[file.BaseName.Substring(baseName.Length + 1)] =
                        SrtDocument.Parse(sr.ReadToEnd());
                }
            }

            return result;
        }

        static Dictionary<string, BilibiliChat>
            GetBilibiliChats(PimixFile parent, string baseName) {
            var result = new Dictionary<string, BilibiliChat>();
            foreach (var file in parent.List(ignoreFiles: false, pattern: $"{baseName}.*.xml")) {
                var chat = new BilibiliChat();
                chat.Load(file.OpenRead());
                result[file.BaseName.Substring(baseName.Length + 1)] = chat;
            }

            return result;
        }
    }
}
