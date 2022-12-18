using System.Net.Http;

namespace Kifa.Rpc;

public interface KifaRpc<TResponse> {
    HttpRequestMessage GetRequest();
    TResponse ParseResponse(HttpResponseMessage responseMessage);
}
