using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using CommandLine;
using Kifa.Api.Files;
using QuickEPUB;

namespace Kifa.Tools.BookUtil.Commands;

[Verb("manga", HelpText = "Generate Epub book based on Manga chapters.")]
public class CreateMangaCommand : KifaCommand {
    [Value(0, Required = true, HelpText = "Target files to link.")]
    public IEnumerable<string> FileNames { get; set; }

    public override int Execute() {
        var epub = new Epub($"天才麻将少女 第1卷", "小林立");

        var sb = new StringBuilder();

        var section = "";
        var right = true;

        foreach (var file in KifaFile.FindAllFiles(FileNames).files.Skip(1)) {
            var newSection =
                file.Parent.ToString().Split("/", StringSplitOptions.RemoveEmptyEntries)[^1]
                    .Split(" ", 2)[^1];
            if (newSection != section) {
                if (sb.Length > 0) {
                    epub.AddSection(section, sb.ToString());
                    sb.Clear();
                }

                section = newSection;
            }

            var name = $"{newSection}/{file.Name}";
            epub.AddResource(name, EpubResourceType.JPEG, file.OpenRead());
            sb.Append($"<img src=\"{name}\" />");
        }

        if (sb.Length > 0) {
            epub.AddSection(section, sb.ToString());
            sb.Clear();
        }

        using var memoryStream = new MemoryStream();
        epub.Export(memoryStream);

        var output = new KifaFile("a.epub");
        output.Delete();
        output.Write(memoryStream.ToArray());

        return 0;
    }
}
