using System.Collections.Generic;
using System.IO;
using CommandLine;
using Kifa.Api.Files;
using Kifa.Jobs;

namespace Kifa.Tools.SubUtil.Commands;

[Verb("clean", HelpText = "Clean subtitle file. Currently it cleans up new line symbols.")]
class CleanCommand : KifaCommand {
    [Value(0, Required = true, HelpText = "Target subtitle files to clean up.")]
    public IEnumerable<string> FileNames { get; set; }

    public override int Execute(KifaTask? task = null) {
        var selected = SelectMany(KifaFile.FindExistingFiles(FileNames),
            choicesName: "subtitle files to clean");
        foreach (var file in selected) {
            ExecuteItem(file.ToString(), () => CleanUpSubtitle(file));
        }

        return LogSummary();
    }

    static void CleanUpSubtitle(KifaFile file) {
        var lines = new List<string>();
        using (var sr = new StreamReader(file.OpenRead())) {
            string? line;
            while ((line = sr.ReadLine()) != null) {
                lines.Add(line);
            }
        }

        file.Delete();
        file.Write(string.Join("\n", lines) + "\n");
    }
}
