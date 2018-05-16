using System;

namespace Pimix.Service {
    public class ActionFailedException : Exception {
        public ActionResult Result { get; set; }

        public override string ToString() => $"Action failed with {Result}.";
    }
}
