using System.Collections.Generic;
using System.Net.Http;

namespace Pimix {
    public abstract class JsonRpc<TResponse> {
        public abstract string UrlPattern { get; }
        public abstract HttpClient HttpClient { get; }

        public TResponse Call(Dictionary<string, string> parameters) {
            var url = UrlPattern.Format(parameters);
            return HttpClient.GetAsync(url).Result.GetObject<TResponse>();
        }
    }
}
