using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using CommandLine;
using NLog;
using Pimix.Api.Files;
using Pimix.IO;

namespace Pimix.Apps {
    public abstract partial class PimixFileCommand : PimixCommand {
        static readonly Logger logger = LogManager.GetCurrentClassLogger();

        static readonly Regex numberPattern = new Regex(@"\d+");

        [Value(0, Required = true, HelpText = "Target file(s) to take action on.")]
        public IEnumerable<string> FileNames { get; set; }

        [Option('i', "id", HelpText = "Treat input files as logical ids.")]
        public virtual bool ById { get; set; } = false;

        [Option('r', "recursive", HelpText = "Take action on files in recursive folders.")]
        public virtual bool Recursive { get; set; } = false;

        /// <summary>
        ///     Iterate over files with this prefix. If it's not, prefix the path with this member.
        /// </summary>
        protected virtual string Prefix => null;

        /// <summary>
        ///     By default, it will only iterate over existing files. When it's set to true, it will iterate over
        ///     logical ones and produce ExecuteOneInstance calls with the two combined.
        /// </summary>
        protected virtual bool IterateOverLogicalFiles => false;

        protected virtual bool NaturalSorting => false;

        public override int Execute() {
            var multi = FileNames.Count() > 1;
            if (ById || IterateOverLogicalFiles) {
                var fileInfos = new List<(string sortKey, string value)>();
                foreach (var fileName in FileNames) {
                    var host = "";
                    var path = fileName;
                    if (IterateOverLogicalFiles) {
                        var f = new PimixFile(path);
                        if (!ById) {
                            host = f.Host;
                        }

                        path = f.Id;
                    }

                    path = Prefix == null ? path : $"{Prefix}{path}";
                    var thisFolder = FileInformation.Client.ListFolder(path, Recursive);
                    if (thisFolder.Count > 0) {
                        multi = true;
                        fileInfos.AddRange(thisFolder.Select(f => (GetSortKey(f), host + f)));
                    } else {
                        fileInfos.Add((GetSortKey(path), host + path));
                    }
                }

                fileInfos.Sort();

                var files = fileInfos.Select(f => f.value).ToList();
                return ById
                    ? ExecuteAllFileInformation(files, multi)
                    : ExecuteAllPimixFiles(files.Select(f => new PimixFile(f)).ToList(), multi);
            } else {
                var files = new List<(string sortKey, PimixFile value)>();
                foreach (var fileName in FileNames) {
                    var fileInfo = new PimixFile(fileName);
                    if (Prefix != null && !fileInfo.Path.StartsWith(Prefix)) {
                        fileInfo = fileInfo.GetFilePrefixed(Prefix);
                    }

                    var thisFolder = fileInfo.List(Recursive).ToList();
                    if (thisFolder.Count > 0) {
                        multi = true;
                        files.AddRange(thisFolder.Select(f => (GetSortKey(f.ToString()), f)));
                    } else {
                        files.Add((GetSortKey(fileInfo.ToString()), fileInfo));
                    }
                }

                files.Sort();

                return ExecuteAllPimixFiles(files.Select(f => f.value).ToList(), multi);
            }
        }

        string GetSortKey(string path)
            => NaturalSorting ? numberPattern.Replace(path, m => $"{int.Parse(m.Value):D5}") : path;
    }

    public abstract partial class PimixFileCommand {
        protected virtual Func<List<string>, string> FileInformationConfirmText => null;
        protected virtual int ExecuteOneFileInformation(string file) => -1;

        int ExecuteAllFileInformation(List<string> files, bool multi) {
            if (multi && FileInformationConfirmText != null) {
                files.ForEach(Console.WriteLine);
                Console.Write(FileInformationConfirmText(files));
                Console.ReadLine();
            }

            var errors = new Dictionary<string, Exception>();
            var result = files.Select(s => {
                try {
                    return ExecuteOneFileInformation(s);
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
        }
    }

    public abstract partial class PimixFileCommand {
        protected virtual Func<List<PimixFile>, string> PimixFileConfirmText => null;
        protected virtual int ExecuteOnePimixFile(PimixFile file) => -1;

        int ExecuteAllPimixFiles(List<PimixFile> files, bool multi) {
            if (multi && PimixFileConfirmText != null) {
                files.ForEach(Console.WriteLine);
                Console.Write(PimixFileConfirmText(files));
                Console.ReadLine();
            }

            var errors = new Dictionary<string, Exception>();
            var result = files.Select(s => {
                try {
                    return ExecuteOnePimixFile(s);
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
}
