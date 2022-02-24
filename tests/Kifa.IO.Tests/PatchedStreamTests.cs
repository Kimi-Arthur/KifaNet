using System.IO;
using Xunit;

namespace Kifa.IO.Tests; 

public class PatchedStreamTests {
    [Fact]
    public void PartialStreamBasicTest() {
        var data = new byte[1024];
        for (var i = 0; i < 1024; i++) {
            data[i] = (byte) i;
        }

        var ms = new MemoryStream(data);
        var ps = new PatchedStream(ms) {
            IgnoreBefore = 12,
            IgnoreAfter = 24,
            BufferBefore = new byte[] {0x12, 0x25},
            BufferAfter = new byte[] {0x01, 0x23}
        };
        ps.Seek(25, SeekOrigin.Begin);
        Assert.Equal(35, ps.ReadByte());

        ps.Seek(-3, SeekOrigin.End);
        Assert.Equal(231, ps.ReadByte());
        Assert.Equal(0x01, ps.ReadByte());
        Assert.Equal(0x23, ps.ReadByte());
        Assert.Equal(-1, ps.ReadByte());

        ps.Seek(0, SeekOrigin.Begin);
        Assert.Equal(0x12, ps.ReadByte());
        Assert.Equal(0x25, ps.ReadByte());
    }
}