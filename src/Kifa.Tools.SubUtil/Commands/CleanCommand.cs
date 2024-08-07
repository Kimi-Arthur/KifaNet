using System;
using System.Collections.Generic;
using System.IO;
using CommandLine;
using Kifa.Api.Files;

namespace Kifa.Tools.SubUtil.Commands;

[Verb("clean", HelpText = "Clean subtitle file.")]
class CleanCommand : KifaFileCommand {
    protected override string Prefix => "/Subtitles";

    protected override Func<List<KifaFile>, string> KifaFileConfirmText
        => files => $"Confirm cleaning comments for the {files.Count} files above?";

    protected override int ExecuteOneKifaFile(KifaFile file) {
        var lines = new List<string>();
        using (var sr = new StreamReader(file.OpenRead())) {
            string line;
            while ((line = sr.ReadLine()) != null) {
                lines.Add(line);
            }
        }

        file.Delete();
        file.Write(string.Join("\n", lines) + "\n");
        return 0;
    }
}
