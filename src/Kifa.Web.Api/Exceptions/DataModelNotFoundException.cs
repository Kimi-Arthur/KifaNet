using System;

namespace Kifa.Web.Api;

public class DataModelNotFoundException : Exception {
    public DataModelNotFoundException() {
    }

    public DataModelNotFoundException(string message) : base(message) {
    }

    public DataModelNotFoundException(string message, Exception inner) : base(message, inner) {
    }
}
