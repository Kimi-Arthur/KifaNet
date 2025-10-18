using System.Web;
using Kifa.Rpc;

namespace Kifa.ArchiveOrg;

// https://archive.org/developers/wayback-cdx-server.html#basic-usage
public class CdxSearchRpc : KifaJsonParameterizedRpc<List<List<string>>> {
    protected override string Url
        => "http://web.archive.org/cdx/search/cdx?url={encoded_url}&output=json";

    protected override HttpMethod Method => HttpMethod.Get;

    public CdxSearchRpc(string url) {
        Parameters = new Dictionary<string, FuncOrValue<string>> {
            ["encoded_url"] = HttpUtility.UrlEncode(url)
        };
    }
}
