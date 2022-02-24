using System.Collections.Generic;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace CG.Web.MegaApiClient;

#region Base

abstract class RequestBase {
    protected RequestBase(string action) {
        Action = action;
    }

    [JsonProperty("a")]
    public string Action { get; private set; }
}

#endregion

#region Login

class LoginRequest : RequestBase {
    public LoginRequest(string userHandle, string passwordHash) : base("us") {
        UserHandle = userHandle;
        PasswordHash = passwordHash;
    }

    [JsonProperty("user")]
    public string UserHandle { get; private set; }

    [JsonProperty("uh")]
    public string PasswordHash { get; private set; }
}

class LoginResponse {
    [JsonProperty("csid")]
    public string SessionId { get; private set; }

    [JsonProperty("tsid")]
    public string TemporarySessionId { get; private set; }

    [JsonProperty("privk")]
    public string PrivateKey { get; private set; }

    [JsonProperty("k")]
    public string MasterKey { get; private set; }
}

class AnonymousLoginRequest : RequestBase {
    public AnonymousLoginRequest(string masterKey, string temporarySession) : base("up") {
        MasterKey = masterKey;
        TemporarySession = temporarySession;
    }

    [JsonProperty("k")]
    public string MasterKey { get; set; }

    [JsonProperty("ts")]
    public string TemporarySession { get; set; }
}

#endregion

#region AccountInformation

class AccountInformationRequest : RequestBase {
    public AccountInformationRequest() : base("uq") {
    }

    [JsonProperty("strg")]
    public int Storage => 1;

    [JsonProperty("xfer")]
    public int Transfer => 0;

    [JsonProperty("pro")]
    public int AccountType => 0;
}

public class AccountInformationResponse {
    [JsonProperty("mstrg")]
    public long TotalQuota { get; private set; }

    [JsonProperty("cstrg")]
    public long UsedQuota { get; private set; }
}

#endregion

#region Nodes

class GetNodesRequest : RequestBase {
    public GetNodesRequest() : base("f") {
        c = 1;
    }

    public int c { get; }
}

class GetNodesResponse {
    public Node[] Nodes { get; private set; }

    [JsonProperty("f")]
    public JRaw NodesSerialized { get; private set; }

    [JsonProperty("ok")]
    public List<SharedKey> SharedKeys { get; private set; }

    [OnDeserialized]
    public void OnDeserialized(StreamingContext ctx) {
        var settings = new JsonSerializerSettings();

        // First Nodes deserialization to retrieve all shared keys
        settings.Context = new StreamingContext(StreamingContextStates.All, new[] { this });
        JsonConvert.DeserializeObject<Node[]>(NodesSerialized.ToString(), settings);

        // Deserialize nodes
        settings.Context =
            new StreamingContext(StreamingContextStates.All, new[] { this, ctx.Context });
        Nodes = JsonConvert.DeserializeObject<Node[]>(NodesSerialized.ToString(), settings);
    }

    internal class SharedKey {
        public SharedKey(string id, string key) {
            Id = id;
            Key = key;
        }

        [JsonProperty("h")]
        public string Id { get; private set; }

        [JsonProperty("k")]
        public string Key { get; private set; }
    }
}

#endregion

#region Delete

class DeleteRequest : RequestBase {
    public DeleteRequest(Node node) : base("d") {
        Node = node.Id;
    }

    [JsonProperty("n")]
    public string Node { get; private set; }
}

#endregion

#region Link

class GetDownloadLinkRequest : RequestBase {
    public GetDownloadLinkRequest(Node node) : base("l") {
        Id = node.Id;
    }

    [JsonProperty("n")]
    public string Id { get; private set; }
}

#endregion

#region Create node

class CreateNodeRequest : RequestBase {
    CreateNodeRequest(Node parentNode, NodeType type, string attributes, string encryptedKey,
        string completionHandle) : base("p") {
        ParentId = parentNode.Id;
        Nodes = new[] {
            new CreateNodeRequestData {
                Attributes = attributes,
                Key = encryptedKey,
                Type = type,
                CompletionHandle = completionHandle
            }
        };
    }

    [JsonProperty("t")]
    public string ParentId { get; private set; }

    [JsonProperty("n")]
    public CreateNodeRequestData[] Nodes { get; private set; }

    public static CreateNodeRequest CreateFileNodeRequest(Node parentNode, string attributes,
        string encryptedkey, string completionHandle)
        => new(parentNode, NodeType.File, attributes, encryptedkey, completionHandle);

    public static CreateNodeRequest CreateFolderNodeRequest(Node parentNode, string attributes,
        string encryptedkey)
        => new(parentNode, NodeType.Directory, attributes, encryptedkey, "xxxxxxxx");

    internal class CreateNodeRequestData {
        [JsonProperty("h")]
        public string CompletionHandle { get; set; }

        [JsonProperty("t")]
        public NodeType Type { get; set; }

        [JsonProperty("a")]
        public string Attributes { get; set; }

        [JsonProperty("k")]
        public string Key { get; set; }
    }
}

#endregion

#region UploadRequest

class UploadUrlRequest : RequestBase {
    public UploadUrlRequest(long fileSize) : base("u") {
        Size = fileSize;
    }

    [JsonProperty("s")]
    public long Size { get; private set; }
}

class UploadUrlResponse {
    [JsonProperty("p")]
    public string Url { get; private set; }
}

#endregion

#region DownloadRequest

class DownloadUrlRequest : RequestBase {
    public DownloadUrlRequest(Node node) : base("g") {
        Id = node.Id;
    }

    public int g => 1;

    [JsonProperty("n")]
    public string Id { get; private set; }
}

class DownloadUrlRequestFromId : RequestBase {
    public DownloadUrlRequestFromId(string id) : base("g") {
        Id = id;
    }

    public int g => 1;

    [JsonProperty("p")]
    public string Id { get; private set; }
}

class DownloadUrlResponse {
    [JsonProperty("g")]
    public string Url { get; private set; }

    [JsonProperty("s")]
    public long Size { get; private set; }

    [JsonProperty("at")]
    public string SerializedAttributes { get; set; }
}

#endregion

#region Move

class MoveRequest : RequestBase {
    public MoveRequest(Node node, Node destinationParentNode) : base("m") {
        Id = node.Id;
        DestinationParentId = destinationParentNode.Id;
    }

    [JsonProperty("n")]
    public string Id { get; private set; }

    [JsonProperty("t")]
    public string DestinationParentId { get; private set; }
}

#endregion

#region Attributes

class Attributes {
    public Attributes(string name) {
        Name = name;
    }

    [JsonProperty("n")]
    public string Name { get; set; }
}

#endregion
