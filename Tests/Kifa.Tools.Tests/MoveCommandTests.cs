using System;
using System.IO;
using Kifa.Api.Files;
using Kifa.IO.StorageClients;
using Kifa.Tools.SubUtil.Commands;
using Xunit;

namespace Kifa.Tools.Tests;

[Collection("FileStorageTests")]
public class MoveCommandTests : IDisposable {
    readonly string tempDir;
    readonly string originalSubtitlesHost;
    readonly string originalIgnoredPattern;

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
    }

    public void Dispose() {
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
}
