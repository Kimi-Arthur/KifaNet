using System;
using System.Collections.Generic;
using System.Linq;
using CommandLine;
using Kifa.Api.Files;
using Kifa.Jobs;
using Kifa.Service;

namespace Kifa.Tools.FileUtil.Commands;

[Verb("retrieve", HelpText = "Retrieve remote files to local mirror storage.")]
public class RetrieveCommand : KifaCommand {
    [Value(0, Required = true, HelpText = "Target file(s) to retrieve.")]
    public IEnumerable<string> FileNames { get; set; }

    [Option('m', "mirror-host", HelpText = "Mirror remote files to the given host.")]
    public string? MirrorHost { get; set; }

    [Option('R', "no-register", HelpText = "Skip registering files in system.")]
    public bool NoRegister { get; set; } = false;

    [Option('a', "include-all", HelpText = "Include all files already registered or ignored.")]
    public bool IncludeAll { get; set; } = false;

    [Option('S', "show-size", HelpText = "Show size for each file and total size (can be slow).")]
    public bool ShowSize { get; set; } = false;

    public override int Execute(KifaTask? task = null) {
        var files = KifaFile.FindExistingFiles(FileNames, shouldIgnoreFiles: !IncludeAll);
        var selected = SelectMany(files,
            file => ShowSize ? $"{file} ({file.Length.ToSizeString()})" : file.ToString(),
            new Func<List<KifaFile>, string>(choices
                => $"files{(ShowSize ? $" ({choices.Sum(c => c.Length).ToSizeString()})" : "")} to retrieve"));

        if (selected.Status != KifaActionStatus.OK) {
            ExecuteItem("files to retrieve", () => selected);
            return LogSummary();
        }

        foreach (var file in selected.Value.Checked()) {
            ExecuteItem(file.ToString(), () => RetrieveFile(file));
            file.Dispose();
        }

        return LogSummary();
    }

    public KifaActionResult RetrieveFile(KifaFile file) {
        if (MirrorHost != null) {
            file.MirrorHost = MirrorHost;
        }

        return file.Retrieve(noRegister: NoRegister,
            confirmPrompt: (prompt, suggested) => Confirm(prompt, suggested));
    }
}
