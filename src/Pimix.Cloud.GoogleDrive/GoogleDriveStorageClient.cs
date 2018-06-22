using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using NLog;
using Pimix.IO;

namespace Pimix.Cloud.GoogleDrive {
    public class GoogleDriveStorageClient : StorageClient {
        static readonly Logger logger = LogManager.GetCurrentClassLogger();

        public static GoogleDriveConfig Config { get; set; }

        public static StorageClient Get(string fileSpec) {
            var specs = fileSpec.Split(';');
            foreach (var spec in specs)
                if (spec.StartsWith("google:")) {
                    Config = GoogleDriveConfig.Get("default");
                    return new GoogleDriveStorageClient {AccountId = spec.Substring(7)};
                }

            return null;
        }

        public override string ToString() => $"google:{AccountId}";

        readonly HttpClient Client;

        string accountId;

        public string AccountId {
            get => accountId;
            set {
                accountId = value;
                Account = Config.Accounts[accountId];
            }
        }

        public AccountInfo Account { get; private set; }

        public GoogleDriveStorageClient() {
            Client = new HttpClient();
        }
        
        public override bool Exists(string path) => throw new System.NotImplementedException();

        public override void Delete(string path) {
            throw new System.NotImplementedException();
        }

        public override Stream OpenRead(string path) {
            var fileId = GetFileId(path);
            var request = GetRequest(Config.APIList.DownloadFile, new Dictionary<string, string>() {
                ["file_id"] = fileId
            });

            using (var response = Client.SendAsync(request).Result) {
                var memoryStream = new MemoryStream();
                response.Content.ReadAsStreamAsync().Result.CopyTo(memoryStream);
                return memoryStream;
            }
        }

        public override void Write(string path, Stream stream) {
            throw new System.NotImplementedException();
        }

        string GetFileId(string path) {
            string fileId = "root";
            foreach (var segment in path.Substring(1).Split('/')) {
                var request = GetRequest(Config.APIList.FindFile, new Dictionary<string, string>() {
                    ["name"] = segment,
                    ["parent_id"] = fileId
                });

                using (var response = Client.SendAsync(request).Result) {
                    var token = response.GetJToken();
                    fileId = (string) token["files"][0]["id"];
                }
            }

            return fileId;
        }

        HttpRequestMessage GetRequest(APIInfo api, Dictionary<string, string> parameters = null) {
            parameters = parameters ?? new Dictionary<string, string>();
            parameters["access_token"] = Account.AccessToken;

            var address = api.Url.Format(parameters);

            logger.Trace($"{api.Method} {address}");
            var request = new HttpRequestMessage(new HttpMethod(api.Method), address);

            foreach (var header in api.Headers) {
                request.Headers.Add(header.Key, header.Value.Format(parameters));
            }

            return request;
        }
    }
}
