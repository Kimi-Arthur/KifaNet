using System.Collections.Generic;
using System.Linq;
using CommandLine;
using Kifa.Api.Files;
using Kifa.Jobs;
using Kifa.Tencent;
using Newtonsoft.Json;
using NLog;

namespace Kifa.Tools.BiliUtil.Commands;

[Verb("qq", HelpText = "Get Tencent chat as json document.")]
class GetTencentChatCommand : KifaCommand {
    static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    [Option('t', "tencent", HelpText = "Tencent video id, like i0045u918s5")]
    public string? VideoId { get; set; }

    [Value(0, Required = true, HelpText = "Target file(s) to add Tencent chat to.")]
    public IEnumerable<string> FileNames { get; set; }

    public override int Execute(KifaTask? task = null) {
        if (VideoId != null) {
            var selectedFiles = SelectMany(KifaFile.FindExistingFiles(FileNames),
                file => file.ToString(), "files to get tencent chat for");
            foreach (var file in selectedFiles) {
                var subtitleFile = file.GetSubtitleFile($".{VideoId}.json");
                ExecuteItem(file.ToString(), () => GetChat(subtitleFile, VideoId));
            }
        }

        return LogSummary();
    }

    void GetChat(KifaFile chatFile, string videoId) {
        var danmuList = TencentVideo.GetDanmuList(videoId);
        chatFile.Write(
            $"{JsonConvert.SerializeObject(danmuList, KifaJsonSerializerSettings.Pretty)}\n");
    }
}
