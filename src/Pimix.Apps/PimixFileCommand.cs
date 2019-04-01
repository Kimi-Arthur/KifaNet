using System;
using System.Collections.Generic;
using System.Linq;
using CommandLine;
using Pimix.Api.Files;
using Pimix.IO;

namespace Pimix.Apps {
    public abstract class PimixFileCommand : PimixCommand {
        [Value(0, Required = true, HelpText = "Target file(s) to take action on.")]
        public IEnumerable<string> FileNames { get; set; }

        [Option('i', "id", HelpText = "Treat files as logical ids.")]
        public bool ById { get; set; } = false;

        [Option('r', "recursive", HelpText = "Take action on files in recursive folders.")]
        public bool Recursive { get; set; } = false;

        public virtual Func<List<string>, string> ConfirmText => null;

        public virtual Func<List<PimixFile>, string> InstanceConfirmText => null;

        public override int Execute() {
            var multi = FileNames.Count() > 1;
            if (ById) {
                var fileIds = new List<string>();
                foreach (var fileName in FileNames) {
                    var thisFolder = FileInformation.ListFolder(fileName, Recursive);
                    if (thisFolder.Count > 0) {
                        multi = true;
                        fileIds.AddRange(thisFolder);
                    } else {
                        fileIds.Add(fileName);
                    }
                }

                fileIds.Sort();
                if (multi && ConfirmText != null) {
                    fileIds.ForEach(Console.WriteLine);

                    Console.Write(ConfirmText(fileIds));
                    Console.ReadLine();
                }

                return fileIds.Select(ExecuteOne).Max();
            }

            var files = new List<PimixFile>();
            foreach (var fileName in FileNames) {
                var fileInfo = new PimixFile(fileName);

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

            return files.Select(ExecuteOneInstance).Max();
        }

        protected virtual int ExecuteOne(string file) {
            return -1;
        }

        protected virtual int ExecuteOneInstance(PimixFile file) {
            return -1;
        }
    }
}
