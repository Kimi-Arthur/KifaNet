using System.Collections.Generic;
using System.Configuration;
using System.Net;
using CommandLine;
using NLog;

namespace Pimix.Apps.FileUtil.Commands {
    abstract class FileUtilCommand {
        [Option("job-id", HelpText = "Job ID to report log as.")]
        public string JobId { get; set; } = null;

        public static HashSet<string> LoggingTargets { get; set; }

        public void Initialize() {
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
