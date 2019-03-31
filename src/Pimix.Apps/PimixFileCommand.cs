using System;
using System.Collections.Generic;
using System.Linq;
using CommandLine;
using Pimix.Api.Files;
using Pimix.IO;

namespace Pimix.Apps {
    public abstract class PimixFileCommand : PimixCommand {
        [Value(0, Required = true, HelpText = "Target file(s) to take action on.")]
        public IEnumerable<string> Files { get; set; }

        [Option('i', "id", HelpText = "Treat files as logical ids.")]
        public bool ById { get; set; } = false;

        public override int Execute() {
            foreach (var file in Files) {
                if (ById) {
                    var files = FileInformation.ListFolder(file, true);
                    files.ForEach(Console.WriteLine);

                    Console.Write($"Confirm adding the {files.Count} files above?");
                    Console.ReadLine();

                    return files.Select(ExecuteOne).Max();
                } else {
                    var source = new PimixFile(file);

                    var files = source.List(true).ToList();
                    if (files.Count > 0) {
                        files.ForEach(Console.WriteLine);

                        Console.Write($"Confirm adding the {files.Count} files above?");
                        Console.ReadLine();

                        return files.Select(f => ExecuteOneInstance(new PimixFile(f.ToString())))
                            .Max();
                    }
                }
            }

            return 0;
        }

        public virtual int ExecuteOneInstance(PimixFile file) {
            return -1;
        }

        public virtual int ExecuteOne(string file) {
            return -1;
        }
    }
}
