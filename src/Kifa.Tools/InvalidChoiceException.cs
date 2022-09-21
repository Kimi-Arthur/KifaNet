using System;

namespace Kifa.Tools;

public class InvalidChoiceException : Exception {
    public InvalidChoiceException() {
    }

    public InvalidChoiceException(string message) : base(message) {
    }

    public InvalidChoiceException(string message, Exception inner) : base(message, inner) {
    }
}
