using System.IO;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Pimix.Utilities;

namespace FileUtilityTests
{
    [TestClass]
    public class FileUtilityTests
    {
        [TestMethod]
        public void GetInformationFromStreamTest()
        {
            UTF8Encoding encoding = new UTF8Encoding();
            Stream s = new MemoryStream(encoding.GetBytes("Test1"));
            Assert.AreEqual(5, FileUtility.GetInformation(s, FileProperties.Size).Size);

            s = new MemoryStream(encoding.GetBytes("中文长度不一样"));
            Assert.AreEqual(21, FileUtility.GetInformation(s, FileProperties.Size).Size);
        }
    }
}
