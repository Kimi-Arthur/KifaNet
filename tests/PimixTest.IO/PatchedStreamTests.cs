using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Pimix.IO;

namespace PimixTest.IO
{
    [TestClass]
    public class PatchedStreamTests
    {
        [TestMethod]
        public void PartialStreamBasicTest()
        {
            byte[] data = new byte[1024];
            for (int i = 0; i < 1024; i++)
            {
                data[i] = (byte)i;
            }

            MemoryStream ms = new MemoryStream(data);
            PatchedStream ps = new PatchedStream(ms)
            {
                IgnoreBefore = 12,
                IgnoreAfter = 24,
                BufferBefore = new byte[] { 0x12, 0x25 },
                BufferAfter = new byte[] { 0x01, 0x23 }
            };
            ps.Seek(25, SeekOrigin.Begin);
            Assert.AreEqual(35, ps.ReadByte());

            ps.Seek(-3, SeekOrigin.End);
            Assert.AreEqual(231, ps.ReadByte());
            Assert.AreEqual(0x01, ps.ReadByte());
            Assert.AreEqual(0x23, ps.ReadByte());
            Assert.AreEqual(-1, ps.ReadByte(), "Expected to be at end.");

            ps.Seek(0, SeekOrigin.Begin);
            Assert.AreEqual(0x12, ps.ReadByte());
            Assert.AreEqual(0x25, ps.ReadByte());
        }
    }
}
