using System;

namespace Kifa.Service {
    class KifaActionFailedException : Exception {
        public KifaActionResult ActionResult { get; set; }

        public override string ToString() => $"Action failed with {ActionResult}.";
    }
}
