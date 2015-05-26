using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Pimix.Cloud.Baidu
{
    public class StorageClient
    {
        private List<Stream> Streams { get; set; } = new List<Stream>();

        public static Config Config { get; set; }

        string accountId;
        public string AccountId
        {
            get
            {
                return accountId;
            }
            set
            {
                accountId = value;
                Account = Config.Accounts[accountId];

                // Clear all current streams since they are all stale.
                foreach (var stream in Streams)
                {
                    stream.Dispose();
                }

                Streams.Clear();
            }
        }

        public AccountInfo Account { get; private set; }

        public StorageClient()
        {

        }

        public void DownloadToStream(string path, Stream output, long offset = 0, long length = -1)
        {
            HttpWebRequest request = ConstructRequest(Config.APIList.DownloadFile,
                new Dictionary<string, string>
                {
                    ["path"] = path
                });

            if (length >= 0)
            {
                request.AddRange(offset, offset + length - 1);
            }
            else
            {
                request.AddRange(offset);
            }

            using (var response = request.GetResponse())
            {
                response.GetResponseStream().CopyTo(output);
            }
        }

        public Stream GetDownloadStream(string path)
        {
            Streams.Add(new DownloadStream(this, path));
            return Streams.Last();
        }

        private HttpWebRequest ConstructRequest(APIInfo api, Dictionary<string, string> parameters)
        {
            string address = api.Url.Format(parameters).Format(
                new Dictionary<string, string>
                {
                    ["access_token"] = Account.AccessToken,
                    ["remote_path_prefix"] = Config.RemotePathPrefix
                });

            HttpWebRequest request = WebRequest.CreateHttp(address);
            request.Method = api.Method;

            return request;
        }

        private class DownloadStream : Stream
        {
            private bool IsOpen = true;

            private List<long> StreamBufferQueue { get; set; } = new List<long>();

            private Dictionary<long, Tuple<MemoryStream, int>> StreamBuffer { get; set; } = new Dictionary<long, Tuple<MemoryStream, int>>();

            public StorageClient Client { get; set; }

            public string Path { get; set; }

            public override bool CanRead
                => IsOpen;

            public override bool CanSeek
                => IsOpen;

            public override bool CanWrite
                => false;

            public DownloadStream(StorageClient client, string path)
            {
                Client = client;
                Path = path;
            }

            private MemoryStream GetBlock(long blockId)
            {
                MemoryStream output = new MemoryStream();
                Client.DownloadFile(path, output);
            }
        }
    }
}
