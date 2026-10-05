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
public class ExtractCommandTests : IDisposable {
    readonly string tempDir;
    readonly FakeFileInformationServiceClient fakeClient;

    public ExtractCommandTests() {
        tempDir = Path.GetFullPath(Path.Combine(Path.GetTempPath(), $"kifa_extract_test_{Guid.NewGuid()}"))
            .Replace('\\', '/');
        Directory.CreateDirectory(tempDir);
        FileStorageClient.ServerConfigs["extract_test_temp"] = new ServerConfig {
            Prefix = tempDir
        };
        fakeClient = new FakeFileInformationServiceClient();
        FileInformation.Client = fakeClient;
    }

    public void Dispose() {
        FileStorageClient.ServerConfigs.Remove("extract_test_temp");
        if (Directory.Exists(tempDir)) {
            Directory.Delete(tempDir, recursive: true);
        }

        FileInformation.Client = new FileInformationRestServiceClient();
    }

    [Fact]
    public void RemoveOneArchiveFile_NonRegistered_DeletesFileDirectly() {
        var filePath = $"{tempDir}/non_registered.zip";
        File.WriteAllText(filePath, "dummy zip content");
        var file = new KifaFile(filePath);

        var cmd = new ExtractCommand();
        var result = cmd.RemoveOneArchiveFile(file);

        Assert.Equal(KifaActionStatus.OK, result.Status);
        Assert.False(File.Exists(filePath));
    }

    [Fact]
    public void RemoveOneArchiveFile_Registered_PromptRejected_Skips() {
        var originalIn = Console.In;
        try {
            // Prompt default is 'n' (suggested: false). Enter accepts 'n'.
            Console.SetIn(new StringReader("\n"));

            var filePath = $"{tempDir}/registered_reject.zip";
            File.WriteAllText(filePath, "dummy zip content");
            fakeClient.Set(new FileInformation {
                Id = "/registered_reject.zip",
                Locations = new() {
                    [$"local:extract_test_temp/registered_reject.zip"] = DateTime.UtcNow
                }
            });
            var file = new KifaFile(filePath);

            var cmd = new ExtractCommand();
            var result = cmd.RemoveOneArchiveFile(file);

            Assert.Equal(KifaActionStatus.Skipped, result.Status);
            Assert.True(File.Exists(filePath));
            Assert.NotNull(fakeClient.Get("/registered_reject.zip"));
        } finally {
            Console.SetIn(originalIn);
        }
    }

    [Fact]
    public void RemoveOneArchiveFile_Registered_PromptConfirmed_RemovesFileAndLocation() {
        var originalIn = Console.In;
        try {
            // Prompt: 'y'
            Console.SetIn(new StringReader("y\n"));

            var filePath = $"{tempDir}/registered_confirm.zip";
            File.WriteAllText(filePath, "dummy zip content");
            fakeClient.Set(new FileInformation {
                Id = "/registered_confirm.zip",
                Locations = new() {
                    [$"local:extract_test_temp/registered_confirm.zip"] = DateTime.UtcNow
                }
            });
            var file = new KifaFile(filePath);

            var cmd = new ExtractCommand();
            var result = cmd.RemoveOneArchiveFile(file);

            Assert.Equal(KifaActionStatus.OK, result.Status);
            Assert.False(File.Exists(filePath));
            Assert.Null(fakeClient.Get("/registered_confirm.zip"));
        } finally {
            Console.SetIn(originalIn);
        }
    }

    [Fact]
    public void RemoveOneArchiveFile_RegisteredLinked_PromptRejected_Skips() {
        var originalIn = Console.In;
        try {
            // Prompt default is 'n' (suggested: false). Enter accepts 'n'.
            Console.SetIn(new StringReader("\n"));

            var filePath = $"{tempDir}/registered_target_reject.zip";
            File.WriteAllText(filePath, "dummy zip content");
            fakeClient.Set(new FileInformation {
                Id = "/registered_target_reject.zip",
                Locations = new() {
                    [$"local:extract_test_temp/registered_target_reject.zip"] = DateTime.UtcNow
                }
            });
            fakeClient.Link("/registered_target_reject.zip", "/linked_target_reject.zip");

            var file = new KifaFile(filePath);

            var cmd = new ExtractCommand();
            var result = cmd.RemoveOneArchiveFile(file);

            Assert.Equal(KifaActionStatus.Skipped, result.Status);
            Assert.True(File.Exists(filePath));
            Assert.NotNull(fakeClient.Get("/registered_target_reject.zip"));
            Assert.NotNull(fakeClient.Get("/linked_target_reject.zip"));
        } finally {
            Console.SetIn(originalIn);
        }
    }

    [Fact]
    public void RemoveOneArchiveFile_RegisteredLinked_PromptConfirmed_RemovesLogically() {
        var originalIn = Console.In;
        try {
            // Prompt: 'y' for first prompt, enter (suggested true) for second prompt
            Console.SetIn(new StringReader("y\n\n"));

            var filePath = $"{tempDir}/registered_target_confirm.zip";
            File.WriteAllText(filePath, "dummy zip content");
            fakeClient.Set(new FileInformation {
                Id = "/registered_target_confirm.zip",
                Locations = new() {
                    [$"local:extract_test_temp/registered_target_confirm.zip"] = DateTime.UtcNow
                }
            });
            fakeClient.Link("/registered_target_confirm.zip", "/linked_target_confirm.zip");

            var file = new KifaFile(filePath);

            var cmd = new ExtractCommand();
            var result = cmd.RemoveOneArchiveFile(file);

            Assert.Equal(KifaActionStatus.OK, result.Status);
            Assert.Null(fakeClient.Get("/registered_target_confirm.zip"));
            Assert.NotNull(fakeClient.Get("/linked_target_confirm.zip"));
        } finally {
            Console.SetIn(originalIn);
        }
    }

    [Fact]
    public void RemoveArchiveFilesIfRequested_AllRegistered_DefaultEmptyInput_RemovesNothing() {
        var originalIn = Console.In;
        try {
            // Press Enter to accept empty default
            Console.SetIn(new StringReader("\n"));

            var filePath = $"{tempDir}/registered_only.zip";
            using (var zipArchive = System.IO.Compression.ZipFile.Open(filePath, System.IO.Compression.ZipArchiveMode.Create)) {
                var entry = zipArchive.CreateEntry("test.txt");
                using var writer = new StreamWriter(entry.Open());
                writer.WriteLine("test content");
            }

            fakeClient.Set(new FileInformation {
                Id = "/registered_only.zip",
                Locations = new() {
                    [$"local:extract_test_temp/registered_only.zip"] = DateTime.UtcNow
                }
            });

            var cmd = new ExtractCommand {
                DeleteSource = true
            };

            using var archive = SharpCompress.Archives.ArchiveFactory.OpenArchive(filePath);
            var results = cmd.RemoveArchiveFilesIfRequested(archive, filePath).ToList();

            Assert.Empty(results);
            Assert.NotNull(fakeClient.Get("/registered_only.zip"));
            Assert.True(File.Exists(filePath));
        } finally {
            Console.SetIn(originalIn);
        }
    }

    [Fact]
    public void RemoveArchiveFilesIfRequested_NonRegistered_DefaultInput_DeletesFile() {
        var originalIn = Console.In;
        try {
            // Press Enter to accept '*' default for non-registered files
            Console.SetIn(new StringReader("\n"));

            var filePath = $"{tempDir}/non_registered_source.zip";
            using (var zipArchive = System.IO.Compression.ZipFile.Open(filePath, System.IO.Compression.ZipArchiveMode.Create)) {
                var entry = zipArchive.CreateEntry("test.txt");
                using var writer = new StreamWriter(entry.Open());
                writer.WriteLine("test content");
            }

            var cmd = new ExtractCommand {
                DeleteSource = true
            };

            using var archive = SharpCompress.Archives.ArchiveFactory.OpenArchive(filePath);
            var results = cmd.RemoveArchiveFilesIfRequested(archive, filePath).ToList();

            Assert.Single(results);
            Assert.Equal(KifaActionStatus.OK, results[0].result.Status);
            Assert.False(File.Exists(filePath));
        } finally {
            Console.SetIn(originalIn);
        }
    }

    [Fact]
    public void RemoveArchiveFilesIfRequested_RegisteredWithLinks_SelectFile_RemovesTargetKeepsLinkedFileInfo() {
        var originalIn = Console.In;
        try {
            // Select all (*), then confirm 2 prompts (y\n\n)
            Console.SetIn(new StringReader("*\ny\n\n"));

            var filePath = $"{tempDir}/registered_multi_link.zip";
            using (var zipArchive = System.IO.Compression.ZipFile.Open(filePath, System.IO.Compression.ZipArchiveMode.Create)) {
                var entry = zipArchive.CreateEntry("test.txt");
                using var writer = new StreamWriter(entry.Open());
                writer.WriteLine("test content");
            }

            fakeClient.Set(new FileInformation {
                Id = "/registered_multi_link.zip",
                Locations = new() {
                    [$"local:extract_test_temp/registered_multi_link.zip"] = DateTime.UtcNow
                }
            });
            fakeClient.Link("/registered_multi_link.zip", "/linked_multi_link.zip");

            var cmd = new ExtractCommand {
                DeleteSource = true
            };

            using var archive = SharpCompress.Archives.ArchiveFactory.OpenArchive(filePath);
            var results = cmd.RemoveArchiveFilesIfRequested(archive, filePath).ToList();

            Assert.Single(results);
            Assert.Equal(KifaActionStatus.OK, results[0].result.Status);
            Assert.Null(fakeClient.Get("/registered_multi_link.zip"));
            Assert.NotNull(fakeClient.Get("/linked_multi_link.zip"));
            Assert.False(File.Exists(filePath));
        } finally {
            Console.SetIn(originalIn);
        }
    }

    class FakeFileInformationServiceClient : BaseKifaServiceClient<FileInformation>,
        FileInformationServiceClient {
        readonly Dictionary<string, FileInformation> data = new();

        public override SortedDictionary<string, FileInformation> List(string folder = "",
            bool recursive = true, KifaDataOptions? options = null)
            => new(data.ToDictionary(kv => kv.Key, kv => kv.Value.Clone()));

        public override FileInformation? Get(string id, KifaDataOptions? options = null) {
            if (id.StartsWith("/$/")) {
                var sha256 = id.Split('/').Last();
                return data.Values.FirstOrDefault(f => f.Sha256 == sha256)?.Clone();
            }

            return data.GetValueOrDefault(id)?.Clone();
        }

        public override KifaActionResult Set(FileInformation item) {
            data[item.Id!] = item.Clone();
            return KifaActionResult.Success();
        }

        public override KifaActionResult Update(FileInformation item) {
            data[item.Id!] = item.Clone();
            return KifaActionResult.Success();
        }

        public override KifaActionResult Delete(string id) {
            if (data.TryGetValue(id, out var info)) {
                data.Remove(id);
                info.Metadata?.Linking?.Links?.Remove(id);
                if (info.Metadata?.Linking?.Target != null &&
                    data.TryGetValue(info.Metadata.Linking.Target, out var targetInfo)) {
                    targetInfo.Metadata?.Linking?.Links?.Remove(id);
                }
            }

            return KifaActionResult.Success();
        }

        public override KifaActionResult Link(string targetId, string linkId) {
            if (data.TryGetValue(targetId, out var targetInfo)) {
                targetInfo.Metadata ??= new DataMetadata();
                targetInfo.Metadata.Linking ??= new LinkingMetadata();
                targetInfo.Metadata.Linking.Links ??= [];
                targetInfo.Metadata.Linking.Links.Add(linkId);

                var linkInfo = new FileInformation {
                    Id = linkId,
                    Locations = targetInfo.Locations,
                    Metadata = new DataMetadata {
                        Linking = new LinkingMetadata {
                            Target = targetId,
                            Links = targetInfo.Metadata.Linking.Links
                        }
                    }
                };
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
}
