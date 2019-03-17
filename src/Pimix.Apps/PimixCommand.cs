using System;
using System.Collections.Generic;
using CommandLine;
using NLog;
using Pimix.Configs;

namespace Pimix.Apps {
    public abstract class PimixCommand {
        public static HashSet<string> LoggingTargets { get; set; }

        public static int Run(Func<ParserResult<object>> parse) {
            Initialize();
            return parse().MapResult<PimixCommand, int>(ExecuteCommand, HandleParseFail);
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
    }
}
