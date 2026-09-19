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

    public static int Run(string[] args, params Type[] types)
        => Run(
            parameters => new Parser(settings => {
                settings.CaseInsensitiveEnumValues = true;
                settings.HelpWriter = Console.Error;
                settings.EnableDashDash = true;
            }).ParseArguments(parameters, types), args);

    public bool StopRequested { get; set; }

    public bool IsPrompting { get; set; }

    static int ExecuteCommand(KifaCommand command) {
        command.StopRequested = false;
        command.IsPrompting = false;
        ConsoleCancelEventHandler cancelHandler = (sender, e) => {
            e.Cancel = true;
            if (command.IsPrompting) {
                Logger.Warn("Exit requested. Cleaning up...");
                KifaShutdown.RunCleanups();
                Environment.Exit(130);
            }

            if (!command.StopRequested) {
                command.StopRequested = true;
                Logger.Warn(
                    "Stop requested. Completing current item before exiting... (Press Ctrl-C again to force quit)");
            } else {
                Logger.Warn("Forced exit requested. Cleaning up...");
                KifaShutdown.RunCleanups();
                Environment.Exit(130);
            }
        };

        Console.CancelKeyPress += cancelHandler;

        KifaConfigs.Init(command.ConfigFile);

        if (command.Verbose) {
            global::Kifa.Logging.EnableNotice = true;
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
        } finally {
            Console.CancelKeyPress -= cancelHandler;
        }
    }

    static int HandleParseFail(IEnumerable<Error> errors) => 2;

    public abstract int Execute(KifaTask? task = null);
}
