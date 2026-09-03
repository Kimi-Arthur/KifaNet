using System;
using FluentAssertions;
using Xunit;

namespace Kifa.Tests;

public class PathExtensionsTests {
    [Fact]
    public void ChopEndToByteCountTest() {
        "hello".ChopEndToByteCount(10).Should().Be("hello");
        "hello".ChopEndToByteCount(5).Should().Be("hello");
        "hello".ChopEndToByteCount(4).Should().Be("hel~");
        "hello".ChopEndToByteCount(3).Should().Be("he~");
        "hello".ChopEndToByteCount(2).Should().Be("h~");
        "hello".ChopEndToByteCount(1).Should().Be("~");

        // Multibyte CJK (3 bytes each)
        "你好世界".ChopEndToByteCount(12).Should().Be("你好世界");
        "你好世界".ChopEndToByteCount(11).Should().Be("你好世~"); // 3+3+3+1 = 10 bytes <= 11
        "你好世界".ChopEndToByteCount(10).Should().Be("你好世~"); // 3+3+3+1 = 10 bytes <= 10
        "你好世界".ChopEndToByteCount(9).Should().Be("你好~"); // 3+3+1 = 7 bytes <= 9
        "你好世界".ChopEndToByteCount(7).Should().Be("你好~"); // 3+3+1 = 7 bytes <= 7
        "你好世界".ChopEndToByteCount(6).Should().Be("你~"); // 3+1 = 4 bytes <= 6

        // Surrogate pairs / Emojis (4 bytes UTF-8, 2 chars in UTF-16)
        "😀😁😂".ChopEndToByteCount(12).Should().Be("😀😁😂");
        "😀😁😂".ChopEndToByteCount(10).Should().Be("😀😁~"); // 4+4+1 = 9 bytes <= 10
        "😀😁😂".ChopEndToByteCount(5).Should().Be("😀~"); // 4+1 = 5 bytes <= 5
        "😀😁😂".ChopEndToByteCount(4).Should().Be("~"); // 1 byte <= 4

        var actZero = () => "hello".ChopEndToByteCount(0);
        actZero.Should().Throw<ArgumentException>();
        var actNegative = () => "hello".ChopEndToByteCount(-2);
        actNegative.Should().NotThrow(); // negative means no limit
    }

    [Fact]
    public void NormalizeFileNameTest() {
        "  a/b\\c: d|e?f*g<h>i\nj  ".NormalizeFileName()
            .Should().Be("a／b＼c：d｜e？f＊g＜h＞i j");

        // Multiple .NoChop() stop markers are allowed
        $"{"a".NoChop()}{"b".NoChop()}c".NormalizeFileName()
            .Should().Be("abc");

        // Duplicate .Choppable(1) throws ArgumentException
        var actDuplicateMarkers = () =>
            $"{"a".Choppable()}{"b".Choppable()}c".NormalizeFileName();
        actDuplicateMarkers.Should().Throw<ArgumentException>();

        // Default max byte count is 250.
        var valid250 = new string('a', 250);
        valid250.NormalizeFileName().Should().Be(valid250);

        // Overlength without chop markers throws ArgumentException
        var overlong251 = new string('a', 251);
        var actOverlong = () => overlong251.NormalizeFileName();
        actOverlong.Should().Throw<ArgumentException>();

        // Overlength with only .NoChop() stop markers throws ArgumentException
        var actOverlongStopOnly = () => overlong251.NoChop().NormalizeFileName();
        actOverlongStopOnly.Should().Throw<ArgumentException>();

        // Overlength with .Choppable() at the end chops
        overlong251.Choppable().NormalizeFileName()
            .Should().Be(new string('a', 249) + "~");

        // Custom reservedBytes with .Choppable() (250 - 244 = 6 max bytes)
        "hello world".Choppable().NormalizeFileName(reservedBytes: 244)
            .Should().Be("hello~");

        // With suffix after .Choppable()
        $"{"hello".Choppable()}.mp4".NormalizeFileName(reservedBytes: 240).Should().Be("hello.mp4");
        $"{"hello world".Choppable()}.mp4".NormalizeFileName(reservedBytes: 241).Should().Be("hell~.mp4");
        $"{"hello world".Choppable()}.mp4".NormalizeFileName(reservedBytes: 240).Should().Be("hello~.mp4");

        // Multibyte with suffix
        $"{"你好世界".Choppable()}.mp4".NormalizeFileName(reservedBytes: 240).Should().Be("你~.mp4");

        // Overlong suffix (non-choppable) exceeding available capacity throws ArgumentException
        var actOverlongSuffix = () => $"{"hello".Choppable()}overlong_suffix".NormalizeFileName(reservedBytes: 245);
        actOverlongSuffix.Should().Throw<ArgumentException>();

        // Reserved bytes exceeding MaxFileNameByteCount throws ArgumentException
        var actExceedsMax = () => "hello".NormalizeFileName(reservedBytes: 300);
        actExceedsMax.Should().Throw<ArgumentException>();

        // Multi-level chopping: text1<stop>text2<chop1>text3<chop2>.suffix
        // text1 (NoChop) untouched, text2 (Choppable 1) cut first, text3 (Choppable 2) cut second
        var multiLevelTemplate =
            $"{"PREFIX_".NoChop()}{"LONG_EPISODE_".Choppable(1)}{"SERIES_TITLE_".Choppable(2)}.mp4";
        // PREFIX_ (7 bytes) + LONG_EPISODE_ (13 bytes) + SERIES_TITLE_ (13 bytes) + .mp4 (4 bytes) = 37 bytes
        
        // When reserving 220 bytes (capacity 30 bytes): cuts LONG_EPISODE_ (1) to fit
        multiLevelTemplate.NormalizeFileName(reservedBytes: 220)
            .Should().Be("PREFIX_LONG_~SERIES_TITLE_.mp4");

        // When reserving 228 bytes (capacity 22 bytes): LONG_EPISODE_ (1) is fully removed, SERIES_TITLE_ (2) is cut
        multiLevelTemplate.NormalizeFileName(reservedBytes: 228)
            .Should().Be("PREFIX_SERIES_TIT~.mp4");

        // Negative limit means no chopping (unlimited)
        $"{"hello world".Choppable()}.mp4".NormalizeFileName(-1).Should().Be("hello world.mp4");
    }

    [Fact]
    public void NormalizeFilePathTest() {
        // Single file segment follows 250 limit
        var valid250 = new string('a', 250);
        valid250.NormalizeFilePath().Should().Be(valid250);

        // Overlong file segment without chop markers throws
        var overlong251 = new string('a', 251);
        var actOverlongFile = () => overlong251.NormalizeFilePath();
        actOverlongFile.Should().Throw<ArgumentException>();

        // Multi-segment: directory follows 255 limit, file follows 250 limit
        var dir255 = new string('d', 255);
        var dir256 = new string('d', 256);
        $"{dir255}/{valid250}".NormalizeFilePath().Should().Be($"{dir255}/{valid250}");

        var actOverlongDir = () => $"{dir256}/{valid250}".NormalizeFilePath();
        actOverlongDir.Should().Throw<ArgumentException>();

        // Multi-segment with .Choppable() chops directory at 255 and file at 250
        $"{dir256.Choppable()}/{overlong251.Choppable()}".NormalizeFilePath()
            .Should().Be($"{new string('d', 254)}~/{new string('a', 249)}~");

        // Custom reserved bytes with .Choppable() (file 250-244=6, folder 255-247=8)
        $"{"folder_long".Choppable()}/{"hello world".Choppable()}".NormalizeFilePath(reservedFileBytes: 244, reservedFolderBytes: 247)
            .Should().Be("folder_~/hello~");

        // Folder and file suffix retention with .Choppable()
        $"{new string('u', 300).Choppable()}.123.bilibili/extra/folder/{"video title".Choppable()}.av123.mp4".NormalizeFilePath()
            .Should().Be($"{new string('u', 255 - ".123.bilibili".Length - 1)}~.123.bilibili/extra/folder/video title.av123.mp4");

        // Safe characters mapping preserved across path segments
        "dir: 1/file? 2".NormalizeFilePath().Should().Be("dir：1/file？ 2");
    }
}
