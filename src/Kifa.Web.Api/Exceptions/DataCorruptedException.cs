using System;

namespace Kifa.Web.Api;

public class DataCorruptedException : Exception {
    public DataCorruptedException() {
    }

    public DataCorruptedException(string message) : base(message) {
    }

    public DataCorruptedException(string message, Exception inner) : base(message, inner) {
    }
}
