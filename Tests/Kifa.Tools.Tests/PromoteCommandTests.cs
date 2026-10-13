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
public class PromoteCommandTests : IDisposable {
    readonly string tempDir;
    readonly FileInformationServiceClient originalClient;
    readonly KifaServiceClient<FileIdInfo> originalFileIdInfoClient;

    public PromoteCommandTests() {
        tempDir = Path
            .GetFullPath(Path.Combine(Path.GetTempPath(), $"kifa_promote_test_{Guid.NewGuid()}"))
            .Replace('\\', '/');
        Directory.CreateDirectory(tempDir);
        FileStorageClient.ServerConfigs["promote_test_temp"] = new ServerConfig {
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
        FileStorageClient.ServerConfigs.Remove("promote_test_temp");
        if (Directory.Exists(tempDir)) {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public void CommandLineParser_WithoutOptionS_SetsShowSizeFalse() {
        var result = Parser.Default.ParseArguments<PromoteCommand>(["@file1.txt"]);
        result.Tag.Should().Be(ParserResultType.Parsed);
        var parsed = ((Parsed<PromoteCommand>) result).Value;
        parsed.ShowSize.Should().BeFalse();
        parsed.FileNames.Should().Equal("@file1.txt");
    }

    [Theory]
    [InlineData("-S")]
    [InlineData("--show-size")]
    public void CommandLineParser_WithOptionS_SetsShowSizeTrue(string option) {
        var result = Parser.Default.ParseArguments<PromoteCommand>([option, "@file1.txt", "@file2.txt"]);
        result.Tag.Should().Be(ParserResultType.Parsed);
        var parsed = ((Parsed<PromoteCommand>) result).Value;
        parsed.ShowSize.Should().BeTrue();
        parsed.FileNames.Should().Equal("@file1.txt", "@file2.txt");
    }

    [Fact]
    public void IsIgnoredFile_ReturnsExpectedResults() {
        new KifaFile("local:promote_test_temp/@sample.txt").IsIgnoredFile.Should().BeTrue();
        new KifaFile("local:promote_test_temp/sub/@sample.txt").IsIgnoredFile.Should().BeTrue();
        new KifaFile("local:promote_test_temp/@.hidden").IsIgnoredFile.Should().BeTrue();

        new KifaFile("local:promote_test_temp/sample.txt").IsIgnoredFile.Should().BeFalse();
        new KifaFile("local:promote_test_temp/@").IsIgnoredFile.Should().BeFalse();
    }

    [Fact]
    public void GetPromotedFile_ReturnsExpectedKifaFile() {
        var hiddenFile = new KifaFile("local:promote_test_temp/@sample.txt");
        var promotedFile = hiddenFile.GetPromotedFile();
        promotedFile.ToString().Should().Be("local:promote_test_temp/sample.txt");

        var normalFile = new KifaFile("local:promote_test_temp/sample.txt");
        var action = () => normalFile.GetPromotedFile();
        action.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void PromoteFile_NonIgnoredFile_ReturnsError() {
        var filePath = $"{tempDir}/normal.txt";
        File.WriteAllText(filePath, "content");
        var file = new KifaFile("local:promote_test_temp/normal.txt");

        var result = PromoteCommand.PromoteFile(file);

        result.Status.Should().Be(KifaActionStatus.Error);
        result.Message.Should().Contain("is not an ignored file");
        File.Exists(filePath).Should().BeTrue();
    }

    [Fact]
    public void PromoteFile_SourceDoesNotExist_ReturnsError() {
        var file = new KifaFile("local:promote_test_temp/@nonexistent.txt");

        var result = PromoteCommand.PromoteFile(file);

        result.Status.Should().Be(KifaActionStatus.Error);
        result.Message.Should().Contain("doesn't exist");
    }

    [Fact]
    public void PromoteFile_TargetAlreadyExists_ReturnsError() {
        var sourcePath = $"{tempDir}/@sample.txt";
        var targetPath = $"{tempDir}/sample.txt";
        File.WriteAllText(sourcePath, "hidden content");
        File.WriteAllText(targetPath, "existing target content");

        var file = new KifaFile("local:promote_test_temp/@sample.txt");
        var result = PromoteCommand.PromoteFile(file);

        result.Status.Should().Be(KifaActionStatus.Error);
        result.Message.Should().Contain("already exists");
        File.ReadAllText(sourcePath).Should().Be("hidden content");
        File.ReadAllText(targetPath).Should().Be("existing target content");
    }

    [Fact]
    public void PromoteFile_Success_MovesFileAndPreservesContent() {
        var sourcePath = $"{tempDir}/@sample.txt";
        var targetPath = $"{tempDir}/sample.txt";
        File.WriteAllText(sourcePath, "precious content");

        var file = new KifaFile("local:promote_test_temp/@sample.txt");
        var result = PromoteCommand.PromoteFile(file);

        result.Status.Should().Be(KifaActionStatus.OK);
        result.Message.Should().Contain("Successfully promoted");
        File.Exists(sourcePath).Should().BeFalse();
        File.Exists(targetPath).Should().BeTrue();
        File.ReadAllText(targetPath).Should().Be("precious content");
    }

    [Fact]
    public void PromoteFile_WithRegisteredFile_RegistersTargetAndRemovesOldFileInformation() {
        var sourcePath = $"{tempDir}/@registered.txt";
        var targetPath = $"{tempDir}/registered.txt";
        File.WriteAllText(sourcePath, "registered content");

        var file = new KifaFile("local:promote_test_temp/@registered.txt");
        file.Register(true);
        file.Registered.Should().BeTrue();

        var result = PromoteCommand.PromoteFile(file);

        result.Status.Should().Be(KifaActionStatus.OK);
        File.Exists(sourcePath).Should().BeFalse();
        File.Exists(targetPath).Should().BeTrue();

        FileInformation.Client.Get(file.Id).Should().BeNull();

        var targetFile = file.GetPromotedFile();
        targetFile.Registered.Should().BeTrue();
        var targetInfo = FileInformation.Client.Get(targetFile.Id);
        targetInfo.Should().NotBeNull();
        targetInfo?.Locations?.ContainsKey(file.ToString()).Should().BeFalse();
        targetInfo?.Locations?.ContainsKey(targetFile.ToString()).Should().BeTrue();
        targetInfo?.Locations?[targetFile.ToString()].Should().NotBeNull();
    }

    [Fact]
    public void PromoteFile_UnregisteredFile_DoesNotRegisterTargetFile() {
        var sourcePath = $"{tempDir}/@unregistered.txt";
        var targetPath = $"{tempDir}/unregistered.txt";
        File.WriteAllText(sourcePath, "unregistered content");

        var file = new KifaFile("local:promote_test_temp/@unregistered.txt");
        file.Registered.Should().BeFalse();

        var result = PromoteCommand.PromoteFile(file);

        result.Status.Should().Be(KifaActionStatus.OK);
        File.Exists(sourcePath).Should().BeFalse();
        File.Exists(targetPath).Should().BeTrue();

        var targetFile = file.GetPromotedFile();
        targetFile.Registered.Should().BeFalse();
        FileInformation.Client.Get(file.Id).Should().BeNull();
        FileInformation.Client.Get(targetFile.Id).Should().BeNull();
    }

    [Fact]
    public void Execute_WithHiddenFileSpecified_PromotesFile() {
        var sourcePath = $"{tempDir}/@episode.mkv";
        var targetPath = $"{tempDir}/episode.mkv";
        File.WriteAllText(sourcePath, "video data");

        var originalIn = Console.In;
        try {
            Console.SetIn(new StringReader("y\n"));

            var cmd = new PromoteCommand {
                FileNames = [$"local:promote_test_temp/@episode.mkv"],
                AutoConfirmDefault = true
            };

            var exitCode = cmd.Execute();
            exitCode.Should().Be(0);

            File.Exists(sourcePath).Should().BeFalse();
            File.Exists(targetPath).Should().BeTrue();
            File.ReadAllText(targetPath).Should().Be("video data");
        } finally {
            Console.SetIn(originalIn);
        }
    }

    [Fact]
    public void Execute_WithUnprefixedNameWhenHiddenFileExists_DoesNotPromote() {
        var sourcePath = $"{tempDir}/@document.pdf";
        var targetPath = $"{tempDir}/document.pdf";
        File.WriteAllText(sourcePath, "pdf data");

        var cmd = new PromoteCommand {
            FileNames = [$"local:promote_test_temp/document.pdf"],
            AutoConfirmDefault = true
        };

        var exitCode = cmd.Execute();
        exitCode.Should().Be(0);

        File.Exists(sourcePath).Should().BeTrue();
        File.Exists(targetPath).Should().BeFalse();
    }

    [Fact]
    public void Execute_WithFolderSpecified_PromotesAllHiddenFiles() {
        var subDir = $"{tempDir}/subfolder";
        Directory.CreateDirectory(subDir);
        File.WriteAllText($"{subDir}/@file1.txt", "1");
        File.WriteAllText($"{subDir}/@file2.txt", "2");
        File.WriteAllText($"{subDir}/normal.txt", "normal");

        var originalIn = Console.In;
        try {
            Console.SetIn(new StringReader("y\n"));

            var cmd = new PromoteCommand {
                FileNames = [$"local:promote_test_temp/subfolder"],
                AutoConfirmDefault = true
            };

            var exitCode = cmd.Execute();
            exitCode.Should().Be(0);

            File.Exists($"{subDir}/@file1.txt").Should().BeFalse();
            File.Exists($"{subDir}/@file2.txt").Should().BeFalse();
            File.Exists($"{subDir}/file1.txt").Should().BeTrue();
            File.Exists($"{subDir}/file2.txt").Should().BeTrue();
            File.Exists($"{subDir}/normal.txt").Should().BeTrue();
        } finally {
            Console.SetIn(originalIn);
        }
    }

    [Fact]
    public void Execute_NoHiddenFilesFound_ReturnsCleanSummary() {
        var filePath = $"{tempDir}/normal_only.txt";
        File.WriteAllText(filePath, "hello");

        var cmd = new PromoteCommand {
            FileNames = [$"local:promote_test_temp/normal_only.txt"],
            AutoConfirmDefault = true
        };

        var exitCode = cmd.Execute();
        exitCode.Should().Be(0);
        File.Exists(filePath).Should().BeTrue();
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
            if (data.TryGetValue(targetId, out var targetInfo)) {
                var linkInfo = targetInfo.Clone();
                linkInfo.Id = linkId;
                linkInfo.Locations = targetInfo.Locations;
                data[linkId] = linkInfo;
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
                var linkInfo = targetInfo.Clone();
                linkInfo.Id = linkId;
                data[linkId] = linkInfo;
            }

            return KifaActionResult.Success();
        }
    }
}
