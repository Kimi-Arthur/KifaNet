using System;

namespace Kifa.Service;

public class NoNeedToFillException : Exception {
}

public class DataIsLinkedException : Exception {
    public string TargetId {
        get => Late.Get(field);
        set => Late.Set(ref field, value);
    }
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
