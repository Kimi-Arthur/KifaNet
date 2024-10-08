using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using CommandLine;
using Kifa.Api.Files;
using Kifa.Bilibili;
using Kifa.Jobs;
using NLog;

namespace Kifa.Tools.BiliUtil.Commands;

[Verb("chat", HelpText = "Get Bilibili chat as xml document.")]
class GetChatCommand : KifaFileCommand {
    static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    [Option('c', "cid", HelpText = "Bilibili cid for comments.")]
    public string? Cid { get; set; }

    [Option('a', "aid",
        HelpText = "Bilibili aid for the video. It can contain one segment or multiple." +
                   "Example: av2044037, av2044037p4p5")]
    public string? Aid { get; set; }

    [Option('b', "bangumi-id", HelpText = "Bangumi id that starts with 'md'.")]
    public string? Bid { get; set; }

    [Option('g', "group", HelpText = "Group name.")]
    public string? Group { get; set; }

    List<(BilibiliVideo video, BilibiliChat chat)> chats = new();

    public override int Execute(KifaTask? task = null) {
        if (Cid != null) {
            var video = BilibiliVideo.Client.Get(BilibiliVideo.GetAid(Cid));
            if (video == null) {
                Logger.Fatal($"Cannot find video for {Cid}.");
                return 1;
            }

            var chat = video.Pages.FirstOrDefault(c => c.Cid == Cid);

            if (chat == null) {
                Logger.Fatal($"Cannot find chat for {Cid}.");
                return 1;
            }

            chats.Add((video, chat));
        }

        if (Aid != null) {
            var ids = Aid.Split('p');
            var v = BilibiliVideo.Client.Get(ids[0]);
            if (v == null) {
                Logger.Fatal($"Cannot find video ({Aid}). Exiting.");
                return 1;
            }

            if (ids.Length == 1) {
                chats.AddRange(v.Pages.Select(p => (v, p)));
            } else {
                foreach (var index in ids.Skip(1)) {
                    chats.Add((v, v.Pages[int.Parse(index) - 1]));
                }
            }
        }

        if (Bid != null) {
            var bangumi = BilibiliBangumi.Client.Get(Bid);
            if (bangumi == null) {
                Logger.Fatal($"Cannot find Bangumi ({Bid}). Exiting.");
                return 1;
            }

            foreach (var aid in bangumi.Aids) {
                var video = BilibiliVideo.Client.Get(aid);
                chats.AddRange(video.Pages.Select(c => (video, c)));
            }
        }

        return base.Execute();
    }

    protected override int ExecuteOneKifaFile(KifaFile file) {
        var (video, pid, _, _) = file.FileInfo!.GetAllLinks()
            .Select(link => BilibiliVideo.Parse(link))
            .FirstOrDefault(v => v.video != null, (null, 0, 0, 0));
        if (video != null) {
            return GetChat(video.Pages[pid - 1], file);
        }

        var selected = SelectOne(chats,
            c => $"{c.video.Title} - {c.chat.Title} {c.video.Id}p{c.chat.Id} (cid={c.chat.Cid})",
            "danmaku", startingIndex: 1, reverse: true).Value;

        chats.RemoveAt(selected.Index);

        return selected.Index >= 0 ? GetChat(selected.Choice.chat, file) : 0;
    }

    int GetChat(BilibiliChat chat, KifaFile rawFile) {
        var memoryStream = new MemoryStream();
        var writer = new XmlTextWriter(memoryStream, new UpperCaseUtf8Encoding()) {
            Formatting = Formatting.Indented
        };
        chat.RawDocument.Save(writer);

        // Append a line break to be consistent with other files.
        memoryStream.Write([Convert.ToByte('\n')]);

        var cidTag = Group != null ? $"c{chat.Cid}-{Group}" : $"c{chat.Cid}";
        var target = rawFile.GetSubtitleFile($"{cidTag}.xml");
        target.Delete();

        memoryStream.Seek(0, SeekOrigin.Begin);
        target.Write(memoryStream);

        memoryStream.Dispose();

        return 0;
    }
}

class UpperCaseUtf8Encoding : UTF8Encoding {
    public override string WebName => base.WebName.ToUpper();
}
