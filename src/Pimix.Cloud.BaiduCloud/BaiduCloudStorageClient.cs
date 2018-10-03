using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NLog;
using Pimix.IO;

namespace Pimix.Cloud.BaiduCloud {
    public class BaiduCloudStorageClient : StorageClient {
        static readonly Logger logger = LogManager.GetCurrentClassLogger();

        static BaiduCloudConfig config;

        static BaiduCloudConfig Config =>
            LazyInitializer.EnsureInitialized(ref config, () => BaiduCloudConfig.Get("default"));

        public override string ToString() => $"baidu:{AccountId}";

        readonly HttpClient client = new HttpClient {Timeout = TimeSpan.FromMinutes(5)};

        string accountId;

        public string AccountId {
            get => accountId;
            set {
                accountId = value;
                Account = Config.Accounts[accountId];
            }
        }

        public AccountInfo Account { get; private set; }

        int Download(byte[] buffer, string path, int bufferOffset = 0, long offset = 0,
            int count = -1) {
            if (count < 0) {
                count = buffer.Length - bufferOffset;
            }

            var maxChunkSize = 1 << 20;

            // The thread limit will help prevent errors with code 31326 and message like
            // "user is not authorized, hitcode:120".
            Parallel.For(0, (count - 1) / maxChunkSize + 1,
                i => {
                    Thread.Sleep(TimeSpan.FromSeconds(i * 4));
                    var chunkOffset = i * maxChunkSize;
                    var chunkSize = Math.Min(maxChunkSize, count - chunkOffset);
                    var readCount = 0;

                    while (readCount != chunkSize) {
                        try {
                            readCount = DownloadChunk(buffer, path, bufferOffset + chunkOffset,
                                offset + chunkOffset,
                                chunkSize);
                            if (readCount != chunkSize) {
                                logger.Warn(
                                    "Internal failure downloading {0} bytes from {1}: only got {2}",
                                    chunkSize, offset + chunkOffset, readCount);
                                Thread.Sleep(TimeSpan.FromSeconds(5));
                            }
                        } catch (Exception ex) {
                            logger.Warn(ex, "Internal failure downloading {0} bytes from {1}:",
                                chunkSize,
                                offset + chunkOffset);
                            Thread.Sleep(TimeSpan.FromSeconds(5));
                        }
                    }
                });

            return count;
        }

        int DownloadChunk(byte[] buffer, string path, int bufferOffset, long offset,
            int count) {
            logger.Debug("Download chunk: [{0}, {1})", offset, offset + count);

            var request = GetRequest(Config.APIList.DownloadFile,
                new Dictionary<string, string> {
                    ["remote_path"] = Uri.EscapeDataString(path.TrimStart('/'))
                });

            while (true) {
                request.Headers.Range = new RangeHeaderValue(offset, offset + count - 1);
                var response = client.SendAsync(request).Result;
                if (response.IsSuccessStatusCode) {
                    var memoryStream = new MemoryStream(buffer, bufferOffset, count, true);
                    response.Content.ReadAsStreamAsync().Result.CopyTo(memoryStream, count);
                    response.Dispose();
                    if (memoryStream.Position != count) {
                        throw new Exception(
                            $"Unexpected download length ({memoryStream.Position}, should be {count}): {response}");
                    }

                    return count;
                }

                if (response.StatusCode >= HttpStatusCode.MultipleChoices &&
                    response.StatusCode < HttpStatusCode.BadRequest) {
                    logger.Trace($"Redirecting to {response.Headers.Location}");
                    request = new HttpRequestMessage(HttpMethod.Get, response.Headers.Location);
                    response.Dispose();
                } else {
                    logger.Fatal(response.Content.ReadAsStringAsync().Result);
                    throw new Exception($"Unexpected download response: {response}");
                }
            }
        }

        /// <summary>
        ///     Upload data from stream with the optimal method.
        /// </summary>
        /// <param name="path">Path for the destination file.</param>
        /// <param name="stream">Input stream to upload.</param>
        public override void Write(string path, Stream stream) {
            UploadNormal(path, stream);
        }

        public override void Delete(string path) {
            var request = ConstructRequest(Config.APIList.RemovePath,
                new Dictionary<string, string> {
                    ["remote_path"] = Uri.EscapeDataString(path.TrimStart('/'))
                });

            request.GetRequestStream().Close();

            using (var response = request.GetResponse()) {
                if (response.GetDictionary() == null) {
                    throw new InvalidOperationException();
                }
            }
        }

        void UploadNormal(string path, Stream input) {
            var size = input.Length;

            var blockSize = GetBlockSize(size);

            var blockLength = 0;
            var buffer = new byte[blockSize];

            if (blockSize >= size) {
                blockLength = input.Read(buffer, 0, (int) size);
                var uploadDirectDone = false;
                while (!uploadDirectDone) {
                    try {
                        logger.Debug("Upload method: Direct");
                        UploadDirect(path, buffer, 0, blockLength);
                        uploadDirectDone = true;
                    } catch (WebException ex) {
                        logger.Warn($"WebException:\n{0}", ex);
                        if (ex.Response != null) {
                            logger.Warn("Response:");
                            using (var s = new StreamReader(ex.Response.GetResponseStream())) {
                                logger.Warn(s.ReadToEnd());
                            }
                        }

                        Thread.Sleep(TimeSpan.FromSeconds(10));
                    } catch (ObjectDisposedException ex) {
                        logger.Warn("Unexpected ObjectDisposedException:\n{0}", ex);
                        Thread.Sleep(TimeSpan.FromSeconds(10));
                    }
                }

                return;
            }

            logger.Debug("Upload method: Block");

            var blockIds = new List<string>();

            for (long position = 0; position < size; position += blockLength) {
                blockLength = input.Read(buffer, 0, blockSize);

                logger.Debug("Upload block ({0}): [{1}, {2})", position / blockSize, position,
                    position + blockLength);

                var done = false;
                while (!done) {
                    try {
                        blockIds.Add(UploadBlock(buffer, 0, blockLength));
                        logger.Debug("Block ID/MD5: {0}", blockIds.Last());
                        done = true;
                    } catch (WebException ex) {
                        logger.Warn(ex, "WebException");
                        if (ex.Response != null) {
                            logger.Warn("Response:");
                            using (var s = new StreamReader(ex.Response.GetResponseStream())) {
                                logger.Warn(s.ReadToEnd());
                            }
                        }

                        Thread.Sleep(TimeSpan.FromSeconds(10));
                    } catch (ObjectDisposedException ex) {
                        logger.Warn("Unexpected ObjectDisposedException:\n{0}", ex);
                        Thread.Sleep(TimeSpan.FromSeconds(10));
                    } catch (UploadBlockException ex) {
                        logger.Warn("MD5 mismatch:\n{0}", ex);
                        Thread.Sleep(TimeSpan.FromSeconds(10));
                    }
                }
            }

            Retry.Run(() => { MergeBlocks(path, blockIds); },
                (ex, i) => {
                    logger.Warn(ex, "Failed when merging");
                    Thread.Sleep(TimeSpan.FromSeconds(5));
                });
        }

        void UploadDirect(string path, byte[] buffer, int offset, int count) {
            var request = ConstructRequest(Config.APIList.UploadFileDirect,
                new Dictionary<string, string> {
                    ["remote_path"] = Uri.EscapeDataString(path.TrimStart('/'))
                });
            request.Timeout = 30 * 60 * 1000;

            using (var requestStream = request.GetRequestStream()) {
                requestStream.Write(buffer, offset, count);
            }

            using (var response = request.GetResponse()) {
                var result = response.GetDictionary();
                if (!result["path"].ToString().EndsWith(path.TrimStart('/'))) {
                    throw new Exception(
                        $"Direct upload may fail: {path}, real path: {result["path"]}");
                }
            }
        }

        string UploadBlock(byte[] buffer, int offset, int count) {
            var expectedMd5 = new MD5CryptoServiceProvider().ComputeHash(buffer, offset, count)
                .ToHexString().ToLower();
            var request = ConstructRequest(Config.APIList.UploadBlock);
            request.Timeout = 30 * 60 * 1000 * Math.Max(1, (count - 1) >> 25);

            using (var requestStream = request.GetRequestStream()) {
                requestStream.Write(buffer, offset, count);
            }

            using (var response = request.GetResponse()) {
                var actualMd5 = response.GetDictionary()["md5"].ToString();
                if (expectedMd5 != actualMd5) {
                    throw new UploadBlockException {
                        ExpectedMd5 = expectedMd5,
                        ActualMd5 = actualMd5
                    };
                }

                return actualMd5;
            }
        }

        void MergeBlocks(string path, List<string> blockList) {
            var request = ConstructRequest(Config.APIList.MergeBlocks,
                new Dictionary<string, string> {
                    ["remote_path"] = Uri.EscapeDataString(path.TrimStart('/'))
                });

            request.ContentType = "application/x-www-form-urlencoded";

            using (var sw = new StreamWriter(request.GetRequestStream())) {
                sw.Write("param=");
                sw.Write(
                    Uri.EscapeDataString(
                        JsonConvert.SerializeObject(
                            new Dictionary<string, List<string>> {
                                ["block_list"] = blockList
                            }
                        )
                    )
                );
            }

            using (var response = request.GetResponse()) {
                var result = response.GetDictionary();
                if (!result["path"].ToString().EndsWith(path)) {
                    throw new Exception(
                        $"Merge may fail! Original path: {path}, real path: {result["path"]}");
                }
            }
        }

        public void UploadStreamRapid(string path, FileInformation fileInformation,
            Stream input = null) {
            fileInformation.AddProperties(input, FileProperties.AllBaiduCloudRapidHashes);

            var request = ConstructRequest(Config.APIList.UploadFileRapid,
                new Dictionary<string, string> {
                    ["remote_path"] = Uri.EscapeDataString(path.TrimStart('/')),
                    ["content_length"] = fileInformation.Size.ToString(),
                    ["content_md5"] = fileInformation.MD5,
                    ["slice_md5"] = fileInformation.SliceMD5,
                    ["content_crc32"] = fileInformation.Adler32
                });

            request.GetRequestStream().Close();

            using (var response = request.GetResponse()) {
                if (!response.GetDictionary()
                    .Contains(new KeyValuePair<string, object>("md5", fileInformation.MD5))) {
                    throw new Exception("Response is unexpected!");
                }
            }
        }

        public long GetDownloadLength(string path) {
            while (true) {
                var request = GetRequest(Config.APIList.GetFileInfo,
                    new Dictionary<string, string> {
                        ["remote_path"] = Uri.EscapeDataString(path.TrimStart('/'))
                    });

                try {
                    using (var response = client.SendAsync(request).Result) {
                        return (long) response.GetJToken()["list"][0]["size"];
                    }
                } catch (AggregateException ae) {
                    ae.Handle(x => {
                        if (x is HttpRequestException) {
                            logger.Warn(x, "Get download length failed once");
                            Thread.Sleep(TimeSpan.FromSeconds(10));
                            return true;
                        }

                        return false;
                    });
                }
            }
        }

        public override Stream OpenRead(string path)
            => new SeekableReadStream(GetDownloadLength(path),
                (buffer, bufferOffset, offset, count)
                    => Download(buffer, path, bufferOffset, offset, count));

        HttpRequestMessage GetRequest(APIInfo api, Dictionary<string, string> parameters = null) {
            var address = api.Url.Format(parameters).Format(
                new Dictionary<string, string> {
                    ["access_token"] = Account.AccessToken,
                    ["remote_path_prefix"] = Config.RemotePathPrefix
                });

            logger.Trace($"{api.Method} {address}");
            return new HttpRequestMessage(new HttpMethod(api.Method), address);
        }

        HttpWebRequest ConstructRequest(APIInfo api, Dictionary<string, string> parameters = null) {
            var address = api.Url.Format(parameters).Format(
                new Dictionary<string, string> {
                    ["access_token"] = Account.AccessToken,
                    ["remote_path_prefix"] = Config.RemotePathPrefix
                });

            logger.Trace("Constructed address: {0}", address);
            var request = WebRequest.CreateHttp(address);
            //request.ReadWriteTimeout = 300000;
            request.Method = api.Method;

            return request;
        }

        public override IEnumerable<FileInformation> List(string path, bool recursive = false,
            string pattern = "*") {
            var infoRequest = GetRequest(Config.APIList.GetFileInfo,
                new Dictionary<string, string> {
                    ["remote_path"] = Uri.EscapeDataString(path.TrimStart('/'))
                });

            bool needWalk;
            using (var response = client.SendAsync(infoRequest).Result) {
                var result = response.GetJToken();
                if (result["list"] == null) {
                    yield break;
                }

                var info = result["list"][0];
                if ((int) info["isdir"] == 0) {
                    yield break;
                }

                needWalk = (int) info["ifhassubdir"] == 1;
            }

            List<JToken> fileList;

            if (recursive && needWalk) {
                var request = GetRequest(Config.APIList.DiffFileList, new Dictionary<string, string> {
                    ["cursor"] = "null"
                });
                var entries = new Dictionary<string, JToken>();
                JToken result;
                using (var response = client.SendAsync(request).Result) {
                    result = response.GetJToken();
                }

                ProcessDiffResponse(result, entries);

                while ((bool) result["has_more"]) {
                    request = GetRequest(Config.APIList.DiffFileList, new Dictionary<string, string> {
                        ["cursor"] = (string) result["cursor"]
                    });
                    using (var response = client.SendAsync(request).Result) {
                        result = response.GetJToken();
                    }

                    ProcessDiffResponse(result, entries);
                }

                fileList = entries.Values.ToList();
            } else {
                var request = GetRequest(Config.APIList.ListFiles,
                    new Dictionary<string, string> {
                        ["remote_path"] = Uri.EscapeDataString(path.TrimStart('/'))
                    });
                using (var response = client.SendAsync(request).Result) {
                    var result = response.GetJToken();
                    fileList = new List<JToken>(result["list"] ?? Enumerable.Empty<JToken>());
                }
            }

            foreach (var file in fileList.OrderBy(f => f["path"])) {
                if ((int) file["isdir"] == 0) {
                    var id = ((string) file["path"]).Substring(Config.RemotePathPrefix.Length);
                    if (!id.StartsWith(path)) {
                        continue;
                    }

                    yield return new FileInformation {
                        Id = id,
                        Size = (long) file["size"],
                        MD5 = ((string) file["md5"]).ToUpper()
                    };
                }
            }
        }

        void ProcessDiffResponse(JToken result, Dictionary<string, JToken> entries) {
            if ((bool) result["reset"]) {
                entries.Clear();
            }

            foreach (var entry in result["entries"].Values()) {
                if ((int) entry["isdelete"] == 0) {
                    entries[(string) entry["path"]] = entry;
                } else {
                    if ((int) entry["isdir"] == 0) {
                        entries.Remove((string) entry["path"]);
                    } else {
                        var path = (string) entry["path"];
                        var toRemove = entries.Keys.Where(x => x.StartsWith(path)).ToArray();
                        foreach (var key in toRemove) {
                            entries.Remove(key);
                        }
                    }
                }
            }
        }

        public override bool Exists(string path) {
            while (true) {
                var request = GetRequest(Config.APIList.GetFileInfo,
                    new Dictionary<string, string> {
                        ["remote_path"] = Uri.EscapeDataString(path.TrimStart('/'))
                    });

                try {
                    using (var response = client.SendAsync(request).Result) {
                        var responseObject = response.GetJToken();

                        if (responseObject["list"] == null) {
                            return false;
                        }

                        return (long) responseObject["list"][0]["size"] > 0;
                    }
                } catch (Exception ex) {
                    logger.Debug(ex, "Existence test failed");
                    Thread.Sleep(TimeSpan.FromSeconds(5));
                }
            }
        }

        public override FileInformation QuickInfo(string path) {
            var request = ConstructRequest(Config.APIList.GetFileInfo,
                new Dictionary<string, string> {
                    ["remote_path"] = Uri.EscapeDataString(path.TrimStart('/'))
                });

            try {
                using (var response = request.GetResponse()) {
                    var data = response.GetJToken()["list"][0];
                    return new FileInformation {
                        Size = (long) data["size"]
                    };
                }
            } catch (Exception) {
                logger.Warn("Failed to get basic info for {0}", path);
                return new FileInformation();
            }
        }

        public override void Copy(string sourcePath, string destinationPath) {
            var request = ConstructRequest(Config.APIList.CopyFile,
                new Dictionary<string, string> {
                    ["from_remote_path"] = sourcePath.TrimStart('/'),
                    ["to_remote_path"] = destinationPath.TrimStart('/')
                });
            try {
                using (var response = request.GetResponse()) {
                    var value = response.GetJToken();
                    if (!((string) value["extra"]["list"][0]["from"]).EndsWith(sourcePath)) {
                        throw new Exception("from field is incorrect");
                    }

                    if (!((string) value["extra"]["list"][0]["to"]).EndsWith(destinationPath)) {
                        throw new Exception("to field is incorrect");
                    }
                }
            } catch (Exception ex) {
                logger.Warn(ex, "Copy failed!");
                throw;
            }
        }

        public override void Move(string sourcePath, string destinationPath) {
            var request = ConstructRequest(Config.APIList.MoveFile,
                new Dictionary<string, string> {
                    ["from_remote_path"] = sourcePath.TrimStart('/'),
                    ["to_remote_path"] = destinationPath.TrimStart('/')
                });
            try {
                using (var response = request.GetResponse()) {
                    var value = response.GetJToken();
                    if (!((string) value["extra"]["list"][0]["from"]).EndsWith(sourcePath)) {
                        throw new Exception("from field is incorrect");
                    }

                    if (!((string) value["extra"]["list"][0]["to"]).EndsWith(destinationPath)) {
                        throw new Exception("to field is incorrect");
                    }
                }
            } catch (Exception ex) {
                logger.Warn(ex, "Move failed!");
                throw;
            }
        }

        const long MaxBlockCount = 1L << 10;
        const long MaxBlockSize = 2L << 30;
        const long MinBlockSize = 32L << 20;

        static int GetBlockSize(long size) {
            var blockSize = MinBlockSize;

            // Special logic:
            //   1. Reserve one block for header
            //   2. Not stop when equals for the 'padding' logic

            while (blockSize <= MaxBlockSize && blockSize * (MaxBlockCount - 1) <= size) {
                blockSize <<= 1;
            }

            return (int) blockSize;
        }

        public override void Dispose() {
            client?.Dispose();
        }

        class UploadBlockException : Exception {
            public string ExpectedMd5 { get; set; }

            public string ActualMd5 { get; set; }

            public override string ToString()
                => $"Expected md5 is {ExpectedMd5}, while actual md5 is {ActualMd5}.";
        }
    }
}
