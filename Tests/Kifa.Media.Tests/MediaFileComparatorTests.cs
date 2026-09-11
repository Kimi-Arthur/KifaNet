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
        result.File1Valid.Should().BeTrue();
        result.File2Valid.Should().BeTrue();
        result.Differences.Should().BeEmpty();
    }

    [Fact]
    public void CorruptedJpegComparisonTest() {
        var validFile = Path.Combine(testDir, "valid.jpg");
        var corruptedFile = Path.Combine(testDir, "corrupted.jpg");

        var execution = Executor.Run("ffmpeg",
            $"-v error -f lavfi -i testsrc=duration=0.1:size=160x120:rate=1 -frames:v 1 \"{validFile}\" -y");
        execution.ExitCode.Should().Be(0);

        var bytes = File.ReadAllBytes(validFile);
        // Truncate or corrupt the last 2 bytes (the FF D9 EOI marker)
        bytes[^1] = 0x00;
        bytes[^2] = 0x00;
        File.WriteAllBytes(corruptedFile, bytes);

        var result = MediaFileComparator.Compare(validFile, corruptedFile);

        result.IsBitExactMatch.Should().BeFalse();
        result.IsContentMatch.Should().BeTrue();
        result.File1Valid.Should().BeTrue();
        result.File1Errors.Should().BeEmpty();
        result.File2Valid.Should().BeFalse();
        result.File2Errors.Should().NotBeEmpty();
        result.File2Errors.Should().Contain(e => e.Contains("EOI"));
    }

    [Fact]
    public void CorruptedPngComparisonTest() {
        var validFile = Path.Combine(testDir, "valid.png");
        var corruptedFile = Path.Combine(testDir, "corrupted.png");

        var execution = Executor.Run("ffmpeg",
            $"-v error -f lavfi -i testsrc=duration=0.1:size=160x120:rate=1 -frames:v 1 \"{validFile}\" -y");
        execution.ExitCode.Should().Be(0);

        var bytes = File.ReadAllBytes(validFile);
        // Corrupt the last byte (IEND CRC)
        bytes[^1] ^= 0xFF;
        File.WriteAllBytes(corruptedFile, bytes);

        var result = MediaFileComparator.Compare(validFile, corruptedFile);

        result.IsBitExactMatch.Should().BeFalse();
        result.IsContentMatch.Should().BeTrue();
        result.File1Valid.Should().BeTrue();
        result.File1Errors.Should().BeEmpty();
        result.File2Valid.Should().BeFalse();
        result.File2Errors.Should().NotBeEmpty();
    }

    [Fact]
    public void ValidateDirectlyTest() {
        var validFile = Path.Combine(testDir, "valid_direct.jpg");
        var corruptedFile = Path.Combine(testDir, "corrupted_direct.jpg");

        var execution = Executor.Run("ffmpeg",
            $"-v error -f lavfi -i testsrc=duration=0.1:size=160x120:rate=1 -frames:v 1 \"{validFile}\" -y");
        execution.ExitCode.Should().Be(0);

        var bytes = File.ReadAllBytes(validFile);
        bytes[^1] = 0xAA;
        File.WriteAllBytes(corruptedFile, bytes);

        var (valid1, errors1) = MediaFileComparator.Validate(validFile);
        valid1.Should().BeTrue();
        errors1.Should().BeEmpty();

        var (valid2, errors2) = MediaFileComparator.Validate(corruptedFile);
        valid2.Should().BeFalse();
        errors2.Should().NotBeEmpty();
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

    [Fact]
    public void ToOneLineStringTest() {
        var exactResult = new MediaComparisonResult {
            IsBitExactMatch = true,
            IsContentMatch = true,
            MatchLevel = ContentMatchLevel.BitExact
        };
        exactResult.ToOneLineString().Should().Be("Bit-Exact: Files are 100% bit-exact identical.");

        var bitstreamResultWithDiff = new MediaComparisonResult {
            IsBitExactMatch = false,
            IsContentMatch = true,
            MatchLevel = ContentMatchLevel.BitstreamMatch,
            AllDifferences = [
                new MetadataFieldDifference {
                    Category = "Format Tags",
                    Name = "title",
                    File1Value = null,
                    File2Value = "NewTitle"
                },
                new MetadataFieldDifference {
                    Category = "Stream #0 (video)",
                    Name = "bit_rate",
                    File1Value = "1000",
                    File2Value = "2000"
                }
            ]
        };
        bitstreamResultWithDiff.ToOneLineString().Should().Be(
            "Bitstream Match: [Format Tags] title: (missing) vs \"NewTitle\", [Stream #0 (video)] bit_rate: \"1000\" vs \"2000\"");

        var bitstreamResultNoDiff = new MediaComparisonResult {
            IsBitExactMatch = false,
            IsContentMatch = true,
            MatchLevel = ContentMatchLevel.BitstreamMatch,
            AllDifferences = []
        };
        bitstreamResultNoDiff.ToOneLineString().Should().Be("Bitstream Match: All metadata fields match.");

        var noMatchResult = new MediaComparisonResult {
            IsBitExactMatch = false,
            IsContentMatch = false,
            MatchLevel = ContentMatchLevel.NoMatch,
            Streams = [
                new StreamComparisonResult {
                    Index = 0,
                    StreamType = "video",
                    IsMatch = false
                },
                new StreamComparisonResult {
                    Index = 1,
                    StreamType = "audio",
                    IsMatch = true
                }
            ],
            AllDifferences = [
                new MetadataFieldDifference {
                    Category = "Format Tags",
                    Name = "title",
                    File1Value = "Old",
                    File2Value = "New"
                }
            ]
        };
        noMatchResult.ToOneLineString().Should().Be("No Match: Mismatched Stream #0 [video]");
        noMatchResult.ToOneLineString(allFields: true).Should().Be(
            "No Match: Mismatched Stream #0 [video]; Diffs: [Format Tags] title: \"Old\" vs \"New\"");
    }
}
