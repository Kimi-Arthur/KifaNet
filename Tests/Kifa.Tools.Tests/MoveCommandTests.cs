using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Kifa.Api.Files;
using Kifa.IO;
using Kifa.IO.StorageClients;
using Kifa.Service;
using Kifa.Tools.SubUtil.Commands;
using Xunit;

namespace Kifa.Tools.Tests;

[Collection("FileStorageTests")]
public class MoveCommandTests : IDisposable {
    readonly string tempDir;
    readonly string originalSubtitlesHost;
    readonly string originalIgnoredPattern;
    readonly FileInformationServiceClient originalClient;
    readonly KifaServiceClient<FileIdInfo> originalFileIdInfoClient;

    public MoveCommandTests() {
        tempDir = Path.GetFullPath(Path.Combine(Path.GetTempPath(), $"kifa_move_test_{Guid.NewGuid()}"))
            .Replace('\\', '/');
        Directory.CreateDirectory(tempDir);
        FileStorageClient.ServerConfigs["move_test_temp"] = new ServerConfig {
            Prefix = tempDir
        };

        originalSubtitlesHost = KifaFile.SubtitlesHost;
        KifaFile.SubtitlesHost = "local:move_test_temp/Subtitles";

        originalIgnoredPattern = KifaFile.IgnoredPattern;
        // Match standard production pattern where .xml, .ass, .srt under /Anime/ are ignored by default
        KifaFile.IgnoredPattern = "^/Anime/.*\\.(xml|ass|srt)$";

        originalClient = FileInformation.Client;
        FileInformation.Client = new TestFileInformationServiceClient();
        originalFileIdInfoClient = FileIdInfo.Client;
        FileIdInfo.Client = new TestFileIdInfoServiceClient();
    }

    public void Dispose() {
        FileInformation.Client = originalClient;
        FileIdInfo.Client = originalFileIdInfoClient;
        KifaFile.SubtitlesHost = originalSubtitlesHost;
        KifaFile.IgnoredPattern = originalIgnoredPattern;
        FileStorageClient.ServerConfigs.Remove("move_test_temp");
        if (Directory.Exists(tempDir)) {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public void MoveXmlFile_MovesToSubtitlesHostAndRemovesLocal() {
        var animeDir = $"{tempDir}/Anime/MyShow";
        Directory.CreateDirectory(animeDir);

        var xmlPath = $"{animeDir}/Episode 01.c12345.xml";
        File.WriteAllText(xmlPath, "<danmaku>test</danmaku>");

        var originalIn = Console.In;
        try {
            // "y\n" to confirm selection, "y\n" to confirm removing local file
            Console.SetIn(new StringReader("y\n"));

            var cmd = new MoveCommand {
                FileNames = [$"local:move_test_temp/Anime/MyShow"],
                AutoConfirmDefault = true
            };

            var exitCode = cmd.Execute();
            Assert.Equal(0, exitCode);

            var targetFile = new KifaFile("local:move_test_temp/Subtitles/Anime/MyShow/Episode 01.c12345.xml");
            Assert.True(targetFile.Exists());
            Assert.Equal("<danmaku>test</danmaku>", targetFile.ReadAsString());

            var sourceFile = new KifaFile($"local:move_test_temp/Anime/MyShow/Episode 01.c12345.xml");
            Assert.False(sourceFile.Exists());
        } finally {
            Console.SetIn(originalIn);
        }
    }

    [Fact]
    public void MoveMixedSubtitleFiles_MovesAllSupportedExtensions() {
        var animeDir = $"{tempDir}/Anime/MyShow";
        Directory.CreateDirectory(animeDir);

        File.WriteAllText($"{animeDir}/Episode 01.c12345.xml", "<danmaku>content</danmaku>");
        File.WriteAllText($"{animeDir}/Episode 01.zh.ass", "[Script Info]\nTitle: Test");
        File.WriteAllText($"{animeDir}/Episode 01.en.srt", "1\n00:00:01,000 --> 00:00:02,000\nHello");
        File.WriteAllText($"{animeDir}/Episode 01.mkv", "dummy video content");

        var originalIn = Console.In;
        try {
            Console.SetIn(new StringReader("*\n"));

            var cmd = new MoveCommand {
                FileNames = [$"local:move_test_temp/Anime/MyShow"],
                AutoConfirmDefault = true
            };

            var exitCode = cmd.Execute();
            Assert.Equal(0, exitCode);

            var targetXml = new KifaFile("local:move_test_temp/Subtitles/Anime/MyShow/Episode 01.c12345.xml");
            var targetAss = new KifaFile("local:move_test_temp/Subtitles/Anime/MyShow/Episode 01.zh.ass");
            var targetSrt = new KifaFile("local:move_test_temp/Subtitles/Anime/MyShow/Episode 01.en.srt");
            var sourceVideo = new KifaFile($"local:move_test_temp/Anime/MyShow/Episode 01.mkv");

            Assert.True(targetXml.Exists());
            Assert.True(targetAss.Exists());
            Assert.True(targetSrt.Exists());
            Assert.True(sourceVideo.Exists()); // Video file should NOT be moved

            var sourceXml = new KifaFile($"local:move_test_temp/Anime/MyShow/Episode 01.c12345.xml");
            var sourceAss = new KifaFile($"local:move_test_temp/Anime/MyShow/Episode 01.zh.ass");
            var sourceSrt = new KifaFile($"local:move_test_temp/Anime/MyShow/Episode 01.en.srt");

            Assert.False(sourceXml.Exists());
            Assert.False(sourceAss.Exists());
            Assert.False(sourceSrt.Exists());
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

        public override FileInformation? Get(string id, KifaDataOptions? options = null)
            => data.GetValueOrDefault(id);

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
