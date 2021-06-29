using CommandLine;
using Kifa.Api.Files;
using Kifa.Languages.German.Goethe;
using Kifa.Service;
using NLog;

namespace Kifa.Tools.DataUtil.Commands {
    [Verb("export", HelpText = "Export data to a specific file.")]
    public class ExportCommand : KifaCommand {
        static readonly Logger logger = LogManager.GetCurrentClassLogger();

        [Option('t', "type", HelpText = "Type of data. Allowed values: goethe/words, goethe/lists")]
        public string Type { get; set; }

        [Value(0, Required = true, HelpText = "File to export data from.")]
        public string File { get; set; }

        public override int Execute() {
            switch (Type) {
                case GoetheGermanWord.ModelId:
                    return (int) logger.LogResult(
                        new DataChef<GoetheGermanWord, GoetheGermanWordRestServiceClient>().Export(new KifaFile(File)),
                        "Summary").Status;
                case GoetheWordList.ModelId:
                    return (int) logger.LogResult(
                        new DataChef<GoetheWordList, GoetheWordListRestServiceClient>().Export(new KifaFile(File)),
                        "Summary").Status;
                default:
                    logger.Error($"Unknown type name: {Type}.");
                    return 1;
            }
        }
    }
}
