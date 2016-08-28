namespace CG.Web.MegaApiClient
{
    using System;
    using System.Collections.Generic;
    using System.Runtime.Serialization;

    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    #region Base

    internal abstract class RequestBase
    {
        protected RequestBase(string action)
        {
            this.Action = action;
        }

        [JsonProperty("a")]
        public string Action { get; private set; }
    }

    #endregion


    #region Login

    internal class LoginRequest : RequestBase
    {
        public LoginRequest(string userHandle, string passwordHash)
          : base("us")
        {
            this.UserHandle = userHandle;
            this.PasswordHash = passwordHash;
        }

        [JsonProperty("user")]
        public string UserHandle { get; private set; }

        [JsonProperty("uh")]
        public string PasswordHash { get; private set; }
    }

    internal class LoginResponse
    {
        [JsonProperty("csid")]
        public string SessionId { get; private set; }

        [JsonProperty("tsid")]
        public string TemporarySessionId { get; private set; }

        [JsonProperty("privk")]
        public string PrivateKey { get; private set; }

        [JsonProperty("k")]
        public string MasterKey { get; private set; }
    }

    internal class AnonymousLoginRequest : RequestBase
    {
        public AnonymousLoginRequest(string masterKey, string temporarySession)
          : base("up")
        {
            this.MasterKey = masterKey;
            this.TemporarySession = temporarySession;
        }

        [JsonProperty("k")]
        public string MasterKey { get; set; }

        [JsonProperty("ts")]
        public string TemporarySession { get; set; }
    }

    #endregion

    #region AccountInformation

    internal class AccountInformationRequest : RequestBase
    {
        public AccountInformationRequest()
          : base("uq")
        {
        }

        [JsonProperty("strg")]
        public int Storage { get { return 1; } }

        [JsonProperty("xfer")]
        public int Transfer { get { return 0; } }

        [JsonProperty("pro")]
        public int AccountType { get { return 0; } }
    }

    public class AccountInformationResponse
    {
        [JsonProperty("mstrg")]
        public long TotalQuota { get; private set; }

        [JsonProperty("cstrg")]
        public long UsedQuota { get; private set; }
    }

    #endregion


    #region Nodes

    internal class GetNodesRequest : RequestBase
    {
        public GetNodesRequest()
          : base("f")
        {
            this.c = 1;
        }

        public int c { get; private set; }
    }

    internal class GetNodesResponse
    {
        public Node[] Nodes { get; private set; }

        [JsonProperty("f")]
        public JRaw NodesSerialized { get; private set; }

        [JsonProperty("ok")]
        public List<SharedKey> SharedKeys { get; private set; }

        [OnDeserialized]
        public void OnDeserialized(StreamingContext ctx)
        {
            JsonSerializerSettings settings = new JsonSerializerSettings();

            // First Nodes deserialization to retrieve all shared keys
            settings.Context = new StreamingContext(StreamingContextStates.All, new[] { this });
            JsonConvert.DeserializeObject<Node[]>(this.NodesSerialized.ToString(), settings);

            // Deserialize nodes
            settings.Context = new StreamingContext(StreamingContextStates.All, new[] { this, ctx.Context });
            this.Nodes = JsonConvert.DeserializeObject<Node[]>(this.NodesSerialized.ToString(), settings);
        }

        internal class SharedKey
        {
            public SharedKey(string id, string key)
            {
                this.Id = id;
                this.Key = key;
            }

            [JsonProperty("h")]
            public string Id { get; private set; }

            [JsonProperty("k")]
            public string Key { get; private set; }
        }
    }

    #endregion


    #region Delete

    internal class DeleteRequest : RequestBase
    {
        public DeleteRequest(Node node)
          : base("d")
        {
            this.Node = node.Id;
        }

        [JsonProperty("n")]
        public string Node { get; private set; }
    }

    #endregion


    #region Link

    internal class GetDownloadLinkRequest : RequestBase
    {
        public GetDownloadLinkRequest(Node node)
          : base("l")
        {
            this.Id = node.Id;
        }

        [JsonProperty("n")]
        public string Id { get; private set; }
    }

    #endregion


    #region Create node

    internal class CreateNodeRequest : RequestBase
    {
        private CreateNodeRequest(Node parentNode, NodeType type, string attributes, string encryptedKey, byte[] key, string completionHandle)
          : base("p")
        {
            this.ParentId = parentNode.Id;
            this.Nodes = new[]
            {
                new CreateNodeRequestData
                {
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

        public static CreateNodeRequest CreateFileNodeRequest(Node parentNode, string attributes, string encryptedkey, byte[] fileKey, string completionHandle)
        {
            return new CreateNodeRequest(parentNode, NodeType.File, attributes, encryptedkey, fileKey, completionHandle);
        }

        public static CreateNodeRequest CreateFolderNodeRequest(Node parentNode, string attributes, string encryptedkey, byte[] key)
        {
            return new CreateNodeRequest(parentNode, NodeType.Directory, attributes, encryptedkey, key, "xxxxxxxx");
        }

        internal class CreateNodeRequestData
        {
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

    internal class UploadUrlRequest : RequestBase
    {
        public UploadUrlRequest(long fileSize)
          : base("u")
        {
            this.Size = fileSize;
        }

        [JsonProperty("s")]
        public long Size { get; private set; }
    }

    internal class UploadUrlResponse
    {
        [JsonProperty("p")]
        public string Url { get; private set; }
    }

    #endregion


    #region DownloadRequest

    internal class DownloadUrlRequest : RequestBase
    {
        public DownloadUrlRequest(Node node)
          : base("g")
        {
            this.Id = node.Id;
        }

        public int g { get { return 1; } }

        [JsonProperty("n")]
        public string Id { get; private set; }
    }

    internal class DownloadUrlRequestFromId : RequestBase
    {
        public DownloadUrlRequestFromId(string id)
          : base("g")
        {
            this.Id = id;
        }

        public int g { get { return 1; } }

        [JsonProperty("p")]
        public string Id { get; private set; }
    }

    internal class DownloadUrlResponse
    {
        [JsonProperty("g")]
        public string Url { get; private set; }

        [JsonProperty("s")]
        public long Size { get; private set; }

        [JsonProperty("at")]
        public string SerializedAttributes { get; set; }
    }

    #endregion


    #region Move

    internal class MoveRequest : RequestBase
    {
        public MoveRequest(Node node, Node destinationParentNode)
          : base("m")
        {
            this.Id = node.Id;
            this.DestinationParentId = destinationParentNode.Id;
        }

        [JsonProperty("n")]
        public string Id { get; private set; }

        [JsonProperty("t")]
        public string DestinationParentId { get; private set; }
    }

    #endregion


    #region Attributes

    internal class Attributes
    {
        public Attributes(string name)
        {
            this.Name = name;
        }

        [JsonProperty("n")]
        public string Name { get; set; }
    }

    #endregion
}
