using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using CommandLine;
using Kifa.Api.Files;
using Kifa.IO;
using Kifa.IO.StorageClients;
using Kifa.Service;
using Kifa.Tools.FileUtil.Commands;
using Xunit;

namespace Kifa.Tools.Tests;

[Collection("FileStorageTests")]
public class RetrieveCommandTests : IDisposable {
    readonly string tempDir;
    readonly FileInformationServiceClient originalClient;
    readonly KifaServiceClient<FileIdInfo> originalFileIdInfoClient;

    public RetrieveCommandTests() {
        tempDir = Path
            .GetFullPath(Path.Combine(Path.GetTempPath(), $"kifa_retrieve_test_{Guid.NewGuid()}"))
            .Replace('\\', '/');
        Directory.CreateDirectory(tempDir);
        FileStorageClient.ServerConfigs["retrieve_test_dev1"] = new ServerConfig {
            Prefix = $"{tempDir}/dev1"
        };
        FileStorageClient.ServerConfigs["retrieve_test_dev2"] = new ServerConfig {
            Prefix = $"{tempDir}/dev2"
        };
        Directory.CreateDirectory($"{tempDir}/dev1");
        Directory.CreateDirectory($"{tempDir}/dev2");

        originalClient = FileInformation.Client;
        FileInformation.Client = new TestFileInformationServiceClient();
        originalFileIdInfoClient = FileIdInfo.Client;
        FileIdInfo.Client = new TestFileIdInfoServiceClient();
    }

    public void Dispose() {
        FileInformation.Client = originalClient;
        FileIdInfo.Client = originalFileIdInfoClient;
        FileStorageClient.ServerConfigs.Remove("retrieve_test_dev1");
        FileStorageClient.ServerConfigs.Remove("retrieve_test_dev2");
        if (Directory.Exists(tempDir)) {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public void CommandLineParsing_Options_ParsedCorrectly() {
        var parser = new Parser();
        var result = parser.ParseArguments<RetrieveCommand>(
            ["baidu:acc/file.txt", "-m", "local:dev2", "-R", "-a", "-S"]);
        Assert.IsType<Parsed<RetrieveCommand>>(result);
        var cmd = ((Parsed<RetrieveCommand>) result).Value;
        Assert.Equal("local:dev2", cmd.MirrorHost);
        Assert.True(cmd.NoRegister);
        Assert.True(cmd.IncludeAll);
        Assert.True(cmd.ShowSize);
    }

    [Fact]
    public void RetrieveFile_UnregisteredRemoteFile_DownloadsToDefaultMirrorHostAndRegisters() {
        KifaFile.DefaultMirrorHost = "local:retrieve_test_dev1";
        var fakeRemote = new FakeRemoteStorageClient("baidu", "test_acc");
        fakeRemote.AddFile("/folder/remote.txt", "Hello World!",
            md5: "ED076287532E86365E841E92BFC50D8C");

        var remoteFile = new KifaFile("baidu:test_acc/folder/remote.txt") {
            Client = fakeRemote
        };

        var cmd = new RetrieveCommand();
        var result = cmd.RetrieveFile(remoteFile);

        Assert.Equal(KifaActionStatus.OK, result.Status);
        var expectedLocalPath = $"{tempDir}/dev1/folder/remote.txt";
        Assert.True(File.Exists(expectedLocalPath));
        Assert.Equal("Hello World!", File.ReadAllText(expectedLocalPath));

        var info = FileInformation.Client.Get("/folder/remote.txt").Checked();
        Assert.Equal("7F83B1657FF1FC53B92DC18148A1D65DFC2D4B1FA3D677284ADDD200126D9069",
            info.Sha256);
        Assert.NotNull(
            info.Locations.GetValueOrDefault("local:retrieve_test_dev1/folder/remote.txt"));
        Assert.NotNull(info.Locations.GetValueOrDefault("baidu:test_acc/folder/remote.txt"));
    }

    [Fact]
    public void RetrieveFile_UnregisteredRemoteFile_WithMirrorHost_DownloadsToSpecifiedMirrorHost() {
        KifaFile.DefaultMirrorHost = "local:retrieve_test_dev1";
        var fakeRemote = new FakeRemoteStorageClient("baidu", "test_acc");
        fakeRemote.AddFile("/folder/remote2.txt", "Hello World!",
            md5: "ED076287532E86365E841E92BFC50D8C");

        var remoteFile = new KifaFile("baidu:test_acc/folder/remote2.txt") {
            Client = fakeRemote
        };

        var cmd = new RetrieveCommand {
            MirrorHost = "local:retrieve_test_dev2"
        };
        var result = cmd.RetrieveFile(remoteFile);

        Assert.Equal(KifaActionStatus.OK, result.Status);
        Assert.False(File.Exists($"{tempDir}/dev1/folder/remote2.txt"));
        var expectedLocalPath = $"{tempDir}/dev2/folder/remote2.txt";
        Assert.True(File.Exists(expectedLocalPath));
        Assert.Equal("Hello World!", File.ReadAllText(expectedLocalPath));

        var info = FileInformation.Client.Get("/folder/remote2.txt").Checked();
        Assert.NotNull(
            info.Locations.GetValueOrDefault("local:retrieve_test_dev2/folder/remote2.txt"));
        Assert.NotNull(info.Locations.GetValueOrDefault("baidu:test_acc/folder/remote2.txt"));
    }

    [Fact]
    public void RetrieveFile_UnregisteredRemoteFile_WithNoRegister_DownloadsWithoutRegistering() {
        var fakeRemote = new FakeRemoteStorageClient("baidu", "test_acc");
        fakeRemote.AddFile("/folder/unregistered.txt", "Hello World!",
            md5: "ED076287532E86365E841E92BFC50D8C");

        var remoteFile = new KifaFile("baidu:test_acc/folder/unregistered.txt") {
            Client = fakeRemote
        };

        var cmd = new RetrieveCommand {
            MirrorHost = "local:retrieve_test_dev1",
            NoRegister = true
        };
        var result = cmd.RetrieveFile(remoteFile);

        Assert.Equal(KifaActionStatus.OK, result.Status);
        var expectedLocalPath = $"{tempDir}/dev1/folder/unregistered.txt";
        Assert.True(File.Exists(expectedLocalPath));
        Assert.Equal("Hello World!", File.ReadAllText(expectedLocalPath));
        Assert.Null(FileInformation.Client.Get("/folder/unregistered.txt"));

        // Second call when file already exists locally should skip
        var secondResult = cmd.RetrieveFile(remoteFile);
        Assert.Equal(KifaActionStatus.Skipped, secondResult.Status);
        Assert.Null(FileInformation.Client.Get("/folder/unregistered.txt"));
    }

    [Fact]
    public void
        Retrieve_UnregisteredRemoteFile_MatchedByQuickInfo_HardLinksLocalFileWithoutDownloading() {
        KifaFile.DefaultMirrorHost = "local:retrieve_test_dev1";
        var existingLocalPath = $"{tempDir}/dev1/existing.txt";
        File.WriteAllText(existingLocalPath, "Hello World!");
        var existingLocalFile = new KifaFile(existingLocalPath);
        existingLocalFile.Add();

        var fakeRemote = new FakeRemoteStorageClient("baidu", "test_acc", throwOnRead: true);
        fakeRemote.AddFile("/new_link.txt", "Hello World!",
            md5: "ED076287532E86365E841E92BFC50D8C");

        var remoteFile = new KifaFile("baidu:test_acc/new_link.txt") {
            Client = fakeRemote
        };

        var result = remoteFile.Retrieve(confirmPrompt: (_, _) => true);

        Assert.Equal(KifaActionStatus.OK, result.Status);
        Assert.Contains("hard linking", result.Message);
        var targetLocalPath = $"{tempDir}/dev1/new_link.txt";
        Assert.True(File.Exists(targetLocalPath));
        Assert.Equal("Hello World!", File.ReadAllText(targetLocalPath));

        var info = FileInformation.Client.Get("/new_link.txt").Checked();
        Assert.NotNull(info.Locations.GetValueOrDefault("local:retrieve_test_dev1/new_link.txt"));
        Assert.NotNull(info.Locations.GetValueOrDefault("baidu:test_acc/new_link.txt"));
    }

    [Fact]
    public void Retrieve_ExistingLocalTargetWithCorruptedContent_ReturnsError() {
        KifaFile.DefaultMirrorHost = "local:retrieve_test_dev1";
        var existingLocalPath = $"{tempDir}/dev1/corrupted.txt";
        // Same length (12 bytes) as "Hello World!", but different content/MD5
        File.WriteAllText(existingLocalPath, "Wrong World!");

        var fakeRemote = new FakeRemoteStorageClient("baidu", "test_acc");
        fakeRemote.AddFile("/corrupted.txt", "Hello World!",
            md5: "ED076287532E86365E841E92BFC50D8C");

        var remoteFile = new KifaFile("baidu:test_acc/corrupted.txt") {
            Client = fakeRemote
        };

        var result = remoteFile.Retrieve();

        Assert.Equal(KifaActionStatus.Error, result.Status);
        Assert.Contains("Target exists, but doesn't match", result.Message);
    }

    [Fact]
    public void Retrieve_LocalFile_ReturnsError() {
        var localPath = $"{tempDir}/dev1/local.txt";
        File.WriteAllText(localPath, "Hello World!");
        var localFile = new KifaFile(localPath);

        var result = localFile.Retrieve();

        Assert.Equal(KifaActionStatus.Error, result.Status);
        Assert.Contains("Use GetFile instead", result.Message);
    }

    class FakeRemoteStorageClient : StorageClient {
        readonly Dictionary<string, (byte[] bytes, string? md5)> files = new();
        readonly bool throwOnRead;

        public FakeRemoteStorageClient(string type, string id, bool throwOnRead = false) {
            Type = type;
            Id = id;
            this.throwOnRead = throwOnRead;
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

        public override Stream OpenRead(string path) {
            if (throwOnRead) {
                throw new InvalidOperationException("OpenRead should not be called.");
            }

            return files.TryGetValue(path, out var f)
                ? new MemoryStream(f.bytes, writable: false)
                : throw new FileNotFoundException(path);
        }

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
            if (id.StartsWith(FileInformation.VirtualItemPrefix + "md5/")) {
                var md5 = id.Split('/').Last();
                var found = data.Values.FirstOrDefault(f
                    => string.Equals(f.Md5, md5, StringComparison.OrdinalIgnoreCase));
                if (found == null) {
                    return null;
                }

                var clone = found.Clone();
                clone.Metadata ??= new DataMetadata();
                clone.Metadata.Linking = new LinkingMetadata {
                    Target = clone.Id
                };
                clone.Id = id;
                return clone;
            }

            if (id.StartsWith("/$/")) {
                var sha256 = id.Split('/').Last();
                return data.Values.FirstOrDefault(f => f.Sha256 == sha256);
            }

            return data.GetValueOrDefault(id);
        }

        public override KifaActionResult Set(FileInformation item) {
            data[item.Id.Checked()] = item;
            return KifaActionResult.Success();
        }

        public override KifaActionResult Update(FileInformation item) {
            if (data.TryGetValue(item.Id.Checked(), out var existing)) {
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
                data[item.Id.Checked()] = item;
            }
            return KifaActionResult.Success();
        }

        public override KifaActionResult Delete(string id) {
            data.Remove(id);
            return KifaActionResult.Success();
        }

        public override KifaActionResult Link(string targetId, string linkId) {
            var target = Get(targetId);
            if (target != null) {
                var realTarget =
                    data.GetValueOrDefault(target.RealId ?? target.Id.Checked()) ?? target;
                data[linkId] = realTarget;
            }
            return KifaActionResult.Success();
        }

        public List<FolderInfo> GetFolder(string folder, List<string> targets) => [];

        public List<string> ListFolder(string folder, bool recursive = false) {
            if (data.ContainsKey(folder)) {
                return [folder];
            }

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
            data[item.Id.Checked()] = item;
            return KifaActionResult.Success();
        }

        public override KifaActionResult Update(FileIdInfo item) {
            data[item.Id.Checked()] = item;
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
