using System;

namespace Kifa.Service;

[Flags]
public enum KifaActionStatus {
    OK,
    BadRequest = 1,
    Warning = 2,
    Error = 4,
    // Needs more work.
    Pending = 8
}
