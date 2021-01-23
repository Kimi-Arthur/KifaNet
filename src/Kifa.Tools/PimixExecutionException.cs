using System;

namespace Kifa.Tools {
    public class PimixExecutionException : Exception {
        public PimixExecutionException() {
        }

        public PimixExecutionException(string? message) : base(message) {
        }

        public PimixExecutionException(string? message, Exception? innerException) : base(message, innerException) {
        }
    }
}
