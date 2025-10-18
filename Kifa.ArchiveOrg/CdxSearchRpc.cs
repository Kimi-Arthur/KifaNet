using System.Web;
using Kifa.Rpc;
using Newtonsoft.Json;

namespace Kifa.ArchiveOrg;

// https://archive.org/developers/wayback-cdx-server.html#basic-usage
public class CdxSearchRpc : KifaParameterizedRpc, KifaRpc<List<CdxSearchRpc.ArchiveEntry>> {
    #region CdxSearchRpc.Response

    // urlkey
    // timestamp
    // original
    // mimetype
    // statuscode
    // digest
    // length
    // "com,youtube)/watch?v=0inry1ixr8i"
    // 20131127172500
    // http://www.youtube.com/watch?v=0iNrY1ixR8I&gl=US&hl=en
    // text/html
    // 200
    // 5P3KHGI3SVWPJKIA7T4TGL3PQAVUWKVQ
    // 24444
    public class ArchiveEntry {
        public string? UrlKey { get; set; }
        public string? Timestamp { get; set; }
        public string? Original { get; set; }
        public string? MimeType { get; set; }
        public int? StatusCode { get; set; }
        public string? Digest { get; set; }
        public int? Length { get; set; }
    }

    #endregion


    protected override string Url
        => "http://web.archive.org/cdx/search/cdx?url={encoded_url}&output=json";

    protected override HttpMethod Method => HttpMethod.Get;

    public CdxSearchRpc(string url) {
        Parameters = new Dictionary<string, FuncOrValue<string>> {
            ["encoded_url"] = HttpUtility.UrlEncode(url)
        };
    }

    public List<ArchiveEntry> ParseResponse(HttpResponseMessage responseMessage) {
        var response =
            JsonConvert.DeserializeObject<List<List<string>>>(responseMessage.GetString());
        var result = new List<ArchiveEntry>();
        foreach (var item in response.Skip(1)) {
            result.Add(new ArchiveEntry {
                UrlKey = item[0],
                Timestamp = item[1],
                Original = item[2],
                MimeType = item[3],
                StatusCode = int.Parse(item[4]),
                Digest = item[5],
                Length = int.Parse(item[6])
            });
        }

        return result;
    }
}
