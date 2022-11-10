using System.Net.Http;

namespace Kifa.Rpc;

public abstract class KifaJsonParameterizedRpc<TResponse> : KifaParameterizedRpc<TResponse> {
    public override TResponse? ParseResponse(HttpResponseMessage responseMessage)
        => responseMessage.GetObject<TResponse>();
}
