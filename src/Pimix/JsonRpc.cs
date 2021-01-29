using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using NLog;

namespace Pimix {
    public abstract class JsonRpc<TResponse> {
        static readonly Logger logger = LogManager.GetCurrentClassLogger();

        public virtual HttpClient HttpClient { get; } = new();

        public abstract string UrlPattern { get; }

        public virtual HttpMethod Method { get; } = HttpMethod.Get;

        public virtual Dictionary<string, string> Headers { get; } = new();

        public virtual string Data { get; set; }

        public TResponse Call(Dictionary<string, string> parameters) {
            var address = UrlPattern.Format(parameters);
            logger.Trace($"{Method} {address}");

            var request = new HttpRequestMessage(Method, address);

            foreach (var (headerName, value) in Headers.Where(h => !h.Key.StartsWith("Content-"))) {
                request.Headers.Add(headerName, value.Format(parameters));
            }

            if (Data != null) {
                request.Content = new ByteArrayContent(Encoding.UTF8.GetBytes(Data.Format(parameters)));

                foreach (var (headerName, value) in Headers.Where(h => h.Key.StartsWith("Content-"))) {
                    request.Content.Headers.Add(headerName, value.Format(parameters));
                }
            }

            return HttpClient.Send(request).GetObject<TResponse>();
        }
    }
}
