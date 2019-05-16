using System;
using System.Collections.Generic;
using System.Linq;
using CommandLine;
using NLog;
using Pimix.Api.Files;
using Pimix.Configs;

namespace Pimix.Apps {
    public abstract class PimixCommand {
        static readonly Logger logger = LogManager.GetCurrentClassLogger();

        public static HashSet<string> LoggingTargets { get; set; }

        public static PimixFile CurrentFolder => new PimixFile(".");

        public static int Run(Func<string[], ParserResult<object>> parse, string[] args) {
            Initialize();
            return parse(args).MapResult<PimixCommand, int>(ExecuteCommand, HandleParseFail);
        }

        static int ExecuteCommand(PimixCommand command) {
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
            AppDomain.CurrentDomain.AssemblyLoad +=
                (sender, eventArgs) => PimixConfigs.LoadFromSystemConfigs(eventArgs.LoadedAssembly);

            PimixConfigs.LoadFromSystemConfigs();

            if (LoggingTargets != null) {
                ConfigureLogger();
            }
        }

        static void ConfigureLogger() {
            LogManager.Configuration.LoggingRules.Clear();

            foreach (var target in LoggingTargets) {
                var minLevel = target.EndsWith("_full") ? LogLevel.Trace : LogLevel.Debug;
                LogManager.Configuration.AddRule(minLevel, LogLevel.Fatal, target);
            }

            LogManager.ReconfigExistingLoggers();
        }

        public abstract int Execute();

        public static (TChoice choice, int index) SelectOne<TChoice>(List<TChoice> choices,
            Func<TChoice, string> choiceToString = null, string choiceName = null,
            TChoice negative = default(TChoice)) {
            var choiceStrings = choiceToString == null
                ? choices.Select(c => c.ToString()).ToList()
                : choices.Select(choiceToString).ToList();

            choiceName = choiceName ?? "items";

            for (var i = 0; i < choices.Count; i++) {
                Console.WriteLine($"[{i + 1}] {choiceStrings[i]}");
            }

            Console.Write($"Choose one from above {choiceName} [1-{choices.Count}]: ");
            var line = Console.ReadLine();
            var choice = string.IsNullOrEmpty(line) ? 0 : int.Parse(line) - 1;
            return (choice < 0 ? negative : choices[choice], choice);
        }

        public static List<TChoice> SelectMany<TChoice>(List<TChoice> choices,
            Func<TChoice, string> choiceToString = null, string choiceName = null) {
            var choiceStrings = choiceToString == null
                ? choices.Select(c => c.ToString()).ToList()
                : choices.Select(choiceToString).ToList();

            choiceName = choiceName ?? "items";

            for (var i = 0; i < choices.Count; i++) {
                Console.WriteLine($"[{i}] {choiceStrings[i]}");
            }

            Console.Write(
                $"Choose 0 or more from above {choiceName} [0-{choices.Count - 1}] (default is all, . is nothing): ");
            var reply = Console.ReadLine() ?? "";
            var chosen = reply == "" ? choices :
                reply == "." ? new List<TChoice>() : reply.Split(',').SelectMany(i
                    => i.Contains('-')
                        ? choices.Take(int.Parse(i.Substring(i.IndexOf('-') + 1)) + 1)
                            .Skip(int.Parse(i.Substring(0, i.IndexOf('-'))))
                        : new List<TChoice> {
                            choices[int.Parse(i)]
                        }).ToList();
            logger.Debug($"Selected {chosen.Count} out of {choices.Count} {choiceName}.");
            return chosen;
        }

        public static string Confirm(string prefix, string suggested = "") {
            while (true) {
                Console.WriteLine($"{prefix}{suggested}?");

                var line = Console.ReadLine();
                if (line == "") {
                    return suggested;
                }

                suggested = line;
            }
        }
    }
}
