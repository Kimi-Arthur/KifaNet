using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Kifa.Api.Files;
using Kifa.IO;
using Kifa.IO.StorageClients;
using Kifa.Service;
using Kifa.Tools.FileUtil.Commands;
using Xunit;

namespace Kifa.Tools.Tests;

[Collection("FileStorageTests")]
public class UploadCommandTests : IDisposable {
    readonly string tempDir;
    readonly FileInformationServiceClient originalClient;
    readonly KifaServiceClient<FileIdInfo> originalFileIdInfoClient;
    readonly List<string> originalDefaultTargets;

    public UploadCommandTests() {
        tempDir = Path.GetFullPath(Path.Combine(Path.GetTempPath(), $"kifa_upload_test_{Guid.NewGuid()}"))
            .Replace('\\', '/');
        Directory.CreateDirectory(tempDir);
        FileStorageClient.ServerConfigs["upload_test_temp"] = new ServerConfig {
            Prefix = tempDir
        };
        originalClient = FileInformation.Client;
        FileInformation.Client = new TestFileInformationServiceClient();
        originalFileIdInfoClient = FileIdInfo.Client;
        FileIdInfo.Client = new TestFileIdInfoServiceClient();
        originalDefaultTargets = UploadCommand.DefaultTargets;
        UploadCommand.DefaultTargets = [];
    }

    public void Dispose() {
        UploadCommand.DefaultTargets = originalDefaultTargets;
        FileInformation.Client = originalClient;
        FileIdInfo.Client = originalFileIdInfoClient;
        FileStorageClient.ServerConfigs.Remove("upload_test_temp");
        if (Directory.Exists(tempDir)) {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public void TwoPasses_SkipPotentiallyUploadFiles_PreservesPass1OKResult() {
        var batchResult = new KifaBatchActionResult();
        batchResult.Add("google.v1", new KifaActionResult {
            Status = KifaActionStatus.OK,
            Message = "Uploaded to destination google:cell1/$/sha.v1."
        });
        batchResult.Add("tele.v2", new KifaActionResult {
            Status = KifaActionStatus.Pending,
            Message = "Skipped uploading of file to tele:cell2/$/sha.v2 for now as it's supposed to be already uploaded..."
        });

        Assert.Equal(KifaActionStatus.Pending, batchResult.Status);

        // Simulate pass 2 with SkipPotentiallyUploadFiles = true
        foreach (var item in batchResult.Results) {
            if (item.Result.Status.HasFlag(KifaActionStatus.Pending)) {
                item.Result = new KifaActionResult {
                    Status = KifaActionStatus.Skipped,
                    Message = "File skipped as it's uploaded, though not verified."
                };
            }
        }

        Assert.Equal(KifaActionStatus.OK, batchResult.Status);
        Assert.Equal(KifaActionStatus.OK, batchResult.Results.First(r => r.Item == "google.v1").Result.Status);
        Assert.Equal(KifaActionStatus.Skipped, batchResult.Results.First(r => r.Item == "tele.v2").Result.Status);
    }

    [Fact]
    public void TwoPasses_SkipPotentiallyUploadFiles_AllPendingBecomesSkipped() {
        var batchResult = new KifaBatchActionResult();
        batchResult.Add("google.v1", new KifaActionResult {
            Status = KifaActionStatus.Pending,
            Message = "Skipped uploading of file to google:cell1/$/sha.v1 for now as it's supposed to be already uploaded..."
        });
        batchResult.Add("tele.v2", new KifaActionResult {
            Status = KifaActionStatus.Pending,
            Message = "Skipped uploading of file to tele:cell2/$/sha.v2 for now as it's supposed to be already uploaded..."
        });

        Assert.Equal(KifaActionStatus.Pending, batchResult.Status);

        // Simulate pass 2 with SkipPotentiallyUploadFiles = true
        foreach (var item in batchResult.Results) {
            if (item.Result.Status.HasFlag(KifaActionStatus.Pending)) {
                item.Result = new KifaActionResult {
                    Status = KifaActionStatus.Skipped,
                    Message = "File skipped as it's uploaded, though not verified."
                };
            }
        }

        Assert.Equal(KifaActionStatus.Skipped, batchResult.Status);
        Assert.All(batchResult.Results, r => Assert.Equal(KifaActionStatus.Skipped, r.Result.Status));
    }

    [Fact]
    public void TwoPasses_Pass2VerificationMerge_PreservesPass1OKResult() {
        var batchResult = new KifaBatchActionResult();
        batchResult.Add("google.v1", new KifaActionResult {
            Status = KifaActionStatus.OK,
            Message = "Uploaded to destination google:cell1/$/sha.v1."
        });
        batchResult.Add("tele.v2", new KifaActionResult {
            Status = KifaActionStatus.Pending,
            Message = "Skipped uploading of file to tele:cell2/$/sha.v2 for now as it's supposed to be already uploaded..."
        });

        Assert.Equal(KifaActionStatus.Pending, batchResult.Status);

        // Simulate pass 2 verification result for tele.v2
        var pass2BatchResult = new KifaBatchActionResult();
        pass2BatchResult.Add("tele.v2", new KifaActionResult {
            Status = KifaActionStatus.Skipped,
            Message = "Destination tele:cell2/$/sha.v2 is already uploaded."
        });

        // Merge pass 2 into pass 1 batchResult
        foreach (var (item, itemResult) in pass2BatchResult.Results) {
            var existing = batchResult.Results.FirstOrDefault(r => r.Item == item);
            if (existing != null) {
                existing.Result = itemResult;
            } else {
                batchResult.Add(item, itemResult);
            }
        }

        Assert.Equal(KifaActionStatus.OK, batchResult.Status);
        Assert.Equal(KifaActionStatus.OK, batchResult.Results.First(r => r.Item == "google.v1").Result.Status);
        Assert.Equal(KifaActionStatus.Skipped, batchResult.Results.First(r => r.Item == "tele.v2").Result.Status);
    }

    [Fact]
    public void Upload_SourceWithIncorrectQuickInfoMd5_ReturnsWarningAndRegistersActualHashes() {
        KifaFile.DefaultMirrorHost = "local:upload_test_temp";
        var fakeRemote = new FakeRemoteStorageClient("baidu", "test_acc");
        fakeRemote.AddFile("/folder/wrong_md5_upload.txt", "Hello World!",
            md5: "00000000000000000000000000000000");

        var remoteFile = new KifaFile("baidu:test_acc/folder/wrong_md5_upload.txt") {
            Client = fakeRemote
        };

        var result = remoteFile.Upload(targets: [], useCache: true);

        Assert.Equal(KifaActionStatus.Warning, result.Status);
        var batchResult = Assert.IsType<KifaBatchActionResult>(result);
        var checkItem = Assert.Single(batchResult.Results, r => r.Item == "check");
        Assert.Equal(KifaActionStatus.Warning, checkItem.Result.Status);
        Assert.Contains("differs from quick info MD5", checkItem.Result.Message);

        var info = FileInformation.Client.Get("/folder/wrong_md5_upload.txt").Checked();
        Assert.Equal("ED076287532E86365E841E92BFC50D8C", info.Md5);
        Assert.Equal("7F83B1657FF1FC53B92DC18148A1D65DFC2D4B1FA3D677284ADDD200126D9069",
            info.Sha256);
    }

    class FakeRemoteStorageClient : StorageClient {
        readonly Dictionary<string, (byte[] bytes, string? md5)> files = new();

        public FakeRemoteStorageClient(string type, string id) {
            Type = type;
            Id = id;
        }

        public override string Type { get; }
        public override string Id { get; }

        public void AddFile(string path, string content, string? md5 = null) {
            files[path] = (Encoding.UTF8.GetBytes(content), md5);
        }

        public override long Length(string path)
            => files.TryGetValue(path, out var f)
                ? f.bytes.Length
                : throw new FileNotFoundException(path);

        public override FileInformation? GetQuickInfo(string path)
            => files.TryGetValue(path, out var f)
                ? new FileInformation {
                    Id = path,
                    Size = f.bytes.Length,
                    Md5 = f.md5
                }
                : null;

        public override Stream OpenRead(string path)
            => files.TryGetValue(path, out var f)
                ? new MemoryStream(f.bytes, writable: false)
                : throw new FileNotFoundException(path);

        public override void Write(string path, Stream stream)
            => throw new NotImplementedException();

        public override void Delete(string path) => files.Remove(path);

        public override void Touch(string path) => throw new NotImplementedException();
    }

    class TestFileInformationServiceClient : BaseKifaServiceClient<FileInformation>,
        FileInformationServiceClient {
        readonly Dictionary<string, FileInformation> data = new();

        public override SortedDictionary<string, FileInformation> List(string folder = "",
            bool recursive = true, KifaDataOptions? options = null)
            => new(data);

        public override FileInformation? Get(string id, KifaDataOptions? options = null) {
            if (id.StartsWith("/$/")) {
                var sha256 = id.Split('/').Last();
                return data.Values.FirstOrDefault(f => f.Sha256 == sha256);
            }

            return data.GetValueOrDefault(id);
        }

        public override KifaActionResult Set(FileInformation item) {
            data[item.Id!] = item;
            return KifaActionResult.Success();
        }

        public override KifaActionResult Update(FileInformation item) {
            if (data.TryGetValue(item.Id!, out var existing)) {
                if (item.Sha256 != null) existing.Sha256 = item.Sha256;
                if (item.Md5 != null) existing.Md5 = item.Md5;
                if (item.Sha1 != null) existing.Sha1 = item.Sha1;
                if (item.BlockSha256 != null) existing.BlockSha256 = item.BlockSha256;
                if (item.Size != null) existing.Size = item.Size;
                if (item.EncryptionKey != null) existing.EncryptionKey = item.EncryptionKey;
                if (item.Locations != null) {
                    existing.Locations ??= new();
                    foreach (var loc in item.Locations) {
                        existing.Locations[loc.Key] = loc.Value;
                    }
                }
            } else {
                data[item.Id!] = item;
            }
            return KifaActionResult.Success();
        }

        public override KifaActionResult Delete(string id) {
            data.Remove(id);
            return KifaActionResult.Success();
        }

        public override KifaActionResult Link(string targetId, string linkId) {
            if (data.TryGetValue(targetId, out var targetInfo)) {
                data[linkId] = targetInfo;
            }
            return KifaActionResult.Success();
        }

        public List<FolderInfo> GetFolder(string folder, List<string> targets) => [];

        public List<string> ListFolder(string folder, bool recursive = false) {
            folder = folder.TrimEnd('/') + "/";
            return data.Keys.Where(k => k.StartsWith(folder)).ToList();
        }

        public KifaActionResult AddLocation(string id, string location, bool verified = false) {
            if (!data.TryGetValue(id, out var info)) {
                info = new FileInformation {
                    Id = id
                };
                data[id] = info;
            }

            info.Locations ??= new();
            info.Locations[location] = verified ? DateTime.UtcNow : null;
            return KifaActionResult.Success();
        }

        public KifaActionResult RemoveLocation(string id, string location) {
            if (data.TryGetValue(id, out var info) && info.Locations != null) {
                info.Locations.Remove(location);
            }

            return KifaActionResult.Success();
        }
    }

    class TestFileIdInfoServiceClient : BaseKifaServiceClient<FileIdInfo> {
        readonly Dictionary<string, FileIdInfo> data = new();

        public override SortedDictionary<string, FileIdInfo> List(string folder = "",
            bool recursive = true, KifaDataOptions? options = null)
            => new(data);

        public override FileIdInfo? Get(string id, KifaDataOptions? options = null)
            => data.GetValueOrDefault(id);

        public override KifaActionResult Set(FileIdInfo item) {
            data[item.Id!] = item;
            return KifaActionResult.Success();
        }

        public override KifaActionResult Update(FileIdInfo item) {
            data[item.Id!] = item;
            return KifaActionResult.Success();
        }

        public override KifaActionResult Delete(string id) {
            data.Remove(id);
            return KifaActionResult.Success();
        }

        public override KifaActionResult Link(string targetId, string linkId) {
            if (data.TryGetValue(targetId, out var targetInfo)) {
                data[linkId] = targetInfo;
            }
            return KifaActionResult.Success();
        }
    }
}
