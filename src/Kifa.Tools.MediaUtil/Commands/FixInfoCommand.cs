using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using CommandLine;
using Kifa.Api.Files;
using Kifa.Jellyfin;
using Kifa.Jobs;
using Kifa.Service;
using NLog;

namespace Kifa.Tools.MediaUtil.Commands;

[Verb("fix", HelpText = "Fix the NFO file for the media file.")]
public class FixInfoCommand : KifaCommand {
    static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    [Value(0, Required = true, HelpText = "Target file(s) to upload.")]
    public IEnumerable<string> FileNames { get; set; }

    public override int Execute(KifaTask? task = null) {
        var files = KifaFile.FindExistingFiles(FileNames).Where(f => f.GetNfoFile().Exists())
            .ToList();

        var selectedFiles = SelectMany(files, choice => choice.ToString(), "files to fix NFO for");

        if (selectedFiles.Count == 0) {
            Logger.Warn("No files selected to fix NFO file.");
            return 1;
        }

        foreach (var file in selectedFiles) {
            ExecuteItem($"Fix of {file}", () => FixOneFile(file));
        }

        return 0;
    }

    KifaActionResult FixOneFile(KifaFile file) {
        var infoFile = file.GetNfoFile();
        var document = XDocument.Load(infoFile.OpenRead());
        if (!JellyfinEpisode.FixNfo(file.PathWithoutSuffix, document)) {
            return new KifaActionResult {
                Status = KifaActionStatus.Skipped,
                Message = "Nothing needs to be fixed"
            };
        }

        if (!Confirm("Confirm the change above?")) {
            return new KifaActionResult {
                Status = KifaActionStatus.Skipped,
                Message = "User skipped"
            };
        }

        using var ms = new MemoryStream();
        document.Save(ms);
        infoFile.Write(ms);

        return KifaActionResult.Success;
    }
}
