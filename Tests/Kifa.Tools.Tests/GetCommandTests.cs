using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CommandLine;
using Kifa.Api.Files;
using Kifa.IO;
using Kifa.IO.StorageClients;
using Kifa.Service;
using Kifa.Tools.FileUtil.Commands;
using Xunit;

namespace Kifa.Tools.Tests;

[Collection("FileStorageTests")]
public class GetCommandTests : IDisposable {
    readonly string tempDir;
    readonly FileInformationServiceClient originalClient;
    readonly KifaServiceClient<FileIdInfo> originalFileIdInfoClient;

    public GetCommandTests() {
        tempDir = Path
            .GetFullPath(Path.Combine(Path.GetTempPath(), $"kifa_get_test_{Guid.NewGuid()}"))
            .Replace('\\', '/');
        Directory.CreateDirectory(tempDir);
        FileStorageClient.ServerConfigs["get_test_dev1"] = new ServerConfig {
            Prefix = $"{tempDir}/dev1"
        };
        FileStorageClient.ServerConfigs["get_test_dev2"] = new ServerConfig {
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
        FileStorageClient.ServerConfigs.Remove("get_test_dev1");
        FileStorageClient.ServerConfigs.Remove("get_test_dev2");
        if (Directory.Exists(tempDir)) {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public void GetFile_SameCell_HardLinksFile() {
        var dev1Dir = $"{tempDir}/dev1";
        var sourcePath = $"{dev1Dir}/source.txt";
        var destPath = $"{dev1Dir}/dest.txt";
        File.WriteAllText(sourcePath, "content");

        var sourceFile = new KifaFile(sourcePath);
        sourceFile.Add();

        // Link dest to source ID in service
        var destFile = new KifaFile(destPath);
        FileInformation.Client.Link(sourceFile.Id, destFile.Id);

        var cmd = new GetCommand();
        var result = cmd.GetFile(destFile);

        Assert.Equal(KifaActionStatus.OK, result.Status);
        Assert.True(File.Exists(destPath));
        Assert.Equal("content", File.ReadAllText(destPath));
    }

    [Fact]
    public void GetFile_NoCopying_CrossDevice_SkipsCopying() {
        var dev1Dir = $"{tempDir}/dev1";
        var dev2Dir = $"{tempDir}/dev2";
        var sourcePath = $"{dev1Dir}/file.txt";
        var destPath = $"{dev2Dir}/file.txt";
        File.WriteAllText(sourcePath, "content");

        var sourceFile = new KifaFile(sourcePath);
        sourceFile.Add();

        var destFile = new KifaFile(destPath);
        // Note: same logical ID /file.txt already registered on dev1

        var cmd = new GetCommand {
            NoCopying = true
        };
        var result = cmd.GetFile(destFile);

        Assert.Equal(KifaActionStatus.Skipped, result.Status);
        Assert.False(File.Exists(destPath));
    }

    [Fact]
    public void GetFile_NoDownloading_CrossDevice_CopiesLocally() {
        var dev1Dir = $"{tempDir}/dev1";
        var dev2Dir = $"{tempDir}/dev2";
        var sourcePath = $"{dev1Dir}/file.txt";
        var destPath = $"{dev2Dir}/file.txt";
        File.WriteAllText(sourcePath, "content");

        var sourceFile = new KifaFile(sourcePath);
        sourceFile.Add();

        var destFile = new KifaFile(destPath);
        var cmd = new GetCommand {
            NoDownloading = true
        };
        var result = cmd.GetFile(destFile);

        Assert.Equal(KifaActionStatus.OK, result.Status);
        Assert.True(File.Exists(destPath));
        Assert.Equal("content", File.ReadAllText(destPath));
    }

    [Fact]
    public void CommandLineParsing_MutuallyExclusiveGroup_OnlyOneAllowed() {
        var parser = new Parser();

        // Providing both --no-copying and --no-downloading fails
        var result1 = parser.ParseArguments<GetCommand>(["file.txt", "-C", "-D"]);
        Assert.IsType<NotParsed<GetCommand>>(result1);

        // Providing both -C and -c fails
        var result2 = parser.ParseArguments<GetCommand>(["file.txt", "-C", "-c", "local"]);
        Assert.IsType<NotParsed<GetCommand>>(result2);

        // Providing both -D and -c fails
        var result3 = parser.ParseArguments<GetCommand>(["file.txt", "-D", "-c", "local"]);
        Assert.IsType<NotParsed<GetCommand>>(result3);

        // Providing individual options succeeds
        var result4 = parser.ParseArguments<GetCommand>(["file.txt", "-C"]);
        Assert.IsType<Parsed<GetCommand>>(result4);
        Assert.True(((Parsed<GetCommand>) result4).Value.NoCopying);

        var result5 = parser.ParseArguments<GetCommand>(["file.txt", "-D"]);
        Assert.IsType<Parsed<GetCommand>>(result5);
        Assert.True(((Parsed<GetCommand>) result5).Value.NoDownloading);

        var result6 = parser.ParseArguments<GetCommand>(["file.txt", "-c", "google,swiss"]);
        Assert.IsType<Parsed<GetCommand>>(result6);
        Assert.Equal("google,swiss", ((Parsed<GetCommand>) result6).Value.AllowedClients);
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
