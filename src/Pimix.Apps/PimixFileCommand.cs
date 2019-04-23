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

        [Option('i', "id", HelpText = "Treat input files as logical ids.")]
        public virtual bool ById { get; set; } = false;

        [Option('r', "recursive", HelpText = "Take action on files in recursive folders.")]
        public virtual bool Recursive { get; set; } = false;

        protected virtual Func<List<string>, string> ConfirmText => null;

        protected virtual Func<List<PimixFile>, string> InstanceConfirmText => null;

        /// <summary>
        /// Iterate over files with this prefix. If it's not, prefix the path with this member.
        /// </summary>
        protected virtual string Prefix => null;

        /// <summary>
        /// By default, it will only iterate over existing files. When it's set to true, it will iterate over logical
        /// ones and produce ExecuteOneInstance calls with the two combined.
        /// </summary>
        protected virtual bool IterateOverLogicalFiles => false;

        public override int Execute() {
            var multi = FileNames.Count() > 1;
            if (ById || IterateOverLogicalFiles) {
                var fileInfos = new List<string>();
                foreach (var fileName in FileNames) {
                    var host = "";
                    var path = fileName;
                    if (IterateOverLogicalFiles) {
                        var f = new PimixFile(path);
                        host = f.Host;
                        path = f.Id;
                    }

                    path = Prefix == null ? path : $"{Prefix}{path}";
                    var thisFolder = FileInformation.ListFolder(path, Recursive);
                    if (thisFolder.Count > 0) {
                        multi = true;
                        fileInfos.AddRange(thisFolder.Select(f => host + f));
                    } else {
                        fileInfos.Add(host + path);
                    }
                }

                fileInfos.Sort();
                if (multi && (ConfirmText != null || IterateOverLogicalFiles && InstanceConfirmText != null)) {
                    fileInfos.ForEach(Console.WriteLine);

                    Console.Write(IterateOverLogicalFiles
                        ? InstanceConfirmText(fileInfos.Select(f => new PimixFile(f)).ToList())
                        : ConfirmText(fileInfos));

                    Console.ReadLine();
                }

                var errors = new Dictionary<string, Exception>();
                var result = fileInfos.Select(s => {
                    try {
                        return IterateOverLogicalFiles
                            ? ExecuteOneInstance(new PimixFile(s))
                            : ExecuteOne(s);
                    } catch (Exception ex) {
                        errors[s] = ex;
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
