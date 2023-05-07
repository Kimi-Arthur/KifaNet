using System.Collections.Generic;
using System.IO;
using System.Linq;
using CommandLine;
using Kifa.Api.Files;
using Kifa.Tencent;
using Newtonsoft.Json;
using NLog;

namespace Kifa.Tools.BiliUtil.Commands;

[Verb("qq", HelpText = "Get Tencent chat as json document.")]
class GetTencentChatCommand : KifaCommand {
    static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    const string SubtitlesPrefix = "/Subtitles";

    [Option('t', "tencent", HelpText = "Tencent video id, like i0045u918s5")]
    public string? VideoId { get; set; }

    [Value(0, Required = true, HelpText = "Target file(s) to add Tencent chat to.")]
    public IEnumerable<string> FileNames { get; set; }

    public override int Execute() {
        var rawFile = FileNames.ElementAt(0);
        if (VideoId != null) {
            var subtitleFile =
                new KifaFile(rawFile[..rawFile.LastIndexOf('.')] + $".{VideoId}.json")
                    .GetFilePrefixed(SubtitlesPrefix);
            GetChat(subtitleFile, VideoId);
        }

        return 0;
    }

    void GetChat(KifaFile chatFile, string videoId) {
        var danmuList = TencentVideo.GetDanmuList(videoId);
        chatFile.Write(
            $"{JsonConvert.SerializeObject(danmuList, KifaJsonSerializerSettings.Pretty)}\n");
    }
}
