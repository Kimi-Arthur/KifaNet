using CommandLine;
using Kifa.Bilibili;
using Kifa.Bilibili.BilibiliApi;
using Kifa.Jobs;
using NLog;

namespace Kifa.Tools.BiliUtil.Commands;

[Verb("shadow", HelpText = "Shadow history.")]
class ShadowCommand : KifaCommand {
    static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    [Option('r', "reset", HelpText = "Reset shadow history status, i.e. history on.")]
    public bool Reset { get; set; } = false;

    public override int Execute(KifaTask? task = null) {
        var response = HttpClients.GetBilibiliClient().Call(new TrackingRpc(!Reset));
        var action = Reset ? "reset" : "shadow";
        if (response.Code != 0) {
            Logger.Error($"Failed to {action} history ({response.Code}): {response.Message}");
        } else {
            Logger.Info($"Succeeded to {action} history.");
        }

        return response.Code;
    }
}
