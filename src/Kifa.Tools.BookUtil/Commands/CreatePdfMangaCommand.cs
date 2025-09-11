using System;
using System.Linq;
using CommandLine;
using Kifa.Api.Files;
using Kifa.Graphics;
using Kifa.Jobs;
using PdfSharp;
using PdfSharp.Drawing;
using PdfSharp.Pdf;

namespace Kifa.Tools.BookUtil.Commands;

[Verb("pdf", HelpText = "Generate Pdf book based on Manga chapters.")]
public class CreatePdfMangaCommand : KifaCommand {
    [Value(0, Required = true, HelpText = "Target files to link.")]
    public string Folder { get; set; }

    #region public late string Author { get; set; }

    string? author;

    [Option('a', "author", HelpText = "Author of the manga.")]
    public string Author {
        get => Late.Get(author);
        set => Late.Set(ref author, value);
    }

    #endregion

    [Option('d', "double-pages",
        HelpText = "Pages on the right of the double pages, separated by ','.")]
    public string DoublePages { get; set; } = "";

    [Option('i', "ignore-double-pages", HelpText = "Ignore double page configs in the system.")]
    public string IgnoreExistingDoublePages { get; set; } = "";

    [Option('c', "duplicate-double-pages",
        HelpText = "Put original pages from double pages after the merged one.")]
    public bool DuplicateDoublePages { get; set; }

    public override int Execute(KifaTask? task = null) {
        // var allDoublePages = DoublePages.Split(",", StringSplitOptions.RemoveEmptyEntries);

        var folderId = new KifaFile(Folder).ToString();
        // var episode = BilibiliMangaEpisode.Parse(folderId);
        var title = GetOutputName(folderId);
        using var document = new PdfDocument();
        document.Info.Author = Author;
        document.Info.Title = title;

        var files = KifaFile.FindAllFiles([Folder]);
        var pairedFiles = files.Skip(1).Select((f, idx) => (File: f, LastFile: files[idx]))
            .ToList();
        var selectedDoublePages = SelectMany(pairedFiles,
            pair => GetPrintImages(pair.File, pair.LastFile),
            "pages to be the start page of double pages").Select(pair => pair.LastFile).ToHashSet();

        XImage? doublePage = null;
        foreach (var file in files) {
            var image = XImage.FromFile(file.GetLocalPath());

            if (selectedDoublePages.Contains(file)) {
                doublePage = image;
                continue;
            }

            var page = document.AddPage();
            if (doublePage != null) {
                DrawDoublePage(page, doublePage, image);
                if (DuplicateDoublePages) {
                    page = document.AddPage();
                    DrawSinglePage(page, doublePage);
                    page = document.AddPage();
                    DrawSinglePage(page, image);
                }

                doublePage = null;
            } else {
                DrawSinglePage(page, image);
            }
        }

        document.Save($"{title}.pdf");

        return 0;
    }

    static string GetPrintImages(KifaFile leftFile, KifaFile rightFile) {
        return ITermImage.GetITermImagesSideBySideFromRawBytes(leftFile.ReadAsBytes(),
            rightFile.ReadAsBytes(), displayImageHeight: "48%");
    }

    static string GetOutputName(string folder) {
        var segments = folder.Split("/", StringSplitOptions.RemoveEmptyEntries);
        return
            $"{segments[^2][..segments[^2].IndexOf("-")]} {segments[^1][(segments[^1].IndexOf(" ") + 1)..]}";
    }

    static void DrawSinglePage(PdfPage page, XImage image) {
        var gfx = XGraphics.FromPdfPage(page);
        page.Orientation = PageOrientation.Portrait;
        page.Width = image.PointWidth;
        page.Height = image.PointHeight;
        gfx.DrawImage(image, 0, 0);
    }

    static void DrawDoublePage(PdfPage page, XImage rightImage, XImage leftImage) {
        var gfx = XGraphics.FromPdfPage(page);
        page.Orientation = PageOrientation.Landscape;
        page.Width = leftImage.PointWidth + rightImage.PointWidth;
        page.Height = Math.Max(leftImage.PointHeight, rightImage.PointHeight);
        page.Rotate = 270;
        gfx.DrawImage(leftImage, 0, 0);
        gfx.DrawImage(rightImage, leftImage.PointWidth, 0);
    }
}
