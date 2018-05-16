using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Pimix.Service {
    public class ActionResult {
        [JsonProperty("status")]
        [JsonConverter(typeof(StringEnumConverter))]
        public ActionStatusCode StatusCode { get; set; }

        [JsonProperty("message")]
        public string Message { get; set; }

        public override string ToString() => $"status: {StatusCode}, message: {Message}";
    }

    public class ActionResult<ResponseType> : ActionResult {
        [JsonProperty("response")]
        public ResponseType Response { get; set; }
    }
}
