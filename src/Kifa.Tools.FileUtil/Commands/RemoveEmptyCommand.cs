using System.Collections.Generic;
using System.IO;
using System.Linq;
using CommandLine;
using NLog;
using Kifa.Api.Files;

namespace Kifa.Tools.FileUtil.Commands {
    [Verb("rm-empty", HelpText = "Remove empty folders recursively.")]
    public class RemoveEmptyCommand : PimixCommand {
        static readonly Logger logger = LogManager.GetCurrentClassLogger();

        [Value(0, Required = true, HelpText = "Folders to be removed.")]
        public IEnumerable<string> FileNames { get; set; }

        public override int Execute() {
            foreach (var fileName in FileNames) {
                RecursivelyRemoveEmptyFolders(fileName);
            }

            return 0;
        }

        private static bool RecursivelyRemoveEmptyFolders(string fileName) {
            if (!Directory.Exists(fileName)) {
                return File.Exists(fileName);
            }

            if (Directory.EnumerateDirectories(fileName).Select(RecursivelyRemoveEmptyFolders).ToList().Any()) {
                return true;
            }

            if (Directory.EnumerateFiles(fileName).Any()) {
                return true;
            }

            logger.Info($"Removing empty folder {fileName}...");
            Directory.Delete(fileName);
            logger.Info($"Removed empty folder {fileName}...");
            return false;
        }
    }
}