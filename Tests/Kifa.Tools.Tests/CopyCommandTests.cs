using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Kifa.IO;
using Kifa.IO.StorageClients;
using Kifa.Service;
using Kifa.Tools.FileUtil.Commands;
using Xunit;

namespace Kifa.Tools.Tests;

[Collection("FileStorageTests")]
public class CopyCommandTests : IDisposable {
    readonly string tempDir;
    readonly FileInformationServiceClient originalClient;

    public CopyCommandTests() {
        tempDir = Path
            .GetFullPath(Path.Combine(Path.GetTempPath(), $"kifa_copy_test_{Guid.NewGuid()}"))
            .Replace('\\', '/');
        Directory.CreateDirectory(tempDir);
        FileStorageClient.ServerConfigs["copy_test_temp"] = new ServerConfig {
            Prefix = tempDir
        };
        originalClient = FileInformation.Client;
        FileInformation.Client = new TestFileInformationServiceClient();
    }

    public void Dispose() {
        FileInformation.Client = originalClient;
        FileStorageClient.ServerConfigs.Remove("copy_test_temp");
        if (Directory.Exists(tempDir)) {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public void GetLocalFileCopyPairs_SingleFolderWithoutTrailingSlash_ConsistentWhenDestExists() {
        var sourceDir = $"{tempDir}/source_folder";
        var subDir = $"{sourceDir}/sub";
        Directory.CreateDirectory(subDir);
        File.WriteAllText($"{sourceDir}/file1.txt", "content1");
        File.WriteAllText($"{subDir}/file2.txt", "content2");

        var destDir = $"{tempDir}/dest_folder";

        // First run when destDir does not exist
        var cmd1 = new CopyCommand {
            Files = [sourceDir, destDir]
        };
        var pairs1 = cmd1.GetLocalFileCopyPairs();
        Assert.Equal(2, pairs1.Count);
        Assert.Contains(pairs1,
            p => p.SourceFile.Path == "/source_folder/file1.txt" &&
                 p.DestinationFile.Path == "/dest_folder/file1.txt");
        Assert.Contains(pairs1,
            p => p.SourceFile.Path == "/source_folder/sub/file2.txt" &&
                 p.DestinationFile.Path == "/dest_folder/sub/file2.txt");

        // Simulate destDir existing after first run
        Directory.CreateDirectory(destDir);

        // Second run when destDir exists: should still map to dest_folder/file1.txt (not dest_folder/source_folder/file1.txt)
        var cmd2 = new CopyCommand {
            Files = [sourceDir, destDir]
        };
        var pairs2 = cmd2.GetLocalFileCopyPairs();
        Assert.Equal(2, pairs2.Count);
        Assert.Contains(pairs2,
            p => p.SourceFile.Path == "/source_folder/file1.txt" &&
                 p.DestinationFile.Path == "/dest_folder/file1.txt");
        Assert.Contains(pairs2,
            p => p.SourceFile.Path == "/source_folder/sub/file2.txt" &&
                 p.DestinationFile.Path == "/dest_folder/sub/file2.txt");
    }

    [Fact]
    public void GetLocalFileCopyPairs_SingleFolderWithTrailingSlash_PutsInsideFolder() {
        var sourceDir = $"{tempDir}/source_folder";
        var subDir = $"{sourceDir}/sub";
        Directory.CreateDirectory(subDir);
        File.WriteAllText($"{sourceDir}/file1.txt", "content1");
        File.WriteAllText($"{subDir}/file2.txt", "content2");

        var destDir = $"{tempDir}/dest_folder/";

        var cmd = new CopyCommand {
            Files = [sourceDir, destDir]
        };
        var pairs = cmd.GetLocalFileCopyPairs();
        Assert.Equal(2, pairs.Count);
        Assert.Contains(pairs,
            p => p.SourceFile.Path == "/source_folder/file1.txt" &&
                 p.DestinationFile.Path == "/dest_folder/source_folder/file1.txt");
        Assert.Contains(pairs,
            p => p.SourceFile.Path == "/source_folder/sub/file2.txt" && p.DestinationFile.Path ==
                "/dest_folder/source_folder/sub/file2.txt");
    }

    [Fact]
    public void GetLocalFileCopyPairs_MultipleFolders_PutsInsideFolder() {
        var sourceDir1 = $"{tempDir}/source1";
        var sourceDir2 = $"{tempDir}/source2";
        Directory.CreateDirectory(sourceDir1);
        Directory.CreateDirectory(sourceDir2);
        File.WriteAllText($"{sourceDir1}/file1.txt", "content1");
        File.WriteAllText($"{sourceDir2}/file2.txt", "content2");

        var destDir = $"{tempDir}/dest_folder";

        var cmd = new CopyCommand {
            Files = [sourceDir1, sourceDir2, destDir]
        };
        var pairs = cmd.GetLocalFileCopyPairs();
        Assert.Equal(2, pairs.Count);
        Assert.Contains(pairs,
            p => p.SourceFile.Path == "/source1/file1.txt" &&
                 p.DestinationFile.Path == "/dest_folder/source1/file1.txt");
        Assert.Contains(pairs,
            p => p.SourceFile.Path == "/source2/file2.txt" &&
                 p.DestinationFile.Path == "/dest_folder/source2/file2.txt");
    }

    [Fact]
    public void GetLocalFileCopyPairs_SingleFileWithoutTrailingSlash_CopiesDirectly() {
        var sourceFile = $"{tempDir}/file1.txt";
        File.WriteAllText(sourceFile, "content1");

        var destFile = $"{tempDir}/file2.txt";

        var cmd = new CopyCommand {
            Files = [sourceFile, destFile]
        };
        var pairs = cmd.GetLocalFileCopyPairs();
        Assert.Single(pairs);
        Assert.Equal("/file1.txt", pairs[0].SourceFile.Path);
        Assert.Equal("/file2.txt", pairs[0].DestinationFile.Path);
    }

    [Fact]
    public void GetLocalFileCopyPairs_SingleFileWithTrailingSlash_CopiesIntoFolder() {
        var sourceFile = $"{tempDir}/file1.txt";
        File.WriteAllText(sourceFile, "content1");

        var destFolder = $"{tempDir}/dest_folder/";

        var cmd = new CopyCommand {
            Files = [sourceFile, destFolder]
        };
        var pairs = cmd.GetLocalFileCopyPairs();
        Assert.Single(pairs);
        Assert.Equal("/file1.txt", pairs[0].SourceFile.Path);
        Assert.Equal("/dest_folder/file1.txt", pairs[0].DestinationFile.Path);
    }

    [Fact]
    public void GetIdFileCopyPairs_SingleFolderWithoutTrailingSlash_ConsistentWhenDestExists() {
        var testClient = (TestFileInformationServiceClient) FileInformation.Client;
        testClient.AddFile("/src_folder/file1.txt");
        testClient.AddFile("/src_folder/sub/file2.txt");

        // First run
        var cmd1 = new CopyCommand {
            Files = ["/src_folder", "/dest_folder"],
            ById = true
        };
        var pairs1 = cmd1.GetIdFileCopyPairs();
        Assert.Equal(2, pairs1.Count);
        Assert.Contains(("/src_folder/file1.txt", "/dest_folder/file1.txt"), pairs1);
        Assert.Contains(("/src_folder/sub/file2.txt", "/dest_folder/sub/file2.txt"), pairs1);

        // Simulate dest files existing
        testClient.AddFile("/dest_folder/file1.txt");
        testClient.AddFile("/dest_folder/sub/file2.txt");

        // Second run: should still map to /dest_folder/file1.txt, not /dest_folder/src_folder/file1.txt
        var cmd2 = new CopyCommand {
            Files = ["/src_folder", "/dest_folder"],
            ById = true
        };
        var pairs2 = cmd2.GetIdFileCopyPairs();
        Assert.Equal(2, pairs2.Count);
        Assert.Contains(("/src_folder/file1.txt", "/dest_folder/file1.txt"), pairs2);
        Assert.Contains(("/src_folder/sub/file2.txt", "/dest_folder/sub/file2.txt"), pairs2);
    }

    [Fact]
    public void GetIdFileCopyPairs_SingleFolderWithTrailingSlash_PutsInsideFolder() {
        var testClient = (TestFileInformationServiceClient) FileInformation.Client;
        testClient.AddFile("/src_folder/file1.txt");
        testClient.AddFile("/src_folder/sub/file2.txt");

        var cmd = new CopyCommand {
            Files = ["/src_folder", "/dest_folder/"],
            ById = true
        };
        var pairs = cmd.GetIdFileCopyPairs();
        Assert.Equal(2, pairs.Count);
        Assert.Contains(("/src_folder/file1.txt", "/dest_folder/src_folder/file1.txt"), pairs);
        Assert.Contains(("/src_folder/sub/file2.txt", "/dest_folder/src_folder/sub/file2.txt"),
            pairs);
    }

    class TestFileInformationServiceClient : BaseKifaServiceClient<FileInformation>,
        FileInformationServiceClient {
        readonly Dictionary<string, FileInformation> data = new();

        public void AddFile(string id) {
            data[id] = new FileInformation {
                Id = id
            };
        }

        public override SortedDictionary<string, FileInformation> List(string folder = "",
            bool recursive = true, KifaDataOptions? options = null)
            => new(data);

        public override FileInformation? Get(string id, bool refresh = false, bool rewrite = false,
            KifaDataOptions? options = null)
            => data.GetValueOrDefault(id);

        public override KifaActionResult Set(FileInformation item) {
            data[item.Id!] = item;
            return KifaActionResult.Success();
        }

        public override KifaActionResult Update(FileInformation item) {
            data[item.Id!] = item;
            return KifaActionResult.Success();
        }

        public override KifaActionResult Delete(string id) {
            data.Remove(id);
            return KifaActionResult.Success();
        }

        public override KifaActionResult Link(string targetId, string linkId) {
            return KifaActionResult.Success();
        }

        public List<FolderInfo> GetFolder(string folder, List<string> targets) => [];

        public List<string> ListFolder(string folder, bool recursive = false) {
            folder = folder.TrimEnd('/') + "/";
            return data.Keys.Where(k => k.StartsWith(folder)).ToList();
        }

        public KifaActionResult AddLocation(string id, string location, bool verified = false)
            => KifaActionResult.Success();

        public KifaActionResult RemoveLocation(string id, string location)
            => KifaActionResult.Success();
    }
}
