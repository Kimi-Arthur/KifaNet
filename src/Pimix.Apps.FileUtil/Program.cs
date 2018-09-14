using System;
using System.Collections.Generic;
using CommandLine;
using NLog;
using Pimix.Apps.FileUtil.Commands;
using Pimix.Configs;

namespace Pimix.Apps.FileUtil {
    class Program {
        public static HashSet<string> LoggingTargets { get; set; }
        static readonly Logger logger = LogManager.GetCurrentClassLogger();

        static int Main(string[] args) {
            if (LoggingTargets != null) {
                ConfigureLogger();
            }

            AppDomain.CurrentDomain.AssemblyLoad +=
                (sender, eventArgs) => PimixConfigs.LoadFromSystemConfigs(eventArgs.LoadedAssembly);

            PimixConfigs.LoadFromSystemConfigs();

            return Parser.Default.ParseArguments<
                    InfoCommand,
                    CopyCommand,
                    VerifyCommand,
                    RemoveCommand,
                    MoveCommand,
                    LinkCommand,
                    ListCommand,
                    UploadCommand,
                    AddCommand,
                    GetCommand
                >(args)
                .MapResult<FileUtilCommand, int>(ExecuteCommand, HandleParseFail);
        }

        static void ConfigureLogger() {
            LogManager.Configuration.LoggingRules.Clear();

            foreach (var target in LoggingTargets) {
                var minLevel = target.EndsWith("_full") ? LogLevel.Trace : LogLevel.Debug;
                LogManager.Configuration.AddRule(minLevel, LogLevel.Fatal, target);
            }
            
            LogManager.ReconfigExistingLoggers();
        }

        static int ExecuteCommand(FileUtilCommand command) {
            command.Initialize();

            try {
                return command.Execute();
            } catch (Exception ex) {
                logger.Fatal(ex, "Unexpected error happened");
                return 1;
            }
        }

        static int HandleParseFail(IEnumerable<Error> errors) => 2;
    }
}
