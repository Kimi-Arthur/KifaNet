using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Pimix.Service;

namespace Pimix.Cloud.MegaNz
{
    [DataModel("configs")]
    public partial class MegaNzConfig
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("accounts")]
        public Dictionary<string, AccountInfo> Accounts { get; private set; }
    }

    public class AccountInfo
    {
        [JsonProperty("username")]
        public string Username { get; set; }

        [JsonProperty("password")]
        public string Password { get; set; }
    }
}
