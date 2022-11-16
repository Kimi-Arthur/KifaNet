using System;
using System.Collections.Generic;
using CommandLine;
using Kifa.Api.Files;
using Kifa.Configs;
using NLog;
using NLog.Config;
using NLog.Targets;

namespace Kifa.Tools;

public abstract partial class KifaCommand {
    static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    public static KifaFile CurrentFolder => new(".");

    [Option('v', "verbose", HelpText = "Show most detailed log.")]
    public bool Verbose { get; set; } = false;

    [Option('V', "non-verbose", HelpText = "Show least detailed log.")]
    public bool NonVerbose { get; set; } = false;

    public static int Run(Func<string[], ParserResult<object>> parse, string[] args) {
        Initialize();
        return parse(args).MapResult<KifaCommand, int>(ExecuteCommand, HandleParseFail);
    }

    static int ExecuteCommand(KifaCommand command) {
        if (command.Verbose) {
            Logging.ConfigureLogger(true);
        } else if (command.NonVerbose) {
            Logging.ConfigureLogger(false);
        } else {
            Logging.ConfigureLogger();
        }

        try {
            return command.Execute();
        } catch (Exception ex) {
            while (ex != null) {
                Console.WriteLine("Caused by:");
                Console.WriteLine(ex);
                ex = ex.InnerException;
            }

            return 1;
        }
    }

    static int HandleParseFail(IEnumerable<Error> errors) => 2;

    public static void Initialize() {
        KifaConfigs.Init();
    }

    public abstract int Execute();
}
