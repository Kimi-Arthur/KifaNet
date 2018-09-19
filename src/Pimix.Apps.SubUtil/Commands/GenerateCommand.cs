using System;
using System.Collections.Generic;
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

            ConfigureStyles();

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

            using (var stream = new MemoryStream()) {
                using (var sw = new StreamWriter(stream, Encoding.UTF8)) {
                    sw.Write(document);
                    sw.Flush();

                    assFile.Write(stream);
                }
            }


            return 0;
        }

        void AddMove(List<AssDialogue> comments) {
            var screenWidth = 1920;

            var sizes = comments
                .Select(x => x.Text.ToString().Length * 50).ToList();

            var speeds = sizes.Zip(comments,
                    (s, c) => (screenWidth + s) / (c.End - c.Start).TotalSeconds)
                .ToList();

            var rows = new List<SortedList<TimeSpan, int>>();
            var maxRows = 14;
            for (int i = 0; i < maxRows; i++) {
                rows.Add(new SortedList<TimeSpan, int>());
            }

            var collide = new Func<int, int, bool>((a, b) => {
                if ((comments[b].Start - comments[a].Start).TotalSeconds * speeds[a] <
                    sizes[a]) {
                    return true;
                }

                if ((comments[a].End - comments[b].Start).TotalSeconds * speeds[b] >
                    screenWidth) {
                    return true;
                }

                return false;
            });

            var notAdded = 0;

            for (int i = 0; i < comments.Count; i++) {
                var added = false;
                foreach (var row in rows) {
                    if (row.ContainsKey(comments[i].Start)) {
                        continue;
                    }

                    row.Add(comments[i].Start, i);
                    var index = row.IndexOfKey(comments[i].Start);
                    if (index > 0 && collide(row.ElementAt(index - 1).Value, i)) {
                        row.RemoveAt(index);
                        continue;
                    }

                    if (index < row.Count - 1 && collide(i, row.ElementAt(index + 1).Value)) {
                        row.RemoveAt(index);
                        continue;
                    }

                    added = true;
                    break;
                }

                if (!added) {
                    logger.Warn("Comment {} not added.", comments[i].Text);
                    notAdded++;
                }
            }

            foreach (var row in rows) {
                logger.Info("Row has {} comments", row.Count);
            }

            logger.Info("{} comments not added.", notAdded);
        }

        void ConfigureStyles() {
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
