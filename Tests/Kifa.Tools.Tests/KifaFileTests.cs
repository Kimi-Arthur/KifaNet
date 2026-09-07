using System;
using System.IO;
using Kifa.Api.Files;
using Kifa.IO;
using Kifa.IO.StorageClients;
using Kifa.Service;
using Xunit;

namespace Kifa.Tools.Tests;

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
}
