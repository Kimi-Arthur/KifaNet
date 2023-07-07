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
using Kifa.IO.FileFormats;
using Kifa.Service;
using NLog;

namespace Kifa.Cloud.Google;

public class GoogleDriveStorageClient : StorageClient, CanCreateStorageClient {
    const int BlockSize = 32 << 20;
    static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    public static string? DefaultCell { get; set; }

    readonly HttpClient client = new(new HttpClientHandler {
        AllowAutoRedirect = false
    }) {
        Timeout = TimeSpan.FromMinutes(30)
    };

    // Always a fresh account. The server will determine whether refresh is needed.
    public GoogleAccount Account => Cell.Data.Checked().Account.Data.Checked();

    Link<GoogleDriveStorageCell> Cell { get; set; }

    public override string Type => "google";

    public override string Id => Cell;

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

    public override void Move(string sourcePath, string destinationPath) {
        var folderId = GetFileId(destinationPath[..destinationPath.LastIndexOf('/')], true)
            .Checked();

        var fileName = destinationPath[(destinationPath.LastIndexOf('/') + 1)..];

        var sourceId = GetFileId(sourcePath);
        if (sourceId == null) {
            throw new ArgumentException($"Source {sourcePath} doesn't exist.");
        }

        client.Call(new MoveFileRpc(sourceId, fileName, folderId, Account.AccessToken));
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

        using var stream =
            client.Call(
                new DownloadFileRpc(fileId, offset, offset + count - 1, Account.AccessToken));

        var memoryStream = new MemoryStream(buffer, bufferOffset, count, true);
        stream.CopyTo(memoryStream, count);
        return (int) memoryStream.Position;
    }

    long GetFileSize(string? fileId) {
        if (fileId == null) {
            throw new FileNotFoundException();
        }

        return client.Call(new GetFileInfoRpc(fileId, Account.AccessToken)).Size;
    }

    string? GetFileId(string path, bool createParents = false) {
        var fileId = Cell.Data.Checked().RootId;
        foreach (var segment in path.Split('/', StringSplitOptions.RemoveEmptyEntries)) {
            var newFileId = FetchFileId(segment, fileId);
            if (newFileId != null) {
                fileId = newFileId;
                continue;
            }

            if (!createParents) {
                return null;
            }

            fileId = CreateFolder(fileId, segment);
        }

        return fileId;
    }

    string? FetchFileId(string segment, string fileId) {
        var response =
            client.Call(new FindFileRpc(parentId: fileId, name: segment, Account.AccessToken));

        return response.Files.Where(file => file.Name == segment).Select(file => file.Id)
            .FirstOrDefault();
    }

    string CreateFolder(string parentId, string name)
        => client.Call(new CreateFolderRpc(parentId: parentId, name: name, Account.AccessToken)).Id;

    public override void Dispose() {
        client?.Dispose();
    }

    public static string CreateLocation(FileInformation fileInfo, KifaFileFormat format) {
        return $"google:{DefaultCell.Checked()}/$/{fileInfo.Sha256}.{format}";
    }

    public static StorageClient Create(string spec)
        => new GoogleDriveStorageClient {
            Cell = spec
        };
}
