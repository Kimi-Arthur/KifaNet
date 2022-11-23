using System.Collections.Generic;
using CommandLine;
using Kifa.Api.Files;
using PdfSharpCore.Drawing;
using PdfSharpCore.Pdf;

namespace Kifa.Tools.BookUtil.Commands;

[Verb("pdf", HelpText = "Generate Pdf book based on Manga chapters.")]
public class CreatePdfMangaCommand : KifaCommand {
    [Value(0, Required = true, HelpText = "Target files to link.")]
    public IEnumerable<string> FileNames { get; set; }

    public override int Execute() {
        using var document = new PdfDocument();
        document.ViewerPreferences.Direction = PdfReadingDirection.RightToLeft;
        document.PageLayout = PdfPageLayout.SinglePage;

        foreach (var file in KifaFile.FindAllFiles(FileNames).files) {
            var page = document.AddPage();
            var gfx = XGraphics.FromPdfPage(page);

            var image = XImage.FromFile(file.GetLocalPath());
            page.Width = image.PointWidth;
            page.Height = image.PointHeight;
            gfx.DrawImage(image, 0, 0);
        }

        document.Save("a.pdf");

        return 0;
    }
}
