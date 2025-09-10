using System;
using System.Collections.Generic;
using CommandLine;
using Kifa.Jobs;

namespace Kifa.Tools.FileUtil.Commands;

[Verb("ls", HelpText = "List files and folders in the FOLDER.")]
class ListCommand : KifaCommand {
    [Option('i', "id", HelpText = "Treat input files as logical ids.")]
    public bool ById { get; set; } = false;

    [Option('l', "long", HelpText = "Long list mode")]
    public bool LongListMode { get; set; } = false;

    [Value(0, Required = true, HelpText = "Target files to list.")]
    public IEnumerable<string> FileNames { get; set; }

    public override int Execute(KifaTask? task = null) {
        if (ById) {
            ListLogicalFiles();
        } else {
            ListFileInstances();
        }

        return 0;
    }

    void ListFileInstances() {
        throw new NotImplementedException();
    }

    void ListLogicalFiles() {
        // Console.WriteLine(LongListMode
        //     ? $"{file}\t{FileInformation.Client.Get(file).Size}\t{FileInformation.Client.Get(file).Sha256}"
        //     : file);
        // return 0;
    }
    //
    // protected override int ExecuteOneKifaFile(KifaFile file) {
    //     counter++;
    //     Console.WriteLine(LongListMode ? $"{file}\t{file.FileInfo.Size}" : file.ToString());
    //     return 0;
    // }
}
