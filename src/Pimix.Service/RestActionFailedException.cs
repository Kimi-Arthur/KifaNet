using System;

namespace Pimix.Service {
    class RestActionFailedException : Exception {
        public RestActionResult Result { get; set; }

        public override string ToString() => $"Action failed with {Result}.";
    }
}
