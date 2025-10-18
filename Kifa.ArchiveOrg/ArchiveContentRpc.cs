using Kifa.Rpc;

namespace Kifa.ArchiveOrg;

// https://gemini.google.com/share/34dd8df44530
public class ArchiveContentRpc : KifaParameterizedRpc, KifaRpc<string> {
    protected override string Url => "https://web.archive.org/web/{timestamp}/{url}";

    protected override HttpMethod Method => HttpMethod.Get;

    public ArchiveContentRpc(string url, string timestamp) {
        Parameters = new Dictionary<string, FuncOrValue<string>> {
            { "url", url },
            { "timestamp", timestamp }
        };
    }

    public string ParseResponse(HttpResponseMessage responseMessage) => responseMessage.GetString();
}
