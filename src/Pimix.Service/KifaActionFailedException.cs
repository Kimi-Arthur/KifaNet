using System;

namespace Pimix.Service {
    class KifaActionFailedException : Exception {
        public KifaActionResult ActionResult { get; set; }

        public override string ToString() => $"Action failed with {ActionResult}.";
    }
}
