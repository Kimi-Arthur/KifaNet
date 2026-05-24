using CommandLine;
using Kifa.Books.ZeroAvenue;
using Kifa.Jobs;

namespace Kifa.Tools.BookUtil.Commands;

public class DownloadZeroAvenueCommand : KifaCommand {
    [Value(0, Required = true, HelpText = "Id of the book to download.")]
    public string BookId {
        get => Late.Get(field);
        set => Late.Set(ref field, value);
    }

    public override int Execute(KifaTask? task = null) {
        var book = ZeroAvenueBook.Client.Get(BookId).Checked();
        foreach (var url in book.GetDownloads()) {
        }

        return 0;
    }
}
