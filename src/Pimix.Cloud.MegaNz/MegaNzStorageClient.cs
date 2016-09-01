using System.IO;
using System.Linq;
using CG.Web.MegaApiClient;
using Pimix.IO;

namespace Pimix.Cloud.MegaNz
{
    public class MegaNzStorageClient : StorageClient
    {
        public static StorageClient Get(string fileSpec)
        {
            var specs = fileSpec.Split(new char[] { ';' });
            foreach (var spec in specs)
            {
                if (spec.StartsWith("mega:"))
                {
                    return new MegaNzStorageClient
                    {
                        AccountId = spec.Substring(5)
                    };
                }
            }

            return null;
        }

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

        public override string ToString()
            => $"mega:{AccountId}";

        public MegaApiClient Client { get; private set; }

        public static MegaNzConfig Config { get; set; }

        // Comment out as this doesn't work now.
        //public override void Move(string sourcePath, string destinationPath)
        //{
        //    Client.Move(GetNode(sourcePath), GetNode(GetParent(destinationPath), true));
        //}

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
            var folder = GetNode(GetParent(path), true);
            var name = path.Substring(path.LastIndexOf('/') + 1);
            Client.Upload(stream, name, folder);
        }

        string GetParent(string path)
            => path.Substring(0, path.LastIndexOf('/'));

        Node GetNode(string path, bool createParents = false)
        {
            path = path.Trim('/');
            var nodes = Client.GetNodes();

            Node parent = nodes.Single(n => n.Type == NodeType.Root);
            Node node = parent;

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
