using System.Net.Http;

namespace Kifa.Rpc;

public interface KifaRpc {
    HttpRequestMessage GetRequest();
}

public interface KifaRpc<TResponse> : KifaRpc {
    TResponse ParseResponse(HttpResponseMessage responseMessage);
}
