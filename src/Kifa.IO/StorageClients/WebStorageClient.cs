using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using Pimix;

namespace Kifa.IO.StorageClients {
    public class WebStorageClient : StorageClient {
        HttpClient httpClient = new();

        public string Protocol { get; set; }

        public override long Length(string path) => httpClient.GetContentLength(GetUrl(path)).GetValueOrDefault(0);

        public override void Delete(string path) {
            throw new NotImplementedException();
        }

        public override void Touch(string path) {
            throw new NotImplementedException();
        }

        public override Stream OpenRead(string path) {
            var url = GetUrl(path);
            return new SeekableReadStream(Length(path), (buffer, bufferOffset, offset, count) => {
                var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.Range = new RangeHeaderValue(offset, offset + count - 1);
                var content = httpClient.Send(request).Content;
                var memoryStream = new MemoryStream(buffer, bufferOffset, count);
                content.CopyToAsync(memoryStream).Wait();
                return (int) memoryStream.Position;
            });
        }

        public override void Write(string path, Stream stream) {
            throw new NotImplementedException();
        }

        public override string Type => Protocol;
        public override string Id => "/";

        string GetUrl(string path) => $"{Protocol}:/{path}";
    }
}
