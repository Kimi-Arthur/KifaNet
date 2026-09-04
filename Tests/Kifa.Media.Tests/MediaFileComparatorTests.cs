using System;
using System.IO;
using FluentAssertions;
using Kifa.Api.Files;
using Kifa.IO;
using Kifa.IO.StorageClients;
using Xunit;

namespace Kifa.Media.Tests;

public class MediaFileComparatorTests : IDisposable {
    readonly string testDir;

    public MediaFileComparatorTests() {
        testDir = Path.GetFullPath(Path.Combine(".agent_temp", $"test_comparator_{Guid.NewGuid():N}")).Replace('\\', '/');
        Directory.CreateDirectory(testDir);
        FileStorageClient.ServerConfigs["test_comparator"] = new ServerConfig {
            Prefix = testDir
        };
    }

    public void Dispose() {
        FileStorageClient.ServerConfigs.Remove("test_comparator");
        if (Directory.Exists(testDir)) {
            Directory.Delete(testDir, recursive: true);
        }
    }

    [Fact]
    public void ExactSameFileTest() {
        var file1 = Path.Combine(testDir, "test1.mp4");
        var execution = Executor.Run("ffmpeg",
            $"-v error -f lavfi -i testsrc=duration=0.5:size=160x120:rate=10 -c:v libx264 -pix_fmt yuv420p \"{file1}\" -y");
        execution.ExitCode.Should().Be(0);

        var result = MediaFileComparator.Compare(file1, file1);

        result.IsBitExactMatch.Should().BeTrue();
        result.IsContentMatch.Should().BeTrue();
        result.MatchLevel.Should().Be(ContentMatchLevel.BitExact);
        result.Differences.Should().BeEmpty();
    }

    [Fact]
    public void MetadataDifferenceVideoTest() {
        var file1 = Path.Combine(testDir, "test1.mp4");
        var file2 = Path.Combine(testDir, "test2.mp4");

        var execution1 = Executor.Run("ffmpeg",
            $"-v error -f lavfi -i testsrc=duration=0.5:size=160x120:rate=10 -c:v libx264 -pix_fmt yuv420p \"{file1}\" -y");
        execution1.ExitCode.Should().Be(0);

        var execution2 = Executor.Run("ffmpeg",
            $"-v error -i \"{file1}\" -c copy -metadata title=\"DifferentTitle\" \"{file2}\" -y");
        execution2.ExitCode.Should().Be(0);

        var result = MediaFileComparator.Compare(file1, file2);

        result.IsBitExactMatch.Should().BeFalse();
        result.IsContentMatch.Should().BeTrue();
        result.MatchLevel.Should().Be(ContentMatchLevel.BitstreamMatch);
        result.Differences.Should().Contain(d => d.Name == "title" && d.File2Value == "DifferentTitle");
    }

    [Fact]
    public void DifferentContentVideoTest() {
        var file1 = Path.Combine(testDir, "test_red.mp4");
        var file2 = Path.Combine(testDir, "test_blue.mp4");

        var execution1 = Executor.Run("ffmpeg",
            $"-v error -f lavfi -i color=c=red:duration=0.5:size=160x120:rate=10 -c:v libx264 -pix_fmt yuv420p \"{file1}\" -y");
        execution1.ExitCode.Should().Be(0);

        var execution2 = Executor.Run("ffmpeg",
            $"-v error -f lavfi -i color=c=blue:duration=0.5:size=160x120:rate=10 -c:v libx264 -pix_fmt yuv420p \"{file2}\" -y");
        execution2.ExitCode.Should().Be(0);

        var result = MediaFileComparator.Compare(file1, file2);

        result.IsBitExactMatch.Should().BeFalse();
        result.IsContentMatch.Should().BeFalse();
        result.MatchLevel.Should().Be(ContentMatchLevel.NoMatch);
    }

    [Fact]
    public void CompareViaKifaFilesTest() {
        var file1 = Path.Combine(testDir, "kifa_test1.mp4");
        var file2 = Path.Combine(testDir, "kifa_test2.mp4");

        var execution1 = Executor.Run("ffmpeg",
            $"-v error -f lavfi -i testsrc=duration=0.5:size=160x120:rate=10 -c:v libx264 -pix_fmt yuv420p \"{file1}\" -y");
        execution1.ExitCode.Should().Be(0);

        var execution2 = Executor.Run("ffmpeg",
            $"-v error -i \"{file1}\" -c copy -metadata title=\"KifaTitle\" \"{file2}\" -y");
        execution2.ExitCode.Should().Be(0);

        var kifaFile1 = new KifaFile(file1, fileInfo: new FileInformation());
        var kifaFile2 = new KifaFile(file2, fileInfo: new FileInformation());

        var result = MediaFileComparator.Compare(kifaFile1, kifaFile2);

        result.IsBitExactMatch.Should().BeFalse();
        result.IsContentMatch.Should().BeTrue();
        result.MatchLevel.Should().Be(ContentMatchLevel.BitstreamMatch);
        result.Differences.Should().Contain(d => d.Name == "title" && d.File2Value == "KifaTitle");
    }
}
