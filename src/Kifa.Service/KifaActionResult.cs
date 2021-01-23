using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Kifa.Service {
    public class KifaActionResult {
        public static readonly KifaActionResult SuccessActionResult = new() {Status = KifaActionStatus.OK};

        public static KifaActionResult FromAction(Action action) {
            try {
                action.Invoke();
            } catch (Exception ex) {
                return new KifaActionResult {Status = KifaActionStatus.Error, Message = ex.ToString()};
            }

            return SuccessActionResult;
        }

        public KifaActionResult And(KifaActionResult nextResult) => Status == KifaActionStatus.OK ? nextResult : this;

        public KifaActionResult And(Action nextAction) => Status == KifaActionStatus.OK ? FromAction(nextAction) : this;

        public KifaActionResult And(Func<KifaActionResult> nextAction) =>
            Status == KifaActionStatus.OK ? nextAction() : this;

        public KifaActionResult<TValue> And<TValue>(KifaActionResult<TValue> nextResult) =>
            Status == KifaActionStatus.OK ? nextResult : new KifaActionResult<TValue>(this);

        public KifaActionResult<TValue> And<TValue>(Func<TValue> nextAction) =>
            Status == KifaActionStatus.OK
                ? KifaActionResult<TValue>.FromAction(nextAction)
                : new KifaActionResult<TValue>(this);

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

        public KifaActionResult(KifaActionResult result) {
            Status = result.Status;
            Message = result.Message;
        }

        public TValue Response { get; set; }

        public static KifaActionResult<TValue> FromAction(Func<TValue> action) {
            try {
                return new KifaActionResult<TValue>(action.Invoke());
            } catch (Exception ex) {
                return new KifaActionResult<TValue> {Status = KifaActionStatus.Error, Message = ex.ToString()};
            }
        }
    }
}
