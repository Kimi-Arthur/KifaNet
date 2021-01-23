using System;
using System.Diagnostics;
using System.Runtime.Serialization;
using Newtonsoft.Json;

namespace CG.Web.MegaApiClient {
    [DebuggerDisplay("Type: {Type} - Name: {Name} - Id: {Id}")]
    public class Node {
        static readonly DateTime OriginalDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0);

        Node() {
        }

        [JsonIgnore]
        public string Name { get; protected set; }

        [JsonProperty("s")]
        public long Size { get; protected set; }

        [JsonProperty("t")]
        public NodeType Type { get; protected set; }

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
        string SharingKey { get; set; }

        [JsonProperty("fa")]
        public string SerializedFileAttributes { get; private set; }

        [JsonIgnore]
        public byte[] Key { get; private set; }

        [JsonIgnore]
        public byte[] FullKey { get; private set; }

        #endregion

        #region Deserialization

        [JsonProperty("a")]
        string SerializedAttributes { get; set; }

        [JsonProperty("k")]
        string SerializedKey { get; set; }

        [OnDeserialized]
        public void OnDeserialized(StreamingContext ctx) {
            var context = (object[]) ctx.Context;
            var nodesResponse = (GetNodesResponse) context[0];
            if (context.Length == 1) {
                // Add key from incoming sharing.
                if (SharingKey != null) {
                    nodesResponse.SharedKeys.Add(new GetNodesResponse.SharedKey(Id, SharingKey));
                }
            } else {
                var masterKey = (byte[]) context[1];

                if (Type == NodeType.File || Type == NodeType.Directory) {
                    // There are cases where the SerializedKey property contains multiple keys separated with /
                    // This can occur when a folder is shared and the parent is shared too.
                    // Both keys are working so we use the first one
                    var serializedKey = SerializedKey.Split('/')[0];
                    var splitPosition =
                        serializedKey.IndexOf(":", StringComparison.InvariantCulture);
                    var encryptedKey = serializedKey.Substring(splitPosition + 1).FromBase64();

                    FullKey = Crypto.DecryptKey(encryptedKey, masterKey);

                    if (Type == NodeType.File) {
                        Key = Crypto.GetPartsFromDecryptedKey(FullKey);
                    } else {
                        Key = FullKey;
                    }

                    var attributes =
                        Crypto.DecryptAttributes(SerializedAttributes.FromBase64(), Key);
                    Name = attributes.Name;
                }
            }
        }

        #endregion

        #region Equality

        public bool Equals(Node other) => other != null && Id == other.Id;

        public override int GetHashCode() => Id.GetHashCode();

        public override bool Equals(object obj) => Equals(obj as Node);

        #endregion
    }
}
