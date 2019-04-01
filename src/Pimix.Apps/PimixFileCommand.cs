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
            foreach (var fileName in FileNames) {
                if (ById) {
                    var fileInfos = FileInformation.ListFolder(fileName, Recursive);
                    fileInfos.Sort();
                    if (fileInfos.Count > 0) {
                        if (ConfirmText != null) {
                            fileInfos.ForEach(Console.WriteLine);

                            Console.Write(ConfirmText(fileInfos));
                            Console.ReadLine();
                        }

                        return fileInfos.Select(ExecuteOne).Max();
                    }

                    return ExecuteOne(fileName);
                }

                var fileInfo = new PimixFile(fileName);

                var files = fileInfo.List(Recursive).ToList();
                files.Sort();
                if (files.Count > 0) {
                    if (InstanceConfirmText != null) {
                        files.ForEach(Console.WriteLine);

                        Console.Write(InstanceConfirmText(files));
                        Console.ReadLine();
                    }

                    return files.Select(ExecuteOneInstance).Max();
                }

                return ExecuteOneInstance(fileInfo);
            }

            return 0;
        }

        public virtual int ExecuteOne(string file) {
            return -1;
        }

        public virtual int ExecuteOneInstance(PimixFile file) {
            return -1;
        }
    }
}
