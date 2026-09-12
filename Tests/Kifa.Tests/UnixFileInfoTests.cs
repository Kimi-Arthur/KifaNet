using System.IO;
using System.Runtime.InteropServices;
using FluentAssertions;
using Xunit;

namespace Kifa.Tests;

public class UnixFileInfoTests {
    [Fact]
    public void GetInodeAndRefCountTest() {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Linux) &&
            !RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) {
            return;
        }

        var tempFile = Path.GetTempFileName();
        try {
            var inode = UnixFileInfo.GetInode(tempFile);
            inode.Should().NotBeNull();
            inode.Should().BeGreaterThan(0);

            var refCount = UnixFileInfo.GetRefCount(tempFile);
            refCount.Should().NotBeNull();
            refCount.Should().Be(1);
        } finally {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public void NonExistentFileReturnsNullTest() {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Linux) &&
            !RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) {
            return;
        }

        var inode = UnixFileInfo.GetInode("/non/existent/path/for/test");
        inode.Should().BeNull();

        var refCount = UnixFileInfo.GetRefCount("/non/existent/path/for/test");
        refCount.Should().BeNull();
    }
}
