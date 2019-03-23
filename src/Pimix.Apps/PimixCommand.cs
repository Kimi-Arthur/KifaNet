using System;
using System.Collections.Generic;
using System.Linq;
using CommandLine;
using NLog;
using Pimix.Configs;

namespace Pimix.Apps {
    public abstract class PimixCommand {
        public static HashSet<string> LoggingTargets { get; set; }

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

        protected TChoice SelectOne<TChoice>(List<TChoice> choices,
            Func<TChoice, string> choiceToString = null, string choiceName = null) {
            var choiceStrings = choiceToString == null
                ? choices.Select(c => c.ToString()).ToList()
                : choices.Select(choiceToString).ToList();

            choiceName = choiceName ?? "items";

            for (int i = 0; i < choices.Count; i++) {
                Console.WriteLine($"[{i}] {choiceStrings[i]}");
            }

            Console.Write($"Choose one {choiceName} from above [0-{choices.Count - 1}]: ");
            return choices[int.Parse(Console.ReadLine() ?? "0")];
        }

        protected List<TChoice> SelectMany<TChoice>(List<TChoice> choices) {
            return choices;
        }

        protected static string Confirm(string prefix, string suggested) {
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
