using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using CommandLine;
using Pimix.Api.Files;
using Pimix.Bilibili;
using Pimix.Subtitle.Ass;
using Pimix.Subtitle.Srt;

namespace Pimix.Apps.SubUtil.Commands {
    [Verb("generate", HelpText = "Generate subtitle.")]
    class GenerateCommand : SubUtilCommand {
        [Value(0, Required = true, HelpText = "Target file to generate subtitle for.")]
        public string FileUri { get; set; }

        public override int Execute() {
            var target = new PimixFile(FileUri);
            var srts = GetSrt(target.Parent, target.BaseName.Normalize(NormalizationForm.FormD));

            var chats = GetBilibiliChats(target.Parent,
                target.BaseName.Normalize(NormalizationForm.FormD));

            var document = new AssDocument();

            document.Sections.Add(new AssScriptInfoSection {
                Title = target.BaseName
            });

            document.Sections.Add(new AssStylesSection {
                Styles = AssStyle.Styles
            });

            var events = new AssEventsSection();

            events.Events.AddRange(srts.Values.First().Lines.Select(x => x.ToAss()));
            events.Events.AddRange(chats.Values.First().Comments
                .Select(x => x.GenerateAssDialogue()));

            document.Sections.Add(events);

            var assFile =
                target.Parent.GetFile(
                    $"{target.BaseName}.{chats.Keys.First()}.{srts.Keys.First()}.ass");

            var bytes = Encoding.UTF8.GetBytes(document.ToString());
            assFile.Write(new MemoryStream(bytes));

            return 0;
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
