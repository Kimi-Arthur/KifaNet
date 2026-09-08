using Kifa.Rpc;

namespace Kifa.ArchiveOrg;

// https://gemini.google.com/share/34dd8df44530
public class ArchiveContentRpc : KifaParameterizedRpc, KifaRpc<string> {
    protected override string Url => "https://web.archive.org/web/{timestamp}/{url}";

    protected override HttpMethod Method => HttpMethod.Get;

    protected override Dictionary<string, string> Headers
        => new() {
            ["User-Agent"] =
                "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36"
        };

    public ArchiveContentRpc(string url, string timestamp) {
        Parameters = new Dictionary<string, FuncOrValue<string>> {
            { "url", url },
            { "timestamp", timestamp }
        };
    }

    public string ParseResponse(HttpResponseMessage responseMessage) => responseMessage.GetString();
}
