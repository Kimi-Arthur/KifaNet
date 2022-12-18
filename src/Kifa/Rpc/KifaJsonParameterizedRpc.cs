using System.Net.Http;

namespace Kifa.Rpc;

public abstract class KifaJsonParameterizedRpc<TResponse> : KifaParameterizedRpc<TResponse> {
    public virtual bool CamelCase { get; set; } = false;

    public override TResponse ParseResponse(HttpResponseMessage responseMessage)
        => responseMessage.GetObject<TResponse>(camelCase: CamelCase)!;
}
