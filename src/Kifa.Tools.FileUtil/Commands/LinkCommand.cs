using System.Collections.Generic;
using System.IO;
using System.Linq;
using CommandLine;
using Kifa.Api.Files;
using Kifa.Infos;
using Kifa.Jobs;
using Kifa.Service;
using NLog;

namespace Kifa.Tools.FileUtil.Commands;

[Verb("link", HelpText = "Link $ folders to create proper directory structure.")]
public class LinkCommand : KifaCommand {
    static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    [Value(0, Required = true, HelpText = "Target $ folder(s) to link.")]
    public IEnumerable<string> Folders { get; set; }

    public static Dictionary<string, string> FolderServices { get; set; } = new();

    public override int Execute(KifaTask? task = null) {
        // Precondition: Must be inside /CAT folder. => last segment is category
        // Goal: /CAT/$/SOME-ID => /CAT/SOME-ID Some Name
        // Info: CAT (service => some/service) + SOME-ID (id) => SOME-ID Some Name (FolderLinks)
        // Action: ln -s $/SOME-ID "SOME-ID (id) => SOME-ID Some Name"
        var folders = Folders.Select(folder => new KifaFile(folder)).ToList();
        var first = folders[0];

        if (first.Parent.Name != "$") {
            Logger.Fatal("The parent folder is not named as '$'. Exit.");
            return 1;
        }

        var category = first.Parent.Parent.Path[1..];
        if (!FolderServices.TryGetValue(category, out var value)) {
            Logger.Error($"Category {category} not found. Exit.");
            return 1;
        }

        FolderLinkableDataModel.ModelId = value;
        var client = new KifaServiceRestClient<FolderLinkableDataModel>();

        if (folders.Any(f => f.ParentPath != first.ParentPath)) {
            Logger.Error(
                $"Not all folders are in the same parent folder {first.Parent.GetLocalPath()}");
            return 1;
        }

        // Because of the previous check, ids are always without '/'.
        var ids = folders.Select(f => f.Name).ToList();
        var folderLinks = client.Get(ids).OnlyNonNull();
        foreach (var (folder, links) in folders.Zip(folderLinks)) {
            if (!links.FolderLinks.Any()) {
                Logger.Error($"No links found for {folder}. Skipped.");
                continue;
            }

            foreach (var link in links.Checked().FolderLinks) {
                var target = folder.Parent.Parent.GetFile(link);
                if (!Confirm($"Linking {folder.GetLocalPath()} => {target.GetLocalPath()}")) {
                    Logger.Warn(
                        $"Linking skipped for {folder.GetLocalPath()} => {target.GetLocalPath()}");
                    continue;
                }

                Directory.CreateSymbolicLink(target.GetLocalPath(), folder.GetLocalPath());
            }
        }

        return 0;
    }
}

class FolderLinkableDataModel : DataModel, WithModelId<FolderLinkableDataModel>, FolderLinkable {
    public static string ModelId { get; set; }

    public IEnumerable<string> FolderLinks { get; set; }
}
