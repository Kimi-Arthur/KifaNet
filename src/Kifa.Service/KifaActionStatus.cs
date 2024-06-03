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
    Pending = 16
}
