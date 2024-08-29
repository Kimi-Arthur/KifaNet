using System.Collections.Generic;
using System.Linq;
using System.Text;
using CommandLine;
using Kifa.Jobs;
using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;

namespace Kifa.Tools.BookUtil.Commands;

[Verb("reorder", HelpText = "Reorder page orders to make it easy to print two paged like a book.")]
public class ReorderCommand : KifaCommand {
    [Value(0, Required = true, HelpText = "Target PDF file(s) to reorder the pages.")]
    public string FileName { get; set; }

    [Option('p', "page-range",
        HelpText = "Select intended pages to reorder, in the format of '1,2,10-12,99-'.")]
    public string PageRanges { get; set; } = "-";

    [Option('a', "force-append",
        HelpText = "Force appending blank pages even if it's multiple of 4.")]
    public bool ForceAppend { get; set; } = false;

    public override int Execute(KifaTask? task = null) {
        var document = PdfReader.Open(FileName, PdfDocumentOpenMode.Import);

        var allPages = document.Pages;
        var pageCount = allPages.Count;
        var selectedPages = new List<PdfPage>();
        foreach (var rangeText in PageRanges.Split(",")) {
            if (rangeText.Contains("-")) {
                var startEnd = rangeText.Split('-');
                var start = string.IsNullOrEmpty(startEnd[0]) ? 1 : int.Parse(startEnd[0]);
                var end = string.IsNullOrEmpty(startEnd[1]) ? pageCount : int.Parse(startEnd[1]);
                selectedPages.AddRange(allPages.Take<PdfPage>(end).Skip(start - 1));
            } else {
                selectedPages.Add(allPages[int.Parse(rangeText) - 1]);
            }
        }

        var firstPage = selectedPages.First();
        if (ForceAppend) {
            selectedPages.Add(new PdfPage {
                Height = firstPage.Height,
                Width = firstPage.Width
            });
        }

        while (selectedPages.Count % 4 > 0) {
            selectedPages.Add(new PdfPage {
                Height = firstPage.Height,
                Width = firstPage.Width
            });
        }

        var reorderedPages = ReorderPages(selectedPages);
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        var newDocument = new PdfDocument(FileName.Replace(".pdf", "_print.pdf"));
        foreach (var page in reorderedPages) {
            newDocument.Pages.Add(page);
        }

        newDocument.Close();
        return 0;
    }

    static List<PdfPage> ReorderPages(List<PdfPage> selectedPages) {
        var pageCount = selectedPages.Count;
        var firstHalf = selectedPages.Take((pageCount + 1) / 2).ToList();
        var index = 0;
        var add = 3;
        foreach (var page in selectedPages.Skip((pageCount + 1) / 2).Reverse()) {
            firstHalf.Insert(index, page);
            index += add;
            add = 4 - add;
        }

        return firstHalf;
    }
}
