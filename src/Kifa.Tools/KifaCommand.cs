using System;
using System.Collections.Generic;
using CommandLine;
using Kifa.Api.Files;
using Kifa.Configs;
using Kifa.Jobs;
using NLog;

namespace Kifa.Tools;

public abstract partial class KifaCommand {
    static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    public static KifaFile CurrentFolder => new(".");

    [Option('v', "verbose", HelpText = "Show most detailed log.")]
    public bool Verbose { get; set; } = false;

    [Option('V', "non-verbose", HelpText = "Show least detailed log.")]
    public bool NonVerbose { get; set; } = false;

    [Option("config", HelpText = "Use an alternative config file.")]
    public string? ConfigFile { get; set; }

    public static int Run(Func<string[], ParserResult<object>> parse, string[] args) {
        return parse(args).MapResult<KifaCommand, int>(ExecuteCommand, HandleParseFail);
    }

    static int ExecuteCommand(KifaCommand command) {
        KifaConfigs.Init(command.ConfigFile);

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

    public abstract int Execute(KifaTask? task = null);
}
