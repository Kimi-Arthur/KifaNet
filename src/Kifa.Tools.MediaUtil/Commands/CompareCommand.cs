using System;
using System.Linq;
using CommandLine;
using Kifa.Api.Files;
using Kifa.Jobs;
using Kifa.Media;
using Newtonsoft.Json;
using NLog;

namespace Kifa.Tools.MediaUtil.Commands;

[Verb("compare", HelpText = "Compare two media files (images, videos, or audio).")]
public class CompareCommand : KifaCommand {
    static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    [Value(0, Required = true, HelpText = "First media file to compare.")]
    public string File1 { get; set; } = "";

    [Value(1, Required = true, HelpText = "Second media file to compare.")]
    public string File2 { get; set; } = "";

    [Option('d', "deep",
        HelpText = "Force full frame-by-frame decoded comparison even if bitstreams match.")]
    public bool Deep { get; set; } = false;

    [Option('a', "all-fields",
        HelpText = "Show all metadata differences even if content does not match.")]
    public bool AllFields { get; set; } = false;

    [Option("json", HelpText = "Output result as JSON.")]
    public bool JsonOutput { get; set; } = false;

    [Option('1', "one-line", HelpText = "Output result in a single line.")]
    public bool OneLine { get; set; } = false;

    public override int Execute(KifaTask? task = null) {
        var file1 = new KifaFile(File1);
        var file2 = new KifaFile(File2);

        if (!file1.Exists()) {
            Logger.Error($"File 1 not found: {file1}");
            return 1;
        }

        if (!file2.Exists()) {
            Logger.Error($"File 2 not found: {file2}");
            return 1;
        }

        var result = MediaFileComparator.Compare(file1, file2, deep: Deep);

        if (JsonOutput) {
            Console.WriteLine(JsonConvert.SerializeObject(result, Formatting.Indented));
            return 0;
        }

        if (OneLine) {
            Console.WriteLine(result.ToOneLineString(AllFields));
            return 0;
        }

        Console.WriteLine(result.ToMultiLineString(AllFields));
        return 0;
    }
}

