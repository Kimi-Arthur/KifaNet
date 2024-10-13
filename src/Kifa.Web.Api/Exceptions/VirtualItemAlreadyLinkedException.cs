using System;

namespace Kifa.Web.Api;

public class VirtualItemAlreadyLinkedException : Exception {
    public VirtualItemAlreadyLinkedException() {
    }

    public VirtualItemAlreadyLinkedException(string message) : base(message) {
    }

    public VirtualItemAlreadyLinkedException(string message, Exception inner) : base(message, inner) {
    }
}
