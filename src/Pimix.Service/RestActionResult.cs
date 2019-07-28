using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Pimix.Service {
    public class RestActionResult {
        [JsonConverter(typeof(StringEnumConverter))]
        public RestActionStatus Status { get; set; }

        public string Message { get; set; }

        public override string ToString() => $"status: {Status}, message: {Message}";
    }

    public class RestActionResult<TValue> : RestActionResult {
        public RestActionResult(TValue response) {
            Response = response;
            Status = RestActionStatus.OK;
        }

        public RestActionResult(RestActionStatus status, string message) {
            Status = status;
            Message = message;
        }

        public TValue Response { get; set; }
    }
}
