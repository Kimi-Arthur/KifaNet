using System;

namespace Kifa.Tools {
    public class KifaExecutionException : Exception {
        public KifaExecutionException() {
        }

        public KifaExecutionException(string? message) : base(message) {
        }

        public KifaExecutionException(string? message, Exception? innerException) : base(message, innerException) {
        }
    }
}
