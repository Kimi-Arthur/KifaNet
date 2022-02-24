using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using NLog;

namespace Kifa.Service; 

public class KifaActionResult {
    public static readonly KifaActionResult Success = new() {
        Status = KifaActionStatus.OK
    };

    public static readonly KifaActionResult UnknownError = new() {
        Status = KifaActionStatus.Error,
        Message = "Unknown Error"
    };

    public static KifaActionResult FromAction(Action action) {
        try {
            action.Invoke();
        } catch (Exception ex) {
            return new KifaActionResult {
                Status = KifaActionStatus.Error,
                Message = ex.ToString()
            };
        }

        return Success;
    }

    public static KifaActionResult FromAction(Func<KifaActionResult> action) {
        try {
            return action.Invoke();
        } catch (Exception ex) {
            return new KifaActionResult {
                Status = KifaActionStatus.Error,
                Message = ex.ToString()
            };
        }
    }

    public KifaActionResult And(KifaActionResult nextResult) =>
        Status == KifaActionStatus.OK ? nextResult : this;

    public KifaActionResult And(Action nextAction) =>
        Status == KifaActionStatus.OK ? FromAction(nextAction) : this;

    public KifaActionResult And(Func<KifaActionResult> nextAction) =>
        Status == KifaActionStatus.OK ? nextAction() : this;

    public KifaActionResult<TValue> And<TValue>(KifaActionResult<TValue> nextResult) =>
        Status == KifaActionStatus.OK ? nextResult : new KifaActionResult<TValue>(this);

    public KifaActionResult<TValue> And<TValue>(Func<TValue> nextAction) =>
        Status == KifaActionStatus.OK
            ? KifaActionResult<TValue>.FromAction(nextAction)
            : new KifaActionResult<TValue>(this);

    [JsonConverter(typeof(StringEnumConverter))]
    public virtual KifaActionStatus Status { get; set; }

    public virtual string? Message { get; set; }

    public override string ToString() =>
        string.IsNullOrEmpty(Message) ? Status.ToString() : $"{Status} ({Message})";
}

public class KifaBatchActionResult : KifaActionResult {
    public List<KifaActionResult> Results { get; set; } = new List<KifaActionResult>();

    public KifaBatchActionResult Add(KifaActionResult moreResult) {
        Results.Add(moreResult);
        return this;
    }

    [JsonConverter(typeof(StringEnumConverter))]
    public override KifaActionStatus Status => Results.Max(r => r.Status);

    public override string Message => string.Join("; ", Results.Where(r => r.Message != null));
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

    public TValue? Response { get; set; }

    public static KifaActionResult<TValue> FromAction(Func<TValue> action) {
        try {
            return new KifaActionResult<TValue>(action.Invoke());
        } catch (Exception ex) {
            return new KifaActionResult<TValue> {
                Status = KifaActionStatus.Error,
                Message = ex.ToString()
            };
        }
    }
}

public static class KifaActionResultLogger {
    public static KifaActionResult LogResult(this Logger logger, KifaActionResult result,
        string action) {
        logger.Log(result.Status == KifaActionStatus.OK ? LogLevel.Info : LogLevel.Warn,
            $"{action}: {result}");
        return result;
    }

    public static KifaActionResult<TValue> LogResult<TValue>(this Logger logger,
        KifaActionResult<TValue> result, string action) {
        logger.Log(result.Status == KifaActionStatus.OK ? LogLevel.Info : LogLevel.Warn,
            $"{action}: {result}");
        return result;
    }
}