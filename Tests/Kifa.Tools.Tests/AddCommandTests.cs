using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CommandLine;
using FluentAssertions;
using Kifa.Api.Files;
using Kifa.IO;
using Kifa.IO.StorageClients;
using Kifa.Service;
using Kifa.Tools.FileUtil.Commands;
using Xunit;

namespace Kifa.Tools.Tests;

[Collection("FileStorageTests")]
public class AddCommandTests : IDisposable {
    readonly string tempDir;
    readonly FileInformationServiceClient originalClient;
    readonly KifaServiceClient<FileIdInfo> originalFileIdInfoClient;

    public AddCommandTests() {
        tempDir = Path
            .GetFullPath(Path.Combine(Path.GetTempPath(), $"kifa_add_test_{Guid.NewGuid()}"))
            .Replace('\\', '/');
        Directory.CreateDirectory(tempDir);
        FileStorageClient.ServerConfigs["add_test_temp"] = new ServerConfig {
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
        FileStorageClient.ServerConfigs.Remove("add_test_temp");
        if (Directory.Exists(tempDir)) {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public void CommandLineParser_WithoutOptionA_SetsIncludeAllFalse() {
        var result = Parser.Default.ParseArguments<AddCommand>(new[] { "file1.txt" });
        result.Tag.Should().Be(ParserResultType.Parsed);
        var parsed = ((Parsed<AddCommand>) result).Value;
        parsed.IncludeAll.Should().BeFalse();
        parsed.FileNames.Should().Equal("file1.txt");
    }

    [Theory]
    [InlineData("-a")]
    [InlineData("--include-all")]
    public void CommandLineParser_WithOptionA_SetsIncludeAllTrue(string option) {
        var result = Parser.Default.ParseArguments<AddCommand>(new[] { option, "file1.txt" });
        result.Tag.Should().Be(ParserResultType.Parsed);
        var parsed = ((Parsed<AddCommand>) result).Value;
        parsed.IncludeAll.Should().BeTrue();
        parsed.FileNames.Should().Equal("file1.txt");
    }

    [Fact]
    public void FindFiles_WithoutIncludeAll_IgnoresFiles() {
        var subDir = $"{tempDir}/source";
        Directory.CreateDirectory(subDir);
        File.WriteAllText($"{subDir}/normal.txt", "normal");
        File.WriteAllText($"{subDir}/@ignored.txt", "ignored");

        var cmd = new AddCommand {
            FileNames = [subDir],
            IncludeAll = false
        };

        var files = KifaFile.FindExistingFiles(cmd.FileNames, shouldIgnoreFiles: !cmd.IncludeAll);
        files.Select(f => f.Name).Should().Equal("normal.txt");
    }

    [Fact]
    public void FindFiles_WithIncludeAll_IncludesAllFiles() {
        var subDir = $"{tempDir}/source_all";
        Directory.CreateDirectory(subDir);
        File.WriteAllText($"{subDir}/normal.txt", "normal");
        File.WriteAllText($"{subDir}/@ignored.txt", "ignored");

        var cmd = new AddCommand {
            FileNames = [subDir],
            IncludeAll = true
        };

        var files = KifaFile.FindExistingFiles(cmd.FileNames, shouldIgnoreFiles: !cmd.IncludeAll);
        files.Select(f => f.Name).Should().BeEquivalentTo("@ignored.txt", "normal.txt");
    }

    class TestFileInformationServiceClient : BaseKifaServiceClient<FileInformation>,
        FileInformationServiceClient {
        readonly Dictionary<string, FileInformation> data = new();

        public override SortedDictionary<string, FileInformation> List(string folder = "",
            bool recursive = true, KifaDataOptions? options = null)
            => new(data);

        public override FileInformation? Get(string id, KifaDataOptions? options = null)
            => data.GetValueOrDefault(id);

        public override List<FileInformation?> Get(List<string> ids, KifaDataOptions? options = null)
            => ids.Select(id => data.GetValueOrDefault(id)).ToList();

        public override KifaActionResult Set(FileInformation item) {
            data[item.Id.Checked()] = item;
            return KifaActionResult.Success();
        }

        public override KifaActionResult Update(FileInformation item) {
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
