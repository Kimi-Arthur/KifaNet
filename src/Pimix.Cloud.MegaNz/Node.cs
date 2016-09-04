namespace CG.Web.MegaApiClient
{
    using System;
    using System.Diagnostics;
    using System.Linq;
    using System.Runtime.Serialization;

    using Newtonsoft.Json;

    [DebuggerDisplay("Type: {Type} - Name: {Name} - Id: {Id}")]
    public class Node
    {
        private static readonly DateTime OriginalDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0);

        private Node()
        {
        }

        #region Public properties

        [JsonProperty("h")]
        public string Id { get; private set; }

        [JsonProperty("p")]
        public string ParentId { get; private set; }

        [JsonProperty("u")]
        public string Owner { get; private set; }

        [JsonProperty("su")]
        public string SharingId { get; private set; }

        [JsonProperty("sk")]
        private string SharingKey { get; set; }

        [JsonProperty("fa")]
        public string SerializedFileAttributes { get; private set; }

        [JsonIgnore]
        public byte[] Key { get; private set; }

        [JsonIgnore]
        public byte[] FullKey { get; private set; }
                
        #endregion

        #region Deserialization

        [JsonProperty("a")]
        private string SerializedAttributes { get; set; }

        [JsonProperty("k")]
        private string SerializedKey { get; set; }

        [OnDeserialized]
        public void OnDeserialized(StreamingContext ctx)
        {
            object[] context = (object[])ctx.Context;
            GetNodesResponse nodesResponse = (GetNodesResponse)context[0];
            if (context.Length == 1)
            {
                // Add key from incoming sharing.
                if (this.SharingKey != null)
                {
                    nodesResponse.SharedKeys.Add(new GetNodesResponse.SharedKey(this.Id, this.SharingKey));
                }
                return;
            }
            else
            {
                byte[] masterKey = (byte[])context[1];

                if (this.Type == NodeType.File || this.Type == NodeType.Directory)
                {
                    // There are cases where the SerializedKey property contains multiple keys separated with /
                    // This can occur when a folder is shared and the parent is shared too.
                    // Both keys are working so we use the first one
                    string serializedKey = this.SerializedKey.Split('/')[0];
                    int splitPosition = serializedKey.IndexOf(":", StringComparison.InvariantCulture);
                    byte[] encryptedKey = serializedKey.Substring(splitPosition + 1).FromBase64();

                    this.FullKey = Crypto.DecryptKey(encryptedKey, masterKey);

                    if (this.Type == NodeType.File)
                    {
                        this.Key = Crypto.GetPartsFromDecryptedKey(this.FullKey);
                    }
                    else
                    {
                        this.Key = this.FullKey;
                    }

                    Attributes attributes = Crypto.DecryptAttributes(this.SerializedAttributes.FromBase64(), this.Key);
                    this.Name = attributes.Name;
                }
            }
        }

        #endregion

        #region Equality

        public bool Equals(Node other)
        {
            return other != null && this.Id == other.Id;
        }

        public override int GetHashCode()
        {
            return this.Id.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            return this.Equals(obj as Node);
        }

        #endregion

        [JsonIgnore]
        public string Name { get; protected set; }

        [JsonProperty("s")]
        public long Size { get; protected set; }

        [JsonProperty("t")]
        public NodeType Type { get; protected set; }
    }

}
