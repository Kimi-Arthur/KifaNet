using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using CommandLine;
using Kifa.Api.Files;
using Kifa.Jobs;
using QuickEPUB;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;

namespace Kifa.Tools.BookUtil.Commands;

[Verb("manga", HelpText = "Generate Epub book based on Manga chapters.")]
public class CreateMangaCommand : KifaCommand {
    [Value(0, Required = true, HelpText = "Target files to link.")]
    public IEnumerable<string> FileNames { get; set; }

    public override int Execute(KifaTask? task = null) {
        var epub = new Epub("天才麻将少女 第1话 邂逅", "小林立");

        var sb = new StringBuilder();

        var section = "";
        var right = true;

        foreach (var file in KifaFile.FindAllFiles(FileNames).Skip(1)) {
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
            var image = Image.Load(file.GetLocalPath());

            image.Mutate(x => x.Rotate(RotateMode.Rotate270));
            var stream = new MemoryStream();
            image.Save(stream, image.Metadata.DecodedImageFormat.Checked());
            stream.Seek(0, SeekOrigin.Begin);

            // TODO: should choose the right format for the resource.
            epub.AddResource(name, EpubResourceType.JPEG, stream);
            sb.Append($"<img src=\"{name}\" style=\"height: 40%\"/>\n");
        }

        if (sb.Length > 0) {
            epub.AddSection(section, sb.ToString());
            sb.Clear();
        }

        using var memoryStream = new MemoryStream();
        epub.Export(memoryStream);

        var output = new KifaFile("天才麻将少女 第1话 邂逅.epub");
        output.Delete();
        output.Write(memoryStream.ToArray());

        return 0;
    }
}
