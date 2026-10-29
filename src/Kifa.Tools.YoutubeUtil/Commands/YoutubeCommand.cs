using Kifa.Api.Files;

namespace Kifa.Tools.YoutubeUtil.Commands;

public abstract class YoutubeCommand : KifaCommand {
    public static string RepoPath { get; set; } = Configs.BasePath ?? "/Downloads/YouTube/$";

    protected static KifaFile GetCanonicalFile(string host, string name) {
        return new KifaFile($"{host}{RepoPath}/{name}");
    }
}
