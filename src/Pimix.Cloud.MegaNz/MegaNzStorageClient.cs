using System;
using System.IO;
using System.Linq;
using CG.Web.MegaApiClient;
using Pimix.IO;

namespace Pimix.Cloud.MegaNz
{
    public class MegaNzStorageClient : StorageClient
    {

        string accountId;
        public string AccountId
        {
            get
            {
                return accountId;
            }
            set
            {
                accountId = value;
                Account = Config.Accounts[accountId];
            }
        }

        public AccountInfo Account { get; private set; }

        public static MegaNzConfig Config { get; set; }

        public override void Copy(string sourcePath, string destinationPath)
        {
            throw new NotImplementedException();
        }

        public override void Delete(string path)
        {
            throw new NotImplementedException();
        }

        public override bool Exists(string path)
        {
            throw new NotImplementedException();
        }

        public override Stream OpenRead(string path)
        {
            MegaApiClient client = new MegaApiClient();

            client.Login(Account.Username, Account.Password);

            var nodes = client.GetNodes();

            INode node = nodes.Single(n => n.Type == NodeType.Root);
            foreach (var p in path.Split('/'))
            {
                Console.WriteLine(node.Name);
                node = client.GetNodes(node).Single(n => n.Name == p);
            }

            MemoryStream memoryStream = new MemoryStream();
            client.Download(node).CopyTo(memoryStream);
            return memoryStream;
        }

        public override void Write(string path, Stream stream = null, FileInformation fileInformation = null, bool match = true)
        {
            throw new NotImplementedException();
        }
    }
}
