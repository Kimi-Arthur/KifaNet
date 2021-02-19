using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using NLog;

namespace Kifa.Rpc {
    public abstract class JsonRpc<TResponse> {
        static readonly Logger logger = LogManager.GetCurrentClassLogger();

        public virtual HttpClient HttpClient { get; set; } = new();

        public abstract string UrlPattern { get; }

        public virtual HttpMethod Method { get; } = HttpMethod.Get;

        public virtual Dictionary<string, string> Headers { get; } = new();

        // Different types of content
        public virtual List<KeyValuePair<string, string>> FormUrlEncodedContent { get; set; }

        public TResponse Call(Dictionary<string, string> parameters) {
            var address = UrlPattern.Format(parameters);
            logger.Trace($"{Method} {address}");

            var request = new HttpRequestMessage(Method, address);

            foreach (var (headerName, value) in Headers.Where(h => !h.Key.StartsWith("Content-"))) {
                request.Headers.Add(headerName, value.Format(parameters));
            }

            if (FormUrlEncodedContent != null) {
                request.Content = new FormUrlEncodedContent(FormUrlEncodedContent.Select(item =>
                    new KeyValuePair<string, string>(item.Key, item.Value.Format(parameters))));

                foreach (var (headerName, value) in Headers.Where(h => h.Key.StartsWith("Content-"))) {
                    request.Content.Headers.Add(headerName, value.Format(parameters));
                }
            }

            return HttpClient.Send(request).GetObject<TResponse>();
        }
    }
}
