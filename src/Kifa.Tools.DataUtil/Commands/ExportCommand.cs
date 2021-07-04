using CommandLine;
using Kifa.Api.Files;
using Kifa.Service;
using NLog;

namespace Kifa.Tools.DataUtil.Commands {
    [Verb("export", HelpText = "Export data to a specific file.")]
    public class ExportCommand : KifaCommand {
        static readonly Logger logger = LogManager.GetCurrentClassLogger();

        [Option('t', "type", HelpText = "Type of data. Allowed values: goethe/words, goethe/lists")]
        public string Type { get; set; }

        [Option('a', "get-all", HelpText = "Whether to get all items that don't even appear in the file.")]
        public bool GetAll { get; set; }

        [Value(0, Required = true, HelpText = "File to export data from.")]
        public string File { get; set; }

        public override int Execute() {
            var chef = DataChef.GetChef(Type);

            if (chef == null) {
                logger.Error($"Unknown type name: {Type}.");
                return 1;
            }

            return (int) logger.LogResult(chef.Export(new KifaFile(File), GetAll), "Summary").Status;
        }
    }
}
