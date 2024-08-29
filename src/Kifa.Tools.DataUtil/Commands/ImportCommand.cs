using CommandLine;
using Kifa.Api.Files;
using Kifa.Jobs;
using Kifa.Service;
using NLog;

namespace Kifa.Tools.DataUtil.Commands;

[Verb("import", HelpText = "Import data from a specific file.")]
public class ImportCommand : KifaCommand {
    static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    [Option('t', "type", HelpText = "Type of data. Allowed values: goethe/words, goethe/lists")]
    public string Type { get; set; }

    [Value(0, Required = true, HelpText = "File to import data from.")]
    public string File { get; set; }

    public override int Execute(KifaTask? task = null) {
        var content = new KifaFile(File).ReadAsString();

        var chef = DataChef.GetChef(Type, content);

        if (chef == null) {
            Logger.Error($"Unknown type name: {Type}.\n{content}");
            return 1;
        }

        return (int) Logger.LogResult(chef.Import(content), "importing data", LogLevel.Info).Status;
    }
}
