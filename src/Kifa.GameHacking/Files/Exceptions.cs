using System;

namespace Kifa.GameHacking.Files {
    public class DecodeException : Exception {
        public DecodeException() {
        }

        public DecodeException(string message)
            : base(message) {
        }

        public DecodeException(string message, Exception inner)
            : base(message, inner) {
        }
    }
}
