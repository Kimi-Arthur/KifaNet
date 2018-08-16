using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using NLog;
using Pimix.IO;

namespace Pimix.Cloud.GoogleDrive {
    public class GoogleDriveStorageClient : StorageClient {
        static readonly Logger logger = LogManager.GetCurrentClassLogger();

        static readonly TimeSpan RefreshAccountInterval = TimeSpan.FromMinutes(50);
        const int BlockSize = 32 << 20;

        readonly Timer refreshTimer;

        public static GoogleDriveConfig Config { get; set; }

        public static StorageClient Get(string fileSpec) {
            var specs = fileSpec.Split(';');
            foreach (var spec in specs)
                if (spec.StartsWith("google:")) {
                    Config = GoogleDriveConfig.Get("default");
                    var client = new GoogleDriveStorageClient {AccountId = spec.Substring(7)};
                    client.RefreshAccount(null);
                    return client;
                }

            return null;
        }

        public override string ToString() => $"google:{AccountId}";

        readonly HttpClient client;

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
            client = new HttpClient(new HttpClientHandler() {
                AllowAutoRedirect = false
            });

            refreshTimer = new Timer(RefreshAccount, null, RefreshAccountInterval, RefreshAccountInterval);
        }

        public override bool Exists(string path) => GetFileId(path) != null;

        public override void Delete(string path) {
            var fileId = GetFileId(path);
            if (fileId != null) {
                var request = GetRequest(Config.APIList.DeleteFile,
                    new Dictionary<string, string> {
                        ["file_id"] = fileId
                    });

                using (var response = client.SendAsync(request).Result) {
                    if (!response.IsSuccessStatusCode) {
                        throw new Exception("Delete is not successful.");
                    }
                }
            }
        }

        public override Stream OpenRead(string path) {
            var fileId = GetFileId(path);
            var fileSize = GetFileSize(fileId);
            return new SeekableReadStream(
                fileSize,
                (buffer, bufferOffset, offset, count)
                    => Download(buffer, fileId, bufferOffset, offset, count)
            );
        }

        public override void Write(string path, Stream input) {
            var folderId = GetFileId(path.Substring(0, path.LastIndexOf('/')), true);
            var request = GetRequest(Config.APIList.CreateFile, new Dictionary<string, string> {
                ["parent_id"] = folderId,
                ["name"] = path.Substring(path.LastIndexOf('/') + 1)
            });

            Uri uploadUri;
            using (var response = client.SendAsync(request).Result) {
                uploadUri = response.Headers.Location;
            }

            long size = input.Length;
            var buffer = new byte[BlockSize];

            for (long position = 0; position < size; position += BlockSize) {
                var blockLength = input.Read(buffer, 0, BlockSize);
                var targetEndByte = position + blockLength - 1;
                var uploadRequest = new HttpRequestMessage(HttpMethod.Put, uploadUri);
                var content = new ByteArrayContent(buffer, 0, blockLength);
                content.Headers.ContentRange =
                    new ContentRangeHeaderValue(position, targetEndByte, size);
                content.Headers.ContentLength = blockLength;
                uploadRequest.Content = content;

                using (var response = client.SendAsync(uploadRequest).Result) {
                    if (targetEndByte + 1 == size) {
                        if (!response.IsSuccessStatusCode) {
                            throw new Exception("Last request should have success code");
                        }
                    } else {
                        var range = RangeHeaderValue.Parse(response.Headers
                            .First(h => h.Key == "Range").Value.First());
                        var fromByte = range.Ranges.First().From;
                        var toByte = range.Ranges.First().To;
                        if (fromByte != 0) {
                            throw new Exception($"Unexpected exception: from byte is {fromByte}");
                        }

                        if (toByte != targetEndByte) {
                            throw new Exception(
                                $"Unexpected exception: to byte is {toByte}, should be {targetEndByte}");
                        }
                    }
                }
            }
        }

        int Download(byte[] buffer, string fileId, int bufferOffset = 0, long offset = 0,
            int count = -1) {
            if (count < 0) count = buffer.Length - bufferOffset;

            var request = GetRequest(Config.APIList.DownloadFile, new Dictionary<string, string> {
                ["file_id"] = fileId
            });

            request.Headers.Range = new RangeHeaderValue(offset, offset + count - 1);
            using (var response = client.SendAsync(request).Result) {
                var memoryStream = new MemoryStream(buffer, bufferOffset, count, true);
                response.Content.ReadAsStreamAsync().Result.CopyTo(memoryStream, count);
                return (int) memoryStream.Position;
            }
        }

        long GetFileSize(string fileId) {
            var request = GetRequest(Config.APIList.GetFileInfo, new Dictionary<string, string>() {
                ["file_id"] = fileId
            });

            using (var response = client.SendAsync(request).Result) {
                var token = response.GetJToken();
                return long.Parse((string) token["size"]);
            }
        }

        string GetFileId(string path, bool createParents = false) {
            string fileId = "root";
            foreach (var segment in $"{Config.RootFolder}{path}".Split('/')) {
                var request = GetRequest(Config.APIList.FindFile, new Dictionary<string, string>() {
                    ["name"] = segment,
                    ["parent_id"] = fileId
                });

                using (var response = client.SendAsync(request).Result) {
                    var files = response.GetJToken()["files"];
                    if (files == null) {
                        return null;
                    }

                    if (files.Any()) {
                        fileId = (string) files[0]["id"];
                        continue;
                    }

                    if (createParents) {
                        fileId = CreateFolder(fileId, segment);
                    } else {
                        return null;
                    }
                }
            }

            return fileId;
        }

        string CreateFolder(string parentId, string name) {
            var request = GetRequest(Config.APIList.CreateFolder, new Dictionary<string, string>() {
                ["parent_id"] = parentId,
                ["name"] = name
            });

            using (var response = client.SendAsync(request).Result) {
                var token = response.GetJToken();
                return (string) token["id"];
            }
        }

        void RefreshAccount(object stateInfo) {
            var request = GetRequest(Config.APIList.OauthRefresh, new Dictionary<string, string> {
                ["refresh_token"] = Account.RefreshToken,
                ["client_id"] = Config.ClientId,
                ["client_secret"] = Config.ClientSecret
            });

            using (var response = client.SendAsync(request).Result) {
                var token = response.GetJToken();
                Account.AccessToken = (string) token["access_token"];
            }
        }

        HttpRequestMessage GetRequest(APIInfo api, Dictionary<string, string> parameters = null) {
            parameters = parameters ?? new Dictionary<string, string>();
            parameters["access_token"] = Account.AccessToken;

            var address = api.Url.Format(parameters);

            logger.Trace($"{api.Method} {address}");
            var request = new HttpRequestMessage(new HttpMethod(api.Method), address);

            foreach (var header in api.Headers.Where(h => !h.Key.StartsWith("Content-"))) {
                request.Headers.Add(header.Key, header.Value.Format(parameters));
            }

            if (api.Data != null) {
                request.Content =
                    new ByteArrayContent(Encoding.UTF8.GetBytes(api.Data.Format(parameters)));

                foreach (var header in api.Headers.Where(h => h.Key.StartsWith("Content-"))) {
                    request.Content.Headers.Add(header.Key, header.Value.Format(parameters));
                }
            }

            return request;
        }

        public override void Dispose() {
            refreshTimer?.Dispose();
            client?.Dispose();
        }
    }
}
