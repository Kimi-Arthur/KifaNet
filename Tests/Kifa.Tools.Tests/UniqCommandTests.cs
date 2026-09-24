using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Kifa.IO;
using Kifa.IO.StorageClients;
using Kifa.Service;
using Kifa.Tools.FileUtil;
using Kifa.Tools.FileUtil.Commands;
using Xunit;

namespace Kifa.Tools.Tests;

[Collection("FileStorageTests")]
public class UniqCommandTests : IDisposable {
    readonly string tempDir;
    readonly FileInformationServiceClient originalClient;
    readonly KifaServiceClient<FileIdInfo> originalFileIdInfoClient;
    readonly List<string> originalDefaultTargets;

    public UniqCommandTests() {
        tempDir = Path.GetFullPath(Path.Combine(Path.GetTempPath(), $"kifa_uniq_test_{Guid.NewGuid()}"))
            .Replace('\\', '/');
        Directory.CreateDirectory(tempDir);
        FileStorageClient.ServerConfigs["uniq_test_temp"] = new ServerConfig {
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
        FileStorageClient.ServerConfigs.Remove("uniq_test_temp");
        if (Directory.Exists(tempDir)) {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public void CommandLineParser_ParsesListFileNamesCorrectly() {
        var parsed = CommandLine.Parser.Default.ParseArguments<UniqCommand>(new[] {
            "path1", "path2", "-i", "-p", "/Anime/Preferred"
        });

        Assert.IsType<CommandLine.Parsed<UniqCommand>>(parsed);
        var cmd = ((CommandLine.Parsed<UniqCommand>) parsed).Value;
        Assert.Equal(new List<string> { "path1", "path2" }, cmd.FileNames);
        Assert.True(cmd.ById);
        Assert.Equal("/Anime/Preferred", cmd.PreferredFolder);
    }

    [Fact]
    public void GetDefaultKeepReply_NoPreferredFolder_ReturnsNull() {
        var files = new List<FileInformation> {
            new() { Id = "/Anime/Show1/01.mp4" },
            new() { Id = "/Anime/Show2/01.mp4" }
        };

        var result = UniqCommand.GetDefaultKeepReply(files, null);
        Assert.Null(result);
    }

    [Fact]
    public void GetDefaultKeepReply_MatchingFolderPrefix_ReturnsFirstIndex() {
        var files = new List<FileInformation> {
            new() { Id = "/Anime/Show1/01.mp4" },
            new() { Id = "/Anime/Show2/01.mp4" }
        };

        var result = UniqCommand.GetDefaultKeepReply(files, "/Anime/Show1");
        Assert.Equal("1", result);
    }

    [Fact]
    public void GetDefaultKeepReply_ExactFileMatch_ReturnsMatchingIndex() {
        var files = new List<FileInformation> {
            new() { Id = "/Anime/Show1/01.mp4" },
            new() { Id = "/Anime/Show2/01.mp4" }
        };

        var result = UniqCommand.GetDefaultKeepReply(files, "/Anime/Show1/01.mp4");
        Assert.Equal("1", result);
    }

    [Fact]
    public void GetDefaultKeepReply_MultipleMatchesInFolder_ReturnsAllIndices() {
        var files = new List<FileInformation> {
            new() { Id = "/Anime/Show1/sub1/01.mp4" },
            new() { Id = "/Anime/Show1/sub2/01.mp4" },
            new() { Id = "/Anime/Show2/01.mp4" }
        };

        var result = UniqCommand.GetDefaultKeepReply(files, "/Anime/Show1");
        Assert.Equal("1,2", result);
    }

    [Fact]
    public void GetDefaultKeepReply_NoMatchesInFolder_ReturnsNull() {
        var files = new List<FileInformation> {
            new() { Id = "/Anime/Show2/01.mp4" },
            new() { Id = "/Anime/Show3/01.mp4" }
        };

        var result = UniqCommand.GetDefaultKeepReply(files, "/Anime/Show1");
        Assert.Null(result);
    }

    [Fact]
    public void GetLogicalId_ById_ValidLogicalId_ReturnsTrimmed() {
        var result = KifaFileCommand.GetLogicalId("/Anime/Show1/", byId: true);
        Assert.Equal("/Anime/Show1", result);
    }

    [Fact]
    public void GetLogicalId_ById_InvalidLogicalId_ThrowsArgumentException() {
        Assert.Throws<ArgumentException>(() =>
            KifaFileCommand.GetLogicalId("Anime/Show1", byId: true));
    }

    [Fact]
    public void GetLogicalId_LocalFolder_ResolvesLogicalId() {
        var folder = $"{tempDir}/Anime/Show1";
        Directory.CreateDirectory(folder);

        var result = KifaFileCommand.GetLogicalId(folder, byId: false);
        Assert.Equal("/Anime/Show1", result);
    }

    [Fact]
    public void GetLogicalId_LocalFolderWithTrailingSlash_ResolvesLogicalIdTrimmed() {
        var folder = $"{tempDir}/Anime/Show1";
        Directory.CreateDirectory(folder);

        var result = KifaFileCommand.GetLogicalId(folder + "/", byId: false);
        Assert.Equal("/Anime/Show1", result);
    }

    [Fact]
    public void Execute_MultipleFolders_DefaultsPreferredFolderToFirstFolder() {
        var folder1 = $"{tempDir}/Anime/Show1";
        var folder2 = $"{tempDir}/Anime/Show2";
        Directory.CreateDirectory(folder1);
        Directory.CreateDirectory(folder2);
        Directory.CreateDirectory($"{tempDir}/$");

        var file1 = $"{folder1}/01.mp4";
        var file2 = $"{folder2}/01.mp4";
        var cloudFile = $"{tempDir}/$/dummy_sha256.v1";
        File.WriteAllText(file1, "duplicate content");
        File.WriteAllText(file2, "duplicate content");
        File.WriteAllText(cloudFile, "cloud content");

        var testClient = (TestFileInformationServiceClient) FileInformation.Client;
        testClient.Set(new FileInformation {
            Id = "/Anime/Show1/01.mp4",
            Sha256 = "dummy_sha256",
            Size = 17,
            Locations = new() {
                ["local:uniq_test_temp/Anime/Show1/01.mp4"] = DateTime.UtcNow,
                ["local:uniq_test_temp/$/dummy_sha256.v1"] = DateTime.UtcNow
            }
        });
        testClient.Set(new FileInformation {
            Id = "/Anime/Show2/01.mp4",
            Sha256 = "dummy_sha256",
            Size = 17,
            Locations = new() {
                ["local:uniq_test_temp/Anime/Show2/01.mp4"] = DateTime.UtcNow
            }
        });

        var cmd = new UniqCommand {
            FileNames = [folder1, folder2],
            AutoConfirmDefault = true
        };

        Assert.Null(cmd.PreferredFolder);

        var exitCode = cmd.Execute();
        Assert.Equal(0, exitCode);
        Assert.Null(cmd.PreferredFolder);

        // File 1 in preferred folder Show1 should be kept, file 2 in Show2 removed
        Assert.NotNull(testClient.Get("/Anime/Show1/01.mp4"));
        Assert.Null(testClient.Get("/Anime/Show2/01.mp4"));
    }

    [Fact]
    public void Execute_SingleFolder_LeavesPreferredFolderNull() {
        var folder1 = $"{tempDir}/Anime/Show1";
        Directory.CreateDirectory(folder1);
        Directory.CreateDirectory($"{tempDir}/$");

        var file1 = $"{folder1}/01.mp4";
        var cloudFile = $"{tempDir}/$/dummy_sha256_single.v1";
        File.WriteAllText(file1, "content");
        File.WriteAllText(cloudFile, "cloud content");

        var testClient = (TestFileInformationServiceClient) FileInformation.Client;
        testClient.Set(new FileInformation {
            Id = "/Anime/Show1/01.mp4",
            Sha256 = "dummy_sha256_single",
            Size = 7,
            Locations = new() {
                ["local:uniq_test_temp/Anime/Show1/01.mp4"] = DateTime.UtcNow,
                ["local:uniq_test_temp/$/dummy_sha256_single.v1"] = DateTime.UtcNow
            }
        });

        var cmd = new UniqCommand {
            FileNames = [folder1],
            AutoConfirmDefault = true
        };

        var exitCode = cmd.Execute();
        Assert.Equal(0, exitCode);
        Assert.Null(cmd.PreferredFolder);
    }

    [Fact]
    public void Execute_ExplicitPreferredFolder_DoesNotOverride() {
        var folder1 = $"{tempDir}/Anime/Show1";
        var folder2 = $"{tempDir}/Anime/Show2";
        var folder3 = $"{tempDir}/Anime/Show3";
        Directory.CreateDirectory(folder1);
        Directory.CreateDirectory(folder2);
        Directory.CreateDirectory(folder3);
        Directory.CreateDirectory($"{tempDir}/$");

        var file1 = $"{folder1}/01.mp4";
        var file2 = $"{folder2}/01.mp4";
        var cloudFile = $"{tempDir}/$/dummy_sha256_override.v1";
        File.WriteAllText(file1, "duplicate content");
        File.WriteAllText(file2, "duplicate content");
        File.WriteAllText(cloudFile, "cloud content");

        var testClient = (TestFileInformationServiceClient) FileInformation.Client;
        testClient.Set(new FileInformation {
            Id = "/Anime/Show1/01.mp4",
            Sha256 = "dummy_sha256_override",
            Size = 17,
            Locations = new() {
                ["local:uniq_test_temp/Anime/Show1/01.mp4"] = DateTime.UtcNow,
                ["local:uniq_test_temp/$/dummy_sha256_override.v1"] = DateTime.UtcNow
            }
        });
        testClient.Set(new FileInformation {
            Id = "/Anime/Show2/01.mp4",
            Sha256 = "dummy_sha256_override",
            Size = 17,
            Locations = new() {
                ["local:uniq_test_temp/Anime/Show2/01.mp4"] = DateTime.UtcNow
            }
        });

        var cmd = new UniqCommand {
            FileNames = [folder1, folder2],
            PreferredFolder = folder3,
            AutoConfirmDefault = true
        };

        var exitCode = cmd.Execute();
        Assert.Equal(0, exitCode);
        Assert.Equal(folder3, cmd.PreferredFolder);
    }

    [Fact]
    public void Execute_ById_MultipleFolders_DefaultsPreferredFolderToFirstFolder() {
        Directory.CreateDirectory($"{tempDir}/$");
        var cloudFile = $"{tempDir}/$/dummy_sha256_byid.v1";
        File.WriteAllText(cloudFile, "cloud content");

        var testClient = (TestFileInformationServiceClient) FileInformation.Client;
        testClient.Set(new FileInformation {
            Id = "/Anime/Show1/01.mp4",
            Sha256 = "dummy_sha256_byid",
            Size = 17,
            Locations = new() {
                ["local:uniq_test_temp/$/dummy_sha256_byid.v1"] = DateTime.UtcNow
            }
        });
        testClient.Set(new FileInformation {
            Id = "/Anime/Show2/01.mp4",
            Sha256 = "dummy_sha256_byid",
            Size = 17,
            Locations = new()
        });

        var cmd = new UniqCommand {
            FileNames = ["/Anime/Show1", "/Anime/Show2"],
            ById = true,
            AutoConfirmDefault = true
        };

        Assert.Null(cmd.PreferredFolder);

        var exitCode = cmd.Execute();
        Assert.Equal(0, exitCode);
        Assert.Null(cmd.PreferredFolder);

        // File 1 in preferred folder Show1 kept, file 2 in Show2 removed
        Assert.NotNull(testClient.Get("/Anime/Show1/01.mp4"));
        Assert.Null(testClient.Get("/Anime/Show2/01.mp4"));
    }

    class TestFileInformationServiceClient : BaseKifaServiceClient<FileInformation>,
        FileInformationServiceClient {
        readonly Dictionary<string, FileInformation> data = new();

        public override SortedDictionary<string, FileInformation> List(string folder = "",
            bool recursive = true, KifaDataOptions? options = null)
            => new(data);

        public override FileInformation? Get(string id, KifaDataOptions? options = null) {
            if (id.StartsWith("/$/")) {
                var sha256 = id[3..];
                return data.Values.FirstOrDefault(f => f.Sha256 == sha256)?.Clone();
            }

            return data.GetValueOrDefault(id)?.Clone();
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
            if (!data.TryGetValue(id, out var info)) {
                return KifaActionResult.Success();
            }

            if (info.Locations != null) {
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
