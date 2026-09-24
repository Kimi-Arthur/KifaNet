using System.IO;
using CommandLine;
using Kifa.Jobs;
using Kifa.Service;
using NLog;

namespace Kifa.Tools.DataUtil.Commands;

[Verb("call", HelpText = "Call a custom RPC / action on a data model.")]
public class CallCommand : KifaCommand {
    static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    [Value(0, Required = true,
        HelpText = "Target action in <model>.<action> format (e.g. files.fix, bilibili/uploaders.refresh).")]
    public string Target { get; set; } = "";

    [Option('p', "parameters", HelpText = "Inline JSON or YAML string of parameters to pass to the action.")]
    public string? Parameters { get; set; }

    [Option('f', "file", HelpText = "Path to file containing parameters (JSON or YAML).")]
    public string? ParametersFile { get; set; }

    public override int Execute(KifaTask? task = null) {
        var lastDot = Target.LastIndexOf('.');
        if (lastDot <= 0 || lastDot == Target.Length - 1) {
            Logger.Error(
                $"Invalid target format '{Target}'. Expected <model>.<action>, e.g. files.fix.");
            return 1;
        }

        var type = Target[..lastDot];
        var action = Target[(lastDot + 1)..];

        var chef = DataChef.GetChef(type);
        if (chef == null) {
            Logger.Error($"Unknown type name: {type}.");
            return 1;
        }

        var paramContent = ParametersFile != null
            ? File.ReadAllText(ParametersFile)
            : Parameters;

        return (int) Logger.LogResult(chef.Call(action, paramContent), $"calling {type}.{action}",
            LogLevel.Info).Status;
    }
}
