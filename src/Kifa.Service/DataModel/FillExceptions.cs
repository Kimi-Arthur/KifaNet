using System;

namespace Kifa.Service;

// Base exception for all data model fill operations.
public abstract class DataFillException : Exception {
    public DataFillException() {
    }

    public DataFillException(string message) : base(message) {
    }

    public DataFillException(string message, Exception inner) : base(message, inner) {
    }
}

// Thrown when data is already fresh or does not need to be filled.
public class NoNeedToFillException : DataFillException {
}

// Thrown when the target data is linked/aliased to another ID.
public class DataIsLinkedException : DataFillException {
    public string TargetId {
        get => Late.Get(field);
        set => Late.Set(ref field, value);
    }
}

// Thrown when the upstream resource definitively does not exist (e.g. 404, deleted, or removed).
public class DataNotFoundException : DataFillException {
    public DataNotFoundException() {
    }

    public DataNotFoundException(string message) : base(message) {
    }

    public DataNotFoundException(string message, Exception inner) : base(message, inner) {
    }
}

// Thrown when filling fails during the process (e.g. partial download, parsing error, or dependency failure).
public class FailedToFillException : DataFillException {
    public FailedToFillException() {
    }

    public FailedToFillException(string message) : base(message) {
    }

    public FailedToFillException(string message, Exception inner) : base(message, inner) {
    }
}
