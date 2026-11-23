using System;

namespace Kifa.Web.Api;

public class VirtualLinkAlreadyExistsException : Exception {
    public VirtualLinkAlreadyExistsException() {
    }

    public VirtualLinkAlreadyExistsException(string message) : base(message) {
    }

    public VirtualLinkAlreadyExistsException(string message, Exception inner) : base(message,
        inner) {
    }
}
