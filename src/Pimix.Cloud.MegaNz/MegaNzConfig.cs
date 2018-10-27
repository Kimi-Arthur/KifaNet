using System.Collections.Generic;
using Newtonsoft.Json;
using Pimix.Service;

namespace Pimix.Cloud.MegaNz {
    [DataModel("configs/mega_nz")]
    public class MegaNzConfig {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("accounts")]
        public Dictionary<string, AccountInfo> Accounts { get; private set; }
    }

    public class AccountInfo {
        [JsonProperty("username")]
        public string Username { get; set; }

        [JsonProperty("password")]
        public string Password { get; set; }
    }
}
