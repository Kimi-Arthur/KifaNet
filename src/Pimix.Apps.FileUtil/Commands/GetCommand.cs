using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using CommandLine;
using NLog;
using Pimix.Api.Files;
using Pimix.IO;

namespace Pimix.Apps.FileUtil.Commands {
    [Verb("get", HelpText = "Get file.")]
    class GetCommand : FileUtilCommand {
        [Value(0, Required = true, MetaName = "File URL")]
        public string FileUri { get; set; }

        [Option('g', "use-google-drive", HelpText = "Use Google Drive as backend storage.")]
        public bool UseGoogleDrive { get; set; } = false;

        [Option('p', "is-primary", HelpText = "Newly created file is primary on this device.")]
        public bool IsPrimary { get; set; } = false;

        static readonly Logger logger = LogManager.GetCurrentClassLogger();

        public override int Execute() {
            var target = new PimixFile(FileUri);

            var files = FileInformation.ListFolder(target.Id, true);
            if (files.Count > 0) {
                foreach (var file in files) {
                    Console.WriteLine(file);
                }

                Console.Write($"Confirm getting the {files.Count} files above?");
                Console.ReadLine();

                return files.Select(f => GetFile(new PimixFile(target.Host + f))).Max();
            }

            return GetFile(target);
        }

        int GetFile(PimixFile target) {
            if (target.Exists()) {
                if (target.CalculateInfo(FileProperties.Size).Size != target.FileInfo.Size) {
                    logger.Info("Target exists but size is incorrect. Assuming incomplete Get result.");
                } else {
                    var targetCheckResult = target.Add();

                    if (targetCheckResult == FileProperties.None) {
                        logger.Info("Already got!");
                        return 0;
                    }

                    logger.Warn("Target exists, but doesn't match.");
                    return 2;
                }
            }

            var info = target.FileInfo;

            if (info.Locations == null) {
                logger.Error($"No instance exists for {info.Id}!");
                return 1;
            }

            foreach (var location in info.Locations) {
                if (location.Value != null) {
                    var linkSource = new PimixFile(location.Key);
                    if (linkSource.IsComaptible(target)) {
                        Link(linkSource, target);
                        FileInformation.AddLocation(target.Id, target.ToString());
                        return 0;
                    }
                }
            }

            var source = new PimixFile(FileInformation.GetLocation(info.Id,
                UseGoogleDrive ? new List<string> {"google", "baidu"} : new List<string> {"baidu", "google"}));
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

        void Link(PimixFile source, PimixFile link) {
            var relativePath = GetRelativePath(link.Path, source.Path);
            var linkPath = link.GetLocalPath();
            Directory.GetParent(linkPath).Create();

            logger.Debug("Linking {0} to point to relative path {1}", linkPath, relativePath);
            CreateSymbolicLink(linkPath, relativePath);
        }

        static void CreateSymbolicLink(string linkPath, string relativePath) {
            using (var proc = new Process()) {
                proc.StartInfo.FileName = "/bin/ln";
                proc.StartInfo.Arguments = $"-s \"{relativePath}\" \"{linkPath}\"";

                proc.StartInfo.RedirectStandardError = true;
                proc.StartInfo.RedirectStandardOutput = true;
                proc.StartInfo.UseShellExecute = false;

                proc.OutputDataReceived += (sender, e) => {
                    if (!string.IsNullOrEmpty(e.Data)) {
                        logger.Debug(e.Data);
                    }
                };

                proc.ErrorDataReceived += (sender, e) => {
                    if (!string.IsNullOrEmpty(e.Data)) {
                        logger.Debug(e.Data);
                    }
                };

                proc.Start();

                proc.BeginOutputReadLine();
                proc.BeginErrorReadLine();

                proc.WaitForExit();

                if (proc.ExitCode != 0) {
                    logger.Fatal($"Error when linking: {proc.ExitCode}");
                    throw new Exception("Linking Error");
                }
            }
        }

        static string GetRelativePath(string basePath, string valuePath) {
            var baseSegments = basePath.Split('/');
            var valueSegments = valuePath.Split('/');
            var commonPrefix = baseSegments.Zip(valueSegments, (b, v) => b == v)
                .Select((value, index) => (value, index)).First(x => !x.Item1).Item2;
            return string.Join("/",
                Enumerable.Repeat("..", baseSegments.Length - commonPrefix - 1)
                    .Concat(valueSegments.Skip(commonPrefix)));
        }
    }
}
