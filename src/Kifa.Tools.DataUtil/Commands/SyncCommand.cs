using CommandLine;
using Kifa.Api.Files;
using Kifa.Service;
using NLog;

namespace Kifa.Tools.DataUtil.Commands;

[Verb("sync", HelpText = "Sync data in a specific file.")]
public class SyncCommand : KifaCommand {
    static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    [Option('t', "type", HelpText = "Type of data. Allowed values: goethe/words, goethe/lists")]
    public string Type { get; set; }

    [Option('a', "get-all",
        HelpText = "Whether to get all items that don't even appear in the file.")]
    public bool GetAll { get; set; }

    [Option('c', "compact", HelpText = "Whether to put leaf list into one line.")]
    public bool Compact { get; set; }

    [Value(0, Required = true, HelpText = "File to export data from.")]
    public string File { get; set; }

    public override int Execute() {
        var file = new KifaFile(File);
        var content = file.ReadAsString();

        var chef = DataChef.GetChef(Type, content);

        if (chef == null) {
            Logger.Error($"Unknown type name: {Type}.\n{content}");
            return 1;
        }

        var importResult = Logger.LogResult(chef.Import(content), "Summary");
        if (importResult.Status != KifaActionStatus.OK) {
            Logger.Error($"Failed to import data for {chef.ModelId}.");
            return (int) importResult.Status;
        }
        
        var result = Logger.LogResult(chef.Export(content, GetAll, Compact), "Summary");
        if (result.Status != KifaActionStatus.OK) {
            Logger.Error($"Failed to export data for {chef.ModelId}.");
            return (int) result.Status;
        }

        if (result.Response != null) {
            file.Delete();
            file.Write(result.Response);
        }

        return 0;
    }
}
