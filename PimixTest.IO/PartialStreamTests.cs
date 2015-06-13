using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Pimix.IO;

namespace PimixTest.IO
{
    [TestClass]
    public class PartialStreamTests
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
            PartialStream ps = new PartialStream(ms, 12, 24);
            ps.Seek(23, SeekOrigin.Begin);
            Assert.AreEqual(35, ps.ReadByte());
        }
    }
}
