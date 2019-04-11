using System;
using System.Collections.Generic;
using System.Linq;
using CommandLine;
using NLog;
using Pimix.Api.Files;
using Pimix.IO;

namespace Pimix.Apps {
    public abstract class PimixFileCommand : PimixCommand {
        static readonly Logger logger = LogManager.GetCurrentClassLogger();

        [Value(0, Required = true, HelpText = "Target file(s) to take action on.")]
        public IEnumerable<string> FileNames { get; set; }

        [Option('i', "id", HelpText = "Treat files as logical ids.")]
        public bool ById { get; set; } = false;

        [Option('r', "recursive", HelpText = "Take action on files in recursive folders.")]
        public bool Recursive { get; set; } = false;

        public virtual Func<List<string>, string> ConfirmText => null;

        public virtual Func<List<PimixFile>, string> InstanceConfirmText => null;

        public virtual string Prefix => null;

        public override int Execute() {
            var multi = FileNames.Count() > 1;
            if (ById) {
                var fileIds = new List<string>();
                foreach (var fileName in FileNames) {
                    var path = Prefix == null ? fileName : $"{Prefix}{fileName}";
                    var thisFolder = FileInformation.ListFolder(path, Recursive);
                    if (thisFolder.Count > 0) {
                        multi = true;
                        fileIds.AddRange(thisFolder);
                    } else {
                        fileIds.Add(path);
                    }
                }

                fileIds.Sort();
                if (multi && ConfirmText != null) {
                    fileIds.ForEach(Console.WriteLine);

                    Console.Write(ConfirmText(fileIds));
                    Console.ReadLine();
                }

                var errors = new Dictionary<string, Exception>();
                var result = fileIds.Select(s => {
                    try {
                        return ExecuteOne(s);
                    } catch (Exception ex) {
                        errors[s] = ex;
                        return 255;
                    }
                }).Max();

                foreach (var (key, value) in errors) {
                    logger.Error($"{key}: {value}");
                }

                logger.Error($"{errors.Count} files failed to be taken action on.");

                return result;
            } else {
                var files = new List<PimixFile>();
                foreach (var fileName in FileNames) {
                    var fileInfo = new PimixFile(fileName);
                    if (Prefix != null && !fileInfo.Path.StartsWith(Prefix)) {
                        fileInfo = fileInfo.GetFilePrefixed(Prefix);
                    }

                    var thisFolder = fileInfo.List(Recursive).ToList();
                    if (thisFolder.Count > 0) {
                        multi = true;
                        files.AddRange(thisFolder);
                    } else {
                        files.Add(fileInfo);
                    }
                }

                files.Sort();

                if (multi && InstanceConfirmText != null) {
                    files.ForEach(Console.WriteLine);

                    Console.Write(InstanceConfirmText(files));
                    Console.ReadLine();
                }

                var errors = new Dictionary<string, Exception>();
                var result = files.Select(s => {
                    try {
                        return ExecuteOneInstance(s);
                    } catch (Exception ex) {
                        errors[s.ToString()] = ex;
                        return 255;
                    }
                }).Max();

                foreach (var (key, value) in errors) {
                    logger.Error($"{key}: {value}");
                }

                if (errors.Count > 0) {
                    logger.Error($"{errors.Count} files failed to be taken action on.");
                }

                return result;
            }
        }

        protected virtual int ExecuteOne(string file) {
            return -1;
        }

        protected virtual int ExecuteOneInstance(PimixFile file) {
            return -1;
        }
    }
}
