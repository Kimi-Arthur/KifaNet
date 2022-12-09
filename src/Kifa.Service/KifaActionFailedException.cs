using System;

namespace Kifa.Service;

public class KifaActionFailedException : Exception {
    public KifaActionResult ActionResult { get; set; }

    public KifaActionFailedException(KifaActionResult actionResult) {
        ActionResult = actionResult;
    }

    public override string ToString() => $"Action failed with {ActionResult}.";
}
