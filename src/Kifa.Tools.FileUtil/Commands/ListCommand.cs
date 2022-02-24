using System;
using CommandLine;
using Kifa.Api.Files;

namespace Kifa.Tools.FileUtil.Commands;

[Verb("ls", HelpText = "List files and folders in the FOLDER.")]
class ListCommand : KifaFileCommand {
    int counter;

    [Option('l', "long", HelpText = "Long list mode")]
    public bool LongListMode { get; set; } = false;

    public override int Execute() {
        var result = base.Execute();
        Console.WriteLine($"\nIn total, {counter} files in {string.Join(", ", FileNames)}");
        return result;
    }

    protected override int ExecuteOneFileInformation(string file) {
        counter++;
        Console.WriteLine(file);
        return 0;
    }

    protected override int ExecuteOneKifaFile(KifaFile file) {
        counter++;
        Console.WriteLine(LongListMode ? $"{file}\t{file.FileInfo.Size}" : file.ToString());
        return 0;
    }
}
