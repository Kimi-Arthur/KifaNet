using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CommandLine;
using NLog;
using Kifa.Api.Files;

namespace Kifa.Tools.FileUtil.Commands;

[Verb("normalize", HelpText = "Rename the file with proper normalization.")]
class NormalizeCommand : KifaFileCommand {
    static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    protected override Func<List<KifaFile>, string> KifaFileConfirmText
        => files => $"Confirm normalizing the {files.Count} files above?";

    protected override int ExecuteOneKifaFile(KifaFile file) {
        var path = file.ToString();
        var segments = path.Split(".");
        if (path.IsNormalized(NormalizationForm.FormC) && segments[^1].ToLower() == segments[^1]) {
            Logger.Info($"{path} is already normalized.");
            return 0;
        }

        segments[^1] = segments[^1].ToLower();

        var newPath = string.Join(".", segments.Select(s => s.Normalize(NormalizationForm.FormC)));
        file.Move(new KifaFile(newPath));
        Logger.Info($"Successfully normalized {path} to {newPath}.");
        return 0;
    }
}
