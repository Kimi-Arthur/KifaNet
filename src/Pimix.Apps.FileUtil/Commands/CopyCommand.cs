using System;
using CommandLine;
using NLog;
using Kifa.Api.Files;
using Pimix.IO;

namespace Pimix.Apps.FileUtil.Commands {
    [Verb("cp", HelpText = "Copy FILE1 to FILE2. The files will be linked.")]
    class CopyCommand : PimixCommand {
        static readonly Logger logger = LogManager.GetCurrentClassLogger();

        [Value(0, MetaName = "FILE1", MetaValue = "STRING", Required = true,
            HelpText = "File to copy from.")]
        public string Target { get; set; }

        [Value(1, MetaName = "FILE2", MetaValue = "STRING", Required = true,
            HelpText = "File to copy to.")]
        public string LinkName { get; set; }

        [Option('i', "id", HelpText =
            "Treat all file names as id. And only file ids are linked")]
        public bool ById { get; set; } = false;

        public override int Execute() {
            if (ById) {
                return LinkFile(Target.TrimEnd('/'), LinkName.TrimEnd('/'));
            }

            LinkLocalFile(new KifaFile(Target), new KifaFile(LinkName));
            return 0;
        }

        static void LinkLocalFile(KifaFile file1, KifaFile file2) {
            LinkFile(file1.Id, file2.Id);
            file1.Copy(file2);
            file2.Add();
        }

        static int LinkFile(string target, string linkName) {
            if (!target.StartsWith("/") || !linkName.StartsWith("/")) {
                logger.Error("You should use absolute file path for the two arguments.");
                return 1;
            }

            var files = FileInformation.Client.ListFolder(target, true);
            if (files.Count == 0) {
                FileInformation.Client.Link(target, linkName);
                logger.Info($"Successfully linked {linkName} => {target}!");
            } else {
                foreach (var file in files) {
                    var linkFile = linkName + file.Substring(target.Length);
                    FileInformation.Client.Link(file, linkFile);
                    Console.WriteLine($"{linkFile} => {file}");
                }

                Console.Write($"Confirm the {files.Count} linkings above?");
                Console.ReadLine();

                foreach (var file in files) {
                    var linkFile = linkName + file.Substring(target.Length);
                    FileInformation.Client.Link(file, linkFile);
                    logger.Info($"Successfully linked {linkFile} => {file}!");
                }
            }

            return 0;
        }
    }
}
