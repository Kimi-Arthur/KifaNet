using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using Kifa.IO;
using Kifa.Service;
using NLog;
using Pimix;

namespace Kifa.Cloud.Swisscom {
    public class SwisscomStorageClient : StorageClient {
        static readonly Logger logger = LogManager.GetCurrentClassLogger();
        const int BlockSize = 8 << 20;
        const long GraceSize = 10 << 20;
        public const long ShardSize = 1 << 30;

        public static APIList APIList { get; set; }

        static SwisscomConfig config;

        static SwisscomConfig Config =>
            LazyInitializer.EnsureInitialized(ref config, () => SwisscomConfig.Client.Get("default"));

        public SwisscomAccount Account => SwisscomAccount.Client.Get(AccountId);

        public override string Type => "swiss";

        public override string Id => AccountId;

        readonly HttpClient client = new HttpClient();

        public SwisscomStorageClient(string accountId = null) {
            AccountId = accountId;
        }

        public string AccountId { get; set; }

        public override long Length(string path) {
            using var response = client.SendWithRetry(() => APIList.GetFileInfo.GetRequest(
                new Dictionary<string, string> {["file_id"] = GetFileId(path), ["access_token"] = Account.AccessToken}));
            if (response.IsSuccessStatusCode) {
                return response.GetJToken().Value<long>("Length");
            }

            if (response.StatusCode != HttpStatusCode.NotFound) {
                logger.Debug($"Get length failed for {path}, status: {response.StatusCode}");
            }

            return response.StatusCode == HttpStatusCode.NotFound ? 0 : -1;
        }

        public override void Delete(string path) {
            using var response = client.SendWithRetry(() =>
                APIList.DeleteFile.GetRequest(new Dictionary<string, string> {
                    ["file_id"] = GetFileId(path), ["access_token"] = Account.AccessToken
                }));
            if (!response.IsSuccessStatusCode) {
                logger.Debug($"Delete of {path} is not successful, but is ignored.");
            }
        }

        public override void Move(string sourcePath, string destinationPath) {
            using var response = client.SendWithRetry(() => APIList.MoveFile.GetRequest(new Dictionary<string, string> {
                ["from_file_path"] = sourcePath, ["to_file_path"] = destinationPath, ["access_token"] = Account.AccessToken
            }));

            if (!response.IsSuccessStatusCode) {
                logger.Error($"Move from {sourcePath} to {destinationPath} failed: {response}");
            }
        }

        public override void Touch(string path) {
            throw new NotImplementedException();
        }

        public override Stream OpenRead(string path) =>
            new SeekableReadStream(Length(path),
                (buffer, bufferOffset, offset, count) =>
                    Download(buffer, GetFileId(path), bufferOffset, offset, count));

        public override void Write(string path, Stream stream) {
            if (Exists(path)) {
                return;
            }

            var size = stream.Length;
            var buffer = new byte[BlockSize];

            var uploadId = InitUpload(path, size);

            var blockIds = new List<(string etag, int length)>();
            for (long position = 0, blockIndex = 0; position < size; position += BlockSize, blockIndex++) {
                var blockLength = stream.Read(buffer, 0, BlockSize);
                var targetEndByte = position + blockLength - 1;
                var content = new ByteArrayContent(buffer, 0, blockLength);
                content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/octet-stream");
                var uploadRequest = APIList.UploadBlock.GetRequest(new Dictionary<string, string> {
                    ["access_token"] = Account.AccessToken, ["upload_id"] = uploadId, ["block_index"] = blockIndex.ToString(),
                });
                uploadRequest.Content = new MultipartFormDataContent {{content, "files[]", path.Split("/").Last()}};
                uploadRequest.Content.Headers.ContentRange = new ContentRangeHeaderValue(position, targetEndByte, size);
                using var response = client.SendAsync(uploadRequest).Result;
                blockIds.Add((response.GetJToken().Value<string>("ETag")[1..^1], blockLength));
            }

            FinishUpload(uploadId, path, blockIds);
        }

        string InitUpload(string path, long length) {
            using var response = client.SendWithRetry(() => APIList.InitUpload.GetRequest(
                new Dictionary<string, string> {
                    ["file_path"] = path,
                    ["file_length"] = length.ToString(),
                    ["file_guid"] = Guid.NewGuid().ToString().ToUpper(),
                    ["utc_now"] = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
                    ["access_token"] = Account.AccessToken
                }));
            return response.GetJToken().Value<string>("Identifier");
        }

        bool FinishUpload(string uploadId, string path, List<(string etag, int length)> blockIds) {
            var partTemplate = "{\"Index\":{index},\"Length\":{length},\"ETag\":\"\\\"{etag}\\\"\"}";
            using var response = client.SendWithRetry(() => APIList.FinishUpload.GetRequest(
                new Dictionary<string, string> {
                    ["upload_id"] = uploadId,
                    ["access_token"] = Account.AccessToken,
                    ["etag"] = blockIds[0].etag.ParseHexString().ToBase64(),
                    ["file_path"] = path,
                    ["parts"] = "[" + string.Join(",",
                        blockIds.Select((item, index) => partTemplate.Format(new Dictionary<string, string> {
                            ["index"] = index.ToString(), ["length"] = item.length.ToString(), ["etag"] = item.etag
                        }))) + "]"
                }));
            return response.GetJToken().Value<string>("Path").EndsWith(path);
        }

        int Download(byte[] buffer, string fileId, int bufferOffset = 0, long offset = 0, int count = -1) {
            if (count < 0) {
                count = buffer.Length - bufferOffset;
            }

            using var response = client.SendWithRetry(() => {
                var request = APIList.DownloadFile.GetRequest(
                    new Dictionary<string, string> {["file_id"] = fileId, ["access_token"] = Account.AccessToken});

                request.Headers.Range = new RangeHeaderValue(offset, offset + count - 1);
                return request;
            });
            var memoryStream = new MemoryStream(buffer, bufferOffset, count, true);
            response.Content.ReadAsStreamAsync().Result.CopyTo(memoryStream, count);
            return (int) memoryStream.Position;
        }

        public static string FindAccounts(string path, long length) {
            var accounts = Config.StorageMappings.First(mapping => path.StartsWith(mapping.Pattern)).Accounts;
            var selectedAccounts = new List<string>();
            for (var i = 0L; i < length; i += ShardSize) {
                selectedAccounts.Add(FindAccount(accounts, Math.Min(ShardSize, length - i)));
            }

            return string.Join("+", selectedAccounts);
        }

        public static string FindAccount(List<string> accounts, long length) {
            var accountIndex = accounts.FindIndex(s =>
                SwisscomAccount.Client.Get(s).LeftQuota >= length + GraceSize);
            if (accountIndex < 0) {
                throw new InsufficientStorageException();
            }

            accounts.AddRange(accounts.Take(accountIndex + 1));
            accounts.RemoveRange(0, accountIndex + 1);
            return accounts.Last();
        }

        static string GetFileId(string path) => $"/Drive{path}".ToBase64();
    }

    public class APIList {
        public API GetFileInfo { get; set; }
        public API DownloadFile { get; set; }
        public API DeleteFile { get; set; }
        public API MoveFile { get; set; }
        public API InitUpload { get; set; }
        public API UploadBlock { get; set; }
        public API FinishUpload { get; set; }
        public API Quota { get; set; }
    }
}