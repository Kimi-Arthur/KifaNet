using NLog;
using NLog.Config;
using NLog.Targets;

namespace Kifa.Tools;

public class Logging {
    public static string LogPath { get; set; } = "/tmp";

    static Target ConsoleTarget
        => new ColoredConsoleTarget("console") {
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

    static Target ConsoleFullTarget
        => new ColoredConsoleTarget("console_full") {
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

    static Target FileFullTarget
        => new FileTarget("file_full") {
            FileName =
                LogPath +
                @"/${appdomain:format={1\}}.${date:format=yyyyMMddHHmmss:cached=true}.${processid}.log",
            Layout =
                @"${pad:padding=1:fixedLength=true:inner=${level}} ${date:format=yyyy-MM-ddTHH\:mm\:ss.ffffff} ${callsite}:${callsite-linenumber} ${message}${onexception:inner=${newline}Exception\:${newline}}${exception:format=toString,Data}"
        };

    public static void ConfigureLogger(bool? fullConsole = null) {
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
}
