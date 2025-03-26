using System.Net.Http.Headers;
using System.Text.RegularExpressions;
using HtmlAgilityPack;
using Kifa.IO;
using Kifa.Service;
using Newtonsoft.Json;
using NLog;

namespace Kifa.Books.ZeroAvenue;

public class ZeroAvenueBook : DataModel, WithModelId<ZeroAvenueBook> {
    public static string ModelId => "books/zeroavenue";

    static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    public static KifaServiceClient<ZeroAvenueBook> Client { get; set; } =
        new KifaServiceRestClient<ZeroAvenueBook>();

    public string? Title { get; set; }

    public string? Author { get; set; }

    public List<string> Narrators { get; set; } = new();

    public string? Link { get; set; }

    [JsonIgnore]
    string Url => $"https://zero-avenue.com/book/{Id}";

    const string AuthorNote = "Written By:&nbsp;&nbsp;&nbsp;&nbsp;";
    const string NarratorsNote = "Narrated By:&nbsp;";

    // "file":"https://scontentzacds.com/fserver/mp3putput.php?file=701-1000/it_a_novel_by_stephen_king_01"}
    static readonly Regex LinkPattern = new("\"file\":\"(.*)_01\"}");

    static HttpClient? httpClient;

    public static HttpClient HttpClient {
        get {
            if (httpClient == null) {
                httpClient = new HttpClient();
                httpClient.DefaultRequestHeaders.Referrer = new Uri("https://zero-avenue.com/");
                httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(
                    "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/87.0.4280.88 Safari/537.36");
            }

            return httpClient;
        }
    }

    public override DateTimeOffset? Fill() {
        var content = HttpClient.SendWithRetry(Url).GetString();
        var doc = new HtmlDocument();
        doc.LoadHtml(content);

        Title = doc.DocumentNode.SelectSingleNode("//h1").InnerText;

        Author = doc.DocumentNode.SelectSingleNode($"//*[text()='{AuthorNote}']").NextSibling
            .NextSibling.InnerText.Checked().Trim();

        Narrators = doc.DocumentNode.SelectSingleNode($"//*[text()='{NarratorsNote}']").NextSibling
            .NextSibling.InnerText.Checked().Trim().Split(",").ToList();

        Link = LinkPattern.Match(content).Groups[1].Value;

        return null;
    }

    public IEnumerable<(Stream Stream, string name)> GetDownloads() {
        for (var i = 1; i < 100; i++) {
            var link = $"{Link.Checked()}_{i:00}";
            var length = HttpClient.GetContentLength(link);
            if (length is null or 0) {
                yield break;
            }

            yield return (new SeekableReadStream(length.Value, (buffer, bufferOffset, offset, count)
                => {
                if (count < 0) {
                    count = buffer.Length - bufferOffset;
                }

                Logger.Trace($"Downloading from {offset} to {offset + count} of {link}...");

                return Retry.Run(() => {
                    var request = new HttpRequestMessage(HttpMethod.Get, link);

                    request.Headers.Range = new RangeHeaderValue(offset, offset + count - 1);
                    using var response = HttpClient.SendAsync(request).GetAwaiter().GetResult();
                    response.EnsureSuccessStatusCode();
                    var memoryStream = new MemoryStream(buffer, bufferOffset, count, true);
                    response.Content.ReadAsStreamAsync().Result.CopyTo(memoryStream, count);
                    return (int) memoryStream.Position;
                }, (ex, i) => {
                    if (i >= 5) {
                        throw ex;
                    }

                    Logger.Warn(ex, $"Download from {offset} to {offset + count} failed ({i})...");
                    Thread.Sleep(TimeSpan.FromSeconds(30));
                });
            }), "");
        }
    }
}
