using CommandLine;
using Kifa.Books.ZeroAvenue;
using Kifa.Jobs;

namespace Kifa.Tools.BookUtil.Commands;

public class DownloadZeroAvenueCommand : KifaCommand {
    string? bookId;

    [Value(0, Required = true, HelpText = "Id of the book to download.")]
    public string BookId {
        get => Late.Get(bookId);
        set => Late.Set(ref bookId, value);
    }

    public override int Execute(KifaTask? task = null) {
        var book = ZeroAvenueBook.Client.Get(BookId).Checked();
        foreach (var url in book.GetDownloads()) {
        }

        return 0;
    }
}
