using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CommandLine;
using NLog;
using Pimix.Api.Files;
using Pimix.IO;

namespace Pimix.Apps.FileUtil.Commands {
    [Verb("clean", HelpText = "Rename the file with proper normalization.")]
    class CleanCommand : PimixFileCommand {
        static readonly Logger logger = LogManager.GetCurrentClassLogger();

        public override Func<List<PimixFile>, string> InstanceConfirmText
            => files => $"Confirm fixing the {files.Count} files above?";

        protected override int ExecuteOneInstance(PimixFile file) {
            var path = file.ToString();
            if (path.IsNormalized(NormalizationForm.FormC)) {
                logger.Info($"{path} is already normalized.");
                return 0;
            }

            file.Move(new PimixFile(path.Normalize(NormalizationForm.FormC)));
            logger.Info($"Successfully normalized {path}.");
            return 0;
        }
    }
}
