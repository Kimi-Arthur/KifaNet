using System;

namespace Kifa.Web.Api;

public class InvalidExternalPropertyException : Exception {
    public InvalidExternalPropertyException() {
    }

    public InvalidExternalPropertyException(string message) : base(message) {
    }

    public InvalidExternalPropertyException(string message, Exception inner) :
        base(message, inner) {
    }
}