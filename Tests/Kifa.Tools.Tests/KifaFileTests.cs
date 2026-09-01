using System;
using System.IO;
using Kifa.Api.Files;
using Kifa.IO;
using Kifa.IO.StorageClients;
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
}
