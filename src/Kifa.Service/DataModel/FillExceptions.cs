// No details needed in this case.

using System;

public class NoNeedToFillException : Exception {
}

public class UnableToFillException : Exception {
    public UnableToFillException() {
    }

    public UnableToFillException(string message) : base(message) {
    }

    public UnableToFillException(string message, Exception inner) : base(message, inner) {
    }
}

public class DataNotFoundException : UnableToFillException {
    public DataNotFoundException() {
    }

    public DataNotFoundException(string message) : base(message) {
    }

    public DataNotFoundException(string message, Exception inner) : base(message, inner) {
    }
}
