using CommandLine;
using Kifa.Cloud.Swisscom;
using NLog;

namespace Kifa.Tools.DataUtil.Commands;

[Verb("add", HelpText = "Add data entity based on type.")]
public partial class AddCommand : KifaCommand {
    static readonly Logger logger = LogManager.GetCurrentClassLogger();

    [Option('t', "type", HelpText = "Type of data. Allowed values: accounts/swisscom")]
    public string Type { get; set; }

    [Value(0, Required = true, HelpText = "Spec for creating items.")]
    public string Spec { get; set; }

    public override int Execute() {
        switch (Type) {
            case SwisscomAccount.ModelId:
                CreateSwisscomAccounts(Spec);
                return 0;
            default:
                logger.Warn($"No add logic found for {Type}");
                return 1;
        }
    }
}
