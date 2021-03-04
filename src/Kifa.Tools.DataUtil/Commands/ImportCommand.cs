using System;
using System.Collections.Generic;
using System.IO;
using YamlDotNet.Serialization;
using CommandLine;
using Kifa.Api.Files;
using Kifa.Memrise;

namespace Kifa.Tools.DataUtil.Commands {
    [Verb("import", HelpText = "Refresh Data for an entity. Currently tv_shows and animes are supported.")]
    public class ImportCommand : KifaCommand {
        [Option('t', "type", HelpText = "Type of data. Allowed values: goethe/words")]
        public string Type { get; set; }

        [Value(0, Required = true, HelpText = "File to import data from.")]
        public string File { get; set; }

        public override int Execute() {
            if (GoetheGermanWord.ModelId == Type) {
                using var reader = new StreamReader(new KifaFile(File).OpenRead());
                var words = new Deserializer().Deserialize<List<GoetheGermanWord>>(reader.ReadToEnd());
                Console.WriteLine(words.Count);
            }

            return 0;
        }
    }
}
