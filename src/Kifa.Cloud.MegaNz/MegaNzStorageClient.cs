using System;
using System.IO;
using System.Linq;
using System.Threading;
using CG.Web.MegaApiClient;
using Kifa.IO;

namespace Kifa.Cloud.MegaNz;

public class MegaNzStorageClient : StorageClient {
    public static int ChunkSize { get; set; } = 32 << 20; // 32 MiB.

    static MegaNzConfig config;
    string accountId;

    public string AccountId {
        get => accountId;
        set {
            if (value != null && accountId != value) {
                var account = Config.Accounts[value];

                // Update Client.
                Client = new MegaApiClient(new Options(chunksPackSize: ChunkSize));
                Client.Login(account.Username, account.Password);
            }

            accountId = value;
        }
    }

    public MegaApiClient Client { get; private set; }

    static MegaNzConfig Config
        => LazyInitializer.EnsureInitialized(ref config, () => MegaNzConfig.Client.Get("default"));

    public override string Type => "mega";

    public override string Id => AccountId;

    // Comment out as this doesn't work now.
    public override void Move(string sourcePath, string destinationPath) {
        var sourceNode = GetNode(sourcePath);
        Client.Move(sourceNode, GetNode(GetParent(destinationPath), true));
        Client.Rename(sourceNode, GetName(destinationPath));
    }

    public override void Delete(string path) {
        var node = GetNode(path);
        if (node != null) {
            Client.Delete(node, false);
        }
    }

    public override void Touch(string path) {
        throw new NotImplementedException();
    }

    public override long Length(string path)
        => GetNode(path)?.Size ?? throw new FileNotFoundException();

    public override Stream OpenRead(string path) => Client.Download(GetNode(path));

    public override void Write(string path, Stream stream) {
        var folder = GetNode(GetParent(path), true);
        var name = path.Substring(path.LastIndexOf('/') + 1);
        Client.Upload(stream, name, folder);
    }

    static string GetParent(string path) => path[..path.LastIndexOf('/')];
    static string GetName(string path) => path[(path.LastIndexOf('/') + 1)..];

    INode GetNode(string path, bool createParents = false) {
        path = path.Trim('/');
        var nodes = Client.GetNodes();

        var parent = nodes.Single(n => n.Type == NodeType.Root);
        var node = parent;

        foreach (var p in path.Split('/')) {
            node = nodes.SingleOrDefault(n => n.ParentId == parent.Id && n.Name == p);
            if (node == null) {
                if (createParents) {
                    node = Client.CreateFolder(p, parent);
                    nodes = Client.GetNodes();
                } else {
                    return null;
                }
            }

            parent = node;
        }

        return node;
    }
}
