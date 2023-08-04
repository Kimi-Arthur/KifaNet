using System;
using System.IO;

namespace Kifa.GameHacking;

public class DecodeException : IOException {
    public DecodeException() {
    }

    public DecodeException(string message) : base(message) {
    }

    public DecodeException(string message, Exception inner) : base(message, inner) {
    }
}
