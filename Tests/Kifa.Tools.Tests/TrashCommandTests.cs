using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Kifa.Api.Files;
using Kifa.IO;
using Kifa.IO.StorageClients;
using Kifa.Service;
using Kifa.Tools.FileUtil.Commands;
using Xunit;

namespace Kifa.Tools.Tests;

[Collection("FileStorageTests")]
public class TrashCommandTests : IDisposable {
    readonly string tempDir;
    readonly FileInformationServiceClient originalClient;
    readonly KifaServiceClient<FileIdInfo> originalFileIdInfoClient;

    public TrashCommandTests() {
        tempDir = Path.GetFullPath(Path.Combine(Path.GetTempPath(), $"kifa_trash_test_{Guid.NewGuid()}"))
            .Replace('\\', '/');
        Directory.CreateDirectory(tempDir);
        FileStorageClient.ServerConfigs["trash_test_temp"] = new ServerConfig {
            Prefix = tempDir
        };
        originalClient = FileInformation.Client;
        FileInformation.Client = new TestFileInformationServiceClient();
        originalFileIdInfoClient = FileIdInfo.Client;
        FileIdInfo.Client = new TestFileIdInfoServiceClient();
    }

    public void Dispose() {
        FileInformation.Client = originalClient;
        FileIdInfo.Client = originalFileIdInfoClient;
        FileStorageClient.ServerConfigs.Remove("trash_test_temp");
        if (Directory.Exists(tempDir)) {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public void GetMostCommonBasePath_EmptyList_ReturnsEmptyString() {
        var result = TrashCommand.GetMostCommonBasePath([]);
        Assert.Equal("", result);
    }

    [Fact]
    public void GetMostCommonBasePath_SingleFile_ReturnsParentName() {
        var files = new List<KifaFile> {
            new("local:trash_test_temp/Anime/Show/01.mkv")
        };
        var result = TrashCommand.GetMostCommonBasePath(files);
        Assert.Equal("Show", result);
    }

    [Fact]
    public void GetMostCommonBasePath_MultipleFilesInSameFolder_ReturnsParentName() {
        var files = new List<KifaFile> {
            new("local:trash_test_temp/Anime/Sousou no Frieren/01.mkv"),
            new("local:trash_test_temp/Anime/Sousou no Frieren/02.mkv"),
            new("local:trash_test_temp/Anime/Sousou no Frieren/03.mkv")
        };
        var result = TrashCommand.GetMostCommonBasePath(files);
        Assert.Equal("Sousou no Frieren", result);
    }

    [Fact]
    public void GetMostCommonBasePath_FilesInSubdirectory_ReturnsSubdirectoryName() {
        var files = new List<KifaFile> {
            new("local:trash_test_temp/Anime/Sousou no Frieren/Season 1/01.mkv"),
            new("local:trash_test_temp/Anime/Sousou no Frieren/Season 1/02.mkv")
        };
        var result = TrashCommand.GetMostCommonBasePath(files);
        Assert.Equal("Season 1", result);
    }

    [Fact]
    public void GetMostCommonBasePath_DifferentSubdirectoriesSameShow_ReturnsShowName() {
        var files = new List<KifaFile> {
            new("local:trash_test_temp/Anime/Sousou no Frieren/Season 1/01.mkv"),
            new("local:trash_test_temp/Anime/Sousou no Frieren/Season 2/01.mkv")
        };
        var result = TrashCommand.GetMostCommonBasePath(files);
        Assert.Equal("Sousou no Frieren", result);
    }

    [Fact]
    public void GetMostCommonBasePath_DifferentFolders_ReturnsCommonParentName() {
        var files = new List<KifaFile> {
            new("local:trash_test_temp/Anime/FrequentShow/01.mkv"),
            new("local:trash_test_temp/Anime/FrequentShow/02.mkv"),
            new("local:trash_test_temp/Anime/FrequentShow/03.mkv"),
            new("local:trash_test_temp/Anime/RareShow/01.mkv")
        };
        var result = TrashCommand.GetMostCommonBasePath(files);
        Assert.Equal("Anime", result);
    }

    [Fact]
    public void GetMostCommonBasePath_FilesInRoot_FallsBackToFileName() {
        var files = new List<KifaFile> {
            new("local:trash_test_temp/01.mkv")
        };
        var result = TrashCommand.GetMostCommonBasePath(files);
        Assert.Equal("01.mkv", result);
    }

    class TestFileInformationServiceClient : BaseKifaServiceClient<FileInformation>,
        FileInformationServiceClient {
        readonly Dictionary<string, FileInformation> data = new();

        public override SortedDictionary<string, FileInformation> List(string folder = "",
            bool recursive = true, KifaDataOptions? options = null)
            => new(data);

        public override FileInformation? Get(string id, KifaDataOptions? options = null)
            => data.GetValueOrDefault(id);

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
