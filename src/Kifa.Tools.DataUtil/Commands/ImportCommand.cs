using CommandLine;
using Kifa.Api.Files;
using Kifa.Languages.German.Goethe;
using Kifa.Service;
using NLog;

namespace Kifa.Tools.DataUtil.Commands {
    [Verb("import", HelpText = "Import data from a specific file.")]
    public class ImportCommand : KifaCommand {
        static readonly Logger logger = LogManager.GetCurrentClassLogger();

        [Option('t', "type", HelpText = "Type of data. Allowed values: goethe/words, goethe/lists")]
        public string Type { get; set; }

        [Value(0, Required = true, HelpText = "File to import data from.")]
        public string File { get; set; }

        public override int Execute() {
            var chef = DataChef.GetChef(Type);

            if (chef == null) {
                logger.Error($"Unknown type name: {Type}.");
                return 1;
            }

            return (int) logger.LogResult(chef.Import(new KifaFile(File).ReadAsString()), "Summary").Status;
        }
    }
}
