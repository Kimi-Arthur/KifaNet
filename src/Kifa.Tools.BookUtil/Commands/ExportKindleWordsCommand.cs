using System.Collections.Generic;
using CommandLine;
using Kifa.Jobs;

namespace Kifa.Tools.BookUtil.Commands; 

[Verb("words", HelpText = "Exports words from Kindle vocab.db files.")]
public class ExportKindleWordsCommand : KifaCommand {
    #region public late string OutputFile { get; set; }

    string? outputFile;

    [Option('o', "output", Required = true, HelpText = "Output file name.")]
    public string OutputFile {
        get => Late.Get(outputFile);
        set => Late.Set(ref outputFile, value);
    }

    #endregion
    

    #region public late IEnumerable<string> FileNames { get; set; }

    IEnumerable<string>? fileNames;

    [Value(0, Required = true, HelpText = "vocab.db files to import words from.")]
    public IEnumerable<string> FileNames {
        get => Late.Get(fileNames);
        set => Late.Set(ref fileNames, value);
    }

    #endregion

    public override int Execute(KifaTask? task = null) {
        
        return 0;
    }
}
