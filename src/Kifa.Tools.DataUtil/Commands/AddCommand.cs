using System;
using System.Collections.Generic;
using CommandLine;
using Kifa.Cloud.MegaNz;
using Kifa.Cloud.Swisscom;
using Kifa.Cloud.Telegram;
using Kifa.Jobs;
using Kifa.Service;
using NLog;

namespace Kifa.Tools.DataUtil.Commands;

[Verb("add", HelpText = "Add data entity based on type.")]
public partial class AddCommand : KifaCommand {
    static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    [Option('t', "type", HelpText = "Type of data. Allowed values: accounts/swisscom")]
    public string Type { get; set; }

    [Option('p', "threads", Default = 8,
        HelpText = "Number of parallel threads to use when creating accounts.")]
    public int ParallelThreads { get; set; }

    [Value(0, Required = true, HelpText = "Spec for creating items.")]
    public IEnumerable<string> Specs { get; set; }

    public override int Execute(KifaTask? task = null) {
        if (Type == SwisscomAccount.ModelId) {
            CreateSwisscomAccounts(Specs);
            return 0;
        }

        if (Type == TelegramAccount.ModelId) {
            CreateTelegramAccount(Specs);
            return 0;
        }

        if (Type == MegaNzAccount.ModelId) {
            AddMegaNzAccountSubCommand.CreateMegaNzAccounts(Specs);
            return 0;
        }

        Logger.Warn($"No add logic found for {Type}");
        return 1;
    }
}
