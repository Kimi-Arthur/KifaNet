using System;
using System.Collections.Generic;
using System.IO;
using Kifa.Api.Files;
using Kifa.IO.StorageClients;
using Kifa.Tools.FileUtil.Commands;
using Xunit;

namespace Kifa.Tools.Tests;

[Collection("FileStorageTests")]
public class TrashCommandTests : IDisposable {
    readonly string tempDir;

    public TrashCommandTests() {
        tempDir = Path.GetFullPath(Path.Combine(Path.GetTempPath(), $"kifa_trash_test_{Guid.NewGuid()}"))
            .Replace('\\', '/');
        Directory.CreateDirectory(tempDir);
        FileStorageClient.ServerConfigs["trash_test_temp"] = new ServerConfig {
            Prefix = tempDir
        };
    }

    public void Dispose() {
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
}
