using CommandLine;
using Kifa.Jobs;
using Kifa.Service;
using NLog;

namespace Kifa.Tools.DataUtil.Commands;

[Verb("link", HelpText = "Link two items.")]
public class LinkCommand : KifaCommand {
    static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    [Value(0, Required = true, HelpText = "Target this link should point.")]
    public string Target { get; set; }

    [Value(1, Required = true, HelpText = "New link name.")]
    public string Link { get; set; }

    [Option('t', "type", HelpText = "Type of data. For supported types, type `datax help`.")]
    public string Type { get; set; }

    public override int Execute(KifaTask? task = null) {
        var chef = DataChef.GetChef(Type);

        if (chef == null) {
            Logger.Error($"Unknown type name: {Type}.");
            return 1;
        }

        return (int) Logger.LogResult(chef.Link(Target, Link), "linking data", LogLevel.Info).Status;
        ;
    }
}
