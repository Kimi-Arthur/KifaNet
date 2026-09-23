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
public class CleanCommandTests : IDisposable {
    readonly string tempDir;
    readonly FileInformationServiceClient originalClient;
    readonly KifaServiceClient<FileIdInfo> originalFileIdInfoClient;

    public CleanCommandTests() {
        tempDir = Path.GetFullPath(Path.Combine(Path.GetTempPath(), $"kifa_clean_test_{Guid.NewGuid()}"))
            .Replace('\\', '/');
        Directory.CreateDirectory(tempDir);
        FileStorageClient.ServerConfigs["clean_test_temp"] = new ServerConfig {
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
        FileStorageClient.ServerConfigs.Remove("clean_test_temp");
        if (Directory.Exists(tempDir)) {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public void GetOutsideLinks_CalculatesCorrectly() {
        var filePath = $"{tempDir}/file1.txt";
        File.WriteAllText(filePath, "hello world");
        var file1 = new KifaFile("local:clean_test_temp/file1.txt");

        var group = new[] { file1 }.GroupBy(f => f.FileId).First();
        var outsideLinks = CleanCommand.GetOutsideLinks(group);
        // Since it is a newly created file with 1 OS link and 1 KifaFile in group, outside links is 0.
        Assert.Equal(0, outsideLinks);
    }

    [Fact]
    public void TestCanLink_WritableDirectory_ReturnsTrue() {
        var srcPath = $"{tempDir}/src.txt";
        File.WriteAllText(srcPath, "source content");
        var source = new KifaFile("local:clean_test_temp/src.txt");

        var dstDir = $"{tempDir}/sub";
        Directory.CreateDirectory(dstDir);
        var destination = new KifaFile("local:clean_test_temp/sub/dst.txt");

        var canLink = CleanCommand.TestCanLink(source, destination);
        Assert.True(canLink);
        // Ensure test temp file was cleaned up
        Assert.False(destination.Exists());
    }

    [Fact]
    public void CanGroupReplaceAllOthers_WritableDirectories_ReturnsTrue() {
        var dirA = $"{tempDir}/groupA";
        var dirB = $"{tempDir}/groupB";
        Directory.CreateDirectory(dirA);
        Directory.CreateDirectory(dirB);

        File.WriteAllText($"{dirA}/file.txt", "same content");
        File.WriteAllText($"{dirB}/file.txt", "same content");

        var fileA = new KifaFile("local:clean_test_temp/groupA/file.txt");
        var fileB = new KifaFile("local:clean_test_temp/groupB/file.txt");

        var groups = new List<KifaFile> { fileA, fileB }.GroupBy(f => f.FileId).ToList();
        Assert.Equal(2, groups.Count);

        Assert.True(CleanCommand.CanGroupReplaceAllOthers(groups[0], groups));
        Assert.True(CleanCommand.CanGroupReplaceAllOthers(groups[1], groups));
    }

    [Fact]
    public void DedupFileGroup_NoOutsideLinks_AutoSelectsWithoutPrompt() {
        var originalIn = Console.In;
        try {
            // Provide empty input if anything were to prompt; but with 0 outside links, it shouldn't prompt.
            Console.SetIn(new StringReader(""));

            var dirA = $"{tempDir}/auto_a";
            var dirB = $"{tempDir}/auto_b";
            Directory.CreateDirectory(dirA);
            Directory.CreateDirectory(dirB);

            File.WriteAllText($"{dirA}/file.txt", "dedup content");
            File.WriteAllText($"{dirB}/file.txt", "dedup content");

            var fileA = new KifaFile("local:clean_test_temp/auto_a/file.txt");
            var fileB = new KifaFile("local:clean_test_temp/auto_b/file.txt");

            var groups = new List<KifaFile> { fileA, fileB }.GroupBy(f => f.FileId).ToList();
            Assert.Equal(2, groups.Count);

            var cmd = new CleanCommand();
            var result = cmd.DedupFileGroup(groups);
            Assert.Equal(KifaActionStatus.OK, result.Status);

            // Re-check inodes
            var updatedA = new KifaFile("local:clean_test_temp/auto_a/file.txt");
            var updatedB = new KifaFile("local:clean_test_temp/auto_b/file.txt");
            Assert.Equal(updatedA.FileId, updatedB.FileId);
        } finally {
            Console.SetIn(originalIn);
        }
    }

    [Fact]
    public void DedupFileGroup_WithOutsideLinks_PromptsAndKeepsDefaultCandidate() {
        var originalIn = Console.In;
        try {
            // Enter accepts the default choice [1]
            Console.SetIn(new StringReader("\n"));

            var dirA = $"{tempDir}/outside_a";
            var dirB = $"{tempDir}/outside_b";
            var dirOutside = $"{tempDir}/outside_extra";
            Directory.CreateDirectory(dirA);
            Directory.CreateDirectory(dirB);
            Directory.CreateDirectory(dirOutside);

            var pathA = $"{dirA}/file.txt";
            var pathB = $"{dirB}/file.txt";

            File.WriteAllText(pathA, "dedup outside content");
            File.WriteAllText(pathB, "dedup outside content");

            // Create an outside OS hard link pointing to pathA
            var fileA = new KifaFile("local:clean_test_temp/outside_a/file.txt");
            var fileExtra = new KifaFile("local:clean_test_temp/outside_extra/extra_link.txt");
            fileA.Copy(fileExtra);

            var fileB = new KifaFile("local:clean_test_temp/outside_b/file.txt");

            // groupA only includes fileA (not fileExtra, simulating outside hard link unknown to Kifa)
            var groups = new List<KifaFile> { fileA, fileB }.GroupBy(f => f.FileId).ToList();
            Assert.Equal(2, groups.Count);

            var groupWithOutsideLinks = groups.First(g => CleanCommand.GetOutsideLinks(g) > 0);
            Assert.Equal(1, CleanCommand.GetOutsideLinks(groupWithOutsideLinks));

            var cmd = new CleanCommand();
            var result = cmd.DedupFileGroup(groups);
            Assert.Equal(KifaActionStatus.OK, result.Status);

            // groupA should have been kept, and fileB should now point to fileA's inode
            var updatedA = new KifaFile("local:clean_test_temp/outside_a/file.txt");
            var updatedB = new KifaFile("local:clean_test_temp/outside_b/file.txt");
            Assert.Equal(updatedA.FileId, updatedB.FileId);
            // fileExtra should still point to the same inode
            var updatedExtra = new KifaFile("local:clean_test_temp/outside_extra/extra_link.txt");
            Assert.Equal(updatedA.FileId, updatedExtra.FileId);
        } finally {
            Console.SetIn(originalIn);
        }
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
