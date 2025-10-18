using System.Web;
using Kifa.Rpc;

namespace Kifa.ArchiveOrg;

public class ArchiveContentRpc : KifaParameterizedRpc, KifaRpc<string> {
    protected override string Url
        => "?";

    protected override HttpMethod Method => HttpMethod.Get;

    public ArchiveContentRpc(string url) {
    }
}
