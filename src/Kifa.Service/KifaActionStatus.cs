using System;
using System.Diagnostics.CodeAnalysis;

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
    [return: NotNullIfNotNull(nameof(text))]
    public static string? Format(this KifaActionStatus status, string? text)
        => text == null
            ? null
            : status switch {
                KifaActionStatus.OK => text.Info(),
                KifaActionStatus.Warning => text.Warn(),
                KifaActionStatus.Cancelled => text.Warn(),
                KifaActionStatus.Pending => text.Debug(),
                KifaActionStatus.BadRequest => text.Error(),
                KifaActionStatus.Error => text.Error(),
                KifaActionStatus.Skipped => text.Trace(),
                _ => status.HasFlag(KifaActionStatus.Error) || status.HasFlag(KifaActionStatus.BadRequest)
                    ? text.Error()
                    : status.HasFlag(KifaActionStatus.Cancelled) || status.HasFlag(KifaActionStatus.Warning)
                        ? text.Warn()
                        : status.HasFlag(KifaActionStatus.Pending)
                            ? text.Debug()
                            : text
            };

    public static string Format(this KifaActionStatus status) => status.Format(status.ToString());
}

