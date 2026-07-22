using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using CommandLine;
using Kifa.Api.Files;
using Kifa.Bilibili;
using Kifa.Jobs;
using Kifa.Service;

namespace Kifa.Tools.BiliUtil.Commands;

[Verb("chat",
    HelpText =
        "Get Bilibili chat as xml document.\nSpecify one of aid, cid and bangumi-id or none if files are downloaded from bilibili.")]
class GetChatCommand : KifaCommand {
    static readonly Regex CidPattern = new(@"\bc(\d+)\b");

    [Value(0, Required = true, HelpText = "Target files to get Bilibili chats for.")]
    public IEnumerable<string> FileNames { get; set; }

    [Option('c', "cid", HelpText = "Bilibili cid for comments.")]
    public string? Cid { get; set; }

    [Option('a', "aid",
        HelpText = "Bilibili aid for the video. It can contain one segment or multiple. " +
                   "Example: av2044037, av2044037p4p5")]
    public string? Aid { get; set; }

    [Option('b', "bangumi-id", HelpText = "Bangumi id that starts with 'md'.")]
    public string? Bid { get; set; }

    [Option('x', "archive-id", HelpText = "Archive id that contains '/'.")]
    public string? ArchiveId { get; set; }

    [Option('n', "version-name", HelpText = "Version name.")]
    public string? VersionName { get; set; }

    public override int Execute(KifaTask? task = null) {
        var chats = GetChatsFromIds().ToList();

        var selectedFiles = SelectMany(KifaFile.FindExistingFiles(FileNames),
            file => file.ToString(), "files to download bilibili danmaku for");

        if (selectedFiles.Status == KifaActionStatus.OK) {
            if (chats.Count > 0) {
                GetChatsWithIds(selectedFiles.Value, chats);
            } else {
                GetChatsWithLinks(selectedFiles.Value);
            }
        } else {
            ExecuteItem("files to download bilibili danmaku for", () => selectedFiles);
        }

        return LogSummary();
    }

    IEnumerable<(BilibiliVideo? Video, BilibiliChat Chat)> GetChatsFromIds() {
        bool returned = false;
        if (Cid != null) {
            yield return (null, new BilibiliChat { Cid = Cid });
            returned = true;
        }

        if (Aid != null) {
            if (returned) {
                throw new InvalidInputException(
                    "Too many types of ids were specified. Specify only one of aid, cid and bangumi-id.");
            }

            var ids = Aid.Split('p');
            var v = BilibiliVideo.Client.Get(ids[0]);
            if (v == null) {
                throw new InvalidInputException($"Cannot find video for {Aid}.");
            }

            if (ids.Length == 1) {
                foreach (var p in v.Pages) {
                    yield return (v, p);
                }
            } else {
                foreach (var index in ids.Skip(1)) {
                    yield return (v, v.Pages[int.Parse(index) - 1]);
                }
            }

            returned = true;
        }

        if (Bid != null) {
            if (returned) {
                throw new InvalidInputException(
                    "Too many types of ids were specified. Specify only one of aid, cid and bangumi-id.");
            }

            var bangumi = BilibiliBangumi.Client.Get(Bid);
            if (bangumi == null) {
                throw new InvalidInputException($"Cannot find Bangumi for {Bid}.");
            }

            foreach (var aid in bangumi.Aids) {
                var video = BilibiliVideo.Client.Get(aid);
                if (video == null) {
                    throw new InvalidInputException($"Cannot find video for {aid}.");
                }

                foreach (var p in video.Pages) {
                    yield return (video, p);
                }
            }

            returned = true;
        }

        if (ArchiveId != null) {
            if (returned) {
                throw new InvalidInputException(
                    "Too many types of ids were specified. Specify only one of aid, cid and bangumi-id.");
            }

            var archive = BilibiliArchive.Client.Get(ArchiveId);
            if (archive == null) {
                throw new InvalidInputException($"Cannot find Archive for {ArchiveId}.");
            }

            foreach (var aid in archive.Videos) {
                var video = BilibiliVideo.Client.Get(aid);
                if (video == null) {
                    throw new InvalidInputException($"Cannot find video for {aid}.");
                }

                foreach (var p in video.Pages) {
                    yield return (video, p);
                }
            }
        }
    }

    void GetChatsWithIds(List<KifaFile> files,
        List<(BilibiliVideo? Video, BilibiliChat Chat)> chats) {
        foreach (var file in files) {
            ExecuteItem(file.ToString(), () => {
                var selected = SelectOne(chats,
                    c => c.Video != null
                        ? $"{c.Video.Title} - {c.Chat.Title} {c.Video.Id}p{c.Chat.Id} (cid={c.Chat.Cid})"
                        : $"cid={c.Chat.Cid}",
                    $"danmaku to download for {file}", startingIndex: 1, reverse: true);

                if (selected == null) {
                    return new KifaActionResult {
                        Status = KifaActionStatus.Skipped,
                        Message = $"No chat selected for {file}"
                    };
                }

                chats.RemoveAt(selected.Value.Index);

                GetChat(selected.Value.Choice.Chat, file);
                return KifaActionResult.Success();
            });
        }
    }

    static readonly Regex DanmakuCidPattern = new(@"\bc(\d+)(?:-([^\.]+))?\.xml$");

    void GetChatsWithLinks(List<KifaFile> files) {
        foreach (var file in files) {
            ExecuteItem(file.ToString(), () => {
                var danmakuFiles = file.GetSubtitleFiles("*.xml").ToList();

                var updatedAny = false;
                foreach (var danmakuFile in danmakuFiles) {
                    var match = DanmakuCidPattern.Match(danmakuFile.Name);
                    if (match.Success) {
                        var cid = match.Groups[1].Value;
                        WriteChat(new BilibiliChat { Cid = cid }, danmakuFile);
                        updatedAny = true;
                    }
                }

                if (updatedAny) {
                    return;
                }

                var links = file.FileInfo?.GetAllLinks().ToList() ?? [file.ToString()];
                if (!links.Contains(file.ToString())) {
                    links.Insert(0, file.ToString());
                }

                var cidMatch = links.Select(link => CidPattern.Match(link))
                    .FirstOrDefault(m => m.Success);
                if (cidMatch != null) {
                    GetChat(new BilibiliChat { Cid = cidMatch.Groups[1].Value }, file);
                    return;
                }

                var (video, pid, _, _) = links.Select(BilibiliVideo.Parse)
                    .FirstOrDefault(v => v.video != null, (null, 0, 0, 0));
                if (video == null) {
                    throw new KifaExecutionException($"Cannot find video for {file}");
                }

                GetChat(video.Pages[pid - 1], file);
            });
        }
    }

    void GetChat(BilibiliChat chat, KifaFile rawFile) {
        var cidTag = VersionName != null ? $"c{chat.Cid}-{VersionName}" : $"c{chat.Cid}";
        var target = rawFile.GetSubtitleFile($"{cidTag}.xml");
        WriteChat(chat, target);
    }

    static void WriteChat(BilibiliChat chat, KifaFile target) {
        using var memoryStream = new MemoryStream();
        var writer = new XmlTextWriter(memoryStream, new UpperCaseUtf8Encoding()) {
            Formatting = Formatting.Indented
        };
        chat.RawDocument.Save(writer);

        // Append a line break to be consistent with other files.
        memoryStream.Write([Convert.ToByte('\n')]);

        target.Delete();

        memoryStream.Seek(0, SeekOrigin.Begin);
        target.Write(memoryStream);
    }
}

class UpperCaseUtf8Encoding : UTF8Encoding {
    public override string WebName => base.WebName.ToUpper();
}
