using System.Net.Http;

namespace Kifa.Rpc;

public abstract class
    KifaJsonParameterizedRpc<TResponse> : KifaParameterizedRpc, KifaRpc<TResponse> {
    protected virtual bool CamelCase => false;

    public TResponse ParseResponse(HttpResponseMessage responseMessage)
        => responseMessage.GetObject<TResponse>(camelCase: CamelCase)!;
}
