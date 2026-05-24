using System.Collections.Generic;
using CommandLine;
using Kifa.Jobs;

namespace Kifa.Tools.BookUtil.Commands; 

[Verb("words", HelpText = "Exports words from Kindle vocab.db files.")]
public class ExportKindleWordsCommand : KifaCommand {
    [Option('o', "output", Required = true, HelpText = "Output file name.")]
    public string OutputFile {
        get => Late.Get(field);
        set => Late.Set(ref field, value);
    }

    [Value(0, Required = true, HelpText = "vocab.db files to import words from.")]
    public IEnumerable<string> FileNames {
        get => Late.Get(field);
        set => Late.Set(ref field, value);
    }

    public override int Execute(KifaTask? task = null) {
        
        return 0;
    }
}
