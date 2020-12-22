namespace Pimix {
    public abstract class JsonRpc<TRequest, TResponse> {
        public abstract TResponse Call(TRequest request);
    }
}
