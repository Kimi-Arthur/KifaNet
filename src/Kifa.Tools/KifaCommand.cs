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

    public static string LogPath { get; set; } = "/tmp";

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
        } else {
            ConfigureLogger();
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
    }

    static Target ConsoleTarget = new ColoredConsoleTarget("console") {
        ErrorStream = true,
        Layout =
            @"${date:format=yyyy-MM-ddTHH\:mm\:ss.ffffff} ${message}${onexception:inner=${newline}Exception\:${newline}}${exception:format=toString,Data}",
        RowHighlightingRules = {
            new ConsoleRowHighlightingRule {
                Condition = "level == LogLevel.Trace",
                ForegroundColor = ConsoleOutputColor.Gray
            },
            new ConsoleRowHighlightingRule {
                Condition = "level == LogLevel.Debug",
                ForegroundColor = ConsoleOutputColor.White
            },
            new ConsoleRowHighlightingRule {
                Condition = "level == LogLevel.Info",
                ForegroundColor = ConsoleOutputColor.Green
            },
            new ConsoleRowHighlightingRule {
                Condition = "level == LogLevel.Warn",
                ForegroundColor = ConsoleOutputColor.Yellow
            },
            new ConsoleRowHighlightingRule {
                Condition = "level == LogLevel.Error",
                ForegroundColor = ConsoleOutputColor.Magenta
            },
            new ConsoleRowHighlightingRule {
                Condition = "level == LogLevel.Fatal",
                ForegroundColor = ConsoleOutputColor.Red
            }
        },
        WordHighlightingRules = {
            new ConsoleWordHighlightingRule {
                Regex = "^[^ ]* ",
                CompileRegex = true,
                ForegroundColor = ConsoleOutputColor.DarkGray
            }
        }
    };

    static Target ConsoleFullTarget = new ColoredConsoleTarget("console_full") {
        ErrorStream = true,
        Layout = @"${pad:padding=1:fixedLength=true:inner=${level}} " +
                 @"${date:format=yyyy-MM-ddTHH\:mm\:ss.ffffff} ${callsite}:${callsite-linenumber} " +
                 @"${message}${onexception:inner=${newline}Exception\:${newline}}${exception:format=toString,Data}",
        RowHighlightingRules = {
            new ConsoleRowHighlightingRule {
                Condition = "level == LogLevel.Trace",
                ForegroundColor = ConsoleOutputColor.Gray
            },
            new ConsoleRowHighlightingRule {
                Condition = "level == LogLevel.Debug",
                ForegroundColor = ConsoleOutputColor.White
            },
            new ConsoleRowHighlightingRule {
                Condition = "level == LogLevel.Info",
                ForegroundColor = ConsoleOutputColor.Green
            },
            new ConsoleRowHighlightingRule {
                Condition = "level == LogLevel.Warn",
                ForegroundColor = ConsoleOutputColor.Yellow
            },
            new ConsoleRowHighlightingRule {
                Condition = "level == LogLevel.Error",
                ForegroundColor = ConsoleOutputColor.Magenta
            },
            new ConsoleRowHighlightingRule {
                Condition = "level == LogLevel.Fatal",
                ForegroundColor = ConsoleOutputColor.Red
            }
        },
        WordHighlightingRules = {
            new ConsoleWordHighlightingRule {
                Regex = "^([^ ]* ){3}",
                CompileRegex = true,
                ForegroundColor = ConsoleOutputColor.DarkGray
            }
        }
    };

    static Target FileFullTarget = new FileTarget("file_full") {
        FileName =
            LogPath + @"/${appdomain:format={1\}}.${date:format=yyyyMMddHHmmss:cached=true}.log",
        Layout =
            @"${pad:padding=1:fixedLength=true:inner=${level}} ${date:format=yyyy-MM-ddTHH\:mm\:ss.ffffff} ${callsite}:${callsite-linenumber} ${message}${onexception:inner=${newline}Exception\:${newline}}${exception:format=toString,Data}"
    };

    static void ConfigureLogger(bool? fullConsole = null) {
        LogManager.Configuration = new LoggingConfiguration();

        LogManager.Configuration.AddTarget(ConsoleTarget);
        LogManager.Configuration.AddTarget(ConsoleFullTarget);
        LogManager.Configuration.AddTarget(FileFullTarget);

        switch (fullConsole) {
            case true:
                LogManager.Configuration.AddRule(LogLevel.Trace, LogLevel.Fatal, "console_full");
                LogManager.Configuration.AddRule(LogLevel.Trace, LogLevel.Fatal, "file_full");
                break;
            case false:
                LogManager.Configuration.AddRule(LogLevel.Info, LogLevel.Fatal, "console");
                LogManager.Configuration.AddRule(LogLevel.Trace, LogLevel.Fatal, "file_full");
                break;
            default:
                LogManager.Configuration.AddRule(LogLevel.Debug, LogLevel.Fatal, "console");
                LogManager.Configuration.AddRule(LogLevel.Trace, LogLevel.Fatal, "file_full");
                break;
        }

        LogManager.ReconfigExistingLoggers();
    }

    public abstract int Execute();
}
