using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Kifa.Api.Files;
using Kifa.IO;
using Kifa.IO.StorageClients;
using Kifa.Service;
using Xunit;

namespace Kifa.Tools.Tests;

[Collection("FileStorageTests")]
public class KifaFileTests : IDisposable {
    readonly string tempDir;

    public KifaFileTests() {
        tempDir = Path.GetFullPath(Path.Combine(Path.GetTempPath(), $"kifa_test_{Guid.NewGuid()}")).Replace('\\', '/');
        Directory.CreateDirectory(tempDir);
        FileStorageClient.ServerConfigs["test_temp"] = new ServerConfig {
            Prefix = tempDir
        };
    }

    public void Dispose() {
        FileStorageClient.ServerConfigs.Remove("test_temp");
        if (Directory.Exists(tempDir)) {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public void IsFolder_Directory_ReturnsTrue() {
        var subDir = $"{tempDir}/test_folder";
        Directory.CreateDirectory(subDir);

        var file = new KifaFile(subDir, fileInfo: new FileInformation());
        Assert.True(file.IsFolder());
        Assert.False(file.Exists());
    }

    [Fact]
    public void IsFolder_DirectoryWithTrailingSlash_ReturnsTrue() {
        var subDir = $"{tempDir}/test_folder";
        Directory.CreateDirectory(subDir);

        var file = new KifaFile($"{subDir}/", fileInfo: new FileInformation());
        Assert.True(file.IsFolder());
        Assert.False(file.Exists());
        Assert.Equal("test_folder", file.Name);
        Assert.Equal("/test_folder", file.Path);
        Assert.Equal("/test_folder", file.Id);
    }

    [Fact]
    public void IsFolder_DirectoryWithMultipleTrailingSlashes_ReturnsTrue() {
        var subDir = $"{tempDir}/test_folder/nested";
        Directory.CreateDirectory(subDir);

        var file = new KifaFile($"local:test_temp/test_folder/nested///", fileInfo: new FileInformation());
        Assert.True(file.IsFolder());
        Assert.False(file.Exists());
        Assert.Equal("nested", file.Name);
        Assert.Equal("/test_folder/nested", file.Path);
        Assert.Equal("/test_folder/nested", file.Id);
        Assert.Equal("/test_folder", file.ParentPath);
        Assert.Equal(new[] { "test_folder", "nested" }, file.PathSegments);
        Assert.Equal("local:test_temp/test_folder/nested", file.ToString());
    }

    [Fact]
    public void Root_WithMultipleTrailingSlashes_NormalizesCorrectly() {
        var root = new KifaFile("local:test_temp///", fileInfo: new FileInformation());
        Assert.Equal("/", root.Path);
        Assert.Equal("/", root.Id);
        Assert.Equal("", root.Name);
        Assert.Equal("local:test_temp/", root.ToString());

        var child = root.GetFile("file.txt", fileInfo: new FileInformation());
        Assert.Equal("/file.txt", child.Path);
        Assert.Equal("local:test_temp/file.txt", child.ToString());
    }

    [Fact]
    public void IsFolder_File_ReturnsFalse() {
        var filePath = $"{tempDir}/test_file.txt";
        File.WriteAllText(filePath, "hello world");

        var file = new KifaFile(filePath, fileInfo: new FileInformation());
        Assert.False(file.IsFolder());
        Assert.True(file.Exists());
    }

    [Fact]
    public void IsFolder_NonExistent_ReturnsFalse() {
        var nonExistent = $"{tempDir}/non_existent";

        var file = new KifaFile(nonExistent, fileInfo: new FileInformation());
        Assert.False(file.IsFolder());
        Assert.False(file.Exists());
    }

    [Fact]
    public void GetFile_NoDoubleSlash_FromFolderWithTrailingSlash() {
        var file = new KifaFile($"local:test_temp/test_folder/", fileInfo: new FileInformation());
        var child = file.GetFile("file.txt", fileInfo: new FileInformation());

        Assert.Equal("/test_folder/file.txt", child.Path);
        Assert.Equal("/test_folder/file.txt", child.Id);
        Assert.Equal("file.txt", child.Name);
        Assert.Equal("local:test_temp/test_folder/file.txt", child.ToString());
    }

    [Fact]
    public void GetFile_NoDoubleSlash_WhenNameHasLeadingSlash() {
        var file = new KifaFile($"local:test_temp/test_folder", fileInfo: new FileInformation());
        var child = file.GetFile("/file.txt", fileInfo: new FileInformation());

        Assert.Equal("/test_folder/file.txt", child.Path);
        Assert.Equal("/test_folder/file.txt", child.Id);
        Assert.Equal("local:test_temp/test_folder/file.txt", child.ToString());
    }

    [Fact]
    public void GetFile_NoDoubleSlash_OnRoot() {
        var root = new KifaFile("local:test_temp/", fileInfo: new FileInformation());
        var child = root.GetFile("file.txt", fileInfo: new FileInformation());

        Assert.Equal("/file.txt", child.Path);
        Assert.Equal("/file.txt", child.Id);
        Assert.Equal("local:test_temp/file.txt", child.ToString());
    }

    [Fact]
    public void NormalizeUri_CollapsesConsecutiveSlashes_InFileUri() {
        var file = new KifaFile("local:test_temp//folder///sub//file.txt", fileInfo: new FileInformation());

        Assert.Equal("/folder/sub/file.txt", file.Path);
        Assert.Equal("/folder/sub/file.txt", file.Id);
        Assert.Equal("file.txt", file.Name);
        Assert.Equal("/folder/sub", file.ParentPath);
        Assert.Equal(new[] { "folder", "sub", "file.txt" }, file.PathSegments);
        Assert.Equal("local:test_temp/folder/sub/file.txt", file.ToString());
    }

    [Fact]
    public void NormalizeUri_CollapsesConsecutiveSlashes_InLocalPath() {
        var localPath = $"{tempDir}//folder///sub//file.txt";
        var file = new KifaFile(localPath, fileInfo: new FileInformation());

        Assert.Equal("/folder/sub/file.txt", file.Path);
        Assert.Equal("/folder/sub/file.txt", file.Id);
        Assert.Equal("file.txt", file.Name);
        Assert.Equal("local:test_temp/folder/sub/file.txt", file.ToString());
    }

    [Fact]
    public void LinkAll_LocalLinking_CreatesLinksAndReturnsSuccess() {
        var sourcePath = $"{tempDir}/source.txt";
        File.WriteAllText(sourcePath, "source content");

        var source = new KifaFile(sourcePath, fileInfo: new FileInformation());
        var link1 = new KifaFile($"{tempDir}/link1.txt", fileInfo: new FileInformation());
        var link2 = new KifaFile($"{tempDir}/link2.txt", fileInfo: new FileInformation());

        var result = KifaFile.LinkAll(source, [source, link1, link2]);
        Assert.Equal(KifaActionStatus.OK, result.Status);
        Assert.Equal("Linked locally 2/2 files.", result.Message);
        Assert.True(link1.Exists());
        Assert.True(link2.Exists());
    }

    [Fact]
    public void LinkAll_LocalLinking_PartialLinks_ReturnsSuccessWithMessage() {
        var sourcePath = $"{tempDir}/source.txt";
        File.WriteAllText(sourcePath, "source content");

        var link1Path = $"{tempDir}/link1.txt";
        File.WriteAllText(link1Path, "source content");

        var source = new KifaFile(sourcePath, fileInfo: new FileInformation());
        var link1 = new KifaFile(link1Path, fileInfo: new FileInformation());
        var link2 = new KifaFile($"{tempDir}/link2.txt", fileInfo: new FileInformation());

        var result = KifaFile.LinkAll(source, [source, link1, link2]);
        Assert.Equal(KifaActionStatus.OK, result.Status);
        Assert.Equal("Linked locally 1/2 files.", result.Message);
        Assert.True(link2.Exists());
    }

    [Fact]
    public void LinkAll_LocalLinking_WhenAllExist_ReturnsSkipped() {
        var sourcePath = $"{tempDir}/source.txt";
        File.WriteAllText(sourcePath, "source content");

        var link1Path = $"{tempDir}/link1.txt";
        File.WriteAllText(link1Path, "source content");

        var source = new KifaFile(sourcePath, fileInfo: new FileInformation());
        var link1 = new KifaFile(link1Path, fileInfo: new FileInformation());

        var result = KifaFile.LinkAll(source, [source, link1]);
        Assert.Equal(KifaActionStatus.Skipped, result.Status);
        Assert.Equal("All 1 files are already linked locally.", result.Message);
    }

    [Fact]
    public void LinkAll_NoTargetFiles_ReturnsSkipped() {
        var sourcePath = $"{tempDir}/source.txt";
        File.WriteAllText(sourcePath, "source content");

        var source = new KifaFile(sourcePath, fileInfo: new FileInformation());

        var result = KifaFile.LinkAll(source, [source]);
        Assert.Equal(KifaActionStatus.Skipped, result.Status);
        Assert.Equal("No files to link locally.", result.Message);
    }

    [Fact]
    public void NormalizeUri_UnknownPath_ThrowsInformativeMessage() {
        var ex = Assert.Throws<FileNotFoundException>(() => new KifaFile("/some/random/unconfigured/path", fileInfo: new FileInformation()));
        Assert.Contains("Path '/some/random/unconfigured/path'", ex.Message);
        Assert.Contains("test_temp", ex.Message);
    }

    [Fact]
    public void NormalizeUri_WithPwdEnvironmentVariable_ResolvesRelativeToPwd() {
        var originalPwd = Environment.GetEnvironmentVariable("PWD");
        try {
            var apparentFolder = $"{tempDir}/category/Show (2024)";
            Environment.SetEnvironmentVariable("PWD", apparentFolder);

            var dotFile = new KifaFile(".", fileInfo: new FileInformation());
            Assert.Equal("/category/Show (2024)", dotFile.Path);
            Assert.Equal("/category/Show (2024)", dotFile.Id);
            Assert.Equal("local:test_temp/category/Show (2024)", dotFile.ToString());

            var childFile = new KifaFile("Episode 01.mkv", fileInfo: new FileInformation());
            Assert.Equal("/category/Show (2024)/Episode 01.mkv", childFile.Path);
            Assert.Equal("/category/Show (2024)/Episode 01.mkv", childFile.Id);
            Assert.Equal("local:test_temp/category/Show (2024)/Episode 01.mkv", childFile.ToString());

            var relativeChild = new KifaFile("./sub/extra.mkv", fileInfo: new FileInformation());
            Assert.Equal("/category/Show (2024)/sub/extra.mkv", relativeChild.Path);

            var parentRelative = new KifaFile("../OtherShow/ep1.mkv", fileInfo: new FileInformation());
            Assert.Equal("/category/OtherShow/ep1.mkv", parentRelative.Path);
        } finally {
            Environment.SetEnvironmentVariable("PWD", originalPwd);
        }
    }

    [Fact]
    public void CalculateInfo_SizeOnly_ReturnsSize() {
        var filePath = $"{tempDir}/info_size_test.txt";
        File.WriteAllText(filePath, "Hello World!");

        var file = new KifaFile(filePath, fileInfo: new FileInformation());
        var info = file.CalculateInfo(FileProperties.Size);

        Assert.Equal(12, info.Size);
        Assert.Null(info.Md5);
        Assert.Null(info.Sha256);
    }

    [Fact]
    public void CalculateInfo_Md5_ReturnsMd5() {
        var filePath = $"{tempDir}/info_md5_test.txt";
        File.WriteAllText(filePath, "Hello World!");

        var file = new KifaFile(filePath, fileInfo: new FileInformation());
        var info = file.CalculateInfo(FileProperties.Md5);

        Assert.Equal("ED076287532E86365E841E92BFC50D8C", info.Md5);
        Assert.Equal(12, info.Size);
        Assert.Null(info.Sha256);
    }

    [Fact]
    public void CalculateInfo_SizeAndMd5_ReturnsBoth() {
        var filePath = $"{tempDir}/info_both_test.txt";
        File.WriteAllText(filePath, "Hello World!");

        var file = new KifaFile(filePath, fileInfo: new FileInformation());
        var info = file.CalculateInfo(FileProperties.Size | FileProperties.Md5);

        Assert.Equal(12, info.Size);
        Assert.Equal("ED076287532E86365E841E92BFC50D8C", info.Md5);
        Assert.Null(info.Sha256);
    }

    [Fact]
    public void List_PreservesSizeAndMd5() {
        var prevClient = FileInformation.Client;
        try {
            FileInformation.Client = new FakeFileInformationServiceClient();
            var subDir = $"{tempDir}/list_test_dir";
            Directory.CreateDirectory(subDir);
            File.WriteAllText($"{subDir}/file1.txt", "Hello World!");

            var dir = new KifaFile(subDir, fileInfo: new FileInformation());
            var listed = dir.List().ToList();

            Assert.Single(listed);
            Assert.NotNull(listed[0].FileInfo);
            Assert.Equal(12, listed[0].FileInfo!.Size);
        } finally {
            FileInformation.Client = prevClient;
        }
    }

    class FakeFileInformationServiceClient : BaseKifaServiceClient<FileInformation>,
        FileInformationServiceClient {
        readonly Dictionary<string, FileInformation> data = new();

        public override SortedDictionary<string, FileInformation> List(string folder = "",
            bool recursive = true, KifaDataOptions? options = null)
            => new(data);

        public override List<FileInformation?> Get(List<string> ids, KifaDataOptions? options = null)
            => ids.Select(id => data.GetValueOrDefault(id)?.Clone()).ToList();

        public override FileInformation? Get(string id, KifaDataOptions? options = null)
            => data.GetValueOrDefault(id)?.Clone();

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
            if (data.TryGetValue(targetId, out var target)) {
                data[linkId] = target;
                return KifaActionResult.Success();
            }

            return new KifaActionResult {
                Status = KifaActionStatus.Error,
                Message = $"Target {targetId} not found"
            };
        }

        public KifaActionResult AddLocation(string id, string location, bool verify = false) {
            if (data.TryGetValue(id, out var info)) {
                info.Locations[location] = DateTime.UtcNow;
                return KifaActionResult.Success();
            }

            return new KifaActionResult {
                Status = KifaActionStatus.Error,
                Message = $"File {id} not found"
            };
        }

        public KifaActionResult RemoveLocation(string id, string location) {
            if (data.TryGetValue(id, out var info)) {
                info.Locations.Remove(location);
                return KifaActionResult.Success();
            }

            return new KifaActionResult {
                Status = KifaActionStatus.Error,
                Message = $"File {id} not found"
            };
        }

        public List<FolderInfo> GetFolder(string folder, List<string> targets) => [];

        public List<string> ListFolder(string folder, bool recursive = false)
            => data.Keys.Where(k => k.StartsWith(folder)).ToList();
    }
}
