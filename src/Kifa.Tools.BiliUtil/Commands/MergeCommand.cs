using System.Collections.Generic;
using System.Linq;
using CommandLine;
using NLog;
using Kifa.Api.Files;

namespace Kifa.Tools.BiliUtil.Commands;

[Verb("merge", HelpText = "Merge flv to mp4.")]
public class MergeCommand : KifaCommand {
    static readonly Logger logger = LogManager.GetCurrentClassLogger();

    [Value(0, Required = true, HelpText = "Target file(s) to upload.")]
    public IEnumerable<string> FileNames { get; set; }

    [Option('o', "output", HelpText = "Output file path.")]
    public string OutputFile { get; set; }

    public override int Execute() {
        var (_, files) = KifaFile.ExpandFiles(FileNames);
        var targetFileName = Confirm($"Confirming merging files {string.Join(", ", files)} to ",
            GetTargetFileName(files));
        Helper.MergePartFiles(files, new KifaFile(targetFileName));
        logger.Info($"Successfully merged files {string.Join(", ", files)} to {targetFileName}!");
        return 0;
    }

    string GetTargetFileName(IEnumerable<KifaFile> files)
        => string.IsNullOrEmpty(OutputFile)
            ? string.Join(".", files.First().ToString().Split(".")[..^1]) + ".mp4"
            : new KifaFile(OutputFile).ToString();
}
