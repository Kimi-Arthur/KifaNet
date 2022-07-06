using System;
using System.Collections.Generic;
using System.Linq;
using CommandLine;
using Kifa.Api.Files;
using Kifa.Configs;
using NLog;

namespace Kifa.Tools;

public abstract class KifaCommand {
    static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    public static HashSet<string> LoggingTargets { get; set; }

    public static KifaFile CurrentFolder => new(".");

    [Option('v', "verbose", HelpText = "Show most detailed log.")]
    public virtual bool Verbose { get; set; } = false;

    public static int Run(Func<string[], ParserResult<object>> parse, string[] args) {
        Initialize();
        return parse(args).MapResult<KifaCommand, int>(ExecuteCommand, HandleParseFail);
    }

    static int ExecuteCommand(KifaCommand command) {
        if (command.Verbose) {
            ConfigureLogger(true);
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

    static void ConfigureLogger(bool fullConsole = false) {
        LogManager.Configuration.LoggingRules.Clear();

        if (fullConsole) {
            LogManager.Configuration.AddRule(LogLevel.Trace, LogLevel.Fatal, "console_full");
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

    static int defaultIndex = 1;

    public static (TChoice choice, int index) SelectOne<TChoice>(List<TChoice> choices,
        Func<TChoice, string> choiceToString = null, string choiceName = null,
        TChoice negative = default) {
        var choiceStrings = choiceToString == null
            ? choices.Select(c => c.ToString()).ToList()
            : choices.Select(choiceToString).ToList();

        choiceName ??= "items";

        for (var i = 0; i < choices.Count; i++) {
            Console.WriteLine($"[{i + 1}] {choiceStrings[i]}");
        }

        Console.WriteLine($"\nDefault [{defaultIndex}]: {choiceStrings[defaultIndex - 1]}\n");

        Console.Write(
            $"Choose one from above {choiceName} [1-{choices.Count}], Default is {defaultIndex}: ");
        var line = Console.ReadLine();
        var choice = string.IsNullOrEmpty(line) ? defaultIndex - 1 : int.Parse(line) - 1;
        defaultIndex = choice + 1;
        return (choice < 0 ? negative : choices[choice], choice);
    }

    public static List<TChoice> SelectMany<TChoice>(List<TChoice> choices,
        Func<TChoice, string> choiceToString = null, string choiceName = null,
        DefaultChoice defaultChoice = DefaultChoice.SelectAll) {
        var choiceStrings = choiceToString == null
            ? choices.Select(c => c.ToString()).ToList()
            : choices.Select(choiceToString).ToList();

        choiceName ??= "items";

        for (var i = 0; i < choices.Count; i++) {
            Console.WriteLine($"[{i}] {choiceStrings[i]}");
        }
        // switch (defaultChoice) {
        //     case DefaultChoice.SelectAll:
        //         Console.Write(
        //             $"Choose 0 or more from above {choiceName} [0-{choices.Count - 1}] (default is all, . is nothing): ");
        //         var reply = Console.ReadLine() ?? "";
        //         break;
        //     case DefaultChoice.SelectFirst:
        // }
        // var chosen = reply == "" ? choices :
        //     reply == "." ? new List<TChoice>() : reply.Split(',').SelectMany(i =>
        //         i.Contains('-')
        //             ? choices.Take(int.Parse(i.Substring(i.IndexOf('-') + 1)) + 1)
        //                 .Skip(int.Parse(i.Substring(0, i.IndexOf('-'))))
        //             : new List<TChoice> {choices[int.Parse(i)]}).ToList();
        // Logger.Debug($"Selected {chosen.Count} out of {choices.Count} {choiceName}.");
        // return chosen;

        Console.Write(
            $"Choose 0 or more from above {choices.Count} {choiceName} [0-{choices.Count - 1}] (default is all, . is nothing): ");
        var reply = Console.ReadLine() ?? "";
        var chosen = reply == "" ? choices :
            reply == "." ? new List<TChoice>() : reply.Split(',').SelectMany(i => i.Contains('-')
                ? choices.Take(int.Parse(i.Substring(i.IndexOf('-') + 1)) + 1)
                    .Skip(int.Parse(i.Substring(0, i.IndexOf('-'))))
                : new List<TChoice> {
                    choices[int.Parse(i)]
                }).ToList();
        Logger.Debug($"Selected {chosen.Count} out of {choices.Count} {choiceName}.");
        return chosen;
    }

    public static string Confirm(string prefix, string suggested) {
        while (true) {
            Console.WriteLine($"{prefix} [{suggested}]?");

            var line = Console.ReadLine();
            if (line == "") {
                return suggested;
            }

            suggested = line;
        }
    }

    public static bool Confirm(string prefix, bool suggested = true) {
        while (true) {
            var suggestedOptions = suggested ? "Y/n" : "y/N";
            Console.Write($"{prefix} [{suggestedOptions}]?");

            var line = Console.ReadLine()!;
            if (line == "") {
                return suggested;
            }

            if (line.ToLower().StartsWith("y")) {
                return true;
            }

            if (line.ToLower().StartsWith("n")) {
                return false;
            }
        }
    }
}

public enum DefaultChoice {
    SelectAll,
    SelectFirst,
    SelectNone
}
