using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using NLog;
using Pimix.IO;
using Pimix.Service;

namespace Pimix.Cloud.GoogleDrive {
    public class GoogleDriveStorageClient : StorageClient {
        const int BlockSize = 32 << 20;
        static readonly Logger logger = LogManager.GetCurrentClassLogger();

        static readonly TimeSpan RefreshAccountInterval = TimeSpan.FromMinutes(50);

        static GoogleDriveConfig config;

        readonly HttpClient client = new HttpClient(new HttpClientHandler {
            AllowAutoRedirect = false
        }) {
            Timeout = TimeSpan.FromMinutes(30)
        };

        AccountInfo account;

        string accountId;

        DateTime lastRefreshed = DateTime.MinValue;

        static GoogleDriveConfig Config =>
            LazyInitializer.EnsureInitialized(ref config, () => GoogleDriveConfig.Client.Get("default"));

        public string AccountId {
            get => accountId;
            set {
                accountId = value;
                account = null;
            }
        }

        public AccountInfo Account => account ??= Config.Accounts[accountId];


        public override string Type => "google";

        public override string Id => AccountId;

        public override IEnumerable<FileInformation> List(string path, bool recursive = false) {
            var fileId = GetFileId(path);
            if (fileId == null) {
                yield break;
            }

            var pageToken = "";

            while (pageToken != null) {
                using var response = client.SendWithRetry(() => GetRequest(Config.APIList.ListFiles,
                    new Dictionary<string, string> {
                        ["parent_id"] = fileId,
                        ["page_token"] = pageToken
                    }));
                if (!response.IsSuccessStatusCode) {
                    throw new Exception(
                        $"List Files is not successful ({response.ReasonPhrase}):\n{response.GetString()}");
                }

                var token = response.GetJToken();
                pageToken = token.Value<string>("nextPageToken");

                foreach (var fileToken in token["files"]) {
                    yield return new FileInformation {
                        Id = $"{path}/{(string) fileToken["name"]}",
                        Size = long.Parse((string) fileToken["size"])
                    };
                }
            }
        }

        public override long Length(string path) => GetFileSize(GetFileId(path));

        public override void Delete(string path) {
            var fileId = GetFileId(path);
            if (fileId != null) {
                using var response = client.SendWithRetry(() => GetRequest(Config.APIList.DeleteFile,
                    new Dictionary<string, string> {
                        ["file_id"] = fileId
                    }));
                if (!response.IsSuccessStatusCode) {
                    throw new Exception("Delete is not successful.");
                }
            }
        }

        public override void Touch(string path) {
            throw new NotImplementedException();
        }

        public override Stream OpenRead(string path) {
            var fileId = GetFileId(path);
            var fileSize = GetFileSize(fileId);
            return new SeekableReadStream(fileSize,
                (buffer, bufferOffset, offset, count)
                    => Download(buffer, fileId, bufferOffset, offset, count));
        }

        public override void Write(string path, Stream input) {
            var folderId = GetFileId(path.Substring(0, path.LastIndexOf('/')), true);

            Uri uploadUri;
            using var uriResponse = client.SendWithRetry(() => GetRequest(Config.APIList.CreateFile,
                new Dictionary<string, string> {
                    ["parent_id"] = folderId,
                    ["name"] = path.Substring(path.LastIndexOf('/') + 1)
                }));

            uploadUri = uriResponse.Headers.Location;

            var size = input.Length;
            var buffer = new byte[BlockSize];

            for (long position = 0; position < size; position += BlockSize) {
                var blockLength = input.Read(buffer, 0, BlockSize);
                var targetEndByte = position + blockLength - 1;
                var content = new ByteArrayContent(buffer, 0, blockLength);
                content.Headers.ContentRange =
                    new ContentRangeHeaderValue(position, targetEndByte, size);
                content.Headers.ContentLength = blockLength;

                var done = false;

                while (!done) {
                    try {
                        using var response = client.SendWithRetry(() =>
                            new HttpRequestMessage(HttpMethod.Put, uploadUri) {
                                Content = content
                            });
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

                        done = true;
                    } catch (AggregateException ae) {
                        ae.Handle(x => {
                            if (x is HttpRequestException) {
                                logger.Warn(x, "Temporary upload failure [{0}, {1})", position,
                                    position + blockLength);
                                Thread.Sleep(TimeSpan.FromSeconds(10));
                                return true;
                            }

                            return false;
                        });
                    }
                }
            }
        }

        int Download(byte[] buffer, string fileId, int bufferOffset = 0, long offset = 0,
            int count = -1) {
            if (count < 0) {
                count = buffer.Length - bufferOffset;
            }

            using var response = client.SendWithRetry(() => {
                var request = GetRequest(Config.APIList.DownloadFile, new Dictionary<string, string> {
                    ["file_id"] = fileId
                });

                request.Headers.Range = new RangeHeaderValue(offset, offset + count - 1);
                return request;
            });
            var memoryStream = new MemoryStream(buffer, bufferOffset, count, true);
            response.Content.ReadAsStreamAsync().Result.CopyTo(memoryStream, count);
            return (int) memoryStream.Position;
        }

        long GetFileSize(string fileId) {
            if (fileId == null) {
                return -1;
            }

            using var response = client.SendWithRetry(() => GetRequest(Config.APIList.GetFileInfo,
                new Dictionary<string, string> {
                    ["file_id"] = fileId
                }));
            var token = response.GetJToken();
            return long.Parse((string) token["size"]);
        }

        string GetFileId(string path, bool createParents = false) {
            var fileId = "root";
            foreach (var segment in $"{Config.RootFolder}{path}".Split('/', StringSplitOptions.RemoveEmptyEntries)) {
                var token = client.FetchJToken(() => GetRequest(Config.APIList.FindFile,
                    new Dictionary<string, string> {
                        ["name"] = segment,
                        ["parent_id"] = fileId
                    }), t => t["error"] == null);
                var files = token["files"];
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

            return fileId;
        }

        string CreateFolder(string parentId, string name) {
            using var response = client.SendWithRetry(() => GetRequest(Config.APIList.CreateFolder,
                new Dictionary<string, string> {
                    ["parent_id"] = parentId,
                    ["name"] = name
                }));
            var token = response.GetJToken();
            return (string) token["id"];
        }

        void RefreshAccount() {
            if (DateTime.Now - lastRefreshed < RefreshAccountInterval) {
                return;
            }

            using var response = client.SendWithRetry(() => GetRequest(Config.APIList.OauthRefresh,
                new Dictionary<string, string> {
                    ["refresh_token"] = Account.RefreshToken,
                    ["client_id"] = Config.ClientId,
                    ["client_secret"] = Config.ClientSecret
                }, false));

            var token = response.GetJToken();
            Account.AccessToken = (string) token["access_token"];
            lastRefreshed = DateTime.Now;
        }

        HttpRequestMessage GetRequest(API api, Dictionary<string, string> parameters = null,
            bool needAccessToken = true) {
            parameters ??= new Dictionary<string, string>();
            if (needAccessToken) {
                RefreshAccount();
                parameters["access_token"] = Account.AccessToken;
            }

            return api.GetRequest(parameters);
        }

        public override void Dispose() {
            client?.Dispose();
        }
    }
}
