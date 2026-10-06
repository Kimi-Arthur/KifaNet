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
    readonly FakeFileInformationServiceClient fakeClient;

    public KifaFileTests() {
        tempDir = Path.GetFullPath(Path.Combine(Path.GetTempPath(), $"kifa_test_{Guid.NewGuid()}")).Replace('\\', '/');
        Directory.CreateDirectory(tempDir);
        FileStorageClient.ServerConfigs["test_temp"] = new ServerConfig {
            Prefix = tempDir
        };
        fakeClient = new FakeFileInformationServiceClient();
        FileInformation.Client = fakeClient;
        FileIdInfo.Client = new FakeFileIdInfoServiceClient();
    }

    public void Dispose() {
        FileStorageClient.ServerConfigs.Remove("test_temp");
        if (Directory.Exists(tempDir)) {
            Directory.Delete(tempDir, recursive: true);
        }

        FileInformation.Client = new FileInformationRestServiceClient();
        FileIdInfo.Client = new KifaServiceRestClient<FileIdInfo>();
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
    public void ExistsSomewhere_LocalFile_ExistsOnDisk_ReturnsTrue() {
        var filePath = $"{tempDir}/valid_file.txt";
        File.WriteAllText(filePath, "hello");

        var file = new KifaFile("local:test_temp/valid_file.txt", fileInfo: new FileInformation {
            Id = "/valid_file.txt",
            Locations = new() {
                ["local:test_temp/valid_file.txt"] = DateTime.UtcNow
            }
        });

        Assert.True(file.ExistsSomewhere());
    }

    [Fact]
    public void ExistsSomewhere_LocalFile_DeletedServer_ReturnsFalse() {
        var file = new KifaFile("local:test_temp/ghost_file.txt", fileInfo: new FileInformation {
            Id = "/ghost_file.txt",
            Locations = new() {
                ["local:deleted_server/ghost_file.txt"] = DateTime.UtcNow
            }
        });

        Assert.False(file.ExistsSomewhere());
    }

    [Fact]
    public void ExistsSomewhere_LocalFile_DeletedFromDisk_ReturnsFalse() {
        var file = new KifaFile("local:test_temp/missing_on_disk.txt", fileInfo: new FileInformation {
            Id = "/missing_on_disk.txt",
            Locations = new() {
                ["local:test_temp/missing_on_disk.txt"] = DateTime.UtcNow
            }
        });

        Assert.False(file.ExistsSomewhere());
    }

    [Fact]
    public void ExistsSomewhere_EmptyLocations_ReturnsFalse() {
        var file = new KifaFile("local:test_temp/empty_locs.txt", fileInfo: new FileInformation {
            Id = "/empty_locs.txt",
            Locations = new()
        });

        Assert.False(file.ExistsSomewhere());
    }

    [Fact]
    public void ExistsSomewhere_UnregisteredLocation_EvenIfExistsOnDisk_ReturnsFalse() {
        var filePath = $"{tempDir}/unregistered_file.txt";
        File.WriteAllText(filePath, "hello");

        var file = new KifaFile("local:test_temp/unregistered_file.txt", fileInfo: new FileInformation {
            Id = "/unregistered_file.txt",
            Locations = new() {
                ["local:test_temp/unregistered_file.txt"] = null
            }
        });

        Assert.False(file.ExistsSomewhere());
    }

    [Fact]
    public void IsCloud_ValidGoogleDriveSha256V1_ReturnsTrue() {
        var file = new KifaFile("google:account/$/e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855.v1",
            fileInfo: new FileInformation());
        Assert.True(file.IsCloud);
    }

    [Fact]
    public void IsCloud_ValidTelegramSha256V2_ReturnsTrue() {
        var file = new KifaFile("tele:cell/$/e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855.v2",
            fileInfo: new FileInformation());
        Assert.True(file.IsCloud);
    }

    [Fact]
    public void IsCloud_Swisscom_ReturnsFalse() {
        var file = new KifaFile("swiss:account/$/e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855.v2",
            fileInfo: new FileInformation());
        Assert.False(file.IsCloud);
    }

    [Fact]
    public void IsCloud_Baidu_ReturnsFalse() {
        var file = new KifaFile("baidu:account/$/e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855.v1",
            fileInfo: new FileInformation());
        Assert.False(file.IsCloud);
    }

    [Fact]
    public void IsCloud_InvalidHashPattern_ReturnsFalse() {
        var file = new KifaFile("google:account/$/not_a_valid_hash.v1",
            fileInfo: new FileInformation());
        Assert.False(file.IsCloud);
    }

    [Fact]
    public void IsCloud_RawFileFormat_ReturnsFalse() {
        var file = new KifaFile("google:account/$/e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855.mp4",
            fileInfo: new FileInformation());
        Assert.False(file.IsCloud);
    }

    [Fact]
    public void IsCloud_LocalFile_ReturnsFalse() {
        var file = new KifaFile("local:test_temp/e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855.v1",
            fileInfo: new FileInformation());
        Assert.False(file.IsCloud);
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

    [Fact]
    public void RemoveLogical_OnlyInstance_WithoutForce_FirstPromptRejected_Skips() {
        var filePath = $"{tempDir}/only_file_reject1.txt";
        File.WriteAllText(filePath, "content");
        fakeClient.Set(new FileInformation {
            Id = "/only_file_reject1.txt",
            Locations = new() {
                [$"local:test_temp/only_file_reject1.txt"] = DateTime.UtcNow
            }
        });

        var prompts = new List<(string prompt, bool suggested)>();
        var result = KifaFile.RemoveLogical("/only_file_reject1.txt", force: false,
            confirmPrompt: (prompt, suggested) => {
                prompts.Add((prompt, suggested));
                return false;
            });

        Assert.Single(prompts);
        Assert.False(prompts[0].suggested);
        Assert.Contains("/only_file_reject1.txt", prompts[0].prompt);
        Assert.Equal(KifaActionStatus.Skipped, result.Status);
        Assert.True(File.Exists(filePath));
        Assert.NotNull(fakeClient.Get("/only_file_reject1.txt"));
    }

    [Fact]
    public void RemoveLogical_OnlyInstance_WithoutForce_SecondPromptRejected_Skips() {
        var filePath = $"{tempDir}/only_file_reject2.txt";
        File.WriteAllText(filePath, "content");
        fakeClient.Set(new FileInformation {
            Id = "/only_file_reject2.txt",
            Locations = new() {
                [$"local:test_temp/only_file_reject2.txt"] = DateTime.UtcNow
            }
        });

        var prompts = new List<(string prompt, bool suggested)>();
        var result = KifaFile.RemoveLogical("/only_file_reject2.txt", force: false,
            confirmPrompt: (prompt, suggested) => {
                prompts.Add((prompt, suggested));
                return prompts.Count == 1; // True for 1st prompt, false for 2nd
            });

        Assert.Equal(2, prompts.Count);
        Assert.False(prompts[0].suggested);
        Assert.True(prompts[1].suggested);
        Assert.Equal(KifaActionStatus.Skipped, result.Status);
        Assert.True(File.Exists(filePath));
        Assert.NotNull(fakeClient.Get("/only_file_reject2.txt"));
    }

    [Fact]
    public void RemoveLogical_OnlyInstance_WithoutForce_BothPromptsConfirmed_RemovesFile() {
        var filePath = $"{tempDir}/only_file_confirm.txt";
        File.WriteAllText(filePath, "content");
        fakeClient.Set(new FileInformation {
            Id = "/only_file_confirm.txt",
            Locations = new() {
                [$"local:test_temp/only_file_confirm.txt"] = DateTime.UtcNow
            }
        });

        var prompts = new List<(string prompt, bool suggested)>();
        var result = KifaFile.RemoveLogical("/only_file_confirm.txt", force: false,
            confirmPrompt: (prompt, suggested) => {
                prompts.Add((prompt, suggested));
                return true;
            });

        Assert.Equal(2, prompts.Count);
        Assert.False(prompts[0].suggested);
        Assert.True(prompts[1].suggested);
        Assert.Equal(KifaActionStatus.OK, result.Status);
        Assert.False(File.Exists(filePath));
        Assert.Null(fakeClient.Get("/only_file_confirm.txt"));
    }

    [Fact]
    public void RemoveLogical_OnlyInstance_WithForce_PromptConfirmed_RemovesFile() {
        var filePath = $"{tempDir}/only_file_force_confirm.txt";
        File.WriteAllText(filePath, "content");
        fakeClient.Set(new FileInformation {
            Id = "/only_file_force_confirm.txt",
            Locations = new() {
                [$"local:test_temp/only_file_force_confirm.txt"] = DateTime.UtcNow
            }
        });

        var prompts = new List<(string prompt, bool suggested)>();
        var result = KifaFile.RemoveLogical("/only_file_force_confirm.txt", force: true,
            confirmPrompt: (prompt, suggested) => {
                prompts.Add((prompt, suggested));
                return true;
            });

        Assert.Single(prompts);
        Assert.True(prompts[0].suggested);
        Assert.Equal(KifaActionStatus.OK, result.Status);
        Assert.False(File.Exists(filePath));
        Assert.Null(fakeClient.Get("/only_file_force_confirm.txt"));
    }

    [Fact]
    public void RemoveLogical_OnlyInstance_WithForce_PromptRejected_Skips() {
        var filePath = $"{tempDir}/only_file_force_reject.txt";
        File.WriteAllText(filePath, "content");
        fakeClient.Set(new FileInformation {
            Id = "/only_file_force_reject.txt",
            Locations = new() {
                [$"local:test_temp/only_file_force_reject.txt"] = DateTime.UtcNow
            }
        });

        var prompts = new List<(string prompt, bool suggested)>();
        var result = KifaFile.RemoveLogical("/only_file_force_reject.txt", force: true,
            confirmPrompt: (prompt, suggested) => {
                prompts.Add((prompt, suggested));
                return false;
            });

        Assert.Single(prompts);
        Assert.True(prompts[0].suggested);
        Assert.Equal(KifaActionStatus.Skipped, result.Status);
        Assert.True(File.Exists(filePath));
        Assert.NotNull(fakeClient.Get("/only_file_force_reject.txt"));
    }

    [Fact]
    public void RemoveInstance_LastInstance_WithoutForce_FirstPromptRejected_Skips() {
        var filePath = $"{tempDir}/single_instance_reject1.txt";
        File.WriteAllText(filePath, "content");
        fakeClient.Set(new FileInformation {
            Id = "/single_instance_reject1.txt",
            Locations = new() {
                [$"local:test_temp/single_instance_reject1.txt"] = DateTime.UtcNow
            }
        });
        var file = new KifaFile(filePath);

        var prompts = new List<(string prompt, bool suggested)>();
        var result = file.RemoveInstance(force: false, confirmPrompt: (prompt, suggested) => {
            prompts.Add((prompt, suggested));
            return false;
        });

        Assert.Single(prompts);
        Assert.False(prompts[0].suggested);
        Assert.Equal(KifaActionStatus.Skipped, result.Status);
        Assert.True(File.Exists(filePath));
        Assert.NotNull(fakeClient.Get("/single_instance_reject1.txt"));
    }

    [Fact]
    public void RemoveInstance_LastInstance_WithoutForce_SecondPromptRejected_Skips() {
        var filePath = $"{tempDir}/single_instance_reject2.txt";
        File.WriteAllText(filePath, "content");
        fakeClient.Set(new FileInformation {
            Id = "/single_instance_reject2.txt",
            Locations = new() {
                [$"local:test_temp/single_instance_reject2.txt"] = DateTime.UtcNow
            }
        });
        var file = new KifaFile(filePath);

        var prompts = new List<(string prompt, bool suggested)>();
        var result = file.RemoveInstance(force: false, confirmPrompt: (prompt, suggested) => {
            prompts.Add((prompt, suggested));
            return prompts.Count == 1; // True for 1st prompt, false for 2nd
        });

        Assert.Equal(2, prompts.Count);
        Assert.False(prompts[0].suggested);
        Assert.True(prompts[1].suggested);
        Assert.Equal(KifaActionStatus.Skipped, result.Status);
        Assert.True(File.Exists(filePath));
        Assert.NotNull(fakeClient.Get("/single_instance_reject2.txt"));
    }

    [Fact]
    public void RemoveInstance_LastInstance_WithoutForce_BothPromptsConfirmed_RemovesFile() {
        var filePath = $"{tempDir}/single_instance_confirm.txt";
        File.WriteAllText(filePath, "content");
        fakeClient.Set(new FileInformation {
            Id = "/single_instance_confirm.txt",
            Locations = new() {
                [$"local:test_temp/single_instance_confirm.txt"] = DateTime.UtcNow
            }
        });
        var file = new KifaFile(filePath);

        var prompts = new List<(string prompt, bool suggested)>();
        var result = file.RemoveInstance(force: false, confirmPrompt: (prompt, suggested) => {
            prompts.Add((prompt, suggested));
            return true;
        });

        Assert.Equal(2, prompts.Count);
        Assert.False(prompts[0].suggested);
        Assert.True(prompts[1].suggested);
        Assert.Equal(KifaActionStatus.OK, result.Status);
        Assert.False(File.Exists(filePath));
        Assert.Null(fakeClient.Get("/single_instance_confirm.txt"));
    }

    [Fact]
    public void RemoveInstance_LastInstance_WithForce_PromptConfirmed_RemovesFile() {
        var filePath = $"{tempDir}/single_instance_force_confirm.txt";
        File.WriteAllText(filePath, "content");
        fakeClient.Set(new FileInformation {
            Id = "/single_instance_force_confirm.txt",
            Locations = new() {
                [$"local:test_temp/single_instance_force_confirm.txt"] = DateTime.UtcNow
            }
        });
        var file = new KifaFile(filePath);

        var prompts = new List<(string prompt, bool suggested)>();
        var result = file.RemoveInstance(force: true, confirmPrompt: (prompt, suggested) => {
            prompts.Add((prompt, suggested));
            return true;
        });

        Assert.Single(prompts);
        Assert.True(prompts[0].suggested);
        Assert.Equal(KifaActionStatus.OK, result.Status);
        Assert.False(File.Exists(filePath));
        Assert.Null(fakeClient.Get("/single_instance_force_confirm.txt"));
    }

    [Fact]
    public void RemoveInstance_LastInstance_WithForce_PromptRejected_Skips() {
        var filePath = $"{tempDir}/single_instance_force_reject.txt";
        File.WriteAllText(filePath, "content");
        fakeClient.Set(new FileInformation {
            Id = "/single_instance_force_reject.txt",
            Locations = new() {
                [$"local:test_temp/single_instance_force_reject.txt"] = DateTime.UtcNow
            }
        });
        var file = new KifaFile(filePath);

        var prompts = new List<(string prompt, bool suggested)>();
        var result = file.RemoveInstance(force: true, confirmPrompt: (prompt, suggested) => {
            prompts.Add((prompt, suggested));
            return false;
        });

        Assert.Single(prompts);
        Assert.True(prompts[0].suggested);
        Assert.Equal(KifaActionStatus.Skipped, result.Status);
        Assert.True(File.Exists(filePath));
        Assert.NotNull(fakeClient.Get("/single_instance_force_reject.txt"));
    }

    [Fact]
    public void Add_WithQuickInfoMd5_Confirmed_LinksAndRegistersWithoutReadingFile() {
        var filePath = $"{tempDir}/quick_md5_confirm.txt";
        File.WriteAllText(filePath, "Hello World!");

        fakeClient.Set(new FileInformation {
            Id = "/target_existing.txt",
            Size = 12,
            Md5 = "ED076287532E86365E841E92BFC50D8C",
            Sha256 = "DUMMY_SHA256_FOR_LINK"
        });

        var file = new KifaFile(filePath, fileInfo: new FileInformation {
            Size = 12,
            Md5 = "ED076287532E86365E841E92BFC50D8C"
        });

        var prompts = new List<(string prompt, bool suggested)>();
        file.Add(confirmPrompt: (prompt, suggested) => {
            prompts.Add((prompt, suggested));
            return true;
        });

        Assert.Single(prompts);
        Assert.True(prompts[0].suggested);
        Assert.Equal(
            $"Confirm linking local:test_temp/quick_md5_confirm.txt to /$/md5/ED/07/ED076287532E86365E841E92BFC50D8C (/target_existing.txt) based on MD5 ED076287532E86365E841E92BFC50D8C and size 12?",
            prompts[0].prompt);
        Assert.True(file.Registered);
        Assert.Equal("DUMMY_SHA256_FOR_LINK", file.FileInfo?.Sha256);
        Assert.NotNull(fakeClient.Get("/quick_md5_confirm.txt"));
    }

    [Fact]
    public void Add_WithQuickInfoMd5_Declined_FallsBackToFullCheck() {
        var filePath = $"{tempDir}/quick_md5_decline.txt";
        File.WriteAllText(filePath, "Hello World!");

        fakeClient.Set(new FileInformation {
            Id = "/target_existing2.txt",
            Size = 12,
            Md5 = "ED076287532E86365E841E92BFC50D8C",
            Sha256 = "DUMMY_SHA256_FOR_LINK"
        });

        var file = new KifaFile(filePath, fileInfo: new FileInformation {
            Size = 12,
            Md5 = "ED076287532E86365E841E92BFC50D8C"
        });

        var prompts = new List<(string prompt, bool suggested)>();
        file.Add(confirmPrompt: (prompt, suggested) => {
            prompts.Add((prompt, suggested));
            return false;
        });

        Assert.Single(prompts);
        Assert.True(prompts[0].suggested);
        Assert.True(file.Registered);
        // Full check calculates the actual SHA256 of "Hello World!"
        Assert.Equal("7F83B1657FF1FC53B92DC18148A1D65DFC2D4B1FA3D677284ADDD200126D9069",
            file.FileInfo?.Sha256);
    }

    [Fact]
    public void Add_WithQuickInfoMd5_NoPrompt_FallsBackToFullCheck() {
        var filePath = $"{tempDir}/quick_md5_noprompt.txt";
        File.WriteAllText(filePath, "Hello World!");

        fakeClient.Set(new FileInformation {
            Id = "/target_existing3.txt",
            Size = 12,
            Md5 = "ED076287532E86365E841E92BFC50D8C",
            Sha256 = "DUMMY_SHA256_FOR_LINK"
        });

        var file = new KifaFile(filePath, fileInfo: new FileInformation {
            Size = 12,
            Md5 = "ED076287532E86365E841E92BFC50D8C"
        });

        file.Add();

        Assert.True(file.Registered);
        Assert.Equal("7F83B1657FF1FC53B92DC18148A1D65DFC2D4B1FA3D677284ADDD200126D9069",
            file.FileInfo?.Sha256);
    }

    [Fact]
    public void Add_WithQuickInfoMd5_SizeMismatch_FallsBackToFullCheck() {
        var filePath = $"{tempDir}/quick_md5_sizemismatch.txt";
        File.WriteAllText(filePath, "Hello World!");

        fakeClient.Set(new FileInformation {
            Id = "/target_existing4.txt",
            Size = 999, // Mismatched size
            Md5 = "ED076287532E86365E841E92BFC50D8C",
            Sha256 = "DUMMY_SHA256_FOR_LINK"
        });

        var file = new KifaFile(filePath, fileInfo: new FileInformation {
            Size = 12,
            Md5 = "ED076287532E86365E841E92BFC50D8C"
        });

        var prompts = new List<(string prompt, bool suggested)>();
        file.Add(confirmPrompt: (prompt, suggested) => {
            prompts.Add((prompt, suggested));
            return true;
        });

        Assert.Empty(prompts);
        Assert.True(file.Registered);
        Assert.Equal("7F83B1657FF1FC53B92DC18148A1D65DFC2D4B1FA3D677284ADDD200126D9069",
            file.FileInfo?.Sha256);
    }

    [Fact]
    public void Add_WithQuickInfoMd5_NullSize_FallsBackToFullCheck() {
        var filePath = $"{tempDir}/quick_md5_nullsize.txt";
        File.WriteAllText(filePath, "Hello World!");

        fakeClient.Set(new FileInformation {
            Id = "/target_existing_nullsize.txt",
            Size = 12,
            Md5 = "ED076287532E86365E841E92BFC50D8C",
            Sha256 = "DUMMY_SHA256_FOR_LINK"
        });

        // FileInfo has Md5 but Size is null
        var file = new KifaFile(filePath, fileInfo: new FileInformation {
            Md5 = "ED076287532E86365E841E92BFC50D8C"
        });

        var prompts = new List<(string prompt, bool suggested)>();
        file.Add(confirmPrompt: (prompt, suggested) => {
            prompts.Add((prompt, suggested));
            return true;
        });

        Assert.Empty(prompts);
        Assert.True(file.Registered);
        Assert.Equal("7F83B1657FF1FC53B92DC18148A1D65DFC2D4B1FA3D677284ADDD200126D9069",
            file.FileInfo?.Sha256);
    }

    [Fact]
    public void Add_WithQuickInfoMd5_ForceRecheck_SkipsQuickInfo() {
        var filePath = $"{tempDir}/quick_md5_force.txt";
        File.WriteAllText(filePath, "Hello World!");

        fakeClient.Set(new FileInformation {
            Id = "/target_existing5.txt",
            Size = 12,
            Md5 = "ED076287532E86365E841E92BFC50D8C",
            Sha256 = "DUMMY_SHA256_FOR_LINK"
        });

        var file = new KifaFile(filePath, fileInfo: new FileInformation {
            Size = 12,
            Md5 = "ED076287532E86365E841E92BFC50D8C"
        });

        var prompts = new List<(string prompt, bool suggested)>();
        file.Add(shouldCheckKnown: true, confirmPrompt: (prompt, suggested) => {
            prompts.Add((prompt, suggested));
            return true;
        });

        Assert.Empty(prompts);
        Assert.True(file.Registered);
        Assert.Equal("7F83B1657FF1FC53B92DC18148A1D65DFC2D4B1FA3D677284ADDD200126D9069",
            file.FileInfo?.Sha256);
    }

    [Fact]
    public void Add_WithKnownFileInfoSha256_SkipsQuickInfoMd5_UsesNormalSha256Matching() {
        var filePath = $"{tempDir}/quick_md5_known_sha256.txt";
        File.WriteAllText(filePath, "Hello World!");

        fakeClient.Set(new FileInformation {
            Id = "/target_existing6.txt",
            Size = 12,
            Md5 = "ED076287532E86365E841E92BFC50D8C",
            Sha256 = "DUMMY_SHA256_FOR_LINK"
        });

        // File has known Sha256 in FileInfo
        var file = new KifaFile(filePath, fileInfo: new FileInformation {
            Size = 12,
            Md5 = "ED076287532E86365E841E92BFC50D8C",
            Sha256 = "7F83B1657FF1FC53B92DC18148A1D65DFC2D4B1FA3D677284ADDD200126D9069"
        });

        var prompts = new List<(string prompt, bool suggested)>();
        file.Add(confirmPrompt: (prompt, suggested) => {
            prompts.Add((prompt, suggested));
            return true;
        });

        Assert.Empty(prompts);
        Assert.True(file.Registered);
        Assert.Equal("7F83B1657FF1FC53B92DC18148A1D65DFC2D4B1FA3D677284ADDD200126D9069",
            file.FileInfo?.Sha256);
    }

    class FakeFileInformationServiceClient : BaseKifaServiceClient<FileInformation>,
        FileInformationServiceClient {
        readonly Dictionary<string, FileInformation> data = new();

        public override SortedDictionary<string, FileInformation> List(string folder = "",
            bool recursive = true, KifaDataOptions? options = null)
            => new(data);

        public override List<FileInformation?> Get(List<string> ids, KifaDataOptions? options = null)
            => ids.Select(id => data.GetValueOrDefault(id)?.Clone()).ToList();

        public override FileInformation? Get(string id, KifaDataOptions? options = null) {
            if (data.TryGetValue(id, out var info)) {
                return info.Clone();
            }

            if (id.StartsWith(FileInformation.VirtualItemPrefix + "md5/")) {
                var md5 = id.Split('/').Last();
                var found = data.Values.FirstOrDefault(f =>
                    string.Equals(f.Md5, md5, StringComparison.OrdinalIgnoreCase));
                if (found == null) {
                    return null;
                }

                var clone = found.Clone();
                clone.Metadata ??= new DataMetadata();
                clone.Metadata.Linking = new LinkingMetadata {
                    Target = clone.Id
                };
                clone.Id = id;
                return clone;
            }

            if (id.StartsWith(FileInformation.VirtualItemPrefix + "sha256/")) {
                var sha256 = id.Split('/').Last();
                var found = data.Values.FirstOrDefault(f =>
                    string.Equals(f.Sha256, sha256, StringComparison.OrdinalIgnoreCase));
                if (found == null) {
                    return null;
                }

                var clone = found.Clone();
                clone.Metadata ??= new DataMetadata();
                clone.Metadata.Linking = new LinkingMetadata {
                    Target = clone.Id
                };
                clone.Id = id;
                return clone;
            }

            return null;
        }

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
            var target = Get(targetId);
            if (target != null) {
                var realTarget = data.GetValueOrDefault(target.RealId ?? target.Id!) ?? target;
                data[linkId] = realTarget;
                return KifaActionResult.Success();
            }

            return new KifaActionResult {
                Status = KifaActionStatus.Error,
                Message = $"Target {targetId} not found"
            };
        }

        public KifaActionResult AddLocation(string id, string location, bool verify = false) {
            if (data.TryGetValue(id, out var info)) {
                info.Locations ??= new();
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
                info.Locations?.Remove(location);
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

    class FakeFileIdInfoServiceClient : BaseKifaServiceClient<FileIdInfo> {
        readonly Dictionary<string, FileIdInfo> data = new();

        public override SortedDictionary<string, FileIdInfo> List(string folder = "",
            bool recursive = true, KifaDataOptions? options = null)
            => new(data);

        public override FileIdInfo? Get(string id, KifaDataOptions? options = null)
            => data.GetValueOrDefault(id)?.Clone();

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
            if (data.TryGetValue(targetId, out var target)) {
                data[linkId] = target;
                return KifaActionResult.Success();
            }

            return new KifaActionResult {
                Status = KifaActionStatus.Error,
                Message = $"Target {targetId} not found"
            };
        }
    }
}
