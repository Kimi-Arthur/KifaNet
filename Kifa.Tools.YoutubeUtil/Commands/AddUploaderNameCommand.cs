using CommandLine;
using Kifa.Jobs;
using Kifa.YouTube;
using NLog;

namespace Kifa.Tools.YoutubeUtil.Commands;

[Verb("name", HelpText = "Add a name or alias for a YouTube uploader.")]
public class AddUploaderNameCommand : KifaCommand {
    static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    [Value(0, Required = true, HelpText = "Uploader ID, handle, or channel ID.")]
    public string UploaderId { get; set; } = "";

    [Value(1, Required = true, HelpText = "Name(s) to add for the uploader.")]
    public IEnumerable<string> Names { get; set; } = [];

    [Option('c', "canonical", HelpText = "Set the (first) name as the canonical Name instead of an alias.")]
    public bool AsCanonical { get; set; } = false;

    [Option('r', "refresh", HelpText = "Force refresh server data before adding the name.")]
    public bool Refresh { get; set; } = false;

    public override int Execute(KifaTask? task = null) {
        var uploader = YouTubeUploader.Get(UploaderId, refresh: Refresh);
        if (uploader == null) {
            Logger.Fatal($"Cannot find uploader ({UploaderId}).");
            return 1;
        }

        var namesToAdd = Names.ToList();
        if (namesToAdd.Count == 0) {
            Logger.Fatal("No names provided to add.");
            return 1;
        }

        if (AsCanonical) {
            var primary = namesToAdd[0];
            if (uploader.Name != null && uploader.Name != primary) {
                uploader.NameAliases.Add(uploader.Name);
            }

            uploader.Name = primary;
            uploader.NameAliases.Remove(primary);
            namesToAdd = namesToAdd.Skip(1).ToList();
        }

        foreach (var name in namesToAdd) {
            if (uploader.Name == null) {
                uploader.Name = name;
            } else if (uploader.Name != name) {
                uploader.NameAliases.Add(name);
            }
        }

        YouTubeUploader.Client.Set(uploader);
        Logger.Info(
            $"Successfully updated uploader {uploader.Id} with Name: '{uploader.Name}', NameAliases: [{string.Join(", ", uploader.NameAliases)}].");
        return 0;
    }
}
