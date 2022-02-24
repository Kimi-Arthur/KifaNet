using CommandLine;
using NLog;
using Kifa.Api.Files;
using Kifa.Bilibili;

namespace Kifa.Tools.BiliUtil.Commands;

[Verb("rename", HelpText = "Rename video file to comply.")]
class RenameVideoCommand : KifaCommand {
    static readonly Logger logger = LogManager.GetCurrentClassLogger();

    [Value(0, Required = true, HelpText = "Target file to rename.")]
    public string FileUri { get; set; }

    public override int Execute() {
        var target = new KifaFile(FileUri);

        var ids = Helper.GetIds(target.BaseName);

        if (ids.aid == null) {
            logger.Error($"Unable to parse file name {target}.");
            return 1;
        }

        var newName = Helper.GetDesiredFileName(BilibiliVideo.Client.Get(ids.aid),
            ids.pid, ids.cid);
        if (newName == null) {
            logger.Error("CID mismatch.");
            return 1;
        }

        var newTarget = new KifaFile(FileUri) {
            BaseName = Confirm($"Confirm renaming\n{target.BaseName}\nto\n", newName)
        };
        logger.Info($"Renaming {target} to {newTarget}");
        target.Move(newTarget);

        return 0;
    }
}
