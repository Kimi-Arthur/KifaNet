using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using CommandLine;
using NLog;
using Kifa.Api.Files;
using Kifa.Bilibili;

namespace Kifa.Tools.BiliUtil.Commands;

[Verb("get", HelpText = "Get Bilibili chat as xml document.")]
class GetChatCommand : KifaFileCommand {
    static readonly Logger logger = LogManager.GetCurrentClassLogger();

    [Option('c', "cid", HelpText = "Bilibili cid for comments.")]
    public string Cid { get; set; }

    [Option('a', "aid",
        HelpText = "Bilibili aid for the video. It can contain one segment or multiple." +
                   "Example: av2044037, av2044037p4")]
    public string Aid { get; set; }

    [Option('g', "group", HelpText = "Group name.")]
    public string Group { get; set; }

    List<(BilibiliVideo video, BilibiliChat chat)> chats = new();

    public override int Execute() {
        if (Cid != null) {
            Aid = BilibiliVideo.GetAid(Cid);
        }

        if (Aid != null) {
            var ids = Aid.Split('p');
            var v = BilibiliVideo.Client.Get(ids[0]);

            if (ids.Length == 1) {
                chats.AddRange(v.Pages.Select(p => (v, p)));
            } else {
                foreach (var index in ids.Skip(1)) {
                    chats.Add((v, v.Pages[int.Parse(index) - 1]));
                }
            }
        }

        return base.Execute();
    }

    protected override int ExecuteOneKifaFile(KifaFile file) {
        var inferredAid = InferAid(file.ToString());
        if (inferredAid != null) {
            var ids = inferredAid.Split('p');
            var v = BilibiliVideo.Client.Get(ids[0]);
            var pid = ids.Length > 1 ? int.Parse(ids[1]) : 1;
            return GetChat(v.Pages[pid - 1], file);
        }

        var ((video, chat), index) = SelectOne(chats,
            c => $"{file} => {c.video.Title} - {c.chat.Title} {c.video.Id}p{c.chat.Id} (cid={c.chat.Cid})",
            "danmaku", (null, null));

        chats.RemoveAt(index);

        return index >= 0 ? GetChat(chat, file) : 0;
    }

    int GetChat(BilibiliChat chat, KifaFile rawFile) {
        var memoryStream = new MemoryStream();
        var writer = new XmlTextWriter(memoryStream, new UpperCaseUtf8Encoding()) {
            Formatting = Formatting.Indented
        };
        chat.RawDocument.Save(writer);

        // Append a line break to be consist with other files.
        memoryStream.Write(new[] { Convert.ToByte('\n') });

        memoryStream.Seek(0, SeekOrigin.Begin);

        var suffix = Group != null ? $"c{chat.Cid}-{Group}" : $"c{chat.Cid}";
        var segments = rawFile.ToString().Split(".");
        var skippedSegments = segments[segments.Length - 2] == suffix ? 2 : 1;
        var targetUri = $"{string.Join(".", segments.SkipLast(skippedSegments))}.{suffix}.xml";
        var target = new KifaFile(targetUri).GetFilePrefixed("/Subtitles");
        target.Delete();
        target.Write(memoryStream);

        memoryStream.Dispose();

        return 0;
    }

    string InferAid(string file) {
        var segments = file.Substring(file.LastIndexOf('-') + 1).Split('.');
        if (segments.Length < 3 || !segments[segments.Length - 3].StartsWith("av")) {
            logger.Debug("Cannot infer CID from file name.");
            return null;
        }

        return segments[segments.Length - 3];
    }
}

class UpperCaseUtf8Encoding : UTF8Encoding {
    public override string WebName => base.WebName.ToUpper();
}
