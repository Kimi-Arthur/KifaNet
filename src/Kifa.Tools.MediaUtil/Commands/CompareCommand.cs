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

        PrintComparisonResult(result);
        return 0;
    }

    void PrintComparisonResult(MediaComparisonResult result) {
        Console.WriteLine("Media Comparison Result");
        Console.WriteLine("=======================");
        Console.WriteLine($"File 1: {result.File1Path} ({result.File1Size:N0} bytes)");
        Console.WriteLine($"File 2: {result.File2Path} ({result.File2Size:N0} bytes)");
        Console.WriteLine();

        // 0) Bit-by-bit match
        Console.WriteLine("[0] Bit-by-Bit Match:");
        if (result.IsBitExactMatch) {
            Console.WriteLine("    MATCH: Files are 100% bit-exact identical.");
            Console.WriteLine($"    SHA-256: {result.File1Sha256}");
        } else {
            Console.WriteLine("    NO MATCH: File binary hashes differ.");
            Console.WriteLine($"    File 1 SHA-256: {result.File1Sha256}");
            Console.WriteLine($"    File 2 SHA-256: {result.File2Sha256}");
        }

        Console.WriteLine();

        // 1) Stream / Content match
        Console.WriteLine("[1] Stream / Content Match:");
        if (result.IsContentMatch) {
            var levelDesc = result.MatchLevel switch {
                ContentMatchLevel.BitExact => "Bit-Exact (Identical files)",
                ContentMatchLevel.BitstreamMatch =>
                    "Bitstream Match (Compressed media bitstreams are identical; container/metadata differs)",
                ContentMatchLevel.DecodedMatch =>
                    "Decoded Match (Decoded frames/samples are identical)",
                _ => "Match"
            };
            Console.WriteLine($"    MATCH: {levelDesc}");
            foreach (var stream in result.Streams) {
                var streamDetails = stream.Details != null ? $" ({stream.Details})" : "";
                var matchType = stream.IsBitstreamMatch ? "Bitstream" : "Decoded";
                var hash = stream.IsBitstreamMatch
                    ? stream.File1BitstreamHash
                    : stream.File1DecodedHash;
                Console.WriteLine(
                    $"    - Stream #{stream.Index} [{stream.StreamType}]{streamDetails}: MATCH ({matchType} SHA-256: {hash})");
            }
        } else {
            Console.WriteLine("    NO MATCH: Media streams or content differ.");
            foreach (var stream in result.Streams) {
                var streamDetails = stream.Details != null ? $" ({stream.Details})" : "";
                var status = stream.IsMatch ? "MATCH" : "MISMATCH";
                Console.WriteLine(
                    $"    - Stream #{stream.Index} [{stream.StreamType}]{streamDetails}: {status}");
                if (!stream.IsMatch) {
                    if (stream.File1BitstreamHash != null || stream.File2BitstreamHash != null) {
                        Console.WriteLine(
                            $"        File 1 Bitstream: {stream.File1BitstreamHash ?? "(n/a)"}");
                        Console.WriteLine(
                            $"        File 2 Bitstream: {stream.File2BitstreamHash ?? "(n/a)"}");
                    }

                    if (stream.File1DecodedHash != null || stream.File2DecodedHash != null) {
                        Console.WriteLine(
                            $"        File 1 Decoded:   {stream.File1DecodedHash ?? "(n/a)"}");
                        Console.WriteLine(
                            $"        File 2 Decoded:   {stream.File2DecodedHash ?? "(n/a)"}");
                    }
                }
            }
        }

        Console.WriteLine();

        // 2) If they match, what fields don't
        Console.WriteLine("[2] Differing Fields:");
        var diffsToShow = result.IsContentMatch || AllFields ? result.AllDifferences : [];

        if (result.IsBitExactMatch) {
            Console.WriteLine("    None. All metadata and binary fields are identical.");
        } else if (!result.IsContentMatch && !AllFields) {
            Console.WriteLine(
                "    Content does not match. (Use --all-fields / -a to see all metadata differences anyway).");
        } else if (diffsToShow.Count == 0) {
            Console.WriteLine("    None. All metadata fields match.");
        } else {
            Console.WriteLine($"    Found {diffsToShow.Count} differing field(s):");
            Console.WriteLine();
            var grouped = diffsToShow.GroupBy(d => d.Category).OrderBy(g => g.Key);
            foreach (var group in grouped) {
                Console.WriteLine($"    • {group.Key} ({group.Count()}):");
                foreach (var diff in group) {
                    var v1 = diff.File1Value != null ? $"\"{diff.File1Value}\"" : "(missing)";
                    var v2 = diff.File2Value != null ? $"\"{diff.File2Value}\"" : "(missing)";
                    Console.WriteLine($"        {diff.Name}:");
                    Console.WriteLine($"            File 1: {v1}");
                    Console.WriteLine($"            File 2: {v2}");
                }

                Console.WriteLine();
            }
        }
    }
}
