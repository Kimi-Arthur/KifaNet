using System;
using System.Collections.Generic;
using System.Linq;
using CommandLine;
using Kifa.Api.Files;
using Kifa.Configs;
using NLog;

namespace Kifa.Tools;

public abstract partial class KifaCommand {
    static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    public static HashSet<string> LoggingTargets { get; set; }

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
            ConfigureLogger(true);
        } else if (command.NonVerbose) {
            ConfigureLogger(false);
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
        AppDomain.CurrentDomain.AssemblyLoad += (sender, eventArgs)
            => KifaConfigs.LoadFromSystemConfigs(eventArgs.LoadedAssembly);

        KifaConfigs.LoadFromSystemConfigs();

        if (LoggingTargets != null) {
            ConfigureLogger();
        }
    }

    static void ConfigureLogger(bool? fullConsole = null) {
        LogManager.Configuration.LoggingRules.Clear();

        if (fullConsole == true) {
            LogManager.Configuration.AddRule(LogLevel.Trace, LogLevel.Fatal, "console_full");
            LogManager.Configuration.AddRule(LogLevel.Trace, LogLevel.Fatal, "file_full");
        } else if (fullConsole == false) {
            LogManager.Configuration.AddRule(LogLevel.Info, LogLevel.Fatal, "console");
            LogManager.Configuration.AddRule(LogLevel.Trace, LogLevel.Fatal, "file_full");
        } else {
            foreach (var target in LoggingTargets) {
                var minLevel = target.EndsWith("_full") ? LogLevel.Trace : LogLevel.Debug;
                LogManager.Configuration.AddRule(minLevel, LogLevel.Fatal, target);
            }
        }

        LogManager.ReconfigExistingLoggers();
    }

    public abstract int Execute();
}
