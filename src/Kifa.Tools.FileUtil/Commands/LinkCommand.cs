using System.Collections.Generic;
using System.IO;
using System.Linq;
using CommandLine;
using Kifa.Infos;
using Kifa.Service;
using NLog;

namespace Kifa.Tools.FileUtil.Commands;

[Verb("link", HelpText = "Link $ folders to create proper directory structure.")]
public class LinkCommand : KifaCommand {
    static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    [Value(0, Required = true, HelpText = "Target $ folder(s) to link.")]
    public IEnumerable<string> Folders { get; set; }

    public static Dictionary<string, string> FolderServices { get; set; } = new();

    public override int Execute() {
        // Precondition: Must be inside /CAT folder. => last segment is category
        // Goal: /CAT/$/SOME-ID => /CAT/SOME-ID Some Name
        // Info: CAT (service => some/service) + SOME-ID (id) => SOME-ID Some Name (FolderLinks)
        // Action: ln -s $/SOME-ID "SOME-ID (id) => SOME-ID Some Name"
        var category = CurrentFolder.Name;
        if (!FolderServices.ContainsKey(category)) {
            Logger.Error($"Category {category} not found. Exit.");
            return 1;
        }

        FolderLinkableDataModel.ModelId = FolderServices[category];
        var client = new KifaServiceRestClient<FolderLinkableDataModel>();

        var folders = Folders.ToList();
        var nonDollarFolders = folders.Where(f => !f.StartsWith('$')).ToList();
        if (nonDollarFolders.Count > 0) {
            Logger.Error(
                $"Not all folders are $ folders, namely: \n\t{string.Join("\n\t", nonDollarFolders)}\n");
            return 1;
        }

        var ids = folders.Select(f => f[2..]).ToList();
        var folderLinks = client.Get(ids);

        foreach (var (folder, links) in folders.Zip(folderLinks)) {
            foreach (var link in links.FolderLinks) {
                Logger.Info($"Linking {folder} => {link}");
                Directory.CreateSymbolicLink(link, folder);
            }
        }

        return 0;
    }
}

class FolderLinkableDataModel : DataModel, WithModelId<FolderLinkableDataModel>, FolderLinkable {
    public static string ModelId { get; set; }

    public IEnumerable<string> FolderLinks { get; set; }
}
