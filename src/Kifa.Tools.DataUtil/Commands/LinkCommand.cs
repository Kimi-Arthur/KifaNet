using CommandLine;
using Kifa.Service;
using NLog;

namespace Kifa.Tools.DataUtil.Commands; 

[Verb("link", HelpText = "Link two items.")]
public class LinkCommand : KifaCommand {
    static readonly Logger logger = LogManager.GetCurrentClassLogger();

    [Value(0, Required = true, HelpText = "Target this link should point.")]
    public string Target { get; set; }

    [Value(1, Required = true, HelpText = "New link name.")]
    public string Link { get; set; }

    [Option('t', "type", HelpText = "Type of data. For supported types, type `datax help`.")]
    public string Type { get; set; }

    public override int Execute() {
        var chef = DataChef.GetChef(Type);

        if (chef == null) {
            logger.Error($"Unknown type name: {Type}.");
            return 1;
        }

        return (int) logger.LogResult(chef.Link(Target, Link), "Summary").Status;
        ;
    }
}