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
                if (value != null && accountId != value)
                {
                    var account = Config.Accounts[value];

                    // Update Client.
                    Client = new MegaApiClient();
                    Client.Login(account.Username, account.Password);
                }

                accountId = value;
            }
        }

        public MegaApiClient Client { get; private set; }

        public static MegaNzConfig Config { get; set; }

        public override void Copy(string sourcePath, string destinationPath)
        {
            throw new NotImplementedException();
        }

        public override void Delete(string path)
        {
            var node = GetNode(path);
            if (node != null)
            {
                Client.Delete(node, false);
            }
        }

        public override bool Exists(string path)
            => GetNode(path) != null;

        public override Stream OpenRead(string path)
            => Client.Download(GetNode(path));

        public override void Write(string path, Stream stream = null, FileInformation fileInformation = null, bool match = true)
        {
            var folder = GetNode(path.Substring(0, path.LastIndexOf('/')), true);
            var name = path.Substring(path.LastIndexOf('/') + 1);
            Client.Upload(stream, name, folder);
        }

        INode GetNode(string path, bool createParents = false)
        {
            path = path.TrimStart('/');
            var nodes = Client.GetNodes();

            INode parent = nodes.Single(n => n.Type == NodeType.Root);
            INode node = parent;

            foreach (var p in path.Split('/'))
            {
                node = nodes.SingleOrDefault(n => n.ParentId == parent.Id && n.Name == p);
                if (node == null)
                {
                    if (createParents)
                    {
                        node = Client.CreateFolder(p, parent);
                        nodes = Client.GetNodes();
                    }
                    else
                    {
                        return null;
                    }
                }

                parent = node;
            }

            return node;
        }
    }
}
