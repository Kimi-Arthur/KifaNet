using System;

namespace Kifa.Service;

public class NoNeedToFillException : Exception {
}

public class DataIsLinkedException : Exception {
    #region public late static string TargetId { get; set; }

    static string? targetId;

    public string TargetId {
        get => Late.Get(targetId);
        set => Late.Set(ref targetId, value);
    }

    #endregion
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
