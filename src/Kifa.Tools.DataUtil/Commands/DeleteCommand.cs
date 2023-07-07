using System;
using System.Collections.Generic;
using System.Linq;
using CommandLine;
using Kifa.Service;
using NLog;

namespace Kifa.Tools.DataUtil.Commands;

[Verb("delete", HelpText = "Add data entity based on type.")]
public class DeleteCommand : KifaCommand {
    static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    [Option('t', "type", HelpText = "Type of data. Allowed values: accounts/swisscom")]
    public string Type { get; set; }

    [Value(0, Required = true, HelpText = "Ids to delete.")]
    public IEnumerable<string> Ids { get; set; }

    public override int Execute() {
        var chef = DataChef.GetChef(Type);
        if (chef == null) {
            Logger.Fatal($"Failed to find Chef for type {Type}. Exiting.");
            return 1;
        }

        var ids = Ids.ToList();
        foreach (var id in ids) {
            Console.WriteLine(id);
        }

        if (!Confirm($"Confirming deleting the {ids.Count} items above from {Type}")) {
            Logger.Info("Canceled.");
            return 0;
        }

        return Logger.LogResult(chef.Delete(Ids.ToList()), "deleting items").Status ==
               KifaActionStatus.OK
            ? 0
            : 1;
    }
}
