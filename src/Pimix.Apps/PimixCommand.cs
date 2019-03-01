using System;
using System.Collections.Generic;
using NLog;
using Pimix.Configs;

namespace Pimix.Apps {
    public abstract class PimixCommand {
        public static HashSet<string> LoggingTargets { get; set; }

        public void Initialize() {
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
