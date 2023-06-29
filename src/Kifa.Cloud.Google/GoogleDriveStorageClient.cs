using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using Kifa.Cloud.Google.Rpcs;
using Kifa.IO;
using Kifa.Service;
using NLog;

namespace Kifa.Cloud.Google;

public class GoogleDriveStorageClient : StorageClient {
    const int BlockSize = 32 << 20;
    static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    public static string RootFolder { get; set; }

    public static APIList APIList { get; set; }

    readonly HttpClient client = new(new HttpClientHandler {
        AllowAutoRedirect = false
    }) {
        Timeout = TimeSpan.FromMinutes(30)
    };

    GoogleAccount? account;

    string? accountId;

    public string AccountId {
        get => accountId;
        set {
            accountId = value;
            account = null;
        }
    }

    // Always a fresh account. The server will determine whether refresh is needed.
    public GoogleAccount Account => GoogleAccount.Client.Get(accountId);

    public override string Type => "google";

    public override string Id => AccountId;

    public override IEnumerable<FileInformation> List(string path, bool recursive = false) {
        var fileId = GetFileId(path);
        if (fileId == null) {
            yield break;
        }

        var pageToken = "";

        while (pageToken != null) {
            var response = client.Call(new ListFilesRpc(parentId: fileId, pageToken: pageToken,
                Account.AccessToken));

            pageToken = response.NextPageToken;
            foreach (var file in response.Files) {
                yield return new FileInformation {
                    Id = $"{path}/{file.Name}",
                    Size = file.Size
                };
            }
        }
    }

    public override long Length(string path) => GetFileSize(GetFileId(path));

    public override void Delete(string path) {
        var fileId = GetFileId(path);
        if (fileId != null) {
            client.Call(new DeleteFileRpc(fileId, Account.AccessToken));
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
        var folderId = GetFileId(path[..path.LastIndexOf('/')], true).Checked();

        var uploadUri = client.Call(new CreateFileRpc(parentId: folderId,
            name: path[(path.LastIndexOf('/') + 1)..], Account.AccessToken));

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
                    if (targetEndByte + 1 == size) {
                        using var response = client.SendWithRetry(()
                            => new HttpRequestMessage(HttpMethod.Put, uploadUri) {
                                Content = content
                            });
                    } else {
                        using var response = client.SendWithRetry(()
                            => new HttpRequestMessage(HttpMethod.Put, uploadUri) {
                                Content = content
                            }, HttpStatusCode.PermanentRedirect);

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
                            Logger.Warn(x, "Temporary upload failure [{0}, {1})", position,
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
            var request = GetRequest(APIList.DownloadFile, new Dictionary<string, string> {
                ["file_id"] = fileId
            });

            request.Headers.Range = new RangeHeaderValue(offset, offset + count - 1);
            return request;
        });
        var memoryStream = new MemoryStream(buffer, bufferOffset, count, true);
        response.Content.ReadAsStreamAsync().Result.CopyTo(memoryStream, count);
        return (int) memoryStream.Position;
    }

    long GetFileSize(string? fileId) {
        if (fileId == null) {
            throw new FileNotFoundException();
        }

        var response = client.FetchJToken(() => GetRequest(APIList.GetFileInfo,
            new Dictionary<string, string> {
                ["file_id"] = fileId
            }));
        var sizeString = (string?) response["size"];
        return sizeString == null ? -1 : long.Parse(sizeString);
    }

    static readonly Dictionary<(string name, string parentId), string> KnownFileIdCache = new();

    string? GetFileId(string path, bool createParents = false) {
        var fileId = "root";
        foreach (var segment in $"{RootFolder}{path}".Split('/',
                     StringSplitOptions.RemoveEmptyEntries)) {
            if (KnownFileIdCache.ContainsKey((segment, fileId))) {
                fileId = KnownFileIdCache[(segment, fileId)];
                continue;
            }

            var newFileId = FetchFileId(segment, fileId);
            if (newFileId != null) {
                fileId = KnownFileIdCache[(segment, fileId)] = newFileId;
                continue;
            }

            if (!createParents) {
                return null;
            }

            fileId = KnownFileIdCache[(segment, fileId)] = CreateFolder(fileId, segment);
        }

        return fileId;
    }

    string? FetchFileId(string segment, string fileId) {
        var response =
            client.Call(new FindFileRpc(parentId: fileId, name: segment, Account.AccessToken));

        return response.Files.Where(file => file.Name == segment).Select(file => file.Id)
            .FirstOrDefault();
    }

    string CreateFolder(string parentId, string name) {
        var token = client.FetchJToken(() => GetRequest(APIList.CreateFolder,
            new Dictionary<string, string> {
                ["parent_id"] = parentId,
                ["name"] = name
            }));
        return (string) token["id"];
    }

    HttpRequestMessage GetRequest(Api api, Dictionary<string, string> parameters) {
        parameters["access_token"] = Account.AccessToken;

        return api.GetRequest(parameters);
    }

    public override void Dispose() {
        client?.Dispose();
    }
}

public class APIList {
    public Api ListFiles { get; set; }
    public Api DeleteFile { get; set; }
    public Api CreateFile { get; set; }
    public Api DownloadFile { get; set; }
    public Api GetFileInfo { get; set; }
    public Api FindFile { get; set; }
    public Api CreateFolder { get; set; }
    public Api OauthRefresh { get; set; }
}
