using System;

namespace Kifa.Service;

[Flags]
public enum KifaActionStatus {
    // Everything is OK.
    OK,
    // Input or internal state is incorrect. Retrying probably won't help.
    BadRequest = 1,
    // Request is processed successfully. But something unexpected (but acceptable) happened.
    Warning = 2,
    // Request is processed unsuccessfully. But retry will help.
    Error = 4,
    // No real action actaully happened.
    Skipped = 8,
    // The final state is yet to be determined.
    Pending = 16,
    // The action was cancelled before completion.
    Cancelled = 32
}

public static class KifaActionStatusExtensions {
    public static string Format(this KifaActionStatus status)
        => status switch {
            KifaActionStatus.OK => status.ToString().Info(),
            KifaActionStatus.Warning => status.ToString().Warn(),
            KifaActionStatus.Cancelled => status.ToString().Warn(),
            KifaActionStatus.Pending => status.ToString().Debug(),
            KifaActionStatus.BadRequest => status.ToString().Error(),
            KifaActionStatus.Error => status.ToString().Error(),
            KifaActionStatus.Skipped => status.ToString().Trace(),
            _ => status.HasFlag(KifaActionStatus.Error) || status.HasFlag(KifaActionStatus.BadRequest)
                ? status.ToString().Error()
                : status.HasFlag(KifaActionStatus.Cancelled) || status.HasFlag(KifaActionStatus.Warning)
                    ? status.ToString().Warn()
                    : status.HasFlag(KifaActionStatus.Pending)
                        ? status.ToString().Debug()
                        : status.ToString()
        };
}

