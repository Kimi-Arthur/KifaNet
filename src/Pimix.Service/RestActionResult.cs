using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Pimix.Service {
    class RestActionResult {
        [JsonConverter(typeof(StringEnumConverter))]
        public RestActionStatus Status { get; set; }

        public string Message { get; set; }

        public override string ToString() => $"status: {Status}, message: {Message}";
    }

    class RestActionResult<TResponse> : RestActionResult {
        public TResponse Response { get; set; }
    }
}
