using Kifa.Api.Files;

namespace Kifa.Tools.BiliUtil.Commands;

public abstract class BiliCommand : KifaCommand {
    public static string RepoPath { get; set; } = "/Downloads/bilibili/$";

    protected static KifaFile GetCanonicalFile(string host, string name) {
        return new KifaFile($"{host}{RepoPath}/{name}");
    }
}
