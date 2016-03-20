using System;
using System.Linq;
using CG.Web.MegaApiClient;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace PimixTest.Cloud.MegaNz
{
    [TestClass]
    public class MegaApiClientTests
    {
        //[TestMethod]
        public void MegaApiClientBasicTest()
        {
            MegaApiClient client = new MegaApiClient();

            client.Login("pimixserver+test@gmail.com", "Pny3YQzV");
            var nodes = client.GetNodes();

            INode root = nodes.Single(n => n.Type == NodeType.Root);
            INode myFolder = client.CreateFolder("Upload", root);

            INode myFile = client.Upload("data.bin", myFolder);

            Uri downloadUrl = client.GetDownloadLink(myFile);
            Assert.IsNotNull(downloadUrl);
        }
    }
}
