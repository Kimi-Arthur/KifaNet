using System;
using System.Collections.Generic;
using System.Text;
using CommandLine;
using NLog;
using Kifa.Api.Files;

namespace Pimix.Apps.FileUtil.Commands {
    [Verb("normalize", HelpText = "Rename the file with proper normalization.")]
    class NormalizeCommand : PimixFileCommand {
        static readonly Logger logger = LogManager.GetCurrentClassLogger();

        protected override Func<List<KifaFile>, string> PimixFileConfirmText
            => files => $"Confirm normalizing the {files.Count} files above?";

        protected override int ExecuteOnePimixFile(KifaFile file) {
            var path = file.ToString();
            if (path.IsNormalized(NormalizationForm.FormC)) {
                logger.Info($"{path} is already normalized.");
                return 0;
            }

            var newPath = path.Normalize(NormalizationForm.FormC);
            file.Move(new KifaFile(newPath));
            logger.Info($"Successfully normalized {path} to {newPath}.");
            return 0;
        }
    }
}
