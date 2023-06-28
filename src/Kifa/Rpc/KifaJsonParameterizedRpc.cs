using System.Net.Http;

namespace Kifa.Rpc;

public abstract class
    KifaJsonParameterizedRpc<TResponse> : KifaParameterizedRpc, KifaRpc<TResponse> {
    public virtual bool CamelCase { get; set; } = false;

    public TResponse ParseResponse(HttpResponseMessage responseMessage)
        => responseMessage.GetObject<TResponse>(camelCase: CamelCase)!;
}
