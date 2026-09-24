using System;
using System.IO;
using System.Text;
using Xunit;

namespace Kifa.IO.Tests;

public class FileInformationTests {
    [Fact]
    public void GetInformationFromStreamTest() {
        var encoding = new UTF8Encoding();
        Stream s = new MemoryStream(encoding.GetBytes("Test1"));
        var info = FileInformation.GetInformation(s, FileProperties.All);
        Assert.Equal(5, info.Size);
        Assert.Equal("E1B849F9631FFC1829B2E31402373E3C", info.Md5);
        Assert.Equal("99EA7BF70F6E69AD71659995677B43F8A8312025", info.Sha1);
        Assert.Equal("8A863B145DC6E4ED7AC41C08F7536C476EBAC7509E028ED2B49F8BD5A3562B9F",
            info.Sha256);
        Assert.Equal("E1B849F9631FFC1829B2E31402373E3C", info.BlockMd5[0]);
        Assert.Equal("99EA7BF70F6E69AD71659995677B43F8A8312025", info.BlockSha1[0]);
        Assert.Equal("8A863B145DC6E4ED7AC41C08F7536C476EBAC7509E028ED2B49F8BD5A3562B9F",
            info.BlockSha256[0]);
        Assert.Equal("E1B849F9631FFC1829B2E31402373E3C", info.SliceMd5);
        Assert.Equal("05AF01D2", info.Adler32);
        Assert.Equal("4B73F3E6", info.Crc32);

        s = new MemoryStream(encoding.GetBytes("中文长度不一样"));
        Assert.Equal(21, FileInformation.GetInformation(s, FileProperties.Size).Size);
    }

    [Fact]
    public void FileInformationComparerTest() {
        var encoding = new UTF8Encoding();
        Stream s = new MemoryStream(encoding.GetBytes("Test1"));
        var info1 = FileInformation.GetInformation(s, FileProperties.All);
        var info2 = FileInformation.GetInformation(s, FileProperties.All);
        Assert.Equal(FileProperties.None,
            info1.CompareProperties(info2, FileProperties.AllVerifiable));
    }

    [Fact]
    public void GetInformationFromPathTest() {
        var info = FileInformation.GetInformation("Test1.txt", FileProperties.All);
        Assert.Equal(5, info.Size);
        Assert.Equal("E1B849F9631FFC1829B2E31402373E3C", info.Md5);
        Assert.Equal("99EA7BF70F6E69AD71659995677B43F8A8312025", info.Sha1);
        Assert.Equal("8A863B145DC6E4ED7AC41C08F7536C476EBAC7509E028ED2B49F8BD5A3562B9F",
            info.Sha256);
        Assert.Equal("E1B849F9631FFC1829B2E31402373E3C", info.SliceMd5);
        Assert.Equal("05AF01D2", info.Adler32);
        Assert.Equal("4B73F3E6", info.Crc32);
    }

    [Fact]
    public void GetInformationSomeHashesTest() {
        var info =
            FileInformation.GetInformation("Test1.txt", FileProperties.Md5 | FileProperties.Sha256);
        Assert.Equal("E1B849F9631FFC1829B2E31402373E3C", info.Md5);
        Assert.Null(info.Sha1);
        Assert.Equal("8A863B145DC6E4ED7AC41C08F7536C476EBAC7509E028ED2B49F8BD5A3562B9F",
            info.Sha256);
    }

    [Fact]
    public void GetInformationEmpty() {
        var info = FileInformation.GetInformation("Test1.txt", FileProperties.None);
        Assert.Null(info.Size);
        Assert.Null(info.Md5);
        Assert.Null(info.Sha1);
        Assert.Null(info.Sha256);
        Assert.Null(info.Crc32);
        Assert.Empty(info.BlockMd5);
        Assert.Empty(info.BlockSha1);
        Assert.Empty(info.BlockSha256);
        Assert.Null(info.SliceMd5);
    }

    [Fact]
    public void GetInformationPartialReadsTest() {
        var data = new byte[(FileInformation.BlockSize) * 2 + 1234];
        new Random(42).NextBytes(data);

        var normalInfo = FileInformation.GetInformation(new MemoryStream(data),
            FileProperties.AllVerifiable);
        var chunkedInfo = FileInformation.GetInformation(
            new ChunkedStream(new MemoryStream(data), 65536), FileProperties.AllVerifiable);

        Assert.Equal(FileProperties.None,
            normalInfo.CompareProperties(chunkedInfo, FileProperties.AllVerifiable));
        Assert.Equal(3, chunkedInfo.BlockSha256.Count);
        Assert.Equal(3, normalInfo.BlockSha256.Count);
    }

    class ChunkedStream : Stream {
        readonly Stream inner;
        readonly int maxChunkSize;

        public ChunkedStream(Stream inner, int maxChunkSize) {
            this.inner = inner;
            this.maxChunkSize = maxChunkSize;
        }

        public override bool CanRead => inner.CanRead;
        public override bool CanSeek => inner.CanSeek;
        public override bool CanWrite => inner.CanWrite;
        public override long Length => inner.Length;

        public override long Position {
            get => inner.Position;
            set => inner.Position = value;
        }

        public override void Flush() => inner.Flush();

        public override int Read(byte[] buffer, int offset, int count) =>
            inner.Read(buffer, offset, Math.Min(count, maxChunkSize));

        public override long Seek(long offset, SeekOrigin origin) => inner.Seek(offset, origin);

        public override void SetLength(long value) => inner.SetLength(value);

        public override void Write(byte[] buffer, int offset, int count) =>
            inner.Write(buffer, offset, count);
    }

    [Fact]
    public void GetVirtualItemsTest() {
        var info = new FileInformation();
        Assert.Empty(info.GetVirtualItems());

        info.Sha256 = "8A863B145DC6E4ED7AC41C08F7536C476EBAC7509E028ED2B49F8BD5A3562B9F";
        Assert.Equal(["/$/8A863B145DC6E4ED7AC41C08F7536C476EBAC7509E028ED2B49F8BD5A3562B9F"],
            info.GetVirtualItems());

        info.Md5 = "E1B849F9631FFC1829B2E31402373E3C";
        Assert.Equal([
            "/$/8A863B145DC6E4ED7AC41C08F7536C476EBAC7509E028ED2B49F8BD5A3562B9F",
            "/$/E1B849F9631FFC1829B2E31402373E3C"
        ], info.GetVirtualItems());

        info.Sha256 = null;
        Assert.Equal(["/$/E1B849F9631FFC1829B2E31402373E3C"], info.GetVirtualItems());
    }
}
