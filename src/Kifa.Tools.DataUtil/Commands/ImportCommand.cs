using System.Collections.Generic;
using System.IO;
using CommandLine;
using Kifa.Api.Files;
using Kifa.Languages.German.Goethe;
using Kifa.Service;
using NLog;
using YamlDotNet.Serialization;

namespace Kifa.Tools.DataUtil.Commands {
    [Verb("import", HelpText = "Import data from a specific file.")]
    public class ImportCommand : KifaCommand {
        static readonly Logger logger = LogManager.GetCurrentClassLogger();

        [Option('t', "type", HelpText = "Type of data. Allowed values: goethe/words, goethe/lists")]
        public string Type { get; set; }

        [Value(0, Required = true, HelpText = "File to import data from.")]
        public string File { get; set; }

        public override int Execute() {
            switch (Type) {
                case GoetheGermanWord.ModelId: {
                    using var reader = new StreamReader(new KifaFile(File).OpenRead());
                    var words = new Deserializer().Deserialize<List<GoetheGermanWord>>(reader.ReadToEnd());

                    var client = new GoetheGermanWordRestServiceClient();
                    foreach (var word in words) {
                        logger.LogResult(client.Update(word), $"Update ({Type}/{word.Id})");
                    }

                    break;
                }
                case GoetheWordList.ModelId: {
                    using var reader = new StreamReader(new KifaFile(File).OpenRead());
                    var lists = new Deserializer().Deserialize<List<GoetheWordList>>(reader.ReadToEnd());

                    var client = new GoetheWordListRestServiceClient();
                    foreach (var list in lists) {
                        logger.LogResult(client.Update(list), $"Update ({Type}/{list.Id})");
                    }

                    break;
                }
            }

            return 0;
        }
    }
}
