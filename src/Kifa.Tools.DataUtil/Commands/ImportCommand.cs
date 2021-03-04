using System;
using System.Collections.Generic;
using System.IO;
using YamlDotNet.Serialization;
using CommandLine;
using Kifa.Api.Files;
using Kifa.Memrise;
using Kifa.Service;
using NLog;

namespace Kifa.Tools.DataUtil.Commands {
    [Verb("import", HelpText = "Import data from a specific file.")]
    public class ImportCommand : KifaCommand {
        static readonly Logger logger = LogManager.GetCurrentClassLogger();

        [Option('t', "type", HelpText = "Type of data. Allowed values: goethe/words")]
        public string Type { get; set; }

        [Value(0, Required = true, HelpText = "File to import data from.")]
        public string File { get; set; }

        public override int Execute() {
            if (GoetheGermanWord.ModelId == Type) {
                using var reader = new StreamReader(new KifaFile(File).OpenRead());
                var words = new Deserializer().Deserialize<List<GoetheGermanWord>>(reader.ReadToEnd());

                var client = new MemriseGermanWordRestServiceClient();
                foreach (var word in words) {
                    logger.LogResult(client.Update(word), $"Update ({word.Id})");
                }
            }

            return 0;
        }
    }
}
