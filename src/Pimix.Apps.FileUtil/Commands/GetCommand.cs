using System;
using System.Linq;
using CommandLine;
using NLog;
using Pimix.Api.Files;
using Pimix.IO;
using Renci.SshNet;

namespace Pimix.Apps.FileUtil.Commands {
    [Verb("get", HelpText = "Get file.")]
    class GetCommand : FileUtilCommand {
        [Value(0, Required = true, MetaName = "File URL")]
        public string FileUri { get; set; }

        [Option('f', "folder", HelpText = "Upload the whole folder.")]
        public bool IsFolder { get; set; } = false;

        static readonly Logger logger = LogManager.GetCurrentClassLogger();

        public override int Execute() {
            var target = new PimixFile(FileUri);
            if (IsFolder) {
                var files = FileInformation.ListFolder(target.Id, true);
                if (files.Count > 0) {
                    foreach (var file in files) {
                        Console.WriteLine(file);
                    }

                    Console.Write($"Confirm getting the {files.Count} files above?");
                    Console.ReadLine();

                    return files.Select(f => GetFile(new PimixFile(target.Spec + f))).Max();
                }

                Console.Write($"No files found in {FileUri}.");
                return 0;
            }

            return GetFile(target);
        }

        int GetFile(PimixFile target) {
            if (target.Exists()) {
                var targetCheckResult = target.Add();

                if (targetCheckResult == FileProperties.None) {
                    logger.Info("Already got!");
                    return 0;
                }

                logger.Warn("Target exists, but doesn't match.");
                return 2;
            }

            var info = target.FileInfo;

            if (info.Locations == null) {
                logger.Error($"No instance exists for {info.Id}!");
                return 1;
            }

            foreach (var location in info.Locations) {
                var linkSource = new PimixFile(location);
                if (linkSource.Spec == target.Spec) {
                    Link(linkSource, target);
                    FileInformation.AddLocation(target.Id, target.ToString());
                    return 0;
                }
            }

            var source = new PimixFile(FileInformation.GetLocation(info.Id));
            source.Copy(target);

            if (target.Exists()) {
                var destinationCheckResult = target.Add();
                if (destinationCheckResult == FileProperties.None) {
                    logger.Info("Successfully got {1} from {0}!", source, target);
                    return 0;
                }

                logger.Error(
                    "Get failed! The following fields differ (not removed): {0}",
                    destinationCheckResult
                );
                return 2;
            }

            logger.Fatal("Destination doesn't exist unexpectedly!");
            return 2;
        }

        void Link(PimixFile source, PimixFile target) {
            var connectionInfo = new ConnectionInfo(target.Spec.Split(':').Last(),
                "root",
                new PasswordAuthenticationMethod("root", "fakepass"));
            using (var client = new SshClient(connectionInfo)) {
                client.Connect();
                var result =
                    client.RunCommand(
                        $"umask 0000 && mkdir -p \"/nfs/files{string.Join("/", target.Path.Split('/').SkipLast(1))}\"");
                if (result.ExitStatus != 0) throw new Exception("Make parent folders failed.");

                result = client.RunCommand(
                    $"ln \"/nfs/files{source.Path}\" \"/nfs/files{target.Path}\"");
                if (result.ExitStatus != 0) throw new Exception("Link command failed");
            }
        }
    }
}
