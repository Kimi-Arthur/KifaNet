using System.Collections.Generic;
using System.IO;
using CommandLine;
using Kifa.Api.Files;
using Kifa.Jobs;
using Kifa.Service;

namespace Kifa.Tools.FileUtil.Commands;

[Verb("truncate",
    HelpText = "Special command to truncate the last byte of the files in case it ends with \\0.")]
class TruncateCommand : KifaCommand {
    [Value(0, Required = true, HelpText = "Target file(s) to upload.")]
    public IEnumerable<string> FileNames { get; set; } = [];

    public override int Execute(KifaTask? task = null) {
        var selected = SelectMany(KifaFile.FindExistingFiles(FileNames),
            choicesName: "subtitle files to fix");
        foreach (var file in selected) {
            ExecuteItem(file.ToString(), () => TruncateFile(file));
        }

        return LogSummary();
    }

    static KifaActionResult TruncateFile(KifaFile file) {
        var truncatedFile = file.GetTruncatedFile();
        file.Copy(truncatedFile, neverLink: true);
        using (var s = File.Open(truncatedFile.GetLocalPath(), FileMode.Open)) {
            s.Seek(-1, SeekOrigin.End);
            if (s.ReadByte() != 0) {
                return new KifaActionResult {
                    Status = KifaActionStatus.BadRequest,
                    Message = $"Last byte of {file} is not \\0"
                };
            }

            s.SetLength(s.Length - 1);
        }

        truncatedFile.Add();
        return new KifaActionResult {
            Status = KifaActionStatus.OK,
            Message = $"Truncated the last byte of {file} as a new file in {truncatedFile}"
        };
    }
}
