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
public class CopyCommandTests : IDisposable {
    readonly string tempDir;
    readonly FileInformationServiceClient originalClient;
    readonly KifaServiceClient<FileIdInfo> originalFileIdInfoClient;

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
        originalFileIdInfoClient = FileIdInfo.Client;
        FileIdInfo.Client = new TestFileIdInfoServiceClient();
    }

    public void Dispose() {
        FileInformation.Client = originalClient;
        FileIdInfo.Client = originalFileIdInfoClient;
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

    [Fact]
    public void ExecuteLocal_CrossDevice_ReusesExistingInstanceOnTargetDevice() {
        var dev1Dir = Path.GetFullPath(Path.Combine(Path.GetTempPath(), $"kifa_copy_dev1_{Guid.NewGuid()}"))
            .Replace('\\', '/');
        var dev2Dir = Path.GetFullPath(Path.Combine(Path.GetTempPath(), $"kifa_copy_dev2_{Guid.NewGuid()}"))
            .Replace('\\', '/');
        Directory.CreateDirectory(dev1Dir);
        Directory.CreateDirectory(dev2Dir);

        FileStorageClient.ServerConfigs["copy_test_dev1"] = new ServerConfig {
            Prefix = dev1Dir
        };
        FileStorageClient.ServerConfigs["copy_test_dev2"] = new ServerConfig {
            Prefix = dev2Dir
        };

        try {
            var content = "test content for hard link reuse";
            var sourceFilePath = $"{dev1Dir}/file.txt";
            var existingFilePath = $"{dev2Dir}/existing.txt";
            var destFilePath = $"{dev2Dir}/dest.txt";

            File.WriteAllText(sourceFilePath, content);
            File.WriteAllText(existingFilePath, content);

            var testClient = (TestFileInformationServiceClient) FileInformation.Client;
            var fileInfo = new FileInformation {
                Id = "/file.txt",
                Size = content.Length,
                Locations = new() {
                    ["local:copy_test_dev2/existing.txt"] = DateTime.UtcNow
                }
            };
            testClient.Set(fileInfo);

            var sourceFile = new KifaFile(sourceFilePath);
            var destFile = new KifaFile(destFilePath);

            var cmd = new CopyCommand {
                Files = [sourceFilePath, destFilePath],
                AutoConfirmDefault = true
            };

            var exitCode = cmd.Execute();
            Assert.Equal(0, exitCode);
            Assert.True(File.Exists(destFilePath));
            Assert.Equal(content, File.ReadAllText(destFilePath));

            var existingKifaFile = new KifaFile(existingFilePath);
            if (destFile.IdInfo?.InternalFildId != null && existingKifaFile.IdInfo?.InternalFildId != null) {
                Assert.Equal(existingKifaFile.IdInfo.InternalFildId, destFile.IdInfo.InternalFildId);
            }
        } finally {
            FileStorageClient.ServerConfigs.Remove("copy_test_dev1");
            FileStorageClient.ServerConfigs.Remove("copy_test_dev2");
            if (Directory.Exists(dev1Dir)) {
                Directory.Delete(dev1Dir, recursive: true);
            }
            if (Directory.Exists(dev2Dir)) {
                Directory.Delete(dev2Dir, recursive: true);
            }
        }
    }

    [Fact]
    public void ExecuteLocal_CrossDevice_NoExistingInstance_CopiesDirectly() {
        var dev1Dir = Path.GetFullPath(Path.Combine(Path.GetTempPath(), $"kifa_copy_dev1_{Guid.NewGuid()}"))
            .Replace('\\', '/');
        var dev2Dir = Path.GetFullPath(Path.Combine(Path.GetTempPath(), $"kifa_copy_dev2_{Guid.NewGuid()}"))
            .Replace('\\', '/');
        Directory.CreateDirectory(dev1Dir);
        Directory.CreateDirectory(dev2Dir);

        FileStorageClient.ServerConfigs["copy_test_dev1"] = new ServerConfig {
            Prefix = dev1Dir
        };
        FileStorageClient.ServerConfigs["copy_test_dev2"] = new ServerConfig {
            Prefix = dev2Dir
        };

        try {
            var content = "test content direct copy";
            var sourceFilePath = $"{dev1Dir}/file.txt";
            var destFilePath = $"{dev2Dir}/dest.txt";

            File.WriteAllText(sourceFilePath, content);

            var cmd = new CopyCommand {
                Files = [sourceFilePath, destFilePath],
                AutoConfirmDefault = true
            };

            var exitCode = cmd.Execute();
            Assert.Equal(0, exitCode);
            Assert.True(File.Exists(destFilePath));
            Assert.Equal(content, File.ReadAllText(destFilePath));
        } finally {
            FileStorageClient.ServerConfigs.Remove("copy_test_dev1");
            FileStorageClient.ServerConfigs.Remove("copy_test_dev2");
            if (Directory.Exists(dev1Dir)) {
                Directory.Delete(dev1Dir, recursive: true);
            }
            if (Directory.Exists(dev2Dir)) {
                Directory.Delete(dev2Dir, recursive: true);
            }
        }
    }

    [Fact]
    public void GetLocalFileCopyPairs_FolderWithIgnoredFiles_ExcludesUnregisteredAndIncludesRegistered() {
        var sourceDir = $"{tempDir}/source_ignored";
        var subDir = $"{sourceDir}/sub";
        Directory.CreateDirectory(subDir);
        File.WriteAllText($"{sourceDir}/normal.txt", "normal");
        File.WriteAllText($"{sourceDir}/@unregistered.txt", "unregistered");
        File.WriteAllText($"{sourceDir}/@registered.txt", "registered");
        File.WriteAllText($"{subDir}/@sub_ignored.txt", "sub_ignored");

        var testClient = (TestFileInformationServiceClient) FileInformation.Client;
        testClient.Set(new FileInformation {
            Id = "/source_ignored/@registered.txt",
            Size = 10,
            Locations = new() {
                ["local:copy_test_temp/source_ignored/@registered.txt"] = DateTime.UtcNow
            }
        });

        var destDir = $"{tempDir}/dest_ignored";

        var cmdDefault = new CopyCommand {
            Files = [sourceDir, destDir]
        };
        var pairsDefault = cmdDefault.GetLocalFileCopyPairs();
        Assert.Equal(2, pairsDefault.Count);
        Assert.Contains(pairsDefault,
            p => p.SourceFile.Path == "/source_ignored/normal.txt" &&
                 p.DestinationFile.Path == "/dest_ignored/normal.txt");
        Assert.Contains(pairsDefault,
            p => p.SourceFile.Path == "/source_ignored/@registered.txt" &&
                 p.DestinationFile.Path == "/dest_ignored/@registered.txt");

        var cmdIncludeAll = new CopyCommand {
            Files = [sourceDir, destDir],
            IncludeAll = true
        };
        var pairsAll = cmdIncludeAll.GetLocalFileCopyPairs();
        Assert.Equal(4, pairsAll.Count);
    }

    [Fact]
    public void GetLocalFileCopyPairs_SingleIgnoredFile_RespectsRegisteredAndIncludeAll() {
        var unregisteredFile = $"{tempDir}/@single_unregistered.txt";
        var registeredFile = $"{tempDir}/@single_registered.txt";
        File.WriteAllText(unregisteredFile, "unregistered");
        File.WriteAllText(registeredFile, "registered");

        var testClient = (TestFileInformationServiceClient) FileInformation.Client;
        testClient.Set(new FileInformation {
            Id = "/@single_registered.txt",
            Size = 10,
            Locations = new() {
                ["local:copy_test_temp/@single_registered.txt"] = DateTime.UtcNow
            }
        });

        var destDir = $"{tempDir}/dest_single/";

        var cmdDefault = new CopyCommand {
            Files = [unregisteredFile, registeredFile, destDir]
        };
        var pairsDefault = cmdDefault.GetLocalFileCopyPairs();
        Assert.Single(pairsDefault);
        Assert.Equal("/@single_registered.txt", pairsDefault[0].SourceFile.Path);
        Assert.Equal("/dest_single/@single_registered.txt", pairsDefault[0].DestinationFile.Path);

        var cmdIncludeAll = new CopyCommand {
            Files = [unregisteredFile, registeredFile, destDir],
            IncludeAll = true
        };
        var pairsAll = cmdIncludeAll.GetLocalFileCopyPairs();
        Assert.Equal(2, pairsAll.Count);
    }

    [Fact]
    public void GetLocalFileCopyPairs_FolderWithOnlyUnregisteredIgnoredFiles_ReturnsEmpty() {
        var sourceDir = $"{tempDir}/only_ignored";
        Directory.CreateDirectory(sourceDir);
        File.WriteAllText($"{sourceDir}/@ignored.txt", "ignored");

        var destDir = $"{tempDir}/dest_empty";

        var cmd = new CopyCommand {
            Files = [sourceDir, destDir]
        };
        var pairs = cmd.GetLocalFileCopyPairs();
        Assert.Empty(pairs);
    }

    [Fact]
    public void FindExistingFiles_DefaultsToIgnoringUnregisteredFiles_AndIncludesRegisteredIgnoredFiles() {
        var originalExtensions = new HashSet<string>(KifaFile.IgnoredExtensions);
        KifaFile.IgnoredExtensions = ["aria2"];
        try {
            var dir = $"{tempDir}/find_test";
            Directory.CreateDirectory(dir);
            var normalFile = $"{dir}/normal.txt";
            var ignoredAtFile = $"{dir}/@hidden.txt";
            var ignoredExtFile = $"{dir}/download.aria2";
            var registeredIgnoredFile = $"{dir}/@registered.txt";
            File.WriteAllText(normalFile, "1");
            File.WriteAllText(ignoredAtFile, "2");
            File.WriteAllText(ignoredExtFile, "3");
            File.WriteAllText(registeredIgnoredFile, "4");

            var testClient = (TestFileInformationServiceClient) FileInformation.Client;
            testClient.Set(new FileInformation {
                Id = "/find_test/@registered.txt",
                Size = 1,
                Locations = new() {
                    ["local:copy_test_temp/find_test/@registered.txt"] = DateTime.UtcNow
                }
            });

            var folderDefault = KifaFile.FindExistingFiles([dir]);
            Assert.Equal(2, folderDefault.Count);
            Assert.Contains(folderDefault, f => f.Path == "/find_test/normal.txt");
            Assert.Contains(folderDefault, f => f.Path == "/find_test/@registered.txt");

            var singleIgnoredDefault =
                KifaFile.FindExistingFiles([ignoredAtFile, ignoredExtFile, registeredIgnoredFile]);
            Assert.Single(singleIgnoredDefault);
            Assert.Equal("/find_test/@registered.txt", singleIgnoredDefault[0].Path);

            var singleIgnoredIncludeAll =
                KifaFile.FindExistingFiles([ignoredAtFile, ignoredExtFile, registeredIgnoredFile],
                    shouldIgnoreFiles: false);
            Assert.Equal(3, singleIgnoredIncludeAll.Count);
        } finally {
            KifaFile.IgnoredExtensions = originalExtensions;
        }
    }

    [Fact]
    public void FindPotentialFiles_DefaultsToIgnoringUnregisteredFiles_AndIncludesRegisteredIgnoredFiles() {
        var testClient = (TestFileInformationServiceClient) FileInformation.Client;
        testClient.Set(new FileInformation {
            Id = "/pot_test/normal.txt",
            Locations = new()
        });
        testClient.Set(new FileInformation {
            Id = "/pot_test/@unregistered_locally.txt",
            Locations = new() {
                ["other:server/pot_test/@unregistered_locally.txt"] = DateTime.UtcNow
            }
        });
        testClient.Set(new FileInformation {
            Id = "/pot_test/@registered_locally.txt",
            Locations = new() {
                ["local:copy_test_temp/pot_test/@registered_locally.txt"] = DateTime.UtcNow
            }
        });

        var dir = $"{tempDir}/pot_test";
        var potentialDefault = KifaFile.FindPotentialFiles([dir]);
        Assert.Equal(2, potentialDefault.Count);
        Assert.Contains(potentialDefault, f => f.Path == "/pot_test/normal.txt");
        Assert.Contains(potentialDefault, f => f.Path == "/pot_test/@registered_locally.txt");

        var potentialAll = KifaFile.FindPotentialFiles([dir], shouldIgnoreFiles: false);
        Assert.Equal(3, potentialAll.Count);
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

        public override FileInformation? Get(string id, KifaDataOptions? options = null) {
            if (id.StartsWith("/$/")) {
                var sha256 = id.Split('/').Last();
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
