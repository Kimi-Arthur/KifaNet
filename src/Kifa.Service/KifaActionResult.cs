using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Kifa.Service {
    public class KifaActionResult {
        public static readonly KifaActionResult SuccessActionResult = new KifaActionResult {Status = KifaActionStatus.OK};

        public static KifaActionResult FromAction(Action action) {
            try {
                action.Invoke();
            } catch (Exception ex) {
                return new KifaActionResult {Status = KifaActionStatus.Error, Message = ex.ToString()};
            }

            return SuccessActionResult;
        }

        [JsonConverter(typeof(StringEnumConverter))]
        public KifaActionStatus Status { get; set; }

        public string Message { get; set; }

        public override string ToString() => $"status: {Status}, message: {Message}";
    }

    public class KifaActionResult<TValue> : KifaActionResult {
        public KifaActionResult() {
        }

        public KifaActionResult(TValue response) {
            Response = response;
            Status = KifaActionStatus.OK;
        }

        public KifaActionResult(KifaActionStatus status, string message) {
            Status = status;
            Message = message;
        }

        public TValue Response { get; set; }
    }
}
