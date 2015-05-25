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
            }
        }

        public AccountInfo Account { get; private set; }

        public StorageClient()
        {

        }

        public void DownloadFile(string path, Stream output, long offset = 0, long length = -1)
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
            // Construct a stream and use the method above. Something like a wrapper.
            return null;
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
    }
}
