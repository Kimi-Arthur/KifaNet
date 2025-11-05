using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using NLog;
using YamlDotNet.Serialization;

namespace Kifa.Service;

public class KifaActionResult {
    [JsonConverter(typeof(StringEnumConverter))]
    public virtual KifaActionStatus Status { get; set; }

    public string? Message { get; set; }

    public static readonly KifaActionResult Success = new() {
        Status = KifaActionStatus.OK
    };

    public static readonly KifaActionResult UnknownError = new() {
        Status = KifaActionStatus.Error,
        Message = "Unknown Error"
    };

    public static Func<KifaActionResult, bool?> ActionValidator
        => result => result.IsAcceptable ? true : result.IsRetryable ? null : false;

    [JsonIgnore]
    [YamlIgnore]
    public bool IsRetryable => Status is KifaActionStatus.Pending or KifaActionStatus.Error;

    [JsonIgnore]
    [YamlIgnore]
    public bool IsAcceptable
        => Status is KifaActionStatus.OK or KifaActionStatus.Warning or KifaActionStatus.Skipped;

    public static KifaActionResult FromAction(Action action) {
        try {
            action.Invoke();
        } catch (KifaActionFailedException ex) {
            return ex.ActionResult;
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

    public static KifaActionResult FromExecutionResult(ExecutionResult result)
        => result.ExitCode == 0
            ? Success
            : new KifaActionResult {
                Status = KifaActionStatus.Error,
                Message = result.StandardError
            };

    public KifaActionResult And(KifaActionResult nextResult)
        => Status == KifaActionStatus.OK ? nextResult : this;

    public KifaActionResult And(Action nextAction)
        => Status == KifaActionStatus.OK ? FromAction(nextAction) : this;

    public KifaActionResult And(Func<KifaActionResult> nextAction)
        => Status == KifaActionStatus.OK ? nextAction() : this;

    public KifaActionResult<TValue> And<TValue>(KifaActionResult<TValue> nextResult)
        => Status == KifaActionStatus.OK ? nextResult : new KifaActionResult<TValue>(this);

    public KifaActionResult<TValue> And<TValue>(Func<TValue> nextAction)
        => Status == KifaActionStatus.OK
            ? KifaActionResult<TValue>.FromAction(nextAction)
            : new KifaActionResult<TValue>(this);

    public override string ToString() => ToString(0);

    public virtual string ToString(int level)
        => string.IsNullOrEmpty(Message) ? $"{Status}" : $"{Status} => {Message}";
}

public class KifaBatchActionResult : KifaActionResult {
    List<(string Item, KifaActionResult Result)> Results { get; set; } = [];

    public KifaBatchActionResult(
        IEnumerable<(string item, KifaActionResult result)>? results = null) {
        if (results != null) {
            AddRange(results);
        }
    }

    public KifaBatchActionResult Add(string item, KifaActionResult moreResult) {
        Results.Add((item, moreResult));
        return this;
    }

    public KifaBatchActionResult AddRange(
        IEnumerable<(string item, KifaActionResult result)> moreResults) {
        foreach (var result in moreResults) {
            Results.Add((result.item, result.result));
        }

        return this;
    }

    [JsonConverter(typeof(StringEnumConverter))]
    public override KifaActionStatus Status
        => Results.Aggregate(KifaActionStatus.OK, (status, item) => status | item.Result.Status);

    public override string ToString() => ToString(0);

    public override string ToString(int level = 0)
        => $"{Status} =>\n{string.Join("\n", Results.Select(r => new string('\t', level + 1) + $"{r.Item}: {r.Result.ToString(level + 1)}"))}";
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

    public static implicit operator KifaActionResult<TValue>(TValue value) => new(value);

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
    public static KifaActionResult LogResult(this Logger Logger, KifaActionResult result,
        string action, LogLevel? defaultLevel = null, bool throwIfError = false) {
        var level = GetLogLevel(result.Status, defaultLevel);
        Logger.Log(level, $"Result of {action}: {result}");
        if (throwIfError && level >= LogLevel.Error) {
            throw new KifaActionFailedException(result);
        }

        return result;
    }

    public static KifaActionResult<TValue> LogResult<TValue>(this Logger Logger,
        KifaActionResult<TValue> result, string action, LogLevel? defaultLevel = null,
        bool throwIfError = false) {
        var level = GetLogLevel(result.Status, defaultLevel);
        Logger.Log(level, $"Result of {action}: {result}");
        if (throwIfError && level >= LogLevel.Error) {
            throw new KifaActionFailedException(result);
        }

        return result;
    }

    static LogLevel GetLogLevel(KifaActionStatus status, LogLevel? defaultLevel)
        => status.HasFlag(KifaActionStatus.Error) || status.HasFlag(KifaActionStatus.BadRequest)
            ?
            LogLevel.Error
            : status.HasFlag(KifaActionStatus.Warning)
                ? LogLevel.Warn
                : status.HasFlag(KifaActionStatus.Pending)
                    ? LogLevel.Debug
                    : defaultLevel ?? LogLevel.Trace;
}
